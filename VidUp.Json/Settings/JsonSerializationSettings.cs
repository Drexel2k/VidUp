using System.IO;
using System.Threading;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Drexel.VidUp.Json.Settings
{
    public class JsonSerializationSettings
    {
        private string settingsFilePath;

        private UserSettings userSettings;

        //serialiazer can also be called on a timer thread from autosetting playlists (saves last autoset playlsit time).
        private  object settingsLock = new object();

        private bool serializationEnabled = true;
        private object serializationEnabledLock = new object();
        private CountdownEvent serializationProcessesCount = new CountdownEvent(0);

        public static JsonSerializationSettings JsonSerializer;

        public JsonSerializationSettings(string serializationFolder, UserSettings userSettings)
        {
            this.settingsFilePath = Path.Combine(serializationFolder, "settings.json");
            this.userSettings = userSettings;
        }

        public void SerializeSettings()
        {
            Tracer.Write($"JsonSerializationSettings.SerializeSettings: Start.", TraceLevel.Detailed);
            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationSettings.SerializeSettings: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Formatting = Formatting.Indented;

            lock (this.settingsLock)
            {
                using (StreamWriter sw = new StreamWriter(this.settingsFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this.userSettings);
                }
            }

            Tracer.Write($"JsonSerializationSettings.SerializeSettings: End.", TraceLevel.Detailed);
            this.serializationProcessesCount.Signal();
        }

        public CountdownEvent StopSerialization()
        {
            lock (this.serializationEnabledLock)
            {
                this.serializationEnabled = false;
            }

            return this.serializationProcessesCount;
        }
    }
}
