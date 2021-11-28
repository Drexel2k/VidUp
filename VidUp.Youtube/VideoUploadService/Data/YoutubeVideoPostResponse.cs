using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUploadService.Data
{
	public class YoutubeVideoPostResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
