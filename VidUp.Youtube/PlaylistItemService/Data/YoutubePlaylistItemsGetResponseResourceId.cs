using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
    public class YoutubePlaylistItemsGetResponseResourceId
    {
        [JsonProperty(PropertyName = "videoId")]
        public string VideoId { get; set; }
    }
}