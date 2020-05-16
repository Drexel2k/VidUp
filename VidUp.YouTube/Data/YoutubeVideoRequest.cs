#region

using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubeVideoRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubeSnippet Snippet { get; set; }

		[JsonProperty(PropertyName = "status")]
		public YoutubeStatus Status { get; set; }
	}
}
