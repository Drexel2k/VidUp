using System;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class StatusInformation
    {
        [JsonProperty]
        private string message;
        [JsonProperty]
        private StatusInformationType statusInformationType;

        public string Message
        {
            get => this.message;
        }

        public bool IsQuotaError
        {
            get
            {
                if ((this.statusInformationType & StatusInformationType.QuotaError) == StatusInformationType.QuotaError)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsAuthenticationError
        {
            get
            {
                if ((this.statusInformationType & StatusInformationType.AuthenticationError) == StatusInformationType.AuthenticationError)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsAuthenticationApiResponseError
        {
            get
            {
                if ((this.statusInformationType & StatusInformationType.AuthenticationApiResponseError) == StatusInformationType.AuthenticationApiResponseError)
                {
                    return true;
                }

                return false;
            }
        }

        public bool IsOtherError
        {
            get
            {
                if ((this.statusInformationType & StatusInformationType.Other) == StatusInformationType.Other)
                {
                    return true;
                }

                return false;
            }
        }

        [JsonConstructor]
        private StatusInformation()
        {

        }

        public StatusInformation(string message, StatusInformationType statusInformationType)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                throw new ArgumentException("Message must not be null.");
            }

            this.message = message;

            this.statusInformationType = statusInformationType;
        }
    }
}