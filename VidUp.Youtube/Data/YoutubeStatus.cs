#region

using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubeStatus
	{
		[JsonProperty(PropertyName = "publishAt")]
		public string PublishAt { get; set; }

		[JsonProperty(PropertyName = "privacyStatus")]
		public string Privacy { get; set; }

		public bool ShouldSerializePublishAt()
		{
			return !string.IsNullOrWhiteSpace(PublishAt);
		}
	}
}
