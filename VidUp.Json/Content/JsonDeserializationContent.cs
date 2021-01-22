using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using Drexel.VidUp.Business;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using Newtonsoft.Json.Linq;

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
            //This converters returns existing Playlist objects from deserialization repository.
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            //templates are set to null, because template objects do not yet exist.
            //templates are set on setTemplateOnUploads by reading json again.
            serializer.Converters.Add(new TemplateNullConverter());

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
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());

            using (StreamReader sr = new StreamReader(this.templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                //set reader to templateList to avoid creation of TemplateList which will cause addition of event listeners etc. in constructor
                reader.Read();
                reader.Read();
                reader.Read();
                this.templates = serializer.Deserialize<List<Template>>(reader);
            }
        }

        //setting templates on uploads by reading json again
        private void setTemplateOnUploads()
        {
            if (JsonDeserializationContent.AllUploads != null && this.templates != null)
            {
                Dictionary<Guid,Guid> uploadTemplateMap = new Dictionary<Guid, Guid>();
                using (StreamReader sr = new StreamReader(this.allUploadsFilePath))
                using (JsonReader reader = new JsonTextReader(sr))
                {
                    JArray jUploads = JArray.Load(reader);
                    foreach (JToken jUpload in jUploads)
                    {
                        string uploadGuid = jUpload["guid"].Value<string>();
                        string templateGuid = jUpload["template"].Value<string>();
                        if (!String.IsNullOrWhiteSpace(templateGuid))
                        {
                            uploadTemplateMap.Add(Guid.Parse(uploadGuid), Guid.Parse(templateGuid));
                        }
                    }
                }

                foreach (Upload upload in JsonDeserializationContent.AllUploads)
                {
                   if(uploadTemplateMap.ContainsKey(upload.Guid))
                   {
                        Type uploadType = typeof(Upload);
                        FieldInfo templateFieldInfo = uploadType.GetField("template", BindingFlags.NonPublic | BindingFlags.Instance);
                        Template template = this.templates.Find(template => template.Guid == uploadTemplateMap[upload.Guid]);

                        if (template == null)
                        {
                            throw new SerializationException("Template not found.");
                        }

                        templateFieldInfo.SetValue(upload, template);
                    }
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
