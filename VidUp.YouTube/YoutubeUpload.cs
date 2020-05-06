using Drexel.VidUp.Business;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace Drexel.VidUp.Youtube
{
    public class YoutubeUpload
    {
        private static string uploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos?uploadType=resumable&part=snippet,status";
        private static string thumbnailEndpoint = "https://www.googleapis.com/upload/youtube/v3/thumbnails/set";
        private static ThrottledBufferedStream stream = null;
        public static long MaxUploadInBytesPerSecond
        {
            set
            {
                ThrottledBufferedStream stream = YoutubeUpload.stream;
                if (stream != null)
                {
                    stream.MaximumBytesPerSecond = value;
                }
            }
        }

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress)
        {
            UploadResult result = new UploadResult()
            {
                ThumbnailSuccessFull = false,
                VideoId = string.Empty
            };

            if(!File.Exists(upload.FilePath))
            {
                upload.UploadErrorMessage = "File does not exist";
                return result;
            }

            YoutubeVideoRequest video = new YoutubeVideoRequest();

            video.Snippet = new YoutubeSnippet();
            video.Snippet.Title = upload.YtTitle;
            video.Snippet.Description = upload.Description;
            video.Snippet.Tags = (upload.Tags != null ? upload.Tags : new List<string>()).ToArray();

            video.Status = new YoutubeStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt.Date != DateTime.MinValue.Date)
            {
                video.Status.PublishAt = upload.PublishAt.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string content = JsonConvert.SerializeObject(video);
            var jsonBytes = Encoding.UTF8.GetBytes(content);

            FileInfo info = new FileInfo(upload.FilePath);

            try
            {
                //request upload session/uri
                HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(YoutubeUpload.uploadEndpoint, "POST", jsonBytes, "application/json; charset=utf-8");
                //slug header adds original video file name to youtube studio
                request.Headers.Add("Slug", Path.GetFileName(upload.FilePath));
                request.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
                request.Headers.Add("X-Upload-Content-Type", "video/*");


                using (Stream dataStream = await request.GetRequestStreamAsync())
                {
                    dataStream.Write(jsonBytes, 0, jsonBytes.Length);
                }

                string location;
                using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync())
                {
                    location = response.Headers["Location"];
                }

                FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open);
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond))
                //using (FileStream inputStream = new FileStream(upload.FilePath, FileMode.Open))
                {
                    YoutubeUpload.stream = inputStream;
                    request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(location, "PUT", upload.FilePath);

                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        //very small buffer increases CPU load >= 10kByte seems OK.
                        byte[] buffer = new byte[10 * 1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        YoutubeUploadStats stats = new YoutubeUploadStats();

                        DateTime lastStatUpdate = DateTime.MinValue;
                        TimeSpan twoSeconds = new TimeSpan(0, 0, 2);
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            await dataStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (DateTime.Now - lastStatUpdate > twoSeconds)
                            {
                                stats.BytesSent = totalBytesRead;
                                stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                updateUploadProgress(stats);
                                lastStatUpdate = DateTime.Now;
                            }
                        }
                    }
                }

                YoutubeUpload.stream = null;

                using (HttpWebResponse httpResponse = (HttpWebResponse)await request.GetResponseAsync())
                {
                    StreamReader reader = new StreamReader(httpResponse.GetResponseStream());

                    var definition = new { Id = "" };
                    var response = JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(), definition);
                    result.VideoId = response.Id;
                }
            }
            catch (WebException e)
            {
                if (e.Response != null)
                {
                    StreamReader reader = new StreamReader(e.Response.GetResponseStream());
                    upload.UploadErrorMessage = $"Video upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                }
                else
                {
                    upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                }

                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeUpload.addThumbnail(upload, result.VideoId);

            return result;
        }
        

        private static async Task<bool> addThumbnail(Upload upload, string videoId)
        {
            if (!string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                try
                {
                    HttpWebRequest request = request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            string.Format("{0}?videoId={1}", YoutubeUpload.thumbnailEndpoint, videoId), "POST", upload.ThumbnailFilePath);

                        using (FileStream inputStream = new FileStream(upload.ThumbnailFilePath, FileMode.Open))
                        using (Stream dataStream = await request.GetRequestStreamAsync())
                        {
                            byte[] buffer = new byte[1024];
                            int bytesRead;
                            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                            {
                                await dataStream.WriteAsync(buffer, 0, bytesRead);
                            }
                        }

                        using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                        {
                        }
                }
                catch (WebException e)
                {
                    if (e.Response != null)
                    {
                        StreamReader reader = new StreamReader(e.Response.GetResponseStream());
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {e.ToString()}";
                    }

                    return false;
                }
            }
            else
            {
                return false;
            }

            return true;
        }
    }
}
