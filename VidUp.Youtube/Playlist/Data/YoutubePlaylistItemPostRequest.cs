using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
	public class YoutubePlaylistItemPostRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubePlaylistItemPostRequestSnippet Snippet { get; set; }
    }
}
