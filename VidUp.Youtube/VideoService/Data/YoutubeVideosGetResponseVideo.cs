using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoService.Data
{
    public class YoutubeVideosGetResponseVideo
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "status")]
        public YoutubeVideosGetResponseStatus Status { get; set; }
    }
}