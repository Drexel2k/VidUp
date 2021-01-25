using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItem.Data
{
    public class YoutubePlaylistItemsGetResponsePlaylistItem
    {
        [JsonProperty(PropertyName = "snippet")]
        public YoutubePlaylistItemsGetResponseSnippet Snippet { get; set; }
    }
}