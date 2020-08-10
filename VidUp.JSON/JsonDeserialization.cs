using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Drexel.VidUp.Json
{
    public class JsonDeserialization
    {
        private string playlistListFilePath;
        private string uploadListFilePath;
        private string templateListFilePath;
        private string allUploadsFilePath;

        private string templateImageFolder;
        private string thumbnailFallbackImageFolder;

        public static List<Upload> AllUploads { get; set; }
        private List<Template> templates { get; set; }
        private List<Guid> uploadListGuids { get; set; }

        public JsonDeserialization(string serializationFolder, string templateImageFolder, string thumbnailFallbackImageFolder)
        {
            this.playlistListFilePath = Path.Combine(serializationFolder, "playlistlist.json");
            this.uploadListFilePath = Path.Combine(serializationFolder, "uploadlist.json");
            this.templateListFilePath = Path.Combine(serializationFolder, "templatelist.json");
            this.allUploadsFilePath = Path.Combine(serializationFolder, "uploads.json");

            this.templateImageFolder = templateImageFolder;
            this.thumbnailFallbackImageFolder = thumbnailFallbackImageFolder;
        }

        public void Deserialize()
        {
            this.deserializePlaylistList();
            this.deserializeAllUploads();
            this.deserializeTemplateList();
            this.deserializeUploadListGuids();
            this.createTemplateListToRepository();
            this.createUploadListToRepository();
        }

        private void deserializePlaylistList()
        {
            if (!File.Exists(this.playlistListFilePath))
            {
                DeserializationRepository.PlaylistList = new PlaylistList(null);
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            using (StreamReader sr = new StreamReader(this.playlistListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepository.PlaylistList = serializer.Deserialize<PlaylistList>(reader);
            }
        }

        private void deserializeAllUploads()
        {
            if (!File.Exists(this.allUploadsFilePath))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            //This converter nulls the template. Templates are added vie reflection after deserialization of templates
            serializer.Converters.Add(new TemplateGuidStringConverter());
            //This converters returns existing Playlist objects from deserialization repository.
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());

            using (StreamReader sr = new StreamReader(this.allUploadsFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonDeserialization.AllUploads = serializer.Deserialize<List<Upload>>(reader);
            }
        }

        private void deserializeTemplateList()
        {
            if (!File.Exists(this.templateListFilePath))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());

            TemplateList templateList;
            using (StreamReader sr = new StreamReader(templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                templateList = serializer.Deserialize<TemplateList>(reader);
            }

            if (templateList == null || templateList.TemplateCount <= 0)
            {
                return;
            }

            this.templates = new List<Template>(templateList.TemplateCount);

            foreach (Template template in templateList)
            {
                this.templates.Add(template);
                foreach (Upload upload in template.Uploads)
                {
                    Type myType = typeof(Upload);
                    FieldInfo myFieldInfo = myType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                    myFieldInfo.SetValue(upload, template);
                }
            }
        }

        private void deserializeUploadListGuids()
        {
            if (!File.Exists(this.uploadListFilePath))
            {
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            UploadListGuids guids;
            using (StreamReader sr = new StreamReader(uploadListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                guids = serializer.Deserialize<UploadListGuids>(reader);
            }

            this.uploadListGuids = guids.Guids; 
        }


        private void createUploadListToRepository()
        {
            List<Upload> uploads = new List<Upload>();
            if (this.uploadListGuids != null && this.uploadListGuids.Count > 0)
            {
                foreach (Guid guid in this.uploadListGuids)
                {
                    uploads.Add(JsonDeserialization.AllUploads.Find(upload => upload.Guid == guid));
                }
            }

            DeserializationRepository.UploadList = new UploadList(uploads, DeserializationRepository.TemplateList, this.thumbnailFallbackImageFolder);
        }

        private void createTemplateListToRepository()
        {
            DeserializationRepository.TemplateList = new TemplateList(this.templates, this.templateImageFolder, this.thumbnailFallbackImageFolder);
        }
    }
}
