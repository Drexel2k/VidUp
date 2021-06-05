using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Video.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Video
{
    public class YoutubeVideoService
    {
        private static string videoEndpoint = "https://www.googleapis.com/youtube/v3/videos";

        public static async Task<Dictionary<string, bool>> IsPublicAsync(List<string> videoIds)
        {
            Tracer.Write($"YoutubeVideoService.IsPublic: Start.");
            Dictionary<string, bool> result = new Dictionary<string, bool>();

            if (videoIds != null && videoIds.Count > 0)
            {
                Tracer.Write($"YoutubeVideoService.IsPublic: {videoIds.Count} Videos to check available.");

                HttpClient client = await HttpHelper.GetAuthenticatedStandardClientAsync().ConfigureAwait(false);
                HttpResponseMessage message;

                int batch = 0;

                Tracer.Write($"YoutubeVideoService.IsPublic: Get video batch {batch}.");
                List<string> videoIdsBatch = YoutubeVideoService.getBatch(videoIds, batch, 50);
                while (videoIdsBatch.Count > 0)
                {
                    try
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: Get video information.");
                        message = await client.GetAsync($"{YoutubeVideoService.videoEndpoint}?part=status&id={string.Join(",", videoIdsBatch)}").ConfigureAwait(false);
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                        throw;
                    }

                    using (message)
                    using (message.Content)
                    {
                        string content = await message.Content.ReadAsStringAsync().ConfigureAwait(false);
                        if (!message.IsSuccessStatusCode)
                        {
                            Tracer.Write($"YoutubeVideoService.IsPublic: End, HttpResponseMessage unexpected status code: {message.StatusCode} {message.ReasonPhrase} with content {content}.");
                            throw new HttpRequestException($"Http error status code: {message.StatusCode}, reason {message.ReasonPhrase}, content {content}.");
                        }

                        YoutubeVideosGetResponse response = JsonConvert.DeserializeObject<YoutubeVideosGetResponse>(content);

                        foreach (YoutubeVideosGetResponseVideo item in response.Items)
                        {
                            result.Add(item.Id, item.Status.PrivacyStatus == "public");
                        }
                    }

                    batch++;
                    videoIdsBatch = YoutubeVideoService.getBatch(videoIds, batch, 50);
                }

                Tracer.Write($"YoutubeVideoService.IsPublic: End.");
                return result;
            }

            Tracer.Write($"YoutubeVideoService.IsPublic: End, no video to check for public state.");
            return result;
        }

        private static List<string> getBatch(List<string> videoIds, int batch, int batchSize)
        {
            Tracer.Write($"YoutubeVideoService.getBatch: Start.");
            List<string> result = new List<string>();

            int startIndex = batch * batchSize;
            Tracer.Write($"YoutubeVideoService.getBatch: startIndex {startIndex}.");

            if (startIndex > videoIds.Count - 1)
            {
                Tracer.Write($"YoutubeVideoService.getBatch: End, return empty batch, start index too high.");
                return result;
            }

            int stopIndex = startIndex + batchSize - 1;
            Tracer.Write($"YoutubeVideoService.getBatch: stopIndex {stopIndex}.");

            if (videoIds.Count < stopIndex + 1)
            {
                stopIndex = videoIds.Count - 1;
                Tracer.Write($"YoutubeVideoService.getBatch: corrected stopIndex due to last batch no full batch {stopIndex}.");
            }

            for (int index = startIndex; index <= stopIndex; index++)
            {
                result.Add(videoIds[index]);
            }

            Tracer.Write($"YoutubeVideoService.getBatch: End.");
            return result;
        }
    }
}