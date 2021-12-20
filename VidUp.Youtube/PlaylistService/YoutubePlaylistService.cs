using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
using Drexel.VidUp.Youtube.PlaylistService.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistService
{
    public class YoutubePlaylistService
    {
        private static string playlistsEndpoint = "https://www.googleapis.com/youtube/v3/playlists";
        private static int maxResults = 50;
        public static async Task<GetPlaylistsResult> GetPlaylistsAsync(YoutubeAccount youtubeAccount)
        {
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: Start.");
            GetPlaylistsResult result = new GetPlaylistsResult();

            StatusInformation statusInformation = await YoutubePlaylistService.addPlaylistsToResultAsync(null, result.Playlists, youtubeAccount).ConfigureAwait(false);
            if (statusInformation != null)
            {
                result.StatusInformation = statusInformation;

                if (statusInformation.IsQuotaError)
                {
                    Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, quota exceeded.");
                }

                if (statusInformation.IsAuthenticationError)
                {
                    Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, authentication error.");
                }

                return result;
            }
            
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: End.");
            return result;
        }

        private static async Task<StatusInformation> addPlaylistsToResultAsync(string pageToken, List<PlaylistApi> result, YoutubeAccount youtubeAccount)
        {
            if (result == null)
            {
                throw new ArgumentException("Result must not be null.");
            }

            Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: Start, pageToken = { pageToken }.");

            try
            {
                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(youtubeAccount, HttpMethod.Get).ConfigureAwait(false))
                {
                    Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: Get Playlists.");
                    requestMessage.RequestUri = string.IsNullOrWhiteSpace(pageToken)
                        ? new Uri($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={YoutubePlaylistService.maxResults}")
                        : new Uri($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={YoutubePlaylistService.maxResults}&pageToken={pageToken}");

                    using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                    {
                        using (responseMessage)
                        using (responseMessage.Content)
                        {
                            string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!responseMessage.IsSuccessStatusCode)
                            {
                                throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                            }

                            YoutubePlaylistsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistsGetResponse>(content);

                            if (response.Items.Length > 0)
                            {
                                foreach (YoutubePlaylistsGetResponsePlaylist item in response.Items)
                                {
                                    result.Add(new PlaylistApi(item.Id, item.Snippet.Title));
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(response.NextPageToken))
                            {
                                StatusInformation statusInformation = await YoutubePlaylistService.addPlaylistsToResultAsync(response.NextPageToken, result, youtubeAccount).ConfigureAwait(false);
                                if (statusInformation != null)
                                {
                                    if (statusInformation.IsQuotaError)
                                    {
                                        Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, quota exceeded.");
                                    }

                                    if (statusInformation.IsAuthenticationError)
                                    {
                                        Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, authentication error.");
                                    }

                                    return statusInformation;
                                }
                            }
                        }
                    }
                }
            }
            catch (AuthenticationException e)
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, authentication error.");
                return StatusInformationCreator.Create("YoutubePlaylistService.addPlaylistsToResult", e);
            }
            catch (HttpStatusException e)
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                StatusInformation statusInformation = StatusInformationCreator.Create("YoutubePlaylistService.addPlaylistsToResult", e);
                if (statusInformation.IsQuotaError)
                {
                    Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, quota exceeded.");
                    return statusInformation;
                }

                throw;
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                throw;
            }

            Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, pageToken = { pageToken }.");
            return null;
        }
    }
}
