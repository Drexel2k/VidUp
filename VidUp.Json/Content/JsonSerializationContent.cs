using System.Collections.Generic;
using System.IO;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Drexel.VidUp.Json.Content
{
    public class JsonSerializationContent
    {
        private string playlistListFilePath;
        private string uploadListFilePath;
        private string templateListFilePath;
        private string allUploadsFilePath;

        private UploadList uploadList;
        private TemplateList templateList;
        private PlaylistList playlistList;

        private object allUploadsLock = new object();

        public static JsonSerializationContent JsonSerializer;

        public JsonSerializationContent(string serializationFolder, UploadList uploadList, TemplateList templateList, PlaylistList playlistList)
        {
            this.playlistListFilePath = Path.Combine(serializationFolder, "playlistlist.json");
            this.uploadListFilePath = Path.Combine(serializationFolder, "uploadlist.json");
            this.templateListFilePath = Path.Combine(serializationFolder, "templatelist.json");
            this.allUploadsFilePath = Path.Combine(serializationFolder, "uploads.json");

            this.uploadList = uploadList;
            this.templateList = templateList;
            this.playlistList = playlistList;
        }

        //on Upload [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public void SerializeAllUploads()
        {
            Tracer.Write($"JsonSerializationContent.SerializeAllUploads: Start.", TraceLevel.Detailed);
            List<Upload> allUploads = new List<Upload>();
            foreach (Upload upload in this.uploadList)
            {
                allUploads.Add(upload);
            }

            foreach (Template template in this.templateList)
            {
                foreach (Upload upload in template.Uploads)
                {
                    if (!allUploads.Contains(upload))
                    {
                        allUploads.Add(upload);
                    }
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new TemplateGuidStringConverter());
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            serializer.Formatting = Formatting.Indented;

            lock(this.allUploadsLock)
            {
                using (StreamWriter sw = new StreamWriter(this.allUploadsFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, allUploads);
                }
            }

            Tracer.Write($"JsonSerializationContent.SerializeAllUploads: End.", TraceLevel.Detailed);
        }

        //on UploadList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public void SerializeUploadList()
        {
            Tracer.Write($"JsonSerializationContent.SerializeUploadList: Start.", TraceLevel.Detailed);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(this.uploadListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.uploadList);
            }

            Tracer.Write($"JsonSerializationContent.SerializeUploadList: End.", TraceLevel.Detailed);
        }

        //on TemplateList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public void SerializeTemplateList()
        {
            Tracer.Write($"JsonSerializationContent.SerializeTemplateList: Start.", TraceLevel.Detailed);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(templateListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.templateList);
            }

            Tracer.Write($"JsonSerializationContent.SerializeTemplateList: End.", TraceLevel.Detailed);
        }

        public void SerializePlaylistList()
        {
            Tracer.Write($"JsonSerializationContent.SerializePlaylistList: Start.", TraceLevel.Detailed);
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(this.playlistListFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, this.playlistList);
            }

            Tracer.Write($"JsonSerializationContent.SerializePlaylistList: End.", TraceLevel.Detailed);
        }

        public void SerializeYoutubeAccount(YoutubeAccount youtubeAccount)
        {
            Tracer.Write($"JsonSerializationContent.SerializeYoutubeAccount: Start.", TraceLevel.Detailed);
            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(youtubeAccount.FilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, youtubeAccount);
            }

            Tracer.Write($"JsonSerializationContent.SerializeYoutubeAccount: End.", TraceLevel.Detailed);
        }
    }
}
