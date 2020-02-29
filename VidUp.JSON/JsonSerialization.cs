using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections.ObjectModel;
using System.Reflection;

namespace Drexel.VidUp.JSON
{
    public class JsonSerialization
    {
        private static string uploadListFilePath = string.Format("{0}\\VidUp\\uploadlist.json",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        private static string templateListFilePath = string.Format("{0}\\VidUp\\templatelist.json",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        private static string allUploadsFilePath = string.Format("{0}\\VidUp\\uploads.json",
            Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
        public static UploadList UploadList;
        public static TemplateList TemplateList;

        public static void DeserializeUploadList()
        {
            if (!File.Exists(JsonSerialization.uploadListFilePath))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UploadGuidStringConverter());

            using (StreamReader sr = new StreamReader(uploadListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeSerializationRepository.UploadList = serializer.Deserialize<UploadList>(reader);
            }
        }

        public static void Deserialize()
        {
            JsonSerialization.DeserializeAllUploads();
            JsonSerialization.DeserializeTemplateList();
            JsonSerialization.DeserializeUploadList();
        }

        private static void DeserializeAllUploads()
        {
            if (!File.Exists(JsonSerialization.allUploadsFilePath))
            {
                return;
            }
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new TemplateGuidStringConverter());

            using (StreamReader sr = new StreamReader(JsonSerialization.allUploadsFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeSerializationRepository.AllUploads = serializer.Deserialize<List<Upload>>(reader);
            }

        }

        public static void DeserializeTemplateList()
        {
            if(!File.Exists(JsonSerialization.templateListFilePath))
            {
                return;
            }

            List<Template> list;
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());

            using (StreamReader sr = new StreamReader(templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                list = serializer.Deserialize<List<Template>>(reader);
            }

            foreach (Template template in list)
            {
                foreach (Upload upload in template.Uploads)
                {
                    Type myType = typeof(Upload);
                    FieldInfo myFieldInfo = myType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                    myFieldInfo.SetValue(upload, template);
                }
            }

            DeSerializationRepository.Templates = list;
        }
        public static void SerializeAllUploads()
        {
            List<Upload> allUploads = new List<Upload>();
            foreach (Upload upload in JsonSerialization.UploadList)
            {
                allUploads.Add(upload);
            }

            foreach (Template template in JsonSerialization.TemplateList)
            {
                foreach(Upload upload in template.Uploads)
                {
                    if(!allUploads.Contains(upload))
                    {
                        allUploads.Add(upload);
                    }
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new TemplateGuidStringConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(JsonSerialization.allUploadsFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, allUploads);
            }
        }

        //on UploadList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public static void SerializeUploadList()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(JsonSerialization.uploadListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, JsonSerialization.UploadList);
            }
        }

        //on TemplateList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public static void SerializeTemplateList()
        {
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Formatting = Formatting.Indented;
            serializer.Converters.Add(new UploadGuidStringConverter());

            using (StreamWriter sw = new StreamWriter(templateListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, JsonSerialization.TemplateList.GetReadonlyTemplateList());
            }
        }
    }
}
