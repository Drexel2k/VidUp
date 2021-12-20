using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;

namespace Drexel.VidUp.Youtube
{
    public static class StatusInformationCreator
    {
        public static StatusInformation Create(string message)
        {
            return new StatusInformation(message, StatusInformationType.Other);
        }

        public static StatusInformation Create(string source, string message)
        {
            return new StatusInformation($"{source}: {message}", StatusInformationType.Other);
        }

        public static StatusInformation Create(string source, AuthenticationException e)
        {
            StatusInformationType statusInformationType = StatusInformationType.AuthenticationError;

            string message = "Authentication error";
            if (e.IsApiResponseError)
            {
                statusInformationType |= StatusInformationType.AuthenticationApiResponseError;
                message += ", server denied authentication";
                HttpStatusException httpStatusException = (HttpStatusException) e.InnerException;
                return new StatusInformation($"{source}: {message}: {httpStatusException.StatusCode} {httpStatusException.Message} with content '{httpStatusException.Content}'.", statusInformationType);
            }

            return new StatusInformation($"{source}: {message}: {e.InnerException.GetType().Name}: {e.InnerException.Message}.", statusInformationType);
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

            return new StatusInformation($"{source}: {message}: {e.StatusCode} {e.Message} with content '{e.Content}'.", statusInformationType);
        }

        public static StatusInformation Create(string source, HttpStatusException e)
        {
            return StatusInformationCreator.Create(source, null, e);
        }

        public static StatusInformation Create(string source, string message, Exception e)
        {
            StatusInformationType statusInformationType = StatusInformationType.Other;

            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Something went wrong";
            }

            return new StatusInformation($"{source}: {message}: {e.GetType().Name}: {e.Message}.", statusInformationType);
        }

        public static StatusInformation Create(string source, Exception e)
        {
            return StatusInformationCreator.Create(source, null, e);
        }
    }
}