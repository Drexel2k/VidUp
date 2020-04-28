using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.Youtube
{
	public class YoutubeVideoResponse
	{
		[JsonProperty(PropertyName = "id")]
		public string Id { get; set; }
	}
}
