using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json
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
