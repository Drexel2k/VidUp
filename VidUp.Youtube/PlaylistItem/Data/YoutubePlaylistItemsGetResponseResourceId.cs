using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItem.Data
{
    public class YoutubePlaylistItemsGetResponseResourceId
    {
        [JsonProperty(PropertyName = "videoId")]
        public string VideoId { get; set; }
    }
}