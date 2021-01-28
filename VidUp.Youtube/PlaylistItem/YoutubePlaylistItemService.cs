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

        public static async Task<bool> AddToPlaylist(Upload upload)
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

                HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                using (ByteArrayContent content = HttpHelper.GetStreamContent(contentJson, "application/json"))
                {
                    HttpResponseMessage message;

                    try
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: Add to Playlist.");
                        message = await client.PostAsync($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet", content);
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.UploadErrorMessage += $"YoutubePlaylistItemService.AddToPlaylist: HttpClient.PostAsync Exception: {e.GetType().ToString()}: {e.Message}.";
                        return false;
                    }

                    using (message)
                    {
                        if (!message.IsSuccessStatusCode)
                        {
                            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                            upload.UploadErrorMessage += $"YoutubePlaylistItemService.AddToPlaylist: HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.";
                            return false;
                        }
                    }

                    Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End.");
                    return true;
                }
            }

            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, nothing to add to playlist.");
            return true;
        }

        public static async Task<Dictionary<string, List<string>>> GetPlaylistsContent(IEnumerable<string> playlistIds)
        {
            Tracer.Write($"YoutubePlaylistItemService.GetPlaylistsContent: Start.");
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            if (playlistIds != null)
            {
                playlistIds = playlistIds.Distinct();
                foreach (string playlistId in playlistIds)
                {
                    List<string> playlistResult = new List<string>();

                    if (await YoutubePlaylistItemService.addPlaylistContentToResult(playlistId, null, playlistResult))
                    {
                        result.Add(playlistId, playlistResult);
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

        private static async Task<bool> addPlaylistContentToResult(string playlistId, string pageToken, List<string> result)
        {
            Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: Start.");

            HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
            HttpResponseMessage message;

            try
            {
                Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: Get Playlist info.");
                if (string.IsNullOrWhiteSpace(pageToken))
                {
                    message = await client.GetAsync($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlistId}&maxResults={YoutubePlaylistItemService.maxResults}");
                }
                else
                {
                    message = await client.GetAsync($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlistId}&maxResults={YoutubePlaylistItemService.maxResults}&pageToken={pageToken}");
                }
            }
            catch (Exception e)
            {
                Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                throw;
            }

            using (message)
            {
                if (!message.IsSuccessStatusCode)
                {
                    if (message.StatusCode != HttpStatusCode.NotFound)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                        throw new HttpRequestException($"Http error status code: {message.StatusCode}, message {message.ReasonPhrase}.");
                    }
                    else
                    {
                        return false;
                    }
                }

                YoutubePlaylistItemsGetResponse response = JsonConvert.DeserializeObject<YoutubePlaylistItemsGetResponse>(await message.Content.ReadAsStringAsync());

                if (response.Items.Length > 0)
                {
                    foreach (YoutubePlaylistItemsGetResponsePlaylistItem item in response.Items)
                    {
                        result.Add(item.Snippet.ResourceId.VideoId);
                    }
                }

                if (!string.IsNullOrWhiteSpace(response.NextPageToken))
                {
                    return await YoutubePlaylistItemService.addPlaylistContentToResult(playlistId, response.NextPageToken, result);
                }
            }

            Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: End.");
            return true;
        }
    }
}
