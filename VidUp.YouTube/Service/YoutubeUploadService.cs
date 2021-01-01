using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeUploadService
    {
        private static string uploadEndpoint = "https://www.googleapis.com/upload/youtube/v3/videos";

        private static ThrottledBufferedStream stream = null;
        private static int uploadChunkSizeInBytes = 40 * 262144; // 40 MegaByte, The chunk size must be a multiple of 256 KiloByte
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

            try
            {
                string range = null;
                if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                {
                    Tracer.Write($"YoutubeUploadService.Upload: Requesting new upload/new resumable session uri.");
                    await YoutubeUploadService.requestNewUpload(upload);
                }
                else
                {
                    Tracer.Write($"YoutubeUploadService.Upload: Continue upload, getting range.");
                    range = await YoutubeUploadService.getRange(upload);
                }

                long uploadByteIndex = 0;
                if (!string.IsNullOrWhiteSpace(range))
                {
                    string[] parts = range.Split('-');
                    uploadByteIndex = Convert.ToInt64(parts[1]) + 1;
                }

                Tracer.Write($"YoutubeUploadService.Upload: Initial uploadByteIndex: {uploadByteIndex}.");

                upload.BytesSent = uploadByteIndex;
                long initialBytesSent = uploadByteIndex;

                using (FileStream fileStream = new FileStream(upload.FilePath, FileMode.Open))
                using (ThrottledBufferedStream inputStream = new ThrottledBufferedStream(fileStream, maxUploadInBytesPerSecond))
                {
                    inputStream.Position = uploadByteIndex;
                    YoutubeUploadService.stream = inputStream;
                    long totalBytesSentInSession = 0;

                    long fileLength = upload.FileLength;
                    HttpWebRequest request = null;

                    DateTime lastStatUpdate = DateTime.Now;

                    //on IOExceptions try 2 times more to upload the chunk.
                    //no response from the server shall be requested on IOException.
                    int uploadTry = 1;
                    bool getResponse;

                    Tracer.Write($"YoutubeUploadService.Upload: fileLength: {fileLength}.");
                    while (fileLength > totalBytesSentInSession + initialBytesSent)
                    {
                        Tracer.Write($"YoutubeUploadService.Upload: Upload try: {uploadTry}.");

                        getResponse = true;
                        if (uploadTry > 1)
                        {
                            if (uploadTry > 3)
                            {
                                Tracer.Write($"YoutubeUploadService.Upload: Upload not successful after 3 tries.");
                                throw new IOException("Upload after 3 retries not successful.");
                            }

                            //give a little time on IOException, e.g. to await router redial in on 24h disconnect
                            await Task.Delay(TimeSpan.FromSeconds(2));

                            Tracer.Write($"YoutubeUploadService.Upload: Getting range due to upload retry.");
                            range = await YoutubeUploadService.getRange(upload);

                            uploadByteIndex = 0;
                            if (!string.IsNullOrWhiteSpace(range))
                            {
                                string[] parts = range.Split('-');
                                uploadByteIndex = Convert.ToInt64(parts[1]) + 1;
                            }

                            Tracer.Write($"YoutubeUploadService.Upload: Upload retry uploadByteIndex: {uploadByteIndex}.");

                            inputStream.Position = uploadByteIndex;
                            upload.BytesSent = uploadByteIndex;
                            totalBytesSentInSession = uploadByteIndex - initialBytesSent;
                        }

                        request = null;
                        if (uploadByteIndex > 0 || fileLength > YoutubeUploadService.uploadChunkSizeInBytes)
                        {
                            Tracer.Write($"YoutubeUploadService.Upload: Creating upload request with max chunk size.");
                            request = await HttpWebRequestCreator.CreateAuthenticatedResumeHttpWebRequest(upload.ResumableSessionUri, "PUT", fileLength, uploadByteIndex, YoutubeUploadService.uploadChunkSizeInBytes, MimeMapping.GetMimeMapping(upload.FilePath));
                        }
                        else
                        {
                            Tracer.Write($"YoutubeUploadService.Upload: Creating upload request with file size.");
                            request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(upload.ResumableSessionUri, "PUT", upload.FilePath);
                        }


                        //Tracer.Write($"YoutubeUploadService.Upload: Upload request: {request.}.");
                        int chunkBytesSent = 0;

                        Tracer.Write($"YoutubeUploadService.Upload: Getting request/data stream.");
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
                                    Tracer.Write($"YoutubeUploadService.Upload: Upload stopped by user.");
                                    request.Abort();
                                    upload.UploadStatus = UplStatus.Stopped;
                                    break;
                                }

                                try
                                {
                                    await dataStream.WriteAsync(buffer, 0, bytesRead);

                                    totalBytesSentInSession += bytesRead;
                                    chunkBytesSent += bytesRead;

                                    if (chunkBytesSent + buffer.Length > YoutubeUploadService.uploadChunkSizeInBytes)
                                    {
                                        readLength = YoutubeUploadService.uploadChunkSizeInBytes - chunkBytesSent;

                                        if (readLength == 0)
                                        {
                                            Tracer.Write($"YoutubeUploadService.Upload: Chunk finished.");
                                            uploadByteIndex += YoutubeUploadService.uploadChunkSizeInBytes;
                                        }
                                    }

                                    if (DateTime.Now - lastStatUpdate > YoutubeUploadService.twoSeconds)
                                    {
                                        stats.CurrentSpeedInBytesPerSecond = inputStream.CurrentSpeedInBytesPerSecond;
                                        upload.BytesSent = initialBytesSent + totalBytesSentInSession;
                                        updateUploadProgress(stats);
                                        lastStatUpdate = DateTime.Now;
                                    }
                                }
                                catch (IOException e)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: IOException: {e.ToString()}.");
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
                                    Tracer.Write($"YoutubeUploadService.Upload: Try getting response for chunk.");
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

                                            Tracer.Write($"YoutubeUploadService.Upload: Upload finished, video Id: {upload.VideoId}.");
                                        }
                                    }
                                }
                            }
                            catch (WebException e)
                            {
                                if (e.Response == null)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: Unexpected chunk response, WebException: {e.ToString()}.");
                                    throw;
                                }

                                HttpWebResponse httpResponse = e.Response as HttpWebResponse;

                                if (httpResponse == null)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: Unexpected chunk response, WebException: {e.ToString()}.");
                                    throw;
                                }

                                int statusCode = (int) httpResponse.StatusCode;
                                if (statusCode != 308)
                                {
                                    Tracer.Write($"YoutubeUploadService.Upload: Unexpected chunk response, http: {statusCode}, WebException: {e.ToString()}.");
                                    throw;
                                }

                                Tracer.Write($"YoutubeUploadService.Upload: Upload not finished (http 308), continue uploading.");
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
                    Tracer.Write($"YoutubeUploadService.Upload: Unexpected WebException: {e.ToString()}.");

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
                Tracer.Write($"YoutubeUploadService.Upload: Unexpected Exception: {e.ToString()}.");

                upload.UploadErrorMessage = $"Video upload failed: {e.ToString()}";
                return result;
            }

            result.ThumbnailSuccessFull = await YoutubeThumbnailService.AddThumbnail(upload);
            result.PlaylistSuccessFull = await YoutubePlaylistService.AddToPlaylist(upload);

            Tracer.Write($"YoutubeUploadService.Upload: End.");
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

            string content = JsonConvert.SerializeObject(video);

            Tracer.Write($"YoutubeUploadService.Upload: New upload/new resumable session uri request content: {content}.");

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
            request.Headers.Add("X-Upload-Content-Type", MimeMapping.GetMimeMapping(upload.FilePath));


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
