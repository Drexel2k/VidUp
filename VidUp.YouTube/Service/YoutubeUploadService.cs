using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeUploadService
    {
        private static string uploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos";

        private static ThrottledBufferedStream stream = null;
        private static int uploadChunkSizeInBytes = 40 * 262144; // 10 MegaByte, The chunk size must be a multiple of 256 KiloByte
        private static TimeSpan twoSeconds = new TimeSpan(0, 0, 2);
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
                return result;
            }

            try
            {
                string range = null;
                if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                {
                    await YoutubeUploadService.requestNewUpload(upload);
                }
                else
                {
                    range = await YoutubeUploadService.getRange(upload);
                }

                long uploadByteIndex = 0;
                if (!string.IsNullOrWhiteSpace(range))
                {
                    string[] parts = range.Split('-');
                    uploadByteIndex = Convert.ToInt64(parts[1]) + 1;
                }

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeUploadService.stream = inputStream;
                    long totalBytesSent = 0;

                    long fileLength = upload.FileLength;
                    HttpWebRequest request = null;

                    DateTime lastStatUpdate = DateTime.Now;

                    //on IOExceptions try 2 times more to uppload the chunk.
                    //no response from the server shall be requested on IOException.
                    int uploadTry = 1;
                    bool getResponse;
                    while (fileLength > totalBytesSent + initialBytesSent)
                    {
                        getResponse = true;
                        if (uploadTry > 1)
                        {
                            if (uploadTry > 3)
                            {
                                throw new IOException("Upload after 3 retries not successful.");
                            }

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            range = await YoutubeUploadService.getRange(upload);
                            uploadByteIndex = 0;
                            if (!string.IsNullOrWhiteSpace(range))
                            {
                                string[] parts = range.Split('-');
                                uploadByteIndex = Convert.ToInt64(parts[1]) + 1;
                            }

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                            totalBytesSent = uploadByteIndex - initialBytesSent;
                        }
                        request = null;
                        if (uploadByteIndex > 0 || fileLength > YoutubeUploadService.uploadChunkSizeInBytes)
                        {
                            request = await HttpWebRequestCreator.CreateAuthenticatedResumeHttpWebRequest(upload.ResumableSessionUri, "PUT", fileLength, uploadByteIndex, YoutubeUploadService.uploadChunkSizeInBytes);
                        }
                        else
                        {
                            request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);
                        }

                        int chunkBytesSent = 0;
                        using (Stream dataStream = await request.GetRequestStreamAsync())
                        {
                            //very small buffer increases CPU load >= 10kByte seems OK.
                            byte[] buffer = new byte[10 * 1024];
                            int bytesRead;
                            YoutubeUploadStats stats = new YoutubeUploadStats();



                            int readLength = buffer.Length;
                            while ((bytesRead = await inputStream.ReadAsync(buffer, 0, readLength)) != 0)
                            {
                                if (stopUpload != null && stopUpload())
                                {
                                    request.Abort();
                                    upload.UploadStatus = UplStatus.Stopped;
                                    break;
                                }

                                try
                                {
                                    await dataStream.WriteAsync(buffer, 0, bytesRead);

                                    totalBytesSent += bytesRead;
                                    chunkBytesSent += bytesRead;

                                    if (chunkBytesSent + buffer.Length > YoutubeUploadService.uploadChunkSizeInBytes)
                                    {
                                        readLength = YoutubeUploadService.uploadChunkSizeInBytes - chunkBytesSent;

                                        if (readLength == 0)
                                        {
                                            uploadByteIndex += YoutubeUploadService.uploadChunkSizeInBytes;
                                        }
                                    }

                                    if (DateTime.Now - lastStatUpdate > YoutubeUploadService.twoSeconds)
                                    {
                                        stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                        upload.BytesSent = initialBytesSent + totalBytesSent;
                                        updateUploadProgress(stats);
                                        lastStatUpdate = DateTime.Now;
                                    }
                                }
                                catch (IOException e)
                                {
                                    uploadTry++;
                                    getResponse = false;
                                    break;
                                }
                            }

                            if (!getResponse)
                            { 
                                continue;
                            }

                            uploadTry = 1;

                            try
                            {
                                if (upload.UploadStatus != UplStatus.Stopped)
                                {
                                    //if only chunk of video is finished, but video is not completed
                                    //this will throw WebException with http status 308.
                                    using (HttpWebResponse httpResponse =
                                        (HttpWebResponse) await request.GetResponseAsync())
                                    {
                                        stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                        upload.BytesSent = upload.FileLength;
                                        updateUploadProgress(stats);
                                        lastStatUpdate = DateTime.Now;

                                        YoutubeUploadService.stream = null;

                                        using (StreamReader reader =
                                            new StreamReader(httpResponse.GetResponseStream()))
                                        {
                                            var definition = new {Id = ""};
                                            var response =
                                                JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(),
                                                    definition);
                                            upload.VideoId = response.Id;
                                            result.UploadSuccessFull = true;
                                        }
                                    }
                                }
                            }
                            catch (WebException e)
                            {
                                if (e.Response == null)
                                {
                                    throw;
                                }

                                HttpWebResponse httpResponse = e.Response as HttpWebResponse;

                                if (httpResponse == null)
                                {

                                    throw;
                                }

                                if ((int)httpResponse.StatusCode != 308)
                                {
                                    throw;
                                }

                                //continue on http status 308 which means resumable upload part was uploaded correctly.
                                httpResponse.Dispose();
                                e.Response.Dispose();
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
                        using (e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage = 
                                $"Video upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                    }
                }

                return result;
            }
            catch (Exception e)
            {
                upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeThumbnailService.AddThumbnail(upload);
            result.PlaylistSuccessFull = await YoutubePlaylistService.AddToPlaylist(upload);

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

                using (e.Response)
                using (HttpWebResponse httpResponse = e.Response as HttpWebResponse)
                {
                    if (httpResponse == null)
                    {
                        throw;
                    }

                    if ((int)httpResponse.StatusCode != 308)
                    {
                        throw;
                    }

                    return httpResponse.Headers["Range"];
                }
            }

            throw new InvalidOperationException("Http status code 308 expected for ResumeInformationHttpWebRequest");
        }

        private static async Task requestNewUpload(Upload upload)
        {
            YoutubeVideoRequest video = new YoutubeVideoRequest();

            video.VideoSnippet = new YoutubeVideoSnippet();
            video.VideoSnippet.Title = upload.YtTitle;
            video.VideoSnippet.Description = upload.Description;
            video.VideoSnippet.Tags = (upload.Tags != null ? upload.Tags : new List<string>()).ToArray();

            video.Status = new YoutubeStatus();
            video.Status.Privacy = upload.Visibility.ToString().ToLower(); // "unlisted", "private" or "public"

            if (upload.PublishAt != null)
            {
                video.Status.PublishAt = upload.PublishAt.Value.ToString("yyyy-MM-ddTHH:mm:ss.ffffzzz");
            }

            string content = JsonConvert.SerializeObject(video);
            var jsonBytes = Encoding.UTF8.GetBytes(content);

            FileInfo info = new FileInfo(upload.FilePath);
            //request upload session/uri
            HttpWebRequest request =
                await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                    $"{YoutubeUploadService.uploadEndpoint}?part=snippet,status&uploadType=resumable", "POST", jsonBytes, "application/json; charset=utf-8");

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

            request.Headers.Add("Slug", httpHeaderCompatibleString);
            request.Headers.Add("X-Upload-Content-Length", info.Length.ToString());
            request.Headers.Add("X-Upload-Content-Type", "video/*");


            using (Stream dataStream = await request.GetRequestStreamAsync())
            {
                dataStream.Write(jsonBytes, 0, jsonBytes.Length);
            }

            using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
            {
                upload.ResumableSessionUri = response.Headers["Location"];
            }
        }
    }
}
