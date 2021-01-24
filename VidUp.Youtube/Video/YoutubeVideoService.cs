using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Video.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Video
{
    public class YoutubeVideoService
    {
        private static string videoEndpoint = "https://www.googleapis.com/youtube/v3/videos";
        public static async Task<Dictionary<string, bool>> IsPublic(List<string> videoIds)
        {
            Tracer.Write($"YoutubeVideoService.IsPublic: Start.");
            Dictionary<string, bool> result = new Dictionary<string, bool>();

            if (videoIds != null && videoIds.Count > 0)
            {
                Tracer.Write($"YoutubeVideoService.IsPublic: Videos to check available.");

                HttpClient client = await HttpHelper.GetAuthenticatedStandardClient();
                HttpResponseMessage message;

                try
                {
                    Tracer.Write($"YoutubeVideoService.IsPublic: Get video information.");
                    message = await client.GetAsync($"{YoutubeVideoService.videoEndpoint}?part=status&id={string.Join(",", videoIds)}");
                }
                catch (Exception e)
                {
                    Tracer.Write($"YoutubeVideoService.IsPublic: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                    throw;
                }

                using (message)
                {
                    if (!message.IsSuccessStatusCode)
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: End, HttpResponseMessage unexpected status code: {message.StatusCode} with message {message.ReasonPhrase}.");
                        throw new HttpRequestException($"Http error status code: {message.StatusCode}, message {message.ReasonPhrase}.");
                    }

                    YoutubeVideosGetResponse response = JsonConvert.DeserializeObject<YoutubeVideosGetResponse>(await message.Content.ReadAsStringAsync());

                    foreach (YoutubeVideosGetResponseVideo item in response.Items)
                    {
                        result.Add(item.Id, item.Status.PrivacyStatus == "public");
                    }
                }

            }

            Tracer.Write($"YoutubeVideoService.IsPublic: End.");
            return result;
        }
    }
}