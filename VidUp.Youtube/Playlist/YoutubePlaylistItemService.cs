using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Playlist.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist
{
    public class YoutubePlaylistItemService
    {
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems";

        public static async Task<bool> AddToPlaylist(Upload upload)
        {
            Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Start.");

            if (!string.IsNullOrWhiteSpace(upload.VideoId) && upload.Playlist != null)
            {
                Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Videos to add available.");

                YoutubePlaylistItemPostRequest playlistItem = new YoutubePlaylistItemPostRequest();
                playlistItem.Snippet = new YoutubePlaylistItemPostRequestSnippet();
                playlistItem.Snippet.PlaylistId = upload.Playlist.PlaylistId;
                playlistItem.Snippet.ResourceId = new YoutubePlaylistItemsGetResponseResourceId();
                playlistItem.Snippet.ResourceId.VideoId = upload.VideoId;

                string contentJson = JsonConvert.SerializeObject(playlistItem);

                HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                using (ByteArrayContent content = HttpHelper.GetStreamContent(contentJson, "application/json"))
                {
                    HttpResponseMessage message;

                    try
                    {
                        Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Add to Playlist.");
                        message = await client.PostAsync($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet", content);
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.UploadErrorMessage += $"YoutubePlaylistService.AddToPlaylist: HttpClient.PostAsync Exception: {e.GetType().ToString()}: {e.Message}.";
                        return false;
                    }

                    using (message)
                    {
                        if (!message.IsSuccessStatusCode)
                        {
                            Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                            upload.UploadErrorMessage += $"YoutubePlaylistService.AddToPlaylist: HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.";
                            return false;
                        }
                    }

                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End.");
                    return true;
                }
            }

            Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End, nothing to add to playlist.");
            return true;
        }


        public static async Task<Dictionary<string, List<string>>> GetPlaylists(IEnumerable<string> playlistIds)
        {
            Tracer.Write($"YoutubePlaylistService.GetPlaylists: Start.");
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            if (playlistIds != null)
            {
                playlistIds = playlistIds.Distinct();
                string[] playlistIdsInternal = playlistIds as string[] ?? playlistIds.ToArray();
                if (playlistIdsInternal.Length > 0)
                {
                    Tracer.Write($"YoutubePlaylistService.GetPlaylists: Playlists to get available.");

                    foreach (string playlistId in playlistIdsInternal)
                    {
                        HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                        HttpResponseMessage message;

                        try
                        {
                            Tracer.Write($"YoutubePlaylistService.GetPlaylists: Get Playlist info.");
                            message = await client.GetAsync($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlistId}");
                        }
                        catch (Exception e)
                        {
                            Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                            throw;
                        }

                        using (message)
                        {
                            if (!message.IsSuccessStatusCode)
                            {
                                if (message.StatusCode != HttpStatusCode.NotFound)
                                {
                                    Tracer.Write($"YoutubePlaylistService.GetPlaylists: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                                    throw new HttpRequestException($"Http error status code: {message.StatusCode}, message {message.ReasonPhrase}.");
                                }

                                continue;
                            }

                            List<string> videoIds = new List<string>();
                            result.Add(playlistId, videoIds);

                            YoutubePlaylistItemsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistItemsGetResponse>(await message.Content.ReadAsStringAsync());

                            if (response.Items.Length > 0)
                            {
                                foreach (YoutubePlaylistItemsGetResponsePlaylistItem item in response.Items)
                                {
                                    videoIds.Add(item.Snippet.ResourceId.VideoId);
                                }
                            }
                        }
                    }
                }
            }

            Tracer.Write($"YoutubePlaylistService.GetPlaylists: End.");
            return result;
        }
    }
}
