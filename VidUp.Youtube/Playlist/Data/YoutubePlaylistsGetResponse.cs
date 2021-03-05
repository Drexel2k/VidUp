using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistsGetResponse
    {
        [JsonProperty(PropertyName = "nextPageToken")]
        public string NextPageToken { get; set; }


        [JsonProperty(PropertyName = "prevPageToken")]
        public string PrevPageToken { get; set; }


        [JsonProperty(PropertyName = "items")]
        public YoutubePlaylistsGetResponsePlaylist[] Items { get; set; }
    }
}