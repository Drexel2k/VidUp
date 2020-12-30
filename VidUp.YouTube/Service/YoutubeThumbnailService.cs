
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeThumbnailService
    {
        private static string thumbnailEndpoint = "https://www.googleapis.com/upload/youtube/v3/thumbnails/set";

        public static async Task<bool> AddThumbnail(Upload upload)
        {
            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Start.");

            if (!string.IsNullOrWhiteSpace(upload.VideoId) && !string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Adding thumbnail.");

                try
                {

                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Creating thumbnail request.");
                    HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            string.Format("{0}?videoId={1}", YoutubeThumbnailService.thumbnailEndpoint, upload.VideoId), "POST", upload.ThumbnailFilePath);

                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Getting request/data stream.");
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

                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Try getting response for thumbnail.");
                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                    }
                }
                catch (WebException e)
                {
                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Unexpected WebException: {e.ToString()}.");

                    if (e.Response != null)
                    {
                        using (e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Thumbnail upload failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Thumbnail upload failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Unexpected Exception: {e.ToString()}.");

                    upload.UploadErrorMessage = $"Thumbnail upload failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                Tracer.Write($"YoutubeThumbnailService.AddThumbnail: No thumbnail to add.");
                return false;
            }

            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End.");
            return true;
        }
    }
}
