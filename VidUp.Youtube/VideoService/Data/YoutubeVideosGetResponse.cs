using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoService.Data
{
    public class YoutubeVideosGetResponse
    {
        [JsonProperty(PropertyName = "items")]
        public YoutubeVideosGetResponseVideo[] Items { get; set; }
    }
}