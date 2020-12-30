using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubePlaylistService
    {
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems";

        public static async Task<bool> AddToPlaylist(Upload upload)
        {
            Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Start.");

            if (!string.IsNullOrWhiteSpace(upload.VideoId) && upload.Playlist != null)
            {
                try
                {
                    YoutubePlaylistItemRequest playlistItem = new YoutubePlaylistItemRequest();

                    playlistItem.PlaylistSnippet = new YoutubePlaylistItemSnippet();
                    playlistItem.PlaylistSnippet.PlaylistId = upload.Playlist.PlaylistId;
                    playlistItem.PlaylistSnippet.ResourceId = new VideoResource();
                    playlistItem.PlaylistSnippet.ResourceId.VideoId = upload.VideoId;

                    string content = JsonConvert.SerializeObject(playlistItem);
                    var jsonBytes = Encoding.UTF8.GetBytes(content);

                    FileInfo info = new FileInfo(upload.FilePath);
                    //request upload session/uri

                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Creating playlist request.");
                    HttpWebRequest request =
                        await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            $"{YoutubePlaylistService.playlistItemsEndpoint}?part=snippet", "POST", jsonBytes, "application/json; charset=utf-8");

                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Getting request/data stream.");
                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        dataStream.Write(jsonBytes, 0, jsonBytes.Length);
                    }

                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Try getting response for playlist.");
                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                    }
                }
                catch (WebException e)
                {
                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Unexpected WebException: {e.ToString()}.");
                    if (e.Response != null)
                    {
                        using (e.Response)
                        using (StreamReader reader = new StreamReader(e.Response.GetResponseStream()))
                        {
                            upload.UploadErrorMessage += $"Playlist addition failed: {await reader.ReadToEndAsync()}, exception: {e.ToString()}";
                        }
                    }
                    else
                    {
                        upload.UploadErrorMessage += $"Playlist addition failed: {e.ToString()}";
                    }

                    return false;
                }
                catch (Exception e)
                {
                    Tracer.Write($"YoutubePlaylistService.AddToPlaylist: Unexpected Exception: {e.ToString()}.");
                    upload.UploadErrorMessage = $"Playlist addition failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End, nothing to add to playlist.");
                return false;
            }

            Tracer.Write($"YoutubePlaylistService.AddToPlaylist: End.");
            return true;
        }


        public static async Task<Dictionary<string, List<string>>> GetPlaylists(IEnumerable<string> playlistIds)
        {
            Dictionary<string, List<string>> result = new Dictionary<string, List<string>>();

            if (playlistIds != null)
            {
                string[] playlistIdsInternal = playlistIds as string[] ?? playlistIds.ToArray();
                foreach (string playlistId in playlistIdsInternal)
                {
                    try
                    {

                        HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedHttpWebRequest(
                            $"{YoutubePlaylistService.playlistItemsEndpoint}?part=snippet&playlistId={playlistId}", "GET");

                        using (HttpWebResponse httpResponse = (HttpWebResponse) await request.GetResponseAsync())
                        {
                            List<string> videoIds;
                            if (!result.TryGetValue(playlistId, out videoIds))
                            {
                                videoIds = new List<string>();
                                result.Add(playlistId, videoIds);
                            }

                            using (StreamReader reader =
                                new StreamReader(httpResponse.GetResponseStream()))
                            {
                                var definition = new
                                {
                                    Items = new[]
                                    {
                                        new
                                        {
                                            Snippet = new
                                            {
                                                ResourceId = new
                                                {
                                                    VideoId = ""
                                                }
                                            }
                                        }
                                    }
                                };

                                var response =
                                    JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(), definition);

                                if (response.Items.Length > 0)
                                {
                                    foreach (var item in response.Items)
                                    {
                                        videoIds.Add(item.Snippet.ResourceId.VideoId);
                                    }
                                }
                            }
                        }

                    }
                    catch (WebException e)
                    {
                        if (e.Response == null)
                        {
                            throw;
                        }

                        HttpWebResponse httpResponse = e.Response as HttpWebResponse;

                        if (httpResponse == null)
                        {
                            throw;
                        }

                        if ((int)httpResponse.StatusCode != 404)
                        {
                            throw;
                        }

                        //continue on http status 404 which means playlist does not exist on Youtube anymore, so it isn't added to result list.
                        httpResponse.Dispose();
                        e.Response.Dispose();
                    }
                }
            }

            return result;
        }
    }
}
