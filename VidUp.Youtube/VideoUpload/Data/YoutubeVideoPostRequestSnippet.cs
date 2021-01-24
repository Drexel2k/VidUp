using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUpload.Data
{
	public class YoutubeVideoPostRequestSnippet
	{
		[JsonProperty(PropertyName = "title")]
		public string Title { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "tags")]
		public string[] Tags { get; set; }

        [JsonProperty(PropertyName = "defaultAudioLanguage")]
        public string VideoLanguage { get; set; }

        [JsonProperty(PropertyName = "categoryId")]
        public int? Category { get; set; }
	}
}
