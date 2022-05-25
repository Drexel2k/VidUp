using System;


namespace Drexel.VidUp.Business
{
    public static class StatusInformationCreator
    {
        public static StatusInformation Create(string code, string message)
        {
            return new StatusInformation(code, message, StatusInformationType.Other);
        }

        public static StatusInformation Create(string code, string message, StatusInformationType statusInformationType)
        {
            return new StatusInformation(code, message, statusInformationType);
        }

        public static StatusInformation Create(string code, string source, string message)
        {
            return new StatusInformation(code, $"{source}: {message}", StatusInformationType.Other);
        }

        public static StatusInformation Create(string code, string source, string message, Exception e)
        {
            if (string.IsNullOrWhiteSpace(message))
            {
                message = "Something went wrong";
            }

            string sourceString = string.Empty;
            if (source != null)
            {
                sourceString = $"{source}: ";
            }

            return new StatusInformation(code, $"{sourceString}{message}: {e.GetType().Name}: {e.Message}", StatusInformationType.Other);
        }

        public static StatusInformation Create(string code, string message, Exception e)
        {
            return StatusInformationCreator.Create(code, null, message, e);
        }
    }
}
