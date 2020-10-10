using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Service
{
    public class YoutubeVideoService
    {
        private static string videoEndpoint = "https://www.googleapis.com/youtube/v3/videos";

        public static async Task<Dictionary<string, bool>> IsPublic(List<string> videoIds)
        {
            Dictionary<string, bool> result = new Dictionary<string, bool>();

            if (videoIds != null || videoIds.Count > 0)
            {
                HttpWebRequest request =
                    await HttpWebRequestCreator.CreateAuthenticatedHttpWebRequest(
                        $"{YoutubeVideoService.videoEndpoint}?part=status&id={string.Join(",", videoIds)}", "GET");

                using (HttpWebResponse httpResponse =
                    (HttpWebResponse)await request.GetResponseAsync())
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
                                    Id = "",
                                    Status = new
                                    {
                                        PrivacyStatus = ""
                                    }
                                }
                            }
                        };

                        var response =
                            JsonConvert.DeserializeAnonymousType(await reader.ReadToEndAsync(), definition);

                        foreach (var item in response.Items)
                        {
                            result.Add(item.Id, item.Status.PrivacyStatus == "public");
                        }
                    }
                }
            }

            return result;
        }
    }
}
