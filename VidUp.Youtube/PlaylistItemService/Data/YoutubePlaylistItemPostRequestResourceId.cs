using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
    public class YoutubePlaylistItemPostRequestResourceId
    {
        [JsonProperty(PropertyName = "kind")]
        public string Kind { get => "youtube#video"; }

        [JsonProperty(PropertyName = "videoId")]
        public string VideoId { get; set; }
    }
}
