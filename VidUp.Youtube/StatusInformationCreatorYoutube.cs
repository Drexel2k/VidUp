using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;

namespace Drexel.VidUp.Youtube
{
    public static class StatusInformationCreatorYoutube
    {
        public static StatusInformation Create(string message, AuthenticationException e)
        {
            StatusInformationType statusInformationType = StatusInformationType.AuthenticationError;

            string messageAdditional = "Authentication error";
            if (e.IsApiResponseError)
            {
                statusInformationType |= StatusInformationType.AuthenticationApiResponseError;
                messageAdditional += "Server denied authentication";
                HttpStatusException httpStatusException = (HttpStatusException) e.InnerException;
                return new StatusInformation($"{message} {messageAdditional}: {httpStatusException.StatusCode} {httpStatusException.Message} with content '{httpStatusException.Content}'.", statusInformationType);
            }

            return new StatusInformation($"{message} {messageAdditional}: {e.InnerException.GetType().Name}: {e.InnerException.Message}.", statusInformationType);
        }

        public static StatusInformation Create(string source, string message, HttpStatusException e)
        {
            StatusInformationType statusInformationType;
            if (e.Content.Contains("quotaExceeded"))
            {
                statusInformationType = StatusInformationType.QuotaError;
            }
            else
            {
                statusInformationType = StatusInformationType.Other;
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Server denied request";
            }

            string sourceString = string.Empty;
            if (source != null)
            {
                sourceString = $"{source}: ";
            }

            return new StatusInformation($"{sourceString}{message} {e.StatusCode} {e.Message} with content '{e.Content}'.", statusInformationType);
        }

        public static StatusInformation Create(string message, HttpStatusException e)
        {
            return StatusInformationCreatorYoutube.Create(null, message, e);
        }
    }
}