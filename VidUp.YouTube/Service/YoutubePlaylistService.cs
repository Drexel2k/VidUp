using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubePlaylistService
    {
        private static string playlistItemsEndpoint = "https://www.googleapis.com/youtube/v3/playlistItems";

        public static async Task<bool> AddToPlaylist(Upload upload)
        {
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
                    HttpWebRequest request =
                        await HttpWebRequestCreator.CreateAuthenticatedUploadHttpWebRequest(
                            $"{YoutubePlaylistService.playlistItemsEndpoint}?part=snippet", "POST", jsonBytes, "application/json; charset=utf-8");

                    using (Stream dataStream = await request.GetRequestStreamAsync())
                    {
                        dataStream.Write(jsonBytes, 0, jsonBytes.Length);
                    }

                    using (HttpWebResponse response = (HttpWebResponse)await request.GetResponseAsync())
                    {
                    }
                }
                catch (WebException e)
                {
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
                    upload.UploadErrorMessage = $"Playlist addition failed: {e.ToString()}";
                    return false;
                }
            }
            else
            {
                return false;
            }

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
                    List<string> videoIds;
                    if (!result.TryGetValue(playlistId, out videoIds))
                    {
                        videoIds = new List<string>();
                        result.Add(playlistId, videoIds);
                    }

                    HttpWebRequest request = await HttpWebRequestCreator.CreateAuthenticatedHttpWebRequest(
                        $"{YoutubePlaylistService.playlistItemsEndpoint}?part=snippet&playlistId={playlistId}", "GET");

                    using (HttpWebResponse httpResponse =
                        (HttpWebResponse) await request.GetResponseAsync())
                    {
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
            }

            return result;
        }
    }
}
