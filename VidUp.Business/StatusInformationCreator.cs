using System;


namespace Drexel.VidUp.Business
{
    public static class StatusInformationCreator
    {
        public static StatusInformation Create(string message)
        {
            return new StatusInformation(message, StatusInformationType.Other);
        }

        public static StatusInformation Create(string message, StatusInformationType statusInformationType)
        {
            return new StatusInformation(message, statusInformationType);
        }

        public static StatusInformation Create(string source, string message)
        {
            return new StatusInformation($"{source}: {message}", StatusInformationType.Other);
        }

        public static StatusInformation Create(string source, string message, Exception e)
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

            return new StatusInformation($"{sourceString}{message}: {e.GetType().Name}: {e.Message}", StatusInformationType.Other);
        }

        public static StatusInformation Create(string message, Exception e)
        {
            return StatusInformationCreator.Create(null, message, e);
        }
    }
}
