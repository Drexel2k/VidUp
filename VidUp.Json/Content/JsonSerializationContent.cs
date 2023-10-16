using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
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
        private string youtubeAccountListFilePath;
        private string uploadProgressFilePath;

        private UploadList uploadList;
        private TemplateList templateList;
        private PlaylistList playlistList;
        private YoutubeAccountList youtubeAccountList;

        //serialiazer can also be called on upload thread or file system watcher event e.g.
        private object allUploadsLock = new object();
        private object uploadListLock = new object();
        private object templateListLock = new object();
        private object playlistListLock = new object();
        private object youtubeAccountListLock = new object();

        private bool serializationEnabled = true;
        private object serializationEnabledLock = new object();
        private CountdownEvent serializationProcessesCount = new CountdownEvent(0);

        public static JsonSerializationContent JsonSerializer;

        public JsonSerializationContent(string serializationFolder, UploadList uploadList, TemplateList templateList, PlaylistList playlistList, YoutubeAccountList youtubeAccountList)
        {
            this.playlistListFilePath = Path.Combine(serializationFolder, "playlistlist.json");
            this.uploadListFilePath = Path.Combine(serializationFolder, "uploadlist.json");
            this.templateListFilePath = Path.Combine(serializationFolder, "templatelist.json");
            this.allUploadsFilePath = Path.Combine(serializationFolder, "uploads.json");
            this.youtubeAccountListFilePath = Path.Combine(serializationFolder, "accountlist.json");
            this.uploadProgressFilePath = Path.Combine(serializationFolder, "uploadprogress.json");

            this.uploadList = uploadList;
            this.templateList = templateList;
            this.playlistList = playlistList;
            this.youtubeAccountList = youtubeAccountList;
        }

        //on Upload [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public void SerializeAllUploads()
        {
            Tracer.Write($"JsonSerializationContent.SerializeAllUploads: Start.");

            lock(this.serializationEnabledLock)
            {
                if(!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializeAllUploads: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

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

            Tracer.Write($"JsonSerializationContent.SerializeAllUploads: End.");
            this.serializationProcessesCount.Signal();
        }

        //on UploadList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        //uploadList changes are only triggered by GUI thread, even file system watcher call
        //is invoked on the GUI thread.
        public void SerializeUploadList()
        {
            Tracer.Write($"JsonSerializationContent.SerializeUploadList: Start.");

            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializeUploadList: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Formatting = Formatting.Indented;
            lock (this.uploadListLock)
            {
                using (StreamWriter sw = new StreamWriter(this.uploadListFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this.uploadList);
                }
            }

            Tracer.Write($"JsonSerializationContent.SerializeUploadList: End.");
            this.serializationProcessesCount.Signal();
        }

        //on TemplateList [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
        //is set so only opt-in properties/fields with [JsonProperty] are serialized
        public void SerializeTemplateList()
        {
            Tracer.Write($"JsonSerializationContent.SerializeTemplateList: Start.");

            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializeTemplateList: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new StringEnumConverter());
            serializer.Converters.Add(new UploadGuidStringConverter());
            serializer.Converters.Add(new PlaylistPlaylistIdConverter());
            serializer.Converters.Add(new CategoryIdConverter());
            serializer.Converters.Add(new CultureInfoCultureStringConverter());
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            serializer.Formatting = Formatting.Indented;

            lock (this.templateListLock)
            {
                using (StreamWriter sw = new StreamWriter(templateListFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this.templateList);
                }
            }

            Tracer.Write($"JsonSerializationContent.SerializeTemplateList: End.");
            this.serializationProcessesCount.Signal();
        }

        public void SerializePlaylistList()
        {
            Tracer.Write($"JsonSerializationContent.SerializePlaylistList: Start.");

            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializePlaylistList: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();
            serializer.Converters.Add(new YoutubeAccountGuidStringConverter());

            serializer.Formatting = Formatting.Indented;

            lock (this.playlistListLock)
            {
                using (StreamWriter sw = new StreamWriter(this.playlistListFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this.playlistList);
                }
            }

            Tracer.Write($"JsonSerializationContent.SerializePlaylistList: End.");
            this.serializationProcessesCount.Signal();
        }

        public void SerializeYoutubeAccountList()
        {
            Tracer.Write($"JsonSerializationContent.SerializeYoutubeAccountList: Start.");

            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializeYoutubeAccountList: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            lock (this.youtubeAccountList)
            { 
                using (StreamWriter sw = new StreamWriter(this.youtubeAccountListFilePath))
                using (JsonWriter writer = new JsonTextWriter(sw))
                {
                    serializer.Serialize(writer, this.youtubeAccountList);
                }
            }

            Tracer.Write($"JsonSerializationContent.SerializeYoutubeAccountList: End.");
            this.serializationProcessesCount.Signal();
        }

        public void SerializeUploadProgress(UploadProgress progress)
        {
            Tracer.Write($"JsonSerializationContent.SerializeUploadProgress: Start.", TraceLevel.Detailed);

            lock (this.serializationEnabledLock)
            {
                if (!this.serializationEnabled)
                {
                    Tracer.Write($"JsonSerializationContent.SerializeUploadProgress: End, serialization disabled.");
                    return;
                }

                if (this.serializationProcessesCount.CurrentCount <= 0)
                {
                    this.serializationProcessesCount.Reset(1);
                }
                else
                {
                    this.serializationProcessesCount.AddCount();
                }
            }

            JsonSerializer serializer = new JsonSerializer();

            serializer.Formatting = Formatting.Indented;

            using (StreamWriter sw = new StreamWriter(this.uploadProgressFilePath))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                serializer.Serialize(writer, progress);
            }

            Tracer.Write($"JsonSerializationContent.SerializeUploadProgress: End.", TraceLevel.Detailed);
            this.serializationProcessesCount.Signal();
        }

        public CountdownEvent StopSerialization()
        {
            lock (this.serializationEnabledLock)
            {
                this.serializationEnabled = false;
            }

            return this.serializationProcessesCount;
        }
    }
}
