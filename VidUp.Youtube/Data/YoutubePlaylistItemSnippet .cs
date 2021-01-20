#region

using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubePlaylistItemSnippet
	{
		[JsonProperty(PropertyName = "playlistId")]
		public string PlaylistId { get; set; }

		[JsonProperty(PropertyName = "resourceId")]
		public VideoResource ResourceId { get; set; }
    }
}
