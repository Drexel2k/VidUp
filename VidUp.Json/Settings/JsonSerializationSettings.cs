using System.IO;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;

namespace Drexel.VidUp.Json.Settings
{
    public class JsonSerializationSettings
    {
        private string settingsFilePath;

        private UserSettings userSettings;

        public static JsonSerializationSettings JsonSerializer;

        public JsonSerializationSettings(string serializationFolder, UserSettings userSettings)
        {
            this.settingsFilePath = Path.Combine(serializationFolder, "settings.json");
            this.userSettings = userSettings;
        }
        public void SerializeSettings()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(this.settingsFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.userSettings);
            }
        }
    }
}
