using System;
using System.IO;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Drexel.VidUp.Json.Settings
{
    public class JsonSerializationUploadResultAutomationInfo
    {
        private string serializationFolder;
        public static JsonSerializationUploadResultAutomationInfo JsonSerializer;

        public JsonSerializationUploadResultAutomationInfo(string serializationFolder)
        {
            this.serializationFolder = serializationFolder;
        }
        public string SerializationFolder
        {
            get => this.serializationFolder;
        }

        public void SerializeUploadResultAutomationInfo(string fileName, UploadResultAutomationInfo uploadResultAutomationInfo)
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Formatting = Formatting.Indented;
            serializer.NullValueHandling = NullValueHandling.Ignore;

            using (StreamWriter sw = new StreamWriter(Path.Combine(serializationFolder, fileName)))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, uploadResultAutomationInfo);
            }
        }
    }
}

