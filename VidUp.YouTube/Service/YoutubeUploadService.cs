using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
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

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, Func<bool> stopUpload)
        {
            Tracer.Write($"YoutubeUploadService.Upload: Start with upload: {upload.FilePath}, maxUploadInBytesPerSecond: {maxUploadInBytesPerSecond}.");

            upload.UploadErrorMessage = string.Empty;

            UploadResult result = new UploadResult()
            {
                UploadSuccessFull = false,
                ThumbnailSuccessFull = false,
                PlaylistSuccessFull = false
            };

            if (!File.Exists(upload.FilePath))
            {
                upload.UploadErrorMessage = "File does not exist.";

                Tracer.Write($"YoutubeUploadService.Upload: End, file doesn't exist.");
                return result;
            }

            StringBuilder errors = new StringBuilder();
            try
            {
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

                Tracer.Write($"YoutubeUploadService.Upload: Initial uploadByteIndex: {uploadByteIndex}.");

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                YoutubeUploadStats stats = new YoutubeUploadStats();
                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond, updateUploadProgress, stats, upload, stopUpload))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeUploadService.stream = inputStream;
                    long totalBytesSentInSession = 0;

                    long fileLength = upload.FileLength;
                    HttpClient client = await HttpHelper.GetAuthenticatedUploadClient();
                    
                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    short uploadTry = 1;
                    bool error = false;

                    Tracer.Write($"YoutubeUploadService.Upload: fileLength: {fileLength}.");
                    PartStream chunkStream;
                    HttpResponseMessage message;
                    StreamContent content;

                    while (fileLength > totalBytesSentInSession + initialBytesSent)
                    {
                        if (totalBytesSentInSession == 0 || error)
                        {
                            Tracer.Write($"YoutubeUploadService.Upload: Upload try: {uploadTry}.");
                        }

                        if (error)
                        {
                            error = false;
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: End, Upload not successful after 3 tries.");
                                upload.UploadErrorMessage = $"YoutubeUploadService.Upload: Upload not successful after 3 tries.";
                                return result;
                            }

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            Tracer.Write($"YoutubeUploadService.Upload: Getting range due to upload retry.");

                            try
                            {
                                uploadByteIndex = await YoutubeUploadService.getUploadByteIndex(upload);
                            }
                            catch (Exception e)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: Get uploadByteIndex due to error Exception: {e.ToString()}.");
                                errors.AppendLine($"YoutubeUploadService.Upload: Get uploadByteIndex due to error Exception: {e.ToString()}.");

                                error = true;
                                continue;
                            }

                            Tracer.Write($"YoutubeUploadService.Upload: Upload retry uploadByteIndex: {uploadByteIndex}.");

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                            totalBytesSentInSession = uploadByteIndex - initialBytesSent;
                        }

                        chunkStream = new PartStream(inputStream, YoutubeUploadService.uploadChunkSizeInBytes);

                        Tracer.Write($"YoutubeUploadService.Upload: Creating content stream.");
                        using (content = HttpHelper.GetStreamContentResumableUpload(chunkStream, uploadByteIndex, YoutubeUploadService.uploadChunkSizeInBytes, MimeTypesMap.GetMimeType(upload.FilePath)))
                        {
                            try
                            {
                                message = await client.PutAsync(upload.ResumableSessionUri, content);
                            }
                            catch (Exception e)
                            {
                                if (stopUpload != null && stopUpload())
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: End, Upload stopped by user.");
                                    upload.UploadStatus = UplStatus.Stopped;
                                    return result;
                                }

                                Tracer.Write($"YoutubeUploadService.Upload: HttpClient.PutAsync Exception: {e.ToString()}.");
                                errors.AppendLine($"YoutubeUploadService.Upload: HttpClient.PutAsync Exception: {e.ToString()}.");

                                error = true;
                                uploadTry++;
                                continue;
                            }
                        }

                        using (message)
                        {
                            if ((int)message.StatusCode != 308)
                            {
                                if (!message.IsSuccessStatusCode)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    errors.AppendLine($"YoutubeUploadService.Upload: HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    error = true;
                                    uploadTry++;
                                    continue;
                                }

                                var definition = new { Id = "" };
                                var response = JsonConvert.DeserializeAnonymousType(await message.Content.ReadAsStringAsync(), definition);
                                upload.VideoId = response.Id;
                                upload.UploadStatus = UplStatus.Finished;
                                result.UploadSuccessFull = true;
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

                upload.UploadStatus = UplStatus.Failed;
                upload.UploadErrorMessage = $"YoutubeUploadService.Upload: Unexpected Exception: {e.ToString()}.";
                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeThumbnailService.AddThumbnail(upload);
            result.PlaylistSuccessFull = await YoutubePlaylistService.AddToPlaylist(upload);

            Tracer.Write($"YoutubeUploadService.Upload: End.");
            return result;
        }

        private static async Task<long> getUploadByteIndex(Upload upload)
        {
            HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
            using (ByteArrayContent content = HttpHelper.GetStreamContentContentRangeOnly(new FileInfo(upload.FilePath).Length))
            using (HttpResponseMessage message = await client.PutAsync(upload.ResumableSessionUri, content))
            {
                string range = message.Headers.GetValues("Range").First();
                if (!string.IsNullOrWhiteSpace(range))
                {
                    string[] parts = range.Split('-');
                    return Convert.ToInt64(parts[1]) + 1;
                }
            }

            return 0;
        }

        private static async Task requestNewUpload(Upload upload)
        {
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

            HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
            using (ByteArrayContent content = HttpHelper.GetStreamContent(contentJson, "application/json"))
            {
                content.Headers.Add("Slug", httpHeaderCompatibleString);
                content.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
                content.Headers.Add("X-Upload-Content-Type", MimeTypesMap.GetMimeType(upload.FilePath));

                using (HttpResponseMessage message = await client.PostAsync($"{YoutubeUploadService.uploadEndpoint}?part=snippet,status&uploadType=resumable", content))
                {
                    message.EnsureSuccessStatusCode();
                    upload.ResumableSessionUri = message.Headers.GetValues("Location").First();
                }
            }
        }
    }
}
