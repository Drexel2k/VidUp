using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Template
    {
        [JsonProperty]
        private Guid guid;
        [JsonProperty]
        private DateTime created;
        [JsonProperty]
        private DateTime lastModified;
        [JsonProperty]
        private string name;
        [JsonProperty]
        private string title;
        [JsonProperty]
        private string description;
        [JsonProperty]
        private List<string> tags;
        [JsonProperty]
        private Visibility visibility;
        [JsonProperty]
        private List<Upload> uploads;
        [JsonProperty]
        private string imageFilePath;
        [JsonProperty]
        private string rootFolderPath;
        [JsonProperty]
        private DateTime defaultPublishAtTime;
        [JsonProperty]
        private string thumbnailFolderPath;
        [JsonProperty]
        private string thumbnailFallbackFilePath;
        [JsonProperty]
        private bool isDefault;

        public Template()
        {
            this.uploads = new List<Upload>();
            this.tags = new List<string>();
        }
        public Template(string name, string imagefilePath, string rootFolderPath)
        {
            this.guid = Guid.NewGuid();
            this.name = name;
            this.ImageFilePathForEditing = imagefilePath;
            this.rootFolderPath = rootFolderPath;
            this.uploads = new List<Upload>();
            this.tags = new List<string>();
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.visibility = Visibility.Private;
        }

        public void SetTemplateReferenceToUploads()
        {
            if (this.uploads == null)
            {
                this.uploads = new List<Upload>();
            }

            foreach (Upload upload in this.uploads)
            {
                upload.Template = this;
            }
        }

        #region properties
        public Guid Guid { get => guid; }
        public DateTime Created { get => created; }
        public DateTime LastModified
        {
            get => lastModified; set => lastModified = value;
        }
        public string Name
        {
            get
            {
                return this.name;
            }
            set
             {
                this.name = value;
                this.lastModified = DateTime.Now;
             }
        }
        public string Title
        {
            get => this.title;
            set 
            {
                this.title = value;
                this.lastModified = DateTime.Now;
            }
        }
        public string Description
        {
            get => this.description;
            set
            {
                this.description = value;
                this.lastModified = DateTime.Now;
            }
        }

        public List<string> Tags
        {
            get => this.tags;
            set
            {
                this.tags = value;
                this.lastModified = DateTime.Now;
            }
        }

        public Visibility YtVisibility
        {
            get => this.visibility;
            set
            {
                this.visibility = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string ImageFilePathForRendering
        {
            get => this.getImageFilePath();
        }

        public string ImageFilePathForEditing
        {
            get => this.imageFilePath;
            set
            {
                this.imageFilePath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string RootFolderPath
        {
            get => this.rootFolderPath;
            set
            {
                this.rootFolderPath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.thumbnailFolderPath;
            set
            {
                this.thumbnailFolderPath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.thumbnailFallbackFilePath;
            set
            {
                this.thumbnailFallbackFilePath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public bool IsDefault
        {
            get => this.isDefault;
            set
            {
                this.isDefault = value;
                this.lastModified = DateTime.Now;
            }
        }

        public List<Upload> Uploads { get => this.uploads; }
        public DateTime DefaultPublishAtTime
        {
            get
            {
                return this.defaultPublishAtTime;
            }
            set
            {
                this.defaultPublishAtTime = new DateTime(1, 1, 1, value.Hour, value.Minute, 0);
            }
        }

        #endregion properties
        private string getImageFilePath()
        {
            if (string.IsNullOrWhiteSpace(this.imageFilePath) || !File.Exists(this.imageFilePath))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/defaultupload.png");
            }

            return this.imageFilePath;
        }

        public void AddUpload(Upload upload)
        {
            this.uploads.Add(upload);
        }
    }
}
