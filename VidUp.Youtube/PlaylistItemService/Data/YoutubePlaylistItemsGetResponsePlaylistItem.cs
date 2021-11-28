using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
    public class YoutubePlaylistItemsGetResponsePlaylistItem
    {
        [JsonProperty(PropertyName = "snippet")]
        public YoutubePlaylistItemsGetResponseSnippet Snippet { get; set; }
    }
}