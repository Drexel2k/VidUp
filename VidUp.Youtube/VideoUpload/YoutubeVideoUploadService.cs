using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.VideoUpload.Data;
using HeyRed.Mime;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUpload
{
    public class YoutubeVideoUploadService
    {
        private static string videoUploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos";

        //to set max upload speed while running
        private static ThrottledBufferedStream stream = null;
        private static long maxUploadInBytesPerSecond = 0;

        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                YoutubeVideoUploadService.maxUploadInBytesPerSecond = value;
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    stream.MaximumBytesPerSecond = value;
                }
            }
        }

        public static long CurrentSpeedInBytesPerSecond
        {
            get
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    return stream.CurrentSpeedInBytesPerSecond;
                }

                return 0;
            }
        }

        public static long CurrentPosition
        {
            get
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    return stream.Position;
                }

                return 0;
            }
        }

        public static async Task<UploadResult> UploadAsync(Upload upload, Action<Upload> resumableSessionUriSet, CancellationToken cancellationToken)
        {
            Tracer.Write($"YoutubeVideoUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {YoutubeVideoUploadService.maxUploadInBytesPerSecond}.");

            upload.UploadErrorMessage = string.Empty;

            if (!File.Exists(upload.FilePath))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, file doesn't exist.");

                upload.UploadErrorMessage = "File does not exist.\n";
                upload.UploadStatus = UplStatus.Failed;
                return UploadResult.FailedWithoutDataSent;
            }

            //todo: transform to error objects so that errors can be better interpreted
            StringBuilder errors = new StringBuilder();
            try
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initialize upload.");
                if (!await YoutubeVideoUploadService.initializeUploadAsync(upload, resumableSessionUriSet).ConfigureAwait(false))
                {
                    return UploadResult.FailedWithoutDataSent;
                }

                long initialUploadBytesSent = upload.BytesSent;
                long lastUploadByteIndexBeforeError = upload.BytesSent;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initial uploadByteIndex: {upload.BytesSent}.");
                ThrottledBufferedStream inputStream;

                using (inputStream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond))
                {
                    YoutubeVideoUploadService.stream = inputStream;

                    long fileLength = upload.FileLength;
                    HttpClient client = await HttpHelper.GetAuthenticatedUploadClientAsync().ConfigureAwait(false);
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    bool error = false;

                    Tracer.Write($"YoutubeVideoUploadService.Upload: fileLength: {fileLength}.");
                    HttpResponseMessage message;
                    StreamContent streamContent;

                    do
                    {
                        if (error)
                        {
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload not successful after 3 tries for uploadByteIndex {lastUploadByteIndexBeforeError}.");

                                upload.UploadErrorMessage += $"YoutubeVideoUploadService.Upload: Upload not successful after 3 tries for uploadByteIndex {lastUploadByteIndexBeforeError}. Errors: {errors.ToString()}.";
                                upload.UploadStatus = UplStatus.Failed;

                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            error = false;

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Getting range due to upload retry.");
                            if(!await YoutubeVideoUploadService.getUploadByteIndexAsync(upload).ConfigureAwait(false))
                            {
                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            //HttpClient disposed inputStream...
                            inputStream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond);
                            YoutubeVideoUploadService.stream = inputStream;

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload retry uploadByteIndex: {upload.BytesSent}.");

                            if (upload.BytesSent != lastUploadByteIndexBeforeError)
                            {
                                uploadTry = 1;
                                lastUploadByteIndexBeforeError = upload.BytesSent;
                            }
                        }

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload try: uploadByteIndex {upload.BytesSent} Try {uploadTry}.");

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Creating content.");
                        using (streamContent = HttpHelper.GetStreamContentResumableUpload(inputStream, inputStream.Length, upload.BytesSent, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Start uploading.");
                                message = await client.PutAsync(upload.ResumableSessionUri, streamContent, cancellationToken).ConfigureAwait(false);
                                Tracer.Write("YoutubeVideoUploadService.Upload: Uploading finished.");
                            }
                            catch (TaskCanceledException e)
                            {
                                if (e.InnerException is TimeoutException)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry} upload timeout.");
                                    errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry} upload timeout.");
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }
                                else
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload stopped by user.");

                                    upload.BytesSent = inputStream.Position;
                                    upload.UploadStatus = UplStatus.Stopped;
                                    return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, true);
                                }
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry}: {e.ToString()}.");
                                errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry}: {e.GetType().ToString()}: {e.Message}.");

                                error = true;
                                uploadTry++;
                                continue;
                            }
                        }

                        using (message)
                        using (message.Content)
                        {
                            Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer.");
                            string content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer finished.");

                            if (!message.IsSuccessStatusCode)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry}: {message.StatusCode} {message.ReasonPhrase} with content {content}.");
                                errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code uploadByteIndex {lastUploadByteIndexBeforeError} try {uploadTry}: {message.StatusCode} {message.ReasonPhrase} with content {content}.");
                                error = true;
                                uploadTry++;
                                continue;
                            }

                            YoutubeVideoPostResponse response = JsonConvert.DeserializeObject<YoutubeVideoPostResponse>(content);
                            upload.VideoId = response.Id;

                            //last stats update to reach 0 bytes and time left.
                            upload.BytesSent = inputStream.Position;

                            upload.UploadStatus = UplStatus.Finished;
                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload finished with uploadByteIndex {lastUploadByteIndexBeforeError}, video id {upload.VideoId}.");
                        }
                    }
                    while(error);
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.UploadErrorMessage += $"YoutubeVideoUploadService.Upload: Unexpected Exception: : {e.GetType().ToString()}: {e.Message}.\n";
                upload.UploadStatus = UplStatus.Failed;

                return UploadResult.FailedWithoutDataSent;
            }

            Tracer.Write($"YoutubeVideoUploadService.Upload: End.");
            return UploadResult.Finished;
        }

        private static UploadResult getResult(long uploadBytesSent, long initialUploadByteSent, bool stopped)
        {
            if (uploadBytesSent - initialUploadByteSent > 1024 * 1024)
            {
                if (stopped)
                {
                    return UploadResult.StoppedWithDataSent;
                }
                
                return UploadResult.FailedWithDataSent;
            }

            if (stopped)
            {
                return UploadResult.StoppedWithoutDataSent;
            }

            return UploadResult.FailedWithoutDataSent;
        }

        private static async Task<bool> initializeUploadAsync(Upload upload, Action<Upload> resumableSessionUriSet)
        {
            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: Start.");

            if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Requesting new upload/new resumable session uri.");
                if (!await YoutubeVideoUploadService.requestNewUploadAsync(upload).ConfigureAwait(false))
                {
                    return false;
                }

                resumableSessionUriSet(upload);
            }
            else
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Continue upload, getting range.");

                if (!await YoutubeVideoUploadService.getUploadByteIndexAsync(upload).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: End.");
            return true;
        }

        private static async Task<bool> getUploadByteIndexAsync(Upload upload)
        {
            Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Start");
            short requestTry = 1;

            StringBuilder errors = new StringBuilder();
            while (requestTry <= 3)
            {
                try
                {
                    HttpClient client = await HttpHelper.GetAuthenticatedStandardClientAsync().ConfigureAwait(false);

                    Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Requesting upload byte index.");
                    using (StreamContent content = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length))
                    using (HttpResponseMessage message = await client.PutAsync(upload.ResumableSessionUri, content).ConfigureAwait(false))
                    {
                        if (message.StatusCode != HttpStatusCode.PermanentRedirect)
                        {
                            string httpContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                            Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: HttpResponseMessage unexpected status code: {message.StatusCode} {message.ReasonPhrase} with content {httpContent}.");
                            throw new HttpRequestException($"Http error status code: {message.StatusCode}, reason {message.ReasonPhrase}, content {httpContent}.");
                        }

                        Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Read header.");

                        try
                        {
                            string range = message.Headers.GetValues("Range").First();
                            if (!string.IsNullOrWhiteSpace(range))
                            {
                                string[] parts = range.Split('-');
                                upload.BytesSent = Convert.ToInt64(parts[1]) + 1;
                            }

                            return true;
                        }
                        catch (Exception e)
                        {
                            errors.AppendLine($"YoutubeVideoUploadService.getUploadByteIndex: Could not get range header: {e.Message}.");
                            Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Range header exception: {e.ToString()}.");

                            if (requestTry >= 3)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: End header, failed 3 times.");
                                upload.UploadErrorMessage += $"YoutubeVideoUploadService.getUploadByteIndex: Getting upload byte index failed 3 times. Errors: {errors.ToString()}";
                                upload.UploadStatus = UplStatus.Failed;
                                return false;
                            }

                            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                            requestTry++;
                        }

                    }
                }
                catch (Exception e)
                {
                    errors.AppendLine($"YoutubeVideoUploadService.getUploadByteIndex: Getting upload index failed: {e.Message}.");
                    Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Exception: {e.ToString()}.");

                    if (requestTry >= 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: End all, failed 3 times.");
                        upload.UploadErrorMessage += $"YoutubeVideoUploadService.getUploadByteIndex: Getting upload byte index failed 3 times. Errors: {errors.ToString()}";
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    requestTry++;
                }
            }

            throw new NotImplementedException("Should not happen.");
        }

        private static async Task<bool> requestNewUploadAsync(Upload upload)
        {
            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Start.");
            YoutubeVideoPostRequest video = new YoutubeVideoPostRequest();

            video.Snippet = new YoutubeVideoPostRequestSnippet();
            video.Snippet.Title = upload.Title;
            video.Snippet.Description = upload.Description;
            video.Snippet.Tags = upload.Tags != null ? upload.Tags.ToArray() : new List<string>().ToArray();
            video.Snippet.VideoLanguage = upload.VideoLanguage != null ? upload.VideoLanguage.Name.Replace('-', '_') : null; //works better with _, with - e.g. German (Switzerland) is not set at all, with _ it is set as German.
            video.Snippet.DescriptionLanguage = upload.DescriptionLanguage != null ? upload.DescriptionLanguage.Name.Replace('-', '_') : null; //works better with _, with - e.g. German (Switzerland) is not set at all, with _ it is set as German.
            video.Snippet.Category = null;
            if (upload.Category != null)
            {
                video.Snippet.Category = upload.Category.Id;
            }


            video.Status = new YoutubeVideoPostRequestStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt != null)
            {
                video.Status.PublishAt = upload.PublishAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string contentJson = JsonConvert.SerializeObject(video);

            Tracer.Write($"YoutubeVideoUploadService.Upload: New upload/new resumable session uri request content: {contentJson}.");

            FileInfo fileInfo = new FileInfo(upload.FilePath);
            //request upload session/uri

            //slug header adds original video file name to youtube studio, lambda filters to valid chars (ascii >=32 and <=255)
            string httpHeaderCompatibleString = new String(Path.GetFileName(upload.FilePath).Select(c =>
            {
                if ((c >= ' ' && c <= '~') || c == '\t')
                {
                    return c;
                }

                return '_';
            }).ToArray());

            short requestTry = 1;

            StringBuilder errors = new StringBuilder();
            while (requestTry <= 3)
            {
                try
                {
                    HttpClient client = await HttpHelper.GetAuthenticatedStandardClientAsync().ConfigureAwait(false);
                    using (ByteArrayContent content = HttpHelper.GetStreamContent(contentJson, "application/json"))
                    {
                        content.Headers.Add("Slug", httpHeaderCompatibleString);
                        content.Headers.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
                        content.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));

                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload.");
                        using (HttpResponseMessage message = await client.PostAsync($"{YoutubeVideoUploadService.videoUploadEndpoint}?part=snippet,status&uploadType=resumable", content).ConfigureAwait(false))
                        {
                            string httpContent = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!message.IsSuccessStatusCode)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: HttpResponseMessage unexpected status code: {message.StatusCode} {message.ReasonPhrase} with content {httpContent}.");
                                throw new HttpRequestException($"Http error status code: {message.StatusCode}, reason {message.ReasonPhrase}, content {httpContent}.");
                            }

                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Read header.");
                            upload.ResumableSessionUri = message.Headers.GetValues("Location").First();
                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End.");
                            return true;
                        }

                    }

                }
                catch (Exception e)
                {
                    errors.AppendLine($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed: {e.Message}.");
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Exception: {e.ToString()}.");

                    if (requestTry >= 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, failed 3 times.");
                        upload.UploadErrorMessage += $"YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed 3 times. Errors: {errors.ToString()}.";
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                    requestTry++;
                }
            }

            throw new NotImplementedException("Should not happen.");
        }
    }
}
