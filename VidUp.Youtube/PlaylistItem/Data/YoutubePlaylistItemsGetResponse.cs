using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItem.Data
{
    public class YoutubePlaylistItemsGetResponse
    {
        [JsonProperty(PropertyName = "items")]
        public YoutubePlaylistItemsGetResponsePlaylistItem[] Items { get; set; }
    }
}