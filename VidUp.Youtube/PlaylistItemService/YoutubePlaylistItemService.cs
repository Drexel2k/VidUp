using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItemService.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService
{
    public class YoutubePlaylistItemService
    {
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems";
        private static int maxResults = 50;

        public static async Task<AddToPlaylistResult> AddToPlaylistAsync(Upload upload)
        {
            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: Start.");
            AddToPlaylistResult result = new AddToPlaylistResult();
            result.Success = true;

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
                                    StatusInformation statusInformation = new StatusInformation(content);
                                    if (statusInformation.IsQuotaError)
                                    {
                                        result.StatusInformation = statusInformation;
                                        result.Success = false;
                                        upload.AddUploadError(statusInformation);
                                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, quota exceeded.");
                                        return result;
                                    }


                                    Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpResponseMessage unexpected status code: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                                    upload.AddUploadError(new StatusInformation($"YoutubePlaylistItemService.AddToPlaylist: Could not add upload to playlist: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'."));
                                    result.Success = false;
                                    return result;
                                }

                                Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End.");
                                return result;
                            }
                        }
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpClient.PostAsync Exception: {e.ToString()}.");
                        upload.AddUploadError(new StatusInformation($"YoutubePlaylistItemService.AddToPlaylist: HttpClient.PostAsync Exception: {e.GetType().ToString()}: {e.Message}."));
                        result.Success = false;
                        return result;
                    }
                }
            }

            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, nothing to add to playlist.");
            return result;
        }

        //gets all item/video ids of the playlists and checks if playlist exists on youtube
        public static async Task<GetPlaylistsPlaylistItemsResult> GetPlaylistsPlaylistItemsAsync(IEnumerable<Playlist> playlists)
        {
            Tracer.Write($"YoutubePlaylistItemService.GetPlaylistsContent: Start.");
            GetPlaylistsPlaylistItemsResult result = new GetPlaylistsPlaylistItemsResult();

            if (playlists != null)
            {
                playlists = playlists.Distinct();
                foreach (Playlist playlist in playlists)
                {
                    List<string> playlistResult = new List<string>();

                    StatusInformation statusInformation = await YoutubePlaylistItemService.addPlaylistItemsToResultAsync(playlist, null, playlistResult).ConfigureAwait(false);
                    if (statusInformation != null)
                    {
                        result.StatusInformation = statusInformation;
                        return result;
                    }

                    if(!playlist.NotExistsOnYoutube)
                    {
                        result.PlaylistItemsByPlaylist.Add(playlist, playlistResult);
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


        //gets all item/video ids of the playlist and checks if playlist exists on youtube
        private static async Task<StatusInformation> addPlaylistItemsToResultAsync(Playlist playlist, string pageToken, List<string> result)
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
                            StatusInformation statusInformation = new StatusInformation($"YoutubePlaylistItemService.addPlaylistContentToResultAsync: Could not get playlist items: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                            if (statusInformation.IsQuotaError)
                            {
                                Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResultAsync: End, quota exceeded.");
                                return statusInformation;
                            }

                            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                            {
                                //http 404 = playlist does not exist anymore on youtube
                                playlist.NotExistsOnYoutube = true;
                                return null;

                            }

                            //unexpected http status
                            Tracer.Write($"YoutubePlaylistItemService.addPlaylistContentToResult: HttpResponseMessage unexpected status code: {(int)responseMessage.StatusCode} {responseMessage.ReasonPhrase} with content '{content}'.");
                            responseMessage.EnsureSuccessStatusCode();
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
                            return await YoutubePlaylistItemService.addPlaylistItemsToResultAsync(playlist, response.NextPageToken, result).ConfigureAwait(false);
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
            return null;
        }
    }
}
