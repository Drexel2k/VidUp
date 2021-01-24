using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUpload.Data
{
	public class YoutubeVideoPostResponse
    {
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; }
    }
}
