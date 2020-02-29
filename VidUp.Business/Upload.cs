using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Text.RegularExpressions;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class Upload
    {
        [JsonProperty]
        private Guid guid;
        [JsonProperty]
        private DateTime created;
        [JsonProperty]
        private DateTime lastModified;
        [JsonProperty]
        private DateTime uploadStart;
        [JsonProperty]
        private DateTime uploadEnd;
        [JsonProperty]
        private string filePath;
        [JsonProperty]
        private Template template;
        [JsonProperty]
        private UplStatus uploadStatus;

        public Upload()
        {

        }

        public Upload(string filePath)
        {
            this.guid = Guid.NewGuid();
            this.filePath = filePath;
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.uploadStatus = UplStatus.ReadyForUpload;
        }

        public Guid Guid { get => this.guid; }
        public DateTime Created { get => this.created; }
        public DateTime LastModified { get => this.lastModified; }
        public DateTime UploadStart { get => this.uploadStart; set => this.uploadStart = value; }
        public DateTime UploadEnd { get => this.uploadEnd; set => this.uploadEnd = value; }
        public string FilePath { get => this.filePath; }
        public Template Template
        { 
            get => this.template;
            set
            {
                if (value != null)
                {
                    if (this.template != null)
                    {
                        this.template.Uploads.Remove(this);
                    }

                    value.Uploads.Add(this);
                }
                else
                {
                    this.template.Uploads.Remove(this);
                }

                this.template = value;
                this.lastModified = DateTime.Now;
            }
        }
        public UplStatus UploadStatus
        {
            get { return this.uploadStatus; }
            set
            {
                this.uploadStatus = value;
                this.lastModified = DateTime.Now;
            }
        }
        public string PictureFilePath { get => getPicturePath(this.template); }

        public string YtTitle
        {
            get
            {
                string title = Path.GetFileNameWithoutExtension(this.FilePath);

                if (this.Template != null)
                {
                    if (!string.IsNullOrWhiteSpace(this.Template.YtTitle))
                    {
                        title = this.Template.YtTitle;
                        Regex regex = new Regex(@"#([^#]+)#");
                        int matchIndex = 0;
                        foreach (Match match in regex.Matches(this.FilePath))
                        {
                            title = title.Replace("#" + matchIndex + "#", match.Groups[1].Value);

                            matchIndex++;
                        }
                    }
                }

                return title;
            }
        }

        private string getPicturePath(Template template)
        {
            if (this.template == null)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/defaultupload.png");
            }

           return this.template.PictureFilePathForRendering;
        }
    }
}
