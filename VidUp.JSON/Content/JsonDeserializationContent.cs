using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Drexel.VidUp.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace Drexel.VidUp.Json.Content
{
    public class JsonDeserializationContent
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

        public JsonDeserializationContent(string serializationFolder, string templateImageFolder, string thumbnailFallbackImageFolder)
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
            this.setTemplateListOnTemplates();
            this.createUploadListToRepository();
            this.checkConsistency();
        }

        private void deserializePlaylistList()
        {
            if (!File.Exists(this.playlistListFilePath))
            {
                DeserializationRepositoryContent.PlaylistList = new PlaylistList(null);
                return;
            }

            JsonSerializer serializer = new JsonSerializer();

            using (StreamReader sr = new StreamReader(this.playlistListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepositoryContent.PlaylistList = serializer.Deserialize<PlaylistList>(reader);
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
                JsonDeserializationContent.AllUploads = serializer.Deserialize<List<Upload>>(reader);
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

            using (StreamReader sr = new StreamReader(templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                //set reader to templateList to avoid creation of TemplateList which will cause addition of event listeners etc. in constructor
                reader.Read();
                reader.Read();
                reader.Read();
                this.templates = serializer.Deserialize<List<Template>>(reader);
            }
        }

        private void setTemplateOnUploads()
        {
            if (JsonDeserializationContent.AllUploads != null && this.templates != null)
            {
                foreach (Upload upload in JsonDeserializationContent.AllUploads)
                {
                    Type uploadType = typeof(Upload);
                    FieldInfo templateGuidFieldInfo = uploadType.GetField("templateGuid", BindingFlags.NonPublic | BindingFlags.Instance);
                    Guid templateGuid = (Guid) templateGuidFieldInfo.GetValue(upload);

                    FieldInfo templateFieldInfo = uploadType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                    templateFieldInfo.SetValue(upload, this.templates.Find(template => template.Guid == templateGuid));
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
                    uploads.Add(JsonDeserializationContent.AllUploads.Find(upload => upload.Guid == guid));
                }
            }

            DeserializationRepositoryContent.UploadList = new UploadList(uploads, DeserializationRepositoryContent.TemplateList, this.thumbnailFallbackImageFolder);
        }

        private void createTemplateListToRepository()
        {
            DeserializationRepositoryContent.TemplateList = new TemplateList(this.templates, this.templateImageFolder, this.thumbnailFallbackImageFolder);
        }

        private void setTemplateListOnTemplates()
        {
            foreach (Template template in DeserializationRepositoryContent.TemplateList)
            {
                Type templateType = typeof(Template);
                FieldInfo templateListFieldInfo = templateType.GetField("templateList", BindingFlags.NonPublic | BindingFlags.Instance);
                templateListFieldInfo.SetValue(template, DeserializationRepositoryContent.TemplateList);
            }
        }

        private void checkConsistency()
        {
            foreach (Upload upload in DeserializationRepositoryContent.UploadList)
            {
                if (upload.Template != null)
                {
                    if (!upload.Template.Uploads.Contains(upload))
                    {
                        throw new InvalidOperationException("Upload is not contained in template uploads.");
                    }
                }
            }

            foreach (Template template in DeserializationRepositoryContent.TemplateList)
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
