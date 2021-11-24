using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Playlist.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist
{
    public class YoutubePlaylistService
    {
        private static string playlistsEndpoint = "https://www.googleapis.com/youtube/v3/playlists";
        private static int maxResults = 50;
        public static async Task<List<Playlist>> GetPlaylistsAsync(string accountName)
        {
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: Start.");
            List<Playlist> result = new List<Playlist>();
            await YoutubePlaylistService.addPlaylistsToResultAsync(null, result, accountName).ConfigureAwait(false);
            
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: End.");
            return result;
        }

        private static async Task<List<Playlist>> addPlaylistsToResultAsync(string pageToken, List<Playlist> result, string accountName)
        {
            if (result == null)
            {
                throw new ArgumentException("result must not be null.");
            }

            Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: Start, pageToken = { pageToken }.");

            try
            {
                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(accountName, HttpMethod.Get).ConfigureAwait(false))
                {
                    Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: Get Playlists.");
                    requestMessage.RequestUri = string.IsNullOrWhiteSpace(pageToken)
                        ? new Uri($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={YoutubePlaylistService.maxResults}")
                        : new Uri($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={YoutubePlaylistService.maxResults}&pageToken={pageToken}");

                    using (HttpResponseMessage responseMessage =
                        await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                    {
                        using (responseMessage)
                        using (responseMessage.Content)
                        {
                            string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                            if (!responseMessage.IsSuccessStatusCode)
                            {
                                Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: HttpResponseMessage unexpected status code: {responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                responseMessage.EnsureSuccessStatusCode();
                            }

                            YoutubePlaylistsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistsGetResponse>(content);

                            if (response.Items.Length > 0)
                            {
                                foreach (YoutubePlaylistsGetResponsePlaylist item in response.Items)
                                {
                                    result.Add(new Playlist(item.Id, item.Snippet.Title));
                                }
                            }

                            if (!string.IsNullOrWhiteSpace(response.NextPageToken))
                            {
                                await YoutubePlaylistService.addPlaylistsToResultAsync(response.NextPageToken, result, accountName).ConfigureAwait(false);
                            }
                        }
                    }
                }
            }
            catch (HttpRequestException)
            {
                throw;
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                throw;
            }

            Tracer.Write($"YoutubePlaylistService.addPlaylistsToResult: End, pageToken = { pageToken }.");
            return result;
        }
    }
}
