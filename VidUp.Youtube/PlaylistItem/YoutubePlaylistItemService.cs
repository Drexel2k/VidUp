using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItem.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItem
{
    public class YoutubePlaylistItemService
    {
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems";
        private static int maxResults = 50;

        public static async Task<bool> AddToPlaylistAsync(Upload upload)
        {
            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: Start.");

            if (!string.IsNullOrWhiteSpace(upload.VideoId) && upload.Playlist != null)
            {
                Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: Videos to add available.");

                YoutubePlaylistItemPostRequest playlistItem = new YoutubePlaylistItemPostRequest();
                playlistItem.Snippet = new YoutubePlaylistItemPostRequestSnippet();
                playlistItem.Snippet.PlaylistId = upload.Playlist.PlaylistId;
                playlistItem.Snippet.ResourceId = new YoutubePlaylistItemPostRequestResourceId();
                playlistItem.Snippet.ResourceId.VideoId = upload.VideoId;

                string contentJson = JsonConvert.SerializeObject(playlistItem);

                using (ByteArrayContent byteContent = HttpHelper.GetStreamContent(contentJson, "application/json"))
                {
                    try
                    {
                        using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(upload.YoutubeAccount, HttpMethod.Post,
                            $"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet").ConfigureAwait(false))
                        {
                            requestMessage.Content = byteContent;
                            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: Add to Playlist.");
                            using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                            using (responseMessage.Content)
                            {
                                string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                if (!responseMessage.IsSuccessStatusCode)
                                {
                                    Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpResponseMessage unexpected status code: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                    upload.UploadErrorMessage += $"YoutubePlaylistItemService.AddToPlaylist: Could not add upload to playlist: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.\n";
                                    return false;
                                }

                                Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End.");
                                return true;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.UploadErrorMessage += $"YoutubePlaylistItemService.AddToPlaylist: HttpClient.PostAsync Exception: {e.GetType().ToString()}: {e.Message}.\n";
                        return false;
                    }
                }
            }

            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, nothing to add to playlist.");
            return true;
        }

        public static async Task<Dictionary<Business.Playlist, List<string>>> GetPlaylistsContentAsync(IEnumerable<Business.Playlist> playlists)
        {
            Tracer.Write($"YoutubePlaylistItemService.GetPlaylistsContent: Start.");
            Dictionary<Business.Playlist, List<string>> result = new Dictionary<Business.Playlist, List<string>>();

            if (playlists != null)
            {
                playlists = playlists.Distinct();
                foreach (Business.Playlist playlist in playlists)
                {
                    List<string> playlistResult = new List<string>();

                    if (await YoutubePlaylistItemService.addPlaylistContentToResultAsync(playlist, null, playlistResult).ConfigureAwait(false))
                    {
                        result.Add(playlist, playlistResult);
                    }
                }
            }
            else
            {
                return result;
            }

            Tracer.Write($"YoutubePlaylistItemService.GetPlaylistsContent: End.");
            return result;
        }

        private static async Task<bool> addPlaylistContentToResultAsync(Business.Playlist playlist, string pageToken, List<string> result)
        {
            Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: Start.");

            try
            {
                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(playlist.YoutubeAccount, HttpMethod.Get).ConfigureAwait(false))
                {
                    Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: Get Playlist info.");
                    requestMessage.RequestUri = string.IsNullOrWhiteSpace(pageToken)
                        ? new Uri($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlist.PlaylistId}&maxResults={YoutubePlaylistItemService.maxResults}")
                        : new Uri($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlist.PlaylistId}&maxResults={YoutubePlaylistItemService.maxResults}&pageToken={pageToken}");

                    using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                    using (responseMessage.Content)
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            if (responseMessage.StatusCode != HttpStatusCode.NotFound)
                            {
                                Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: HttpResponseMessage unexpected status code: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                responseMessage.EnsureSuccessStatusCode();
                            }
                            else
                            {
                                return false;
                            }
                        }

                        YoutubePlaylistItemsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistItemsGetResponse>(content);

                        if (response.Items.Length > 0)
                        {
                            foreach (YoutubePlaylistItemsGetResponsePlaylistItem item in response.Items)
                            {
                                result.Add(item.Snippet.ResourceId.VideoId);
                            }
                        }

                        if (!string.IsNullOrWhiteSpace(response.NextPageToken))
                        {
                            return await YoutubePlaylistItemService.addPlaylistContentToResultAsync(playlist, response.NextPageToken, result).ConfigureAwait(false);
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
                Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                throw;
            }

            Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: End.");
            return true;
        }
    }
}
