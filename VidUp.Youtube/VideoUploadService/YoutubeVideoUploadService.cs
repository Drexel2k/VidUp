using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Security.Authentication;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.VideoUploadService.Data;
using HeyRed.Mime;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUploadService
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

        public static long? CurrentPosition
        {
            get
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    return stream.Position;
                }

                return null;
            }
        }

        public static async Task<UploadResult> UploadAsync(Upload upload, Action<Upload> resumableSessionUriSet, CancellationToken cancellationToken, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {YoutubeVideoUploadService.maxUploadInBytesPerSecond}.");

            upload.ClearUploadErrors();

            if (!File.Exists(upload.FilePath))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, file doesn't exist.");

                upload.AddUploadError(new StatusInformation("File does not exist."));
                resetEvent.WaitOne();
                upload.UploadStatus = UplStatus.Failed;
                return UploadResult.FailedWithoutDataSent;
            }

            try
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initialize upload.");
                if (!await YoutubeVideoUploadService.initializeUploadAsync(upload, resumableSessionUriSet, resetEvent).ConfigureAwait(false))
                {
                    return UploadResult.FailedWithoutDataSent;
                }

                long initialUploadBytesSent = upload.BytesSent;
                long lastResumePositionBeforeError = upload.BytesSent;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initial resume position: {upload.BytesSent}.");

                using (YoutubeVideoUploadService.stream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond))
                {
                    long fileLength = upload.FileLength;
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    bool error = false;

                    Tracer.Write($"YoutubeVideoUploadService.Upload: fileLength: {fileLength}.");

                    do
                    {
                        if (error)
                        {
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload not successful after 3 tries for resume position {lastResumePositionBeforeError}.");

                                upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.Upload: Upload not successful after 3 tries for resume position {lastResumePositionBeforeError}."));
                                resetEvent.WaitOne();
                                upload.UploadStatus = UplStatus.Failed;

                                YoutubeVideoUploadService.stream = null;
                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            error = false;

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Getting range due to upload retry.");
                            if(!await YoutubeVideoUploadService.getResumePositionAsync(upload, resetEvent).ConfigureAwait(false))
                            {
                                YoutubeVideoUploadService.stream = null;
                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                            }

                            //HttpClient disposed inputStream...
                            YoutubeVideoUploadService.stream = new ThrottledBufferedStream(upload.FilePath, upload.BytesSent, YoutubeVideoUploadService.maxUploadInBytesPerSecond); ;

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload retry resume postion: {upload.BytesSent}.");

                            if (upload.BytesSent != lastResumePositionBeforeError)
                            {
                                uploadTry = 1;
                                lastResumePositionBeforeError = upload.BytesSent;
                            }
                        }

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload try: resume position {upload.BytesSent} Try {uploadTry}.");
                        Tracer.Write($"YoutubeVideoUploadService.Upload: Creating content.");

                        using (StreamContent streamContent = HttpHelper.GetStreamContentResumableUpload(YoutubeVideoUploadService.stream, YoutubeVideoUploadService.stream.Length, upload.BytesSent, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                                        upload.YoutubeAccount, HttpMethod.Put, upload.ResumableSessionUri).ConfigureAwait(false))
                                {
                                    
                                    requestMessage.Content = streamContent;
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: Start uploading.");
                                    using (HttpResponseMessage responseMessage = await HttpHelper.UploadClient.SendAsync(requestMessage, cancellationToken).ConfigureAwait(false))
                                    using (responseMessage.Content)
                                    {
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Uploading finished.");
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer.");
                                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                        Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer finished.");

                                        if (!responseMessage.IsSuccessStatusCode)
                                        {
                                            Tracer.Write($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code resume position {lastResumePositionBeforeError} try {uploadTry}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content {content}.");
                                            StatusInformation statusInformation = new StatusInformation($"YoutubeVideoUploadService.Upload: Upload failed with resume position {lastResumePositionBeforeError} try {uploadTry}: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                            upload.AddUploadError(statusInformation);
                                            if (statusInformation.IsQuotaError)
                                            {
                                                Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                                                resetEvent.WaitOne();
                                                upload.UploadStatus = UplStatus.Failed;
                                                return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, false);
                                            }

                                            responseMessage.EnsureSuccessStatusCode();
                                        }

                                        YoutubeVideoPostResponse response = JsonConvert.DeserializeObject<YoutubeVideoPostResponse>(content);
                                        upload.VideoId = response.Id;

                                        //last stats update to reach 0 bytes and time left.
                                        upload.BytesSent = YoutubeVideoUploadService.stream.Position;
                                        resetEvent.WaitOne();
                                        upload.UploadStatus = UplStatus.Finished;
                                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload finished with resume position {lastResumePositionBeforeError}, video id {upload.VideoId}.");
                                    }
                                }
                            }
                            catch (TaskCanceledException e)
                            {
                                if (e.InnerException is TimeoutException)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: resume position {lastResumePositionBeforeError} try {uploadTry} upload timeout.");
                                    upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.Upload: HttpClient.SendAsync resume position {lastResumePositionBeforeError} try {uploadTry} upload timeout."));
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }
                                else
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload stopped by user.");

                                    upload.BytesSent = YoutubeVideoUploadService.stream.Position;
                                    resetEvent.WaitOne();
                                    upload.UploadStatus = UplStatus.Stopped;

                                    YoutubeVideoUploadService.stream = null;
                                    return YoutubeVideoUploadService.getResult(upload.BytesSent, initialUploadBytesSent, true);
                                }
                            }
                            catch (Exception e)
                            {
                                //HttpRequestExceptions with no inner exceptions shall not be logged, because they are from successful
                                //http requests but with not successful http status and are logged above.
                                //All other exceptions shall be logged, too.
                                if (!(e is HttpRequestException) || e.InnerException != null)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: HttpClient.SendAsync Exception resume position {lastResumePositionBeforeError} try {uploadTry}: {e.ToString()}.");
                                    upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.Upload: HttpClient.SendAsync Exception resume position {lastResumePositionBeforeError} try {uploadTry}: {e.GetType().ToString()}: {e.Message}."));
                                }

                                error = true;
                                uploadTry++;
                            }
                        }
                    }
                    while(error);
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.Upload: Unexpected Exception: : {e.GetType().ToString()}: {e.Message}."));
                
                resetEvent.WaitOne();
                upload.UploadStatus = UplStatus.Failed;

                YoutubeVideoUploadService.stream = null;
                return UploadResult.FailedWithoutDataSent;
            }

            YoutubeVideoUploadService.stream = null;
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

        private static async Task<bool> initializeUploadAsync(Upload upload, Action<Upload> resumableSessionUriSet, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: Start.");

            if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
            {
                Tracer.Write($"YoutubeVideoUploadService.initializeUploadAsync: Requesting new upload/new resumable session uri.");
                if (!await YoutubeVideoUploadService.requestNewUploadAsync(upload, resetEvent).ConfigureAwait(false))
                {
                    return false;
                }

                resumableSessionUriSet(upload);
            }
            else
            {
                Tracer.Write($"YoutubeVideoUploadService.initializeUploadAsync: Continue upload, getting range.");

                if (!await YoutubeVideoUploadService.getResumePositionAsync(upload, resetEvent).ConfigureAwait(false))
                {
                    return false;
                }
            }

            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: End.");
            return true;
        }

        private static async Task<bool> getResumePositionAsync(Upload upload, AutoResetEvent resetEvent)
        {
            Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Start");

            StringBuilder errors = new StringBuilder();
            short requestTry = 1;
            bool error = false;

            do
            {
                if (error)
                {
                    if (requestTry > 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: End, failed 3 times.");
                        upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.getResumePositionAsync: Getting resume position failed 3 times. Errors: {errors.ToString()}"));
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    error = false;

                    //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }

                try
                {
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Requesting resume position, try {requestTry}.");
                    StreamContent streamContent = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length);
                    using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                        upload.YoutubeAccount, HttpMethod.Put, upload.ResumableSessionUri).ConfigureAwait(false))
                    {
                        requestMessage.Content = streamContent;
                        using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                        {
                            string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (responseMessage.StatusCode != HttpStatusCode.PermanentRedirect)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: HttpResponseMessage unexpected status code: {(int) responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                StatusInformation statusInformation = new StatusInformation($"YoutubeVideoUploadService.getResumePositionAsync: Could not resume position: {(int) responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                upload.AddUploadError(statusInformation);
                                if (statusInformation.IsQuotaError)
                                {
                                    resetEvent.WaitOne();
                                    upload.UploadStatus = UplStatus.Failed;
                                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                                    return false;
                                }

                                //no EnsureStatusCode here as the only expected code is 503.
                                error = true;
                                requestTry++;
                            }
                            else
                            {
                                Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Read header.");

                                try
                                {
                                    string range = responseMessage.Headers.GetValues("Range").First();
                                    if (!string.IsNullOrWhiteSpace(range))
                                    {
                                        string[] parts = range.Split('-');
                                        upload.BytesSent = Convert.ToInt64(parts[1]) + 1;
                                    }
                                }
                                catch (InvalidOperationException e)
                                {
                                    errors.AppendLine($"YoutubeVideoUploadService.getResumePositionAsync: Range header not found, BytesSent set to 0: {e.Message}.");
                                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Range header not found, restarting upload, exception: {e.ToString()}.");
                                    upload.BytesSent = 0;
                                }
                                catch (Exception e)
                                {
                                    errors.AppendLine($"YoutubeVideoUploadService.getResumePositionAsync: Could not get range header: {e.GetType().ToString()}: {e.Message}.");
                                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Range header exception: {e.ToString()}.");

                                    error = true;
                                    requestTry++;
                                }
                            }
                        }
                    }
                }
                catch (Exception e)
                {
                    errors.AppendLine($"YoutubeVideoUploadService.getResumePositionAsync: Getting upload index failed: {e.Message}.");
                    Tracer.Write($"YoutubeVideoUploadService.getResumePositionAsync: Exception: {e.ToString()}.");

                    error = true;
                    requestTry++;
                }
            }
            while (error);

            return true;
        }

        private static async Task<bool> requestNewUploadAsync(Upload upload, AutoResetEvent resetEvent)
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

            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: New upload/new resumable session uri request content: {contentJson}.");

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
            bool error = false;

            do
            {
                if (error)
                {
                    if (requestTry > 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, failed 3 times.");
                        upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed 3 times."));
                        resetEvent.WaitOne();
                        upload.UploadStatus = UplStatus.Failed;
                        return false;
                    }

                    error = false;

                    //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                    await Task.Delay(TimeSpan.FromSeconds(5)).ConfigureAwait(false);
                }

                try
                {
                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload.");
                    using (ByteArrayContent byteArrayContent = HttpHelper.GetStreamContent(contentJson, "application/json"))
                    using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                        upload.YoutubeAccount, HttpMethod.Post, $"{YoutubeVideoUploadService.videoUploadEndpoint}?part=snippet,status&uploadType=resumable").ConfigureAwait(false))
                    {
                        byteArrayContent.Headers.Add("Slug", httpHeaderCompatibleString);
                        byteArrayContent.Headers.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
                        byteArrayContent.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));
                        requestMessage.Content = byteArrayContent;

                        using (HttpResponseMessage message = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                        {
                            string content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!message.IsSuccessStatusCode)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: HttpResponseMessage unexpected status code: {(int)message.StatusCode} {message.ReasonPhrase} with content '{content}'.");
                                StatusInformation statusInformation = new StatusInformation($"YoutubeVideoUploadService.requestNewUpload: Could not request new upload: {(int)message.StatusCode} {message.ReasonPhrase} with content '{content}'.");
                                upload.AddUploadError(statusInformation);
                                if (statusInformation.IsQuotaError)
                                {
                                    resetEvent.WaitOne();
                                    upload.UploadStatus = UplStatus.Failed;
                                    Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, quota exceeded.");
                                    return false;
                                }

                                message.EnsureSuccessStatusCode();
                            }

                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Read header.");
                            upload.ResumableSessionUri = message.Headers.GetValues("Location").First();
                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End.");
                        }
                    }
                }
                catch (Exception e)
                {
                    //HttpRequestExceptions with no inner exceptions shall not be logged, because they are from successful
                    //http requests but with not successful http status and are logged above.
                    //All other exceptions shall be logged, too.
                    if (!(e is HttpRequestException) || e.InnerException != null)
                    {
                        upload.AddUploadError(new StatusInformation($"YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed: {e.GetType().ToString()}: {e.Message}."));
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Exception: {e.ToString()}.");
                    }

                    error = true;
                    requestTry++;
                }

            } while (error);

            return true;
        }
    }
}
