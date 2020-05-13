using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.Youtube.Data
{
	public class YoutubeSnippet
	{
		[JsonProperty(PropertyName = "title")]
		public string Title { get; set; }

		[JsonProperty(PropertyName = "description")]
		public string Description { get; set; }

		[JsonProperty(PropertyName = "tags")]
		public string[] Tags { get; set; }
	}
}
