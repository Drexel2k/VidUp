using System.IO;
using System.Reflection;
using Newtonsoft.Json.Linq;

namespace Drexel.VidUp.Json.Settings
{
    public class JsonDeserializationSettings
    {
        public string DeserializeUser()
        {
            string path = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "appsettings.json");
            JObject appSettings = JObject.Parse(File.ReadAllText(path));
            return appSettings["User"].Value<string>();
        }
    }
}
