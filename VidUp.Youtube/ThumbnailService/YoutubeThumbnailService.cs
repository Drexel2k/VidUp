using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
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
                    try
                    {
                        using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                                upload.YoutubeAccount, HttpMethod.Post,
                                $"{YoutubeThumbnailService.thumbnailEndpoint}?videoId={upload.VideoId}")
                            .ConfigureAwait(false))
                        {

                            requestMessage.Content = streamContent;
                            Tracer.Write($"YoutubeThumbnailService.AddThumbnail: Add thumbnail.");

                            using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient
                                .SendAsync(requestMessage).ConfigureAwait(false))
                            using (responseMessage.Content)
                            {
                                string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                if (!responseMessage.IsSuccessStatusCode)
                                {
                                    throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                                }
                            }
                        }
                    }
                    catch (AuthenticationException e)
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, authentication exception: {e.ToString()}.");

                        StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0007", $"Could not add thumbnail to video.", e);
                        upload.AddStatusInformation(statusInformation);

                        return false;
                    }
                    catch (HttpStatusException e)
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                        upload.AddStatusInformation(StatusInformationCreatorYoutube.Create("ERR0008", "Could not add thumbnail to video.", e));
                        return false;
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubeThumbnailService.AddThumbnail: End, HttpClient.SendAsync Exception: {e.ToString()}.");
                        upload.AddStatusInformation(StatusInformationCreator.Create("ERR0009", "Could not add thumbnail to video.", e));
                        return false;
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
