using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistItemsGetResponseResourceId
    {
        [JsonProperty(PropertyName = "videoId")]
        public string VideoId { get; set; }
    }
}