using System.IO;
using System.Reflection;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Drexel.VidUp.Json.Settings
{
    public class JsonDeserializationSettings
    {
        private string settingsFilePath;

        public JsonDeserializationSettings(string serializationFolder)
        {
            this.settingsFilePath = Path.Combine(serializationFolder, "settings.json");
        }

        public string DeserializeUser()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
            JObject appSettings = JObject.Parse(File.ReadAllText(path));
            return appSettings["User"].Value<string>();
        }



        public void DeserializeSettings()
        {
            if (!File.Exists(this.settingsFilePath))
            {
                DeserializationRepositorySettings.UserSettings = new UserSettings();
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            using (StreamReader sr = new StreamReader(this.settingsFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepositorySettings.UserSettings = serializer.Deserialize<UserSettings>(reader);
            }
        }
    }
}
