using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
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
                                    throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                                }

                                Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End.");
                                return result;
                            }
                        }
                    }
                    catch (AuthenticationException e)
                    {
                        StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0010", "Could not add video to playlist.", e);
                        result.StatusInformation = statusInformation;
                        result.Success = false;
                        upload.AddStatusInformation(statusInformation);
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, authentication error.");
                        return result;
                    }
                    catch(HttpStatusException e)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");

                        StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0011", "Could not add video to playlist.", e);
                        if (statusInformation.IsQuotaError)
                        {
                            result.StatusInformation = statusInformation;
                            result.Success = false;
                            upload.AddStatusInformation(statusInformation);
                            Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, quota exceeded.");
                            return result;
                        }

                        upload.AddStatusInformation(StatusInformationCreatorYoutube.Create("ERR0012", "Could not add video to playlist.", e));
                        result.Success = false;
                        return result;
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubePlaylistItemService.AddToPlaylist: End, HttpClient.SendAsync Exception: {e.ToString()}.");
                        upload.AddStatusInformation(StatusInformationCreator.Create("ERR0001", "Could not add video to playlist.", e));
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
            Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: Start.");
            try
            {
                using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(playlist.YoutubeAccount, HttpMethod.Get).ConfigureAwait(false))
                {
                    Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: Get Playlist info.");
                    requestMessage.RequestUri = string.IsNullOrWhiteSpace(pageToken) ? 
                        new Uri($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlist.PlaylistId}&maxResults={YoutubePlaylistItemService.maxResults}") :
                        new Uri($"{YoutubePlaylistItemService.playlistItemsEndpoint}?part=snippet&playlistId={playlist.PlaylistId}&maxResults={YoutubePlaylistItemService.maxResults}&pageToken={pageToken}");

                    using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                    using (responseMessage.Content)
                    {
                        string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!responseMessage.IsSuccessStatusCode)
                        {
                            if (responseMessage.StatusCode == HttpStatusCode.NotFound)
                            {
                                //http 404 = playlist does not exist anymore on youtube
                                playlist.NotExistsOnYoutube = true;
                                return null;
                            }

                            throw new HttpStatusException(responseMessage.ReasonPhrase, (int) responseMessage.StatusCode, content);
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
                            StatusInformation statusInformation = await YoutubePlaylistItemService.addPlaylistItemsToResultAsync(playlist, response.NextPageToken, result).ConfigureAwait(false);

                            if (statusInformation != null)
                            {
                                if (statusInformation.IsQuotaError)
                                {
                                    Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: End, quota exceeded.");
                                }

                                if (statusInformation.IsAuthenticationError)
                                {
                                    Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: End, authentication error.");
                                }

                                return statusInformation;
                            }
                        }
                    }
                }
            }
            catch (AuthenticationException e)
            {
                Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: End, authentication error.");
                StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0013", "Could not receive playlist videos.", e);

                return statusInformation;
            }
            catch (HttpStatusException e)
            {
                Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0014", "Could not receive playlist videos.", e);
                if (statusInformation.IsQuotaError)
                {
                    Tracer.Write($"YoutubePlaylistItemService.addPlaylistItemsToResultAsync: End, quota exceeded.");
                    return statusInformation;
                }

                //unexpected http status
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
