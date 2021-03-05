using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
    public class YoutubePlaylistsGetResponseSnippet
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
    }
}