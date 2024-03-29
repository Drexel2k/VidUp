﻿using Newtonsoft.Json;

namespace Drexel.VidUp.Youtube.VideoUploadService.Data
{
	public class YoutubeVideoPostRequest
	{
		[JsonProperty(PropertyName = "snippet")]
		public YoutubeVideoPostRequestSnippet Snippet { get; set; }

		[JsonProperty(PropertyName = "status")]
		public YoutubeVideoPostRequestStatus Status { get; set; }
	}
}
