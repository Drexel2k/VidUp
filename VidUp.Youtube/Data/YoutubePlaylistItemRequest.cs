#region

using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubePlaylistItemRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubePlaylistItemSnippet PlaylistSnippet { get; set; }
    }
}
