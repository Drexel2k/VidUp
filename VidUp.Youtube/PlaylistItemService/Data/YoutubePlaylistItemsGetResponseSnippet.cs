using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
    public class YoutubePlaylistItemsGetResponseSnippet
    {
        [JsonProperty(PropertyName = "resourceId")]
        public YoutubePlaylistItemsGetResponseResourceId ResourceId { get; set; }
    }
}