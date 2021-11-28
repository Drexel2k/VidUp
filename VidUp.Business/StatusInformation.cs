using System;
using System.Runtime.Serialization;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class StatusInformation
    {
        [JsonProperty]
        private string message;
        private bool isQuotaError;

        public string Message
        {
            get => this.message;
        }

        public bool IsQuotaError
        {
            get => this.isQuotaError;
        }

        [JsonConstructor]
        public StatusInformation()
        {

        }

        public StatusInformation(string message)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message must not be null.");
            }

            this.message = message;

            this.setQuotaError();
        }

        private void setQuotaError()
        {
            if (this.message.Contains("quotaExceeded"))
            {
                this.isQuotaError = true;
            }
        }

        [OnDeserialized]
        private void afterDeserialization(StreamingContext context)
        {
            this.setQuotaError();
        }
    }
}