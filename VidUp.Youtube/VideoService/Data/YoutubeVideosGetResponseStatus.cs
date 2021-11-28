using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoService.Data
{
    public class YoutubeVideosGetResponseStatus
    {
        [JsonProperty(PropertyName = "privacyStatus")]
        public string PrivacyStatus { get; set; }
    }
}
