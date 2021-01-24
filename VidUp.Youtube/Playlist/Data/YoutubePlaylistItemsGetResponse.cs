using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistItemsGetResponse
    {
        [JsonProperty(PropertyName = "items")]
        public YoutubePlaylistItemsGetResponsePlaylistItem[] Items { get; set; }
    }
}