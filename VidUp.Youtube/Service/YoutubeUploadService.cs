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
using Drexel.VidUp.Youtube.Data;
using HeyRed.Mime;
using Newtonsoft.Json;
using Timer = System.Timers.Timer;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeUploadService
    {
        private static string uploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos";

        private static ThrottledBufferedStream stream = null;
        private static int uploadChunkSizeInBytes = 40 * 4 * 262144; // 40 MegaByte, The chunk size must be a multiple of 256 KiloByte
        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                ThrottledBufferedStream stream = YoutubeUploadService.stream;
                if (stream != null)
                {
                    stream.MaximumBytesPerSecond = value;
                }
            }
        }

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, CancellationToken cancellationToken)
        {
            Tracer.Write($"YoutubeUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {maxUploadInBytesPerSecond}.");

            upload.UploadErrorMessage = string.Empty;

            UploadResult uploadResult = new UploadResult()
            {
                VideoResult = VideoResult.Failed,
                ThumbnailSuccessFull = false,
                PlaylistSuccessFull = false
            };

            if (!File.Exists(upload.FilePath))
            {
                Tracer.Write($"YoutubeUploadService.Upload: End, file doesn't exist.");

                upload.UploadErrorMessage = "File does not exist.";
                upload.UploadStatus = UplStatus.Failed;
                return uploadResult;
            }

            StringBuilder errors = new StringBuilder();
            try
            {
                Tracer.Write($"YoutubeUploadService.Upload: Initialize upload.");
                long uploadByteIndex = await YoutubeUploadService.initializeUpload(upload);
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeUploadService.Upload: Initial uploadByteIndex: {uploadByteIndex}.");

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                YoutubeUploadStats stats = new YoutubeUploadStats();
                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond, updateUploadProgress, stats, upload))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeUploadService.stream = inputStream;
                    long totalBytesSentInSession = 0;

                    long fileLength = upload.FileLength;
                    HttpClient client = await HttpHelper.GetAuthenticatedUploadClient();
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    int package = 0;
                    bool error = false;

                    Tracer.Write($"YoutubeUploadService.Upload: fileLength: {fileLength}.");
                    PartStream chunkStream;
                    HttpResponseMessage message;
                    StreamContent content;

                    while (fileLength > totalBytesSentInSession + initialBytesSent)
                    {
                        if (error)
                        {
                            error = false;
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: End, Upload not successful after 3 tries for package {package}.");

                                upload.UploadErrorMessage = $"YoutubeUploadService.Upload: Upload not successful after 3 tries for package {package}. Errors: {errors.ToString()}";
                                upload.UploadStatus = UplStatus.Failed;
                                return uploadResult;
                            }

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            Tracer.Write($"YoutubeUploadService.Upload: Getting range due to upload retry.");
                            uploadByteIndex = await YoutubeUploadService.getUploadByteIndex(upload);
                            Tracer.Write($"YoutubeUploadService.Upload: Upload retry uploadByteIndex: {uploadByteIndex}.");

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                            totalBytesSentInSession = uploadByteIndex - initialBytesSent;
                        }
                        else
                        {
                            package++;
                            Tracer.Write($"YoutubeUploadService.Upload: Upload try: Package {package} Try {uploadTry}.");
                        }

                        chunkStream = new PartStream(inputStream, YoutubeUploadService.uploadChunkSizeInBytes);

                        Tracer.Write($"YoutubeUploadService.Upload: Creating content.");
                        using (content = HttpHelper.GetStreamContentResumableUpload(chunkStream, inputStream.Length, uploadByteIndex, YoutubeUploadService.uploadChunkSizeInBytes, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: Start uploading.");
                                message = await client.PutAsync(upload.ResumableSessionUri, content, cancellationToken);
                            }
                            catch (TaskCanceledException)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: End, Upload stopped by user.");

                                upload.UploadStatus = UplStatus.Stopped;
                                uploadResult.VideoResult = VideoResult.Stopped;
                                return uploadResult;
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: HttpClient.PutAsync Exception package {package} try {uploadTry}: {e.ToString()}.");
                                errors.AppendLine($"YoutubeUploadService.Upload: HttpClient.PutAsync Exception package {package} try {uploadTry}: {e.GetType().ToString()}: {e.Message}.");

                                error = true;
                                uploadTry++;
                                continue;
                            }
                        }

                        using (message)
                        {
                            if ((int)message.StatusCode == 308)
                            {
                                //one upload package succeeded, reset upload counter.
                                uploadTry = 1;
                                Tracer.Write($"YoutubeUploadService.Upload: Package {package} finished.");
                            }
                            else
                            {
                                if (!message.IsSuccessStatusCode)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: HttpResponseMessage unexpected status code package {package} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    errors.AppendLine($"YoutubeUploadService.Upload: HttpResponseMessage unexpected status code package {package} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }

                                var definition = new { Id = "" };
                                var response = JsonConvert.DeserializeAnonymousType(await message.Content.ReadAsStringAsync(), definition);
                                upload.VideoId = response.Id;

                                //last stats update to reach 0 bytes and time left.
                                stats.CurrentSpeedInBytesPerSecond = 1;
                                upload.BytesSent = inputStream.Position;
                                updateUploadProgress(stats);

                                upload.UploadStatus = UplStatus.Finished;
                                uploadResult.VideoResult = VideoResult.Finished;
                                Tracer.Write($"YoutubeUploadService.Upload: Upload finished with {package}, video id {upload.VideoId}.");
                            }
                        }

                        uploadByteIndex += YoutubeUploadService.uploadChunkSizeInBytes;
                        totalBytesSentInSession += YoutubeUploadService.uploadChunkSizeInBytes;
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.UploadErrorMessage += $"YoutubeUploadService.Upload: Unexpected Exception: : {e.GetType().ToString()}: {e.Message}.";
                upload.UploadStatus = UplStatus.Failed;
                return uploadResult;
            }

            uploadResult.ThumbnailSuccessFull = await YoutubeThumbnailService.AddThumbnail(upload);
            uploadResult.PlaylistSuccessFull = await YoutubePlaylistService.AddToPlaylist(upload);

            Tracer.Write($"YoutubeUploadService.Upload: End.");
            return uploadResult;
        }

        private static async Task<long> initializeUpload(Upload upload)
        {
            Tracer.Write($"YoutubeUploadService.initializeUpload: Start.");

            long uploadByteIndex = 0;
            if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
            {
                Tracer.Write($"YoutubeUploadService.Upload: Requesting new upload/new resumable session uri.");
                await YoutubeUploadService.requestNewUpload(upload);
            }
            else
            {
                Tracer.Write($"YoutubeUploadService.Upload: Continue upload, getting range.");
                uploadByteIndex = await YoutubeUploadService.getUploadByteIndex(upload);
            }

            Tracer.Write($"YoutubeUploadService.initializeUpload: End.");
            return uploadByteIndex;
        }

        private static async Task<long> getUploadByteIndex(Upload upload)
        {
            Tracer.Write($"YoutubeUploadService.getUploadByteIndex: Start");
            short requestTry = 1;

            while (requestTry <= 3)
            {
                try
                {
                    HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                    using (StreamContent content = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length))
                    using (HttpResponseMessage message = await client.PutAsync(upload.ResumableSessionUri, content))
                    {
                        Tracer.Write($"YoutubeUploadService.getUploadByteIndex: Read header.");
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
                        Tracer.Write($"YoutubeUploadService.getUploadByteIndex: End, exception: {e.ToString()}.");
                        upload.UploadErrorMessage += "YoutubeUploadService.getUploadByteIndex: Getting upload byte index failed 3 times.";
                        throw;
                    }

                    requestTry++;
                }
            }

            throw new NotImplementedException("Should not happen.");
        }

        private static async Task requestNewUpload(Upload upload)
        {
            Tracer.Write($"YoutubeUploadService.requestNewUpload: Start.");
            YoutubeVideoRequest video = new YoutubeVideoRequest();

            video.VideoSnippet = new YoutubeVideoSnippet();
            video.VideoSnippet.Title = upload.YtTitle;
            video.VideoSnippet.Description = upload.Description;
            video.VideoSnippet.Tags = (upload.Tags != null ? upload.Tags : new List<string>()).ToArray();
            video.VideoSnippet.VideoLanguage = upload.VideoLanguage != null ? upload.VideoLanguage.Name : null;
            video.VideoSnippet.Category = null;
            if (upload.Category != null)
            {
                video.VideoSnippet.Category = upload.Category.Id;
            }


            video.Status = new YoutubeStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt != null)
            {
                video.Status.PublishAt = upload.PublishAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string contentJson = JsonConvert.SerializeObject(video);

            Tracer.Write($"YoutubeUploadService.Upload: New upload/new resumable session uri request content: {contentJson}.");

            FileInfo info = new FileInfo(upload.FilePath);
            //request upload session/uri

            //slug header adds original video file name to youtube studio, lambda filters to valid chars (ascii >=32 and <=255)
            string httpHeaderCompatibleString = new String(Path.GetFileName(upload.FilePath).Where(c =>
            {
                char ch = (char)((uint)byte.MaxValue & (uint)c);
                if ((ch >= ' ' || ch == '\t') && ch != '\x007F')
                {
                    return true;
                }

                return false;
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
                        content.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
                        content.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));

                        using (HttpResponseMessage message = await client.PostAsync($"{YoutubeUploadService.uploadEndpoint}?part=snippet,status&uploadType=resumable", content))
                        {
                            message.EnsureSuccessStatusCode();
                            Tracer.Write($"YoutubeUploadService.requestNewUpload: Read header.");
                            upload.ResumableSessionUri = message.Headers.GetValues("Location").First();
                            Tracer.Write($"YoutubeUploadService.requestNewUpload: End.");
                            break;
                        }
                    }

                }
                catch (Exception e)
                {

                    if (requestTry >= 3)
                    {
                        Tracer.Write($"YoutubeUploadService.requestNewUpload: End, exception: {e.ToString()}.");
                        upload.UploadErrorMessage += "YoutubeUploadService.requestNewUpload: Requesting new upload failed 3 times.";
                        throw;
                    }

                    requestTry++;
                }
            }
        }
    }
}
