using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistsGetResponsePlaylist
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }

        [JsonProperty(PropertyName = "snippet")]
        public YoutubePlaylistsGetResponseSnippet Snippet { get; set; }
    }
}