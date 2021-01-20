using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Data
{
    public class VideoResource
    {
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get => "youtube#video"; }

        [JsonProperty(PropertyName = "videoId")]
        public string VideoId { get; set; }
    }
}
