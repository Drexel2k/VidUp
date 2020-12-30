using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Json.Settings
{
    public class DeserializationRepositorySettings
    {
        public static UserSettings UserSettings { get; set; }

        public static void ClearRepositories()
        {
            DeserializationRepositorySettings.UserSettings = null;
        }
    }
}
