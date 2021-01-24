using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistItemsGetResponsePlaylistItem
    {
        [JsonProperty(PropertyName = "snippet")]
        public YoutubePlaylistItemsGetResponseSnippet Snippet { get; set; }
    }
}