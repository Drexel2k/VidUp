using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
    public class YoutubePlaylistItemsGetResponse
    {
        [JsonProperty(PropertyName = "nextPageToken")]
        public string NextPageToken { get; set; }

        [JsonProperty(PropertyName = "prevPageToken")]
        public string PrevPageToken { get; set; }

        [JsonProperty(PropertyName = "items")]
        public YoutubePlaylistItemsGetResponsePlaylistItem[] Items { get; set; }
    }
}