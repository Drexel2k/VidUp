using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.Video.Data
{
    public class YoutubeVideosGetResponseStatus
    {
        [JsonProperty(PropertyName = "privacyStatus")]
        public string PrivacyStatus { get; set; }
    }
}
