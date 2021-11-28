using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
	public class YoutubePlaylistItemPostRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubePlaylistItemPostRequestSnippet Snippet { get; set; }
    }
}
