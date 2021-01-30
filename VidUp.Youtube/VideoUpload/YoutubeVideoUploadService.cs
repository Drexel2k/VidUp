using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

        private static ThrottledBufferedStream stream = null;
        private static int uploadChunkSizeInBytes = 40 * 4 * 262144; // 40 MegaByte, The chunk size must be a multiple of 256 KiloByte
        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                ThrottledBufferedStream stream = YoutubeVideoUploadService.stream;
                if (stream != null)
                {
                    stream.MaximumBytesPerSecond = value;
                }
            }
        }

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, CancellationToken cancellationToken)
        {
            Tracer.Write($"YoutubeVideoUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {maxUploadInBytesPerSecond}.");

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

                upload.UploadErrorMessage = "File does not exist.";
                upload.UploadStatus = UplStatus.Failed;
                return uploadResult;
            }

            StringBuilder errors = new StringBuilder();
            try
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initialize upload.");
                long uploadByteIndex = await YoutubeVideoUploadService.initializeUpload(upload);
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                Tracer.Write($"YoutubeVideoUploadService.Upload: Initial uploadByteIndex: {uploadByteIndex}.");

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                YoutubeUploadStats stats = new YoutubeUploadStats();
                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond, updateUploadProgress, stats, upload))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeVideoUploadService.stream = inputStream;
                    long totalBytesSentInSession = 0;

                    long fileLength = upload.FileLength;
                    HttpClient client = await HttpHelper.GetAuthenticatedUploadClient();
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    int package = 0;
                    bool error = false;

                    Tracer.Write($"YoutubeVideoUploadService.Upload: fileLength: {fileLength}.");
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
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload not successful after 3 tries for package {package}.");

                                upload.UploadErrorMessage = $"YoutubeVideoUploadService.Upload: Upload not successful after 3 tries for package {package}. Errors: {errors.ToString()}";
                                upload.UploadStatus = UplStatus.Failed;
                                return uploadResult;
                            }

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            Tracer.Write($"YoutubeVideoUploadService.Upload: Getting range due to upload retry.");
                            uploadByteIndex = await YoutubeVideoUploadService.getUploadByteIndex(upload);
                            Tracer.Write($"YoutubeVideoUploadService.Upload: Upload retry uploadByteIndex: {uploadByteIndex}.");

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                            totalBytesSentInSession = uploadByteIndex - initialBytesSent;
                        }
                        else
                        {
                            package++;
                        }

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Upload try: Package {package} Try {uploadTry}.");

                        chunkStream = new PartStream(inputStream, YoutubeVideoUploadService.uploadChunkSizeInBytes);

                        Tracer.Write($"YoutubeVideoUploadService.Upload: Creating content.");
                        using (content = HttpHelper.GetStreamContentResumableUpload(chunkStream, inputStream.Length, uploadByteIndex, YoutubeVideoUploadService.uploadChunkSizeInBytes, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Start uploading.");
                                message = await client.PutAsync(upload.ResumableSessionUri, content, cancellationToken);
                            }
                            catch (TaskCanceledException)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Upload stopped by user.");

                                upload.UploadStatus = UplStatus.Stopped;
                                uploadResult.VideoResult = VideoResult.Stopped;
                                return uploadResult;
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception package {package} try {uploadTry}: {e.ToString()}.");
                                errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpClient.PutAsync Exception package {package} try {uploadTry}: {e.GetType().ToString()}: {e.Message}.");

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
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Package {package} finished.");
                            }
                            else
                            {
                                if (!message.IsSuccessStatusCode)
                                {
                                    Tracer.Write($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code package {package} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    errors.AppendLine($"YoutubeVideoUploadService.Upload: HttpResponseMessage unexpected status code package {package} try {uploadTry}: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }

                                YoutubeVideoPostResponse response = JsonConvert.DeserializeObject<YoutubeVideoPostResponse>(await message.Content.ReadAsStringAsync());
                                upload.VideoId = response.Id;

                                //last stats update to reach 0 bytes and time left.
                                stats.CurrentSpeedInBytesPerSecond = 1;
                                upload.BytesSent = inputStream.Position;
                                updateUploadProgress(stats);

                                upload.UploadStatus = UplStatus.Finished;
                                uploadResult.VideoResult = VideoResult.Finished;
                                Tracer.Write($"YoutubeVideoUploadService.Upload: Upload finished with {package}, video id {upload.VideoId}.");
                            }
                        }

                        uploadByteIndex += YoutubeVideoUploadService.uploadChunkSizeInBytes;
                        totalBytesSentInSession += YoutubeVideoUploadService.uploadChunkSizeInBytes;
                    }
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubeVideoUploadService.Upload: End, Unexpected Exception: {e.ToString()}.");

                upload.UploadErrorMessage += $"YoutubeVideoUploadService.Upload: Unexpected Exception: : {e.GetType().ToString()}: {e.Message}.";
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
                        upload.UploadErrorMessage += "YoutubeVideoUploadService.getUploadByteIndex: Getting upload byte index failed 3 times.";
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
            video.Snippet.VideoLanguage = upload.VideoLanguage != null ? upload.VideoLanguage.Name : null;
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

            FileInfo info = new FileInfo(upload.FilePath);
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
                        content.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
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
                        upload.UploadErrorMessage += "YoutubeVideoUploadService.requestNewUpload: Requesting new upload failed 3 times.";
                        throw;
                    }

                    await Task.Delay(TimeSpan.FromSeconds(2));
                    requestTry++;
                }
            }
        }
    }
}
