using System;

namespace Drexel.VidUp.Youtube.Http
{
    //thrown if http request returns content to differ from exceptions like connection problems
    public class HttpStatusException : ApplicationException
    {
        public string Content { get; }

        public int StatusCode { get; }

        public HttpStatusException (string message, int statusCode, string content) : base(message)
        {
            this.StatusCode = statusCode;
            this.Content = content;
        }
    }
}