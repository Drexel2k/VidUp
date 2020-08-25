using System;

namespace Drexel.VidUp.Business
{
    public class InvalidScheduleException : Exception
    {
        public InvalidScheduleException(string message) : base(message)
        {

        }
    }
}
