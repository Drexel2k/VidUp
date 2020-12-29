using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Content
{
    public class UploadListGuids
    {
        [JsonProperty(PropertyName = "uploads")]
        private List<Guid> guids;


        public List<Guid> Guids
        {
            get => guids;
        }
    }
}
