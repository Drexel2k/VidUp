using System.IO;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

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
            Tracer.Write($"JsonSerializationSettings.SerializeSettings: Start.", TraceLevel.Detailed);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(this.settingsFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.userSettings);
            }

            Tracer.Write($"JsonSerializationSettings.SerializeSettings: End.", TraceLevel.Detailed);
        }
    }
}
