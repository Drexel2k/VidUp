using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using HeyRed.Mime;

namespace Drexel.VidUp.Youtube.Thumbnail
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
                using (StreamContent streamcontent = HttpHelper.GetStreamContentUpload(fs, MimeTypesMap.GetMimeType(upload.ThumbnailFilePath)))
                {
                    HttpResponseMessage message;
                    try
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Add thumbnail.");
                        message = await client.PostAsync($"{YoutubeThumbnailService.thumbnailEndpoint}?videoId={upload.VideoId}", streamcontent);
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.UploadErrorMessage += $"YoutubeThumbnailService.AddThumbnail: HttpClient.PutAsync Exception: {e.GetType().ToString()}: {e.Message}.\n";
                        return false;
                    }

                    using (message)
                    using (message.Content)
                    {
                        string content = await message.Content.ReadAsStringAsync();
                        if (!message.IsSuccessStatusCode)
                        {
                            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpResponseMessage unexpected status code: {message.StatusCode} {message.ReasonPhrase} with content {content}.");
                            upload.UploadErrorMessage += $"YoutubeThumbnailService.AddThumbnail: HttpResponseMessage unexpected status code: {message.StatusCode} {message.ReasonPhrase} with content {content}.\n";
                            return false;
                        }
                    }

                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End.");
                    return true;
                }
            }

            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, no thumbnail to add.");
            return true;
        }
    }
}
