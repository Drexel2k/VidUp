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
        private bool isAuthenticationError;

        public string Message
        {
            get => this.message;
        }

        public bool IsQuotaError
        {
            get => this.isQuotaError;
        }

        public bool IsAuthenticationError
        {
            get => this.isAuthenticationError;
        }

        [JsonConstructor]
        private StatusInformation()
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
            this.setAuthenticationError();
        }

        private void setQuotaError()
        {
            if (this.message.Contains("quotaExceeded"))
            {
                this.isQuotaError = true;
            }
        }

        private void setAuthenticationError()
        {
            if (this.message.Contains("Authentication"))
            {
                this.isAuthenticationError = true;
            }
        }

        [OnDeserialized]
        private void afterDeserialization(StreamingContext context)
        {
            this.setQuotaError();
            this.setAuthenticationError();
        }
    }
}