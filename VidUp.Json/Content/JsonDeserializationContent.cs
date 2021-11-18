using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Drexel.VidUp.Json.Content
{
    public class JsonDeserializationContent
    {
        private string playlistListFilePath;
        private string uploadListFilePath;
        private string templateListFilePath;
        private string allUploadsFilePath;

        private string thumbnailFallbackImageFolder;

        public static List<Upload> AllUploads { get; set; }
        private List<Template> templates { get; set; }
        private List<Guid> uploadListGuids { get; set; }

        public JsonDeserializationContent(string serializationFolder, string thumbnailFallbackImageFolder)
        {
            this.playlistListFilePath = Path.Combine(serializationFolder, "playlistlist.json");
            this.uploadListFilePath = Path.Combine(serializationFolder, "uploadlist.json");
            this.templateListFilePath = Path.Combine(serializationFolder, "templatelist.json");
            this.allUploadsFilePath = Path.Combine(serializationFolder, "uploads.json");

            this.thumbnailFallbackImageFolder = thumbnailFallbackImageFolder;
        }

        public ReSerialize Deserialize(YoutubeAccountList youtubeAccountList)
        {
            Tracer.Write($"JsonDeserializationContent.Deserialize: Start.");
            DeserializationRepositoryContent.YoutubeAccountList = youtubeAccountList;
            ReSerialize reSerialize = new ReSerialize();

            reSerialize.PlaylistList = this.deserializePlaylistList();
            reSerialize.AllUploads = this.deserializeAllUploads();
            reSerialize.TemplateList = this.deserializeTemplateList();
            this.setTemplateOnUploads();
            reSerialize.UploadList = this.deserializeUploadListGuids();
            this.createTemplateListToRepository();
            this.createUploadListToRepository();
            this.checkConsistency();

            Tracer.Write($"JsonDeserializationContent.Deserialize: End.");

            return reSerialize;
        }

        private bool deserializePlaylistList()
        {
            Tracer.Write($"JsonDeserializationContent.deserializePlaylistList: Start.");
            if (!File.Exists(this.playlistListFilePath))
            {
                DeserializationRepositoryContent.PlaylistList = new PlaylistList(null);
                Tracer.Write($"JsonDeserializationContent.deserializePlaylistList: End, no playlist storage file found.");
                return false;
            }

            //todo: remove compatibility ode
            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new YoutubeAccountNameStringConverter());

            using (StreamReader sr = new StreamReader(this.playlistListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepositoryContent.PlaylistList = serializer.Deserialize<PlaylistList>(reader);
            }

            Tracer.Write($"JsonDeserializationContent.deserializePlaylistList: End.");
            return false;
        }

        private bool deserializeAllUploads()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: Start.");
            if (!File.Exists(this.allUploadsFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: End, no all uploads storage file found.");
                return false;
            }

            JsonSerializer serializer = new JsonSerializer();
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

            Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: End.");

            return false;
        }

        private bool deserializeTemplateList()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: Start.");
            if (!File.Exists(this.templateListFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: End, no template list storage file found.");
                return false;
            }

            JsonSerializer serializer = new JsonSerializer();
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

            Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: End.");

            return false;
        }

        //setting templates on uploads by reading json again
        private void setTemplateOnUploads()
        {
            Tracer.Write($"JsonDeserializationContent.setTemplateOnUploads: Start.");
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

            Tracer.Write($"JsonDeserializationContent.setTemplateOnUploads: End.");
        }

        private bool deserializeUploadListGuids()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: Start.");
            if (!File.Exists(this.uploadListFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: End, no upload list guid storage file found.");
                return false;
            }

            JsonSerializer serializer = new JsonSerializer();

            UploadListGuids guids;
            using (StreamReader sr = new StreamReader(uploadListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                guids = serializer.Deserialize<UploadListGuids>(reader);
            }

            this.uploadListGuids = guids.Guids;

            Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: End.");

            return false;
        }

        private void createUploadListToRepository()
        {
            Tracer.Write($"JsonDeserializationContent.createUploadListToRepository: Start.");
            List<Upload> uploads = new List<Upload>();
            if (this.uploadListGuids != null && this.uploadListGuids.Count > 0)
            {
                foreach (Guid guid in this.uploadListGuids)
                {
                    uploads.Add(JsonDeserializationContent.AllUploads.Find(upload => upload.Guid == guid));
                }
            }

            DeserializationRepositoryContent.UploadList = new UploadList(uploads, DeserializationRepositoryContent.TemplateList, this.thumbnailFallbackImageFolder);

            Tracer.Write($"JsonDeserializationContent.createUploadListToRepository: End.");
        }

        private void createTemplateListToRepository()
        {
            Tracer.Write($"JsonDeserializationContent.createTemplateListToRepository: Start.");
            DeserializationRepositoryContent.TemplateList = new TemplateList(this.templates);
            Tracer.Write($"JsonDeserializationContent.createTemplateListToRepository: End.");
        }

        private void checkConsistency()
        {
            Tracer.Write($"JsonDeserializationContent.checkConsistency: Start.");
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

            Tracer.Write($"JsonDeserializationContent.checkConsistency: End.");
        }
    }
}
