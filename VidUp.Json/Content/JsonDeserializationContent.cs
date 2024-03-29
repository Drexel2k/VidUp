﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Metadata;
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
        private string youtubeAccountListFilePath;
        private string uploadProgressFilePath;
        private string serializationFolder;

        private string thumbnailFallbackImageFolder;

        private static bool allPlaylistsDeserialiozed = true;
        private static bool allYoutubeAccountsDeserialiozed = true;
        private static YoutubeAccount youtubeAccountForNullReplacement;
        public static bool AllPlaylistsDeserialiozed
        {
            set => JsonDeserializationContent.allPlaylistsDeserialiozed = value;
        }

        public static bool AllYoutubeAccountsDeserialized
        {
            set => JsonDeserializationContent.allYoutubeAccountsDeserialiozed = value;
        }

        public static YoutubeAccount YoutubeAccountForNullReplacement
        {
            get
            {
                if(JsonDeserializationContent.youtubeAccountForNullReplacement == null)
                {
                    JsonDeserializationContent.youtubeAccountForNullReplacement = new YoutubeAccount($"Missing account replacement {DateTime.Now.ToString("yyyyy-MM-dd_HH-mm-ss")}");
                    DeserializationRepositoryContent.YoutubeAccountList.AddYoutubeAccount(JsonDeserializationContent.youtubeAccountForNullReplacement);
                }

                return JsonDeserializationContent.youtubeAccountForNullReplacement;
            }
        }

        public static List<Upload> AllUploads { get; set; }
        private List<Template> templates { get; set; }
        private List<Guid> uploadListGuids { get; set; }

        public JsonDeserializationContent(string serializationFolder, string thumbnailFallbackImageFolder)
        {
            this.serializationFolder = serializationFolder;
            this.playlistListFilePath = Path.Combine(this.serializationFolder, "playlistlist.json");
            this.uploadListFilePath = Path.Combine(this.serializationFolder, "uploadlist.json");
            this.templateListFilePath = Path.Combine(this.serializationFolder, "templatelist.json");
            this.allUploadsFilePath = Path.Combine(this.serializationFolder, "uploads.json");
            this.youtubeAccountListFilePath = Path.Combine(this.serializationFolder, "accountlist.json");
            this.uploadProgressFilePath = Path.Combine(serializationFolder, "uploadprogress.json");

            this.thumbnailFallbackImageFolder = thumbnailFallbackImageFolder;
        }

        public void Deserialize(ReSerialize reSerialize)
        {
            Tracer.Write($"JsonDeserializationContent.Deserialize: Start.");

            reSerialize.YoutubeAccountList = this.deserializeYoutubeAccountList();
            reSerialize.PlaylistList = this.deserializePlaylistList();
            if (!JsonDeserializationContent.allYoutubeAccountsDeserialiozed)
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one youtube account on playlist list deserialization was missing and replaced.");
                reSerialize.PlaylistList = true;
                reSerialize.YoutubeAccountList = true;
            }

            JsonDeserializationContent.allYoutubeAccountsDeserialiozed = true;
            this.deserializeAllUploads();
            if(!JsonDeserializationContent.allPlaylistsDeserialiozed)
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one playlist on all uploads deserialization was missing.");
                reSerialize.AllUploads = true;
            }

            if (!JsonDeserializationContent.allYoutubeAccountsDeserialiozed)
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one youtube account on all uploads deserialization was missing and replaced.");
                reSerialize.AllUploads = true;
                reSerialize.YoutubeAccountList = true;
            }

            JsonDeserializationContent.allPlaylistsDeserialiozed = true;
            JsonDeserializationContent.allYoutubeAccountsDeserialiozed = true;
            reSerialize.TemplateList = this.deserializeTemplateList();
            if (!JsonDeserializationContent.allPlaylistsDeserialiozed)
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one playlist on template list deserialization was missing.");
                reSerialize.TemplateList = true;
            }

            if (!JsonDeserializationContent.allYoutubeAccountsDeserialiozed)
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one youtube account on template list deserialization was missing and replaced.");
                reSerialize.TemplateList = true;
                reSerialize.YoutubeAccountList = true;
            }

            if (!this.setTemplateOnUploads())
            {
                reSerialize.TemplateList = true;
            }

            this.deserializeUploadListGuids();
            this.createTemplateListToRepository();
            reSerialize.UploadList = this.createUploadListToRepository();

            if(!this.checkUploadListUploadInTemplateUploads())
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one upload with template in upload list was not in template uploads.");
                reSerialize.AllUploads = true;
            }

            if(!this.checkUploadTemplateEqualsContainingTemplate())
            {
                Tracer.Write($"JsonDeserializationContent.Deserialize: At least one upload's template in template uploads was different to containing template.");
                reSerialize.AllUploads = true;
                reSerialize.TemplateList = true;
            }

            if (!this.deserializeUploadProgress())
            {
                reSerialize.AllUploads = true;
            }

            Tracer.Write($"JsonDeserializationContent.Deserialize: End.");
        }

        private bool deserializeUploadProgress()
        {
            bool result = true;
            Tracer.Write($"JsonDeserializationContent.deserializeUploadProgress: Start.");
            if (!File.Exists(this.uploadProgressFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeUploadProgress: End, no upload progress storage file found.");
                return true;
            }

            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(this.uploadProgressFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                try
                {
                    UploadProgress progress = serializer.Deserialize<UploadProgress>(reader);
                    if (progress != null)
                    {
                        Upload upload = JsonDeserializationContent.AllUploads.Find(upl => upl.Guid == progress.UploadGuid);

                        if (upload != null)
                        {
                            if (upload.UploadStatusOnDeserialization == UplStatus.Uploading)
                            {
                                upload.BytesSent = progress.BytesSent;
                                result = false;
                            }
                        }
                    }
                } 
                catch (Exception ex)
                {
                    Tracer.Write($"JsonDeserializationContent.deserializeUploadProgress: End, upload progress storage file could not be deserialized.");
                }
            }

            File.Delete(this.uploadProgressFilePath);

            Tracer.Write($"JsonDeserializationContent.deserializeUploadProgress: End.");
            return result;
        }

        private bool deserializeYoutubeAccountList()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeYoutubeAccountList: Start.");
            if (!File.Exists(this.youtubeAccountListFilePath))
            {
                DeserializationRepositoryContent.YoutubeAccountList = new YoutubeAccountList(null);
                DeserializationRepositoryContent.YoutubeAccountList.AddYoutubeAccount(new YoutubeAccount("Default Account"));
                Tracer.Write($"JsonDeserializationContent.deserializeYoutubeAccountList: Adding default account.");
                return true;
            }

            JsonSerializer serializer = new JsonSerializer();
            using (StreamReader sr = new StreamReader(this.youtubeAccountListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepositoryContent.YoutubeAccountList = serializer.Deserialize<YoutubeAccountList>(reader);
            }

            Tracer.Write($"JsonDeserializationContent.deserializeYoutubeAccountList: End.");
            return false;
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

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            using (StreamReader sr = new StreamReader(this.playlistListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                DeserializationRepositoryContent.PlaylistList = serializer.Deserialize<PlaylistList>(reader);
            }

            Tracer.Write($"JsonDeserializationContent.deserializePlaylistList: End.");
            return false;
        }

        private void deserializeAllUploads()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: Start.");
            if (!File.Exists(this.allUploadsFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: End, no all uploads storage file found.");
                return;
            }

            JsonSerializer serializer = new JsonSerializer();
            //This converters returns existing Playlist objects from deserialization repository.
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            //templates are set to null, because template objects do not yet exist.
            //templates are set on setTemplateOnUploads by reading json again.
            serializer.Converters.Add(new TemplateNullConverter());
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            using (StreamReader sr = new StreamReader(this.allUploadsFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                JsonDeserializationContent.AllUploads = serializer.Deserialize<List<Upload>>(reader);
            }

            Tracer.Write($"JsonDeserializationContent.deserializeAllUploads: End.");
        }

        private bool deserializeTemplateList()
        {
            bool reserialize = false;
            Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: Start.");
            if (!File.Exists(this.templateListFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: End, no template list storage file found.");
                return reserialize;
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            using (StreamReader sr = new StreamReader(this.templateListFilePath))
            using (JsonReader reader = new JsonTextReader(sr))
            {
                //set reader to templateList to avoid creation of TemplateList which will cause addition of event listeners etc. in constructor
                reader.Read();
                reader.Read();
                reader.Read();
                this.templates = serializer.Deserialize<List<Template>>(reader);
            }

            foreach (Template template in this.templates) 
            {
                if (template.Uploads.Contains(null))
                {
                    template.RemoveUpload(null);
                    reserialize = true;
                }
            }

            if (reserialize)
            {
                Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: Uploads from template uploads were missing in all uploads.");
            }

            Tracer.Write($"JsonDeserializationContent.deserializeTemplateList: End.");

            return reserialize;
        }

        //setting templates on uploads by reading json again
        private bool setTemplateOnUploads()
        {
            bool result = true;
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

                        if (template != null)
                        {
                            templateFieldInfo.SetValue(upload, template);
                        }      
                        else
                        {
                            Tracer.Write($"JsonDeserializationContent.setTemplateOnUploads: Template {uploadTemplateMap[upload.Guid]} not found.");
                            result = false;
                        }
                    }
                }
            }

            Tracer.Write($"JsonDeserializationContent.setTemplateOnUploads: End.");
            return result;
        }

        private void deserializeUploadListGuids()
        {
            Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: Start.");
            if (!File.Exists(this.uploadListFilePath))
            {
                Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: End, no upload list guid storage file found.");
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

            Tracer.Write($"JsonDeserializationContent.deserializeUploadListGuids: End.");
        }

        private bool createUploadListToRepository()
        {
            bool reserialize = false;
            Tracer.Write($"JsonDeserializationContent.createUploadListToRepository: Start.");
            List<Upload> uploads = new List<Upload>();
            if (this.uploadListGuids != null && this.uploadListGuids.Count > 0)
            {
                Upload upload;
                foreach (Guid guid in this.uploadListGuids)
                {
                    upload = null;

                    if (JsonDeserializationContent.AllUploads != null)
                    {
                        upload = JsonDeserializationContent.AllUploads.Find(upload => upload.Guid == guid);
                    }

                    if (upload != null)
                    {
                        uploads.Add(upload);
                    }
                    else
                    {
                        reserialize = true;
                    }
                }
            }

            if(reserialize)
            {
                Tracer.Write($"JsonDeserializationContent.createUploadListToRepository: At least one upload from upload list were missing in all uploads.");
            }

            DeserializationRepositoryContent.UploadList = new UploadList(uploads, DeserializationRepositoryContent.TemplateList, DeserializationRepositoryContent.PlaylistList, this.thumbnailFallbackImageFolder);

            Tracer.Write($"JsonDeserializationContent.createUploadListToRepository: End.");
            return reserialize;
        }

        private void createTemplateListToRepository()
        {
            Tracer.Write($"JsonDeserializationContent.createTemplateListToRepository: Start.");
            DeserializationRepositoryContent.TemplateList = new TemplateList(this.templates, DeserializationRepositoryContent.PlaylistList);
            Tracer.Write($"JsonDeserializationContent.createTemplateListToRepository: End.");
        }

        private bool checkUploadListUploadInTemplateUploads()
        {
            Tracer.Write($"JsonDeserializationContent.checkUploadListUploadInTemplateUploads: Start.");

            bool result = true;
            foreach (Upload upload in DeserializationRepositoryContent.UploadList)
            {
                if (upload.Template != null)
                {
                    if (!upload.Template.Uploads.Contains(upload))
                    {
                        upload.Template = null;
                        result = false;
                    }
                }
            }

            Tracer.Write($"JsonDeserializationContent.checkUploadListUploadInTemplateUploads: End.");
            return result;
        }

        private bool checkUploadTemplateEqualsContainingTemplate()
        {
            Tracer.Write($"JsonDeserializationContent.checkUploadTemplateEqualsContainingTemplate: Start.");

            bool result = true;
            foreach (Template template in DeserializationRepositoryContent.TemplateList)
            {
                foreach (Upload upload in template.Uploads)
                {
                    if (upload.Template != template)
                    {
                        upload.Template = null;
                        template.RemoveUpload(upload);
                        result = false;
                    }
                }
            }

            Tracer.Write($"JsonDeserializationContent.checkUploadTemplateEqualsContainingTemplate: End.");
            return result;
        }
    }
}
