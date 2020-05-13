using Drexel.VidUp.Business;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Security.Authentication;
using System.Security.Cryptography;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using Drexel.VidUp.Youtube.Data;

namespace Drexel.VidUp.Youtube.Service
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

        public static async Task<UploadResult> Upload(Upload upload, long maxUploadInBytesPerSecond, Action<YoutubeUploadStats> updateUploadProgress, Func<bool> stopUpload)
        {
            UploadResult result = new UploadResult()
            {
                ThumbnailSuccessFull = false,
                VideoId = string.Empty
            };

            if(!File.Exists(upload.FilePath))
            {
                upload.UploadErrorMessage = "File does not exist.";
                return result;
            }

            try
            {
                string range = null;
                if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                {
                    await YoutubeUpload.requestNewUpload(upload);
                }
                else
                {
                    range = await YoutubeUpload.getRange(upload);
                }

                long uploadStartByteIndex = 0;
                if (!string.IsNullOrWhiteSpace(range))
                {
                    string[] parts = range.Split('-');
                    uploadStartByteIndex = Convert.ToInt64(parts[1]) + 1;
                }

                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond))
                {
                    inputStream.Position = uploadStartByteIndex;
                    YoutubeUpload.stream = inputStream;
                    HttpWebRequest request = null;
                    if (uploadStartByteIndex > 0)
                    {
                        request = await HttpWebRequestCreator.CreateAuthenticatedResumeHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath, uploadStartByteIndex);
                    }
                    else
                    {
                        request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);
                    }


                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        //very small buffer increases CPU load >= 10kByte seems OK.
                        byte[] buffer = new byte[10 * 1024];
                        int bytesRead;
                        long totalBytesRead = 0;
                        YoutubeUploadStats stats = new YoutubeUploadStats();

                        DateTime lastStatUpdate = DateTime.Now;
                        TimeSpan twoSeconds = new TimeSpan(0, 0, 2);
                        while ((bytesRead = await inputStream.ReadAsync(buffer, 0, buffer.Length)) != 0)
                        {
                            if (stopUpload != null && stopUpload())
                            {
                                request.Abort();
                                upload.UploadStatus = UplStatus.Stopped;
                                break;
                            }

                            await dataStream.WriteAsync(buffer, 0, bytesRead);
                            totalBytesRead += bytesRead;

                            if (DateTime.Now - lastStatUpdate > twoSeconds)
                            {
                                stats.BytesSent = totalBytesRead;
                                stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                upload.BytesSent = uploadStartByteIndex + totalBytesRead;
                                updateUploadProgress(stats);
                                lastStatUpdate = DateTime.Now;
                            }
                        }
                    }

                    if (upload.UploadStatus != UplStatus.Stopped)
                    {
                        upload.BytesSent = upload.FileLength;
                        YoutubeUpload.stream = null;

                        using (HttpWebResponse httpResponse = (HttpWebResponse)await request.GetResponseAsync())
                        {
                            using (StreamReader reader = new StreamReader(httpResponse.GetResponseStream()))
                            {
                                var definition = new { Id = "" };
                                var response = JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(), definition);
                                result.VideoId = response.Id;
                            }
                        }
                    }
                }
            }
            catch (WebException e)
            {
                if (upload.UploadStatus != UplStatus.Stopped)
                {
                    if (e.Response != null)
                    {
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage =
                                $"Video upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                            e.Response.Close();
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                    }
                }

                return result;
            }
            catch(IOException e)
            {
                upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeUpload.addThumbnail(upload, result.VideoId);

            return result;
        }

        private static async Task<string> getRange(Upload upload)
        {
            HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedResumeInformationHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);

            try
            {
                using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                {
                }
            }
            catch (WebException e)
            {
                if (e.Response == null)
                {
                    throw;
                }

                HttpWebResponse response = e.Response as HttpWebResponse;
                if (response == null)
                {
                    throw;
                }

                if ((int)response.StatusCode != 308)
                {
                    throw;
                }

                return response.Headers["Range"];
            }

            throw new InvalidOperationException("Http status code 308 expected for ResumeInformationHttpWebRequest");
        }

        private static async Task requestNewUpload(Upload upload)
        {
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
            //request upload session/uri
            HttpWebRequest request =
                await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                    YoutubeUpload.uploadEndpoint, "POST", jsonBytes, "application/json; charset=utf-8");
            //slug header adds original video file name to youtube studio, lambda filters to valid chars (ascii >=32 and <=255)
            request.Headers.Add("Slug", new String(Path.GetFileName(upload.FilePath).Where(c =>
            {
                char ch = (char) ((uint) byte.MaxValue & (uint) c);
                if (ch >= ' ' || ch == '\t')
                {
                    return true;
                }

                return false;
            }).ToArray()));
            request.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
            request.Headers.Add("X-Upload-Content-Type", "video/*");


            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse) await request.GetResponseAsync())
            {
                upload.ResumableSessionUri = response.Headers["Location"];
            }
        }


        private static async Task<bool> addThumbnail(Upload upload, string videoId)
        {
            if (!string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                try
                {
                    HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
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
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Thumbnail upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                            e.Response.Close();
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (IOException e)
                {
                    upload.UploadErrorMessage = $"Thumbnail upload failed: {e.ToString()}";
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
