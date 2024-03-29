﻿using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
using Drexel.VidUp.Youtube.VideoService.Data;
using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoService
{
    public class YoutubeVideoService
    {
        private static string videoEndpoint = "https://www.googleapis.com/youtube/v3/videos";

        public static async Task<IsPublicResult> IsPublicAsync(YoutubeAccount youtubeAccount, List<string> videoIds)
        {
            Tracer.Write($"YoutubeVideoService.IsPublic: Start.");
            IsPublicResult result = new IsPublicResult();

            if (videoIds != null && videoIds.Count > 0)
            {
                Tracer.Write($"YoutubeVideoService.IsPublic: {videoIds.Count} Videos to check available.");

                int batch = 0;

                Tracer.Write($"YoutubeVideoService.IsPublic: Get video batch {batch}.");
                List<string> videoIdsBatch = YoutubeVideoService.getBatch(videoIds, batch, 50);
                while (videoIdsBatch.Count > 0)
                {
                    try
                    {
                        using (HttpRequestMessage requestMessage = await HttpHelper.GetAuthenticatedRequestMessageAsync(
                            youtubeAccount, HttpMethod.Get, $"{YoutubeVideoService.videoEndpoint}?part=status&id={string.Join(",", videoIdsBatch)}").ConfigureAwait(false))
                        {
                            Tracer.Write($"YoutubeVideoService.IsPublic: Get video information.");

                            using (HttpResponseMessage responseMessage = await HttpHelper.StandardClient.SendAsync(requestMessage).ConfigureAwait(false))
                            using (responseMessage.Content)
                            {
                                string content = await responseMessage.Content.ReadAsStringAsync().ConfigureAwait(false);
                                if (!responseMessage.IsSuccessStatusCode)
                                {
                                    throw new HttpStatusException(responseMessage.ReasonPhrase, (int)responseMessage.StatusCode, content);
                                }

                                YoutubeVideosGetResponse response = JsonConvert.DeserializeObject<YoutubeVideosGetResponse>(content);

                                foreach (YoutubeVideosGetResponseVideo item in response.Items)
                                {
                                    result.IsPublicByVideoId.Add(item.Id, item.Status.PrivacyStatus == "public");
                                }
                            }

                            batch++;
                            videoIdsBatch = YoutubeVideoService.getBatch(videoIds, batch, 50);
                        }
                    }
                    catch (AuthenticationException e)
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: Authentication exception: {e.ToString()}.");
                        StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0018", "Could not check if videos are public.", e);
                        result.StatusInformation = statusInformation;
                        Tracer.Write($"YoutubeVideoService.IsPublic: End, authentication error.");
                        return result;
                    }
                    catch (HttpStatusException e)
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: HttpResponseMessage unexpected status code: {e.StatusCode} {e.Message} with content '{e.Content}'.");
                        StatusInformation statusInformation = StatusInformationCreatorYoutube.Create("ERR0019", "Could not check if videos are public.", e);
                        if (statusInformation.IsQuotaError)
                        {
                            result.StatusInformation = statusInformation;
                            Tracer.Write($"YoutubeVideoService.IsPublic: End, quota exceeded.");
                            return result;
                        }

                        throw;
                    }
                    catch (Exception e)
                    {
                        Tracer.Write($"YoutubeVideoService.IsPublic: End, HttpClient.GetAsync Exception: {e.ToString()}.");
                        throw;
                    }

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