using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Video.Data
{
    public class YoutubeVideosGetResponse
    {
        [JsonProperty(PropertyName = "items")]
        public YoutubeVideosGetResponseVideo[] Items { get; set; }
    }
}