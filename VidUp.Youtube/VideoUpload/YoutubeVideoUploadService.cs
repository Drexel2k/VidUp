using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItem;
using Drexel.VidUp.Youtube.Thumbnail;
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

        public static async Task<UploadResult> Upload(Upload upload, Action<YoutubeUploadStats> updateUploadProgress, CancellationToken cancellationToken)
        {
            Tracer.Write($"YoutubeVideoUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {YoutubeVideoUploadService.maxUploadInBytesPerSecond}.");

            upload.UploadErrorMessage = string.Empty;

            UploadResult uploadResult = new UploadResult()
            {
                VideoResult = VideoResult.Failed,
                ThumbnailSuccessFull = false,
                PlaylistSuccessFull = false
            };

            if (!File.Exists(upload.FilePath))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, file doesn't exist.");

                upload.UploadErrorMessage = "File does not exist. ";
                upload.UploadStatus = UplStatus.Failed;
                return uploadResult;
            }

            StringBuilder errors = new StringBuilder();
            try
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initialize upload.");
                long uploadByteIndex = await YoutubeVideoUploadService.initializeUpload(upload);
                long lastUploadByteIndexBeforeError = uploadByteIndex;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initial uploadByteIndex: {uploadByteIndex}.");

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                YoutubeUploadStats stats = new YoutubeUploadStats();
                FileStream fileStream;
                ThrottledBufferedStream inputStream;

                using (fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (inputStream = new ThrottledBufferedStream(fileStream, YoutubeVideoUploadService.maxUploadInBytesPerSecond, updateUploadProgress, stats, upload))
                {
                    YoutubeVideoUploadService.stream = inputStream;
                    inputStream.Position = uploadByteIndex;

                    long fileLength = upload.FileLength;
                    HttpClient client = await HttpHelper.GetAuthenticatedUploadClient();
                    
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
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload not successful after 3 tries for uploadByteIndex {uploadByteIndex}.");

                                upload.UploadErrorMessage += $"YoutubeVideoUploadService.Upload: Upload not successful after 3 tries for uploadByteIndex {uploadByteIndex}. Errors: {errors.ToString()}. ";
                                upload.UploadStatus = UplStatus.Failed;
                                return uploadResult;
                            }

                            error = false;

                            //HttpClient disposed inputStream...
                            fileStream = new FileStream(upload.FilePath, FileMode.Open);
                            inputStream = new ThrottledBufferedStream(fileStream, YoutubeVideoUploadService.maxUploadInBytesPerSecond, updateUploadProgress, stats, upload);
                            YoutubeVideoUploadService.stream = inputStream;

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Getting range due to upload retry.");
                            uploadByteIndex = await YoutubeVideoUploadService.getUploadByteIndex(upload);
                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload retry uploadByteIndex: {uploadByteIndex}.");

                            if (uploadByteIndex != lastUploadByteIndexBeforeError)
                            {
                                uploadTry = 1;
                                lastUploadByteIndexBeforeError = uploadByteIndex;
                            }

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                        }

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload try: uploadByteIndex {uploadByteIndex} Try {uploadTry}.");

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Creating content.");
                        using (streamContent = HttpHelper.GetStreamContentResumableUpload(inputStream, inputStream.Length, uploadByteIndex, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Start uploading.");
                                message = await client.PutAsync(upload.ResumableSessionUri, streamContent, cancellationToken);
                                Tracer.Write("YoutubeVideoUploadService.Upload: Uploading finished.");
                            }
                            catch (TaskCanceledException e)
                            {
                                if (e.InnerException is TimeoutException)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: uploadByteIndex {uploadByteIndex} try {uploadTry} upload timeout.");
                                    errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync uploadByteIndex {uploadByteIndex} try {uploadTry} upload timeout.");
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }
                                else
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload stopped by user.");

                                    upload.UploadStatus = UplStatus.Stopped;
                                    uploadResult.VideoResult = VideoResult.Stopped;
                                    return uploadResult;
                                }
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception uploadByteIndex {uploadByteIndex} try {uploadTry}: {e.ToString()}.");
                                errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception uploadByteIndex {uploadByteIndex} try {uploadTry}: {e.GetType().ToString()}: {e.Message}.");

                                error = true;
                                uploadTry++;
                                continue;
                            }
                        }

                        using (message)
                        {
                            Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer.");
                            string content = await message.Content.ReadAsStringAsync();
                            Tracer.Write("YoutubeVideoUploadService.Upload: Reading answer finished.");

                            if (!message.IsSuccessStatusCode)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code uploadByteIndex {uploadByteIndex} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase} and content {content}.");
                                errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code uploadByteIndex {uploadByteIndex} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase} and content {content}.");
                                error = true;
                                uploadTry++;
                                continue;
                            }

                            YoutubeVideoPostResponse response = JsonConvert.DeserializeObject<YoutubeVideoPostResponse>(content);
                            upload.VideoId = response.Id;

                            //last stats update to reach 0 bytes and time left.
                            stats.CurrentSpeedInBytesPerSecond = 1;
                            upload.BytesSent = inputStream.Position;
                            updateUploadProgress(stats);

                            upload.UploadStatus = UplStatus.Finished;
                            uploadResult.VideoResult = VideoResult.Finished;
                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload finished with uploadByteIndex {uploadByteIndex}, video id {upload.VideoId}.");
                        }
                    }
                    while(error);
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.UploadErrorMessage += $"YoutubeVideoUploadService.Upload: Unexpected Exception: : {e.GetType().ToString()}: {e.Message}. ";
                upload.UploadStatus = UplStatus.Failed;
                return uploadResult;
            }

            uploadResult.ThumbnailSuccessFull = await YoutubeThumbnailService.AddThumbnail(upload);
            uploadResult.PlaylistSuccessFull = await YoutubePlaylistItemService.AddToPlaylist(upload);

            Tracer.Write($"YoutubeVideoUploadService.Upload: End.");
            return uploadResult;
        }

        private static async Task<long> initializeUpload(Upload upload)
        {
            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: Start.");

            long uploadByteIndex = 0;
            if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Requesting new upload/new resumable session uri.");
                await YoutubeVideoUploadService.requestNewUpload(upload);
            }
            else
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Continue upload, getting range.");
                uploadByteIndex = await YoutubeVideoUploadService.getUploadByteIndex(upload);
            }

            Tracer.Write($"YoutubeVideoUploadService.initializeUpload: End.");
            return uploadByteIndex;
        }

        private static async Task<long> getUploadByteIndex(Upload upload)
        {
            Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Start");
            short requestTry = 1;

            while (requestTry <= 3)
            {
                try
                {
                    HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                    using (StreamContent content = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length))
                    using (HttpResponseMessage message = await client.PutAsync(upload.ResumableSessionUri, content))
                    {
                        Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: Read header.");
                        string range = message.Headers.GetValues("Range").First();
                        if (!string.IsNullOrWhiteSpace(range))
                        {
                            string[] parts = range.Split('-');
                            return Convert.ToInt64(parts[1]) + 1;
                        }
                    }

                    return 0;
                }
                catch (Exception e)
                {

                    if (requestTry >= 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.getUploadByteIndex: End, exception: {e.ToString()}.");
                        upload.UploadErrorMessage += "YoutubeVideoUploadService.getUploadByteIndex: Getting upload byte index failed 3 times. ";
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    requestTry++;
                }
            }

            throw new NotImplementedException("Should not happen.");
        }

        private static async Task requestNewUpload(Upload upload)
        {
            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Start.");
            YoutubeVideoPostRequest video = new YoutubeVideoPostRequest();

            video.Snippet = new YoutubeVideoPostRequestSnippet();
            video.Snippet.Title = upload.Title;
            video.Snippet.Description = upload.Description;
            video.Snippet.Tags = (upload.Tags != null ? upload.Tags : new List<string>()).ToArray();
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

            while (requestTry <= 3)
            {
                try
                {
                    HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                    using (ByteArrayContent content = HttpHelper.GetStreamContent(contentJson, "application/json"))
                    {
                        content.Headers.Add("Slug", httpHeaderCompatibleString);
                        content.Headers.Add("X-Upload-Content-Length", fileInfo.Length.ToString());
                        content.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));

                        using (HttpResponseMessage message = await client.PostAsync($"{YoutubeVideoUploadService.videoUploadEndpoint}?part=snippet,status&uploadType=resumable", content))
                        {
                            message.EnsureSuccessStatusCode();
                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: Read header.");
                            upload.ResumableSessionUri = message.Headers.GetValues("Location").First();
                            Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End.");
                            break;
                        }
                    }

                }
                catch (Exception e)
                {

                    if (requestTry >= 3)
                    {
                        Tracer.Write($"YoutubeVideoUploadService.requestNewUpload: End, exception: {e.ToString()}.");
                        upload.UploadErrorMessage += "YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed 3 times. ";
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    requestTry++;
                }
            }
        }
    }
}
