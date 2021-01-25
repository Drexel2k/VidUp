using System;
using System.Collections.Generic;
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
        public static async Task<List<Playlist>> GetPlaylists()
        {
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: Start.");
            List<Playlist> result = new List<Playlist>();
            await YoutubePlaylistService.addPlaylists(null, result);
            
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: End.");
            return result;
        }

        private static async Task<List<Playlist>> addPlaylists(string pageToken, List<Playlist> result)
        {
            if (result == null)
            {
                throw new ArgumentException("result must not be null.");
            }

            Tracer.Write($"YoutubePlaylistService.addPlaylists: Start, pageToken = { pageToken }.");
            HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
            HttpResponseMessage message;

            try
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylists: Get Playlists.");
                if (string.IsNullOrWhiteSpace(pageToken))
                {
                    message = await client.GetAsync($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={ YoutubePlaylistService.maxResults }");
                }
                else
                {
                    message = await client.GetAsync($"{YoutubePlaylistService.playlistsEndpoint}?part=snippet&mine=true&maxResults={ YoutubePlaylistService.maxResults }&pageToken={pageToken}");
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubePlaylistService.addPlaylists: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                throw;
            }

            using (message)
            {
                if (!message.IsSuccessStatusCode)
                {
                    Tracer.Write($"YoutubePlaylistService.addPlaylists: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                    throw new HttpRequestException($"Http error status code: {message.StatusCode}, message {message.ReasonPhrase}.");
                }

                YoutubePlaylistsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistsGetResponse>(await message.Content.ReadAsStringAsync());

                if (response.Items.Length > 0)
                {
                    foreach (YoutubePlaylistsGetResponsePlaylist item in response.Items)
                    {
                        result.Add(new Playlist(item.Id, item.Snippet.Title));
                    }
                }

                if (!string.IsNullOrWhiteSpace(response.NextPageToken))
                {
                    await YoutubePlaylistService.addPlaylists(response.NextPageToken, result);
                }
            }


            Tracer.Write($"YoutubePlaylistService.addPlaylists: End, pageToken = { pageToken }.");
            return result;
        }
    }
}
