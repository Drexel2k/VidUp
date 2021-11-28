using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUploadService.Data
{
	public class YoutubeVideoPostRequestStatus
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
