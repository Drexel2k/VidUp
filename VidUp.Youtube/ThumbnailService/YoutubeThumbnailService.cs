using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using HeyRed.Mime;

namespace Drexel.VidUp.Youtube.ThumbnailService
{
    public class YoutubeThumbnailService
    {
        private static string thumbnailEndpoint = "https://www.googleapis.com/upload/youtube/v3/thumbnails/set";

        public static async Task<bool> AddThumbnailAsync(Upload upload)
        {
            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Start.");

            if (!string.IsNullOrWhiteSpace(upload.VideoId) && !string.IsNullOrWhiteSpace(upload.ThumbnailFilePath) && File.Exists(upload.ThumbnailFilePath))
            {
                Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Video with thumbnail to add available.");

                using (FileStream fs = new FileStream(upload.ThumbnailFilePath, FileMode.Open))
                using (StreamContent streamContent = HttpHelper.GetStreamContentUpload(fs, MimeTypesMap.GetMimeType(upload.ThumbnailFilePath)))
                {
                    using(HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                        upload.YoutubeAccount, HttpMethod.Post, $"{YoutubeThumbnailService.thumbnailEndpoint}?videoId={upload.VideoId}").ConfigureAwait(false))
                    {
                        try
                        {
                            requestMessage.Content = streamContent;
                            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Add thumbnail.");

                            using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                            using (responseMessage.Content)
                            {
                                string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                if (!responseMessage.IsSuccessStatusCode)
                                {
                                    Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpResponseMessage unexpected status code: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                    upload.AddUploadError(new StatusInformation($"YoutubeThumbnailService.AddThumbnail: Could not add thumbnail: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'."));
                                    return false;
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                            upload.AddUploadError(new StatusInformation($"YoutubeThumbnailService.AddThumbnail: HttpClient.PutAsync Exception: {e.GetType().ToString()}: {e.Message}."));
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
