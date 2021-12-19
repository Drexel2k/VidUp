using System;
using System.ComponentModel.Design.Serialization;

namespace Drexel.VidUp.Youtube
{
    //thrown if http request returns content to differ from exceptions like connection problems
    public class HttpStatusException : ApplicationException
    {
        public string Content { get; }

        public string ReasonPhrase { get; }

        public int StatusCode { get; }

        public HttpStatusException (int statusCode, string reasonPhrase, string content)
        {
            this.StatusCode = statusCode;
            this.ReasonPhrase = reasonPhrase;
            this.Content = content;
        }
    }
}