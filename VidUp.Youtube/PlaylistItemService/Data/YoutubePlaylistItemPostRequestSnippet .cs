using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.PlaylistItemService.Data
{
	public class YoutubePlaylistItemPostRequestSnippet
	{
		[JsonProperty(PropertyName = "playlistId")]
		public string PlaylistId { get; set; }

		[JsonProperty(PropertyName = "resourceId")]
		public YoutubePlaylistItemPostRequestResourceId ResourceId { get; set; }
    }
}
