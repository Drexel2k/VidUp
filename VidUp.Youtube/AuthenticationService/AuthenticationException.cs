using System;

namespace Drexel.VidUp.Youtube.AuthenticationService
{
    public class AuthenticationException : ApplicationException
    {
        public bool IsApiResponseError { get; }

        public AuthenticationException(string message, bool isApiResponseError) : base(message)
        {
            this.IsApiResponseError = isApiResponseError;
        }

        public AuthenticationException(string message, Exception innerException, bool isApiResponseError) : base(message, innerException)
        {
            this.IsApiResponseError = isApiResponseError;
        }


    }
}