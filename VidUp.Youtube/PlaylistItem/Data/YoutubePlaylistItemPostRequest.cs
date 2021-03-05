using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItem.Data
{
	public class YoutubePlaylistItemPostRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubePlaylistItemPostRequestSnippet Snippet { get; set; }
    }
}
