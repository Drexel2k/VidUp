using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.Youtube
{
	public class YoutubeVideoRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubeSnippet Snippet { get; set; }

		[JsonProperty(PropertyName = "status")]
		public YoutubeStatus Status { get; set; }
	}
}
