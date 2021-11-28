using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistService.Data
{
    public class YoutubePlaylistsGetResponseSnippet
    {
        [JsonProperty(PropertyName = "title")]
        public string Title { get; set; }
    }
}