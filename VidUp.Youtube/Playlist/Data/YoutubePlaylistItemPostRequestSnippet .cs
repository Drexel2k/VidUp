using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Playlist.Data
{
	public class YoutubePlaylistItemPostRequestSnippet
	{
		[JsonProperty(PropertyName = "playlistId")]
		public string PlaylistId { get; set; }

		[JsonProperty(PropertyName = "resourceId")]
		public YoutubePlaylistItemsGetResponseResourceId ResourceId { get; set; }
    }
}
