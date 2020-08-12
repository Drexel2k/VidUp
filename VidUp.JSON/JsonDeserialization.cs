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
            this.setTemplateOnUploads();
            this.deserializeUploadListGuids();
            this.createTemplateListToRepository();
            this.createUploadListToRepository();
            this.checkConsistency();
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
            serializer.Converters.Add(new GuidNullConverter());
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

            //Desrialized template list does not contain all information, e.g. folder paths, so this only temporary
            foreach (Template template in templateList)
            {
                this.templates.Add(template);
            }
        }

        private void setTemplateOnUploads()
        {
            if (JsonDeserialization.AllUploads != null && this.templates != null)
            {
                foreach (Upload upload in JsonDeserialization.AllUploads)
                {
                    Type uploadType = typeof(Upload);
                    FieldInfo templateGuidFieldInfo = uploadType.GetField("templateGuid", BindingFlags.NonPublic | BindingFlags.Instance);
                    Guid templateGuid = (Guid) templateGuidFieldInfo.GetValue(upload);

                    FieldInfo templateFieldInfo = uploadType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                    templateFieldInfo.SetValue(upload, this.templates.Find(templateInternal => templateInternal.Guid == templateGuid));
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

        private void checkConsistency()
        {
            foreach (Upload upload in DeserializationRepository.UploadList)
            {
                if (upload.Template != null)
                {
                    if (!upload.Template.Uploads.Contains(upload))
                    {
                        throw new InvalidOperationException("Upload is not contained in template uploads.");
                    }
                }
            }

            foreach (Template template in DeserializationRepository.TemplateList)
            {
                foreach (Upload upload in template.Uploads)
                {
                    if (upload.Template != template)
                    {
                        throw new InvalidOperationException("Wrong upload in template.");
                    }
                }
            }
        }
    }
}
