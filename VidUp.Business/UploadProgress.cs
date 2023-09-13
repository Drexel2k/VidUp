using Newtonsoft.Json;
using System;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UploadProgress
    {
        [JsonProperty]
        private Guid uploadGuid;
        [JsonProperty]
        private long bytesSent;

        public Guid UploadGuid
        {
            get => this.uploadGuid;
            set => this.uploadGuid = value;
        }

        public long BytesSent
        {
            get => this.bytesSent;
            set => this.bytesSent = value;
        }
    }
}
