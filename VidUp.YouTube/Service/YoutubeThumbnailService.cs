using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
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
                Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Video with thumbnail to add available.");


                HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                using (FileStream fs = new FileStream(upload.ThumbnailFilePath, FileMode.Open))
                using (StreamContent content = HttpHelper.GetStreamContentUpload(fs, MimeMapping.GetMimeMapping(upload.ThumbnailFilePath)))
                {
                    HttpResponseMessage message;
                    try
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Add thumbnail.");
                        message = await client.PostAsync($"{YoutubeThumbnailService.thumbnailEndpoint}?videoId={upload.VideoId}", content);
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.UploadErrorMessage += $"YoutubeThumbnailService.AddThumbnail: HttpClient.PutAsync Exception: {e.ToString()}.";
                        return false;
                    }

                    using (message)
                    {
                        if (!message.IsSuccessStatusCode)
                        {
                            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                            upload.UploadErrorMessage += $"YoutubeThumbnailService.AddThumbnail: HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.";
                            return false;
                        }
                    }

                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End.");
                    return true;
                }
            }

            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: No thumbnail to add.");
            return false;
        }
    }
}
