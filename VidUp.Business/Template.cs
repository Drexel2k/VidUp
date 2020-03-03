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
        private string ytTitle;
        [JsonProperty]
        private string ytDescription;
        [JsonProperty]
        private List<string> tags;
        [JsonProperty]
        private YtVisibility ytVisibility;
        [JsonProperty]
        private string gameTitle;     
        [JsonProperty]
        private List<Upload> uploads;
        [JsonProperty]
        private string pictureFilePath;
        [JsonProperty]
        private string rootFolderPath;
        [JsonProperty]
        private DateTime defaultPublishAtTime;
        [JsonProperty]
        private string thumbnailFolderPath;

        public Template()
        {
            this.uploads = new List<Upload>();
            this.tags = new List<string>();
        }
        public Template(string name, string picturefilePath, string rootFolderPath)
        {
            this.guid = Guid.NewGuid();
            this.name = name;
            this.PictureFilePathForEditing = picturefilePath;
            this.rootFolderPath = rootFolderPath;
            this.uploads = new List<Upload>();
            this.tags = new List<string>();
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.ytVisibility = YtVisibility.Private;
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
        public string YtTitle
        {
            get => this.ytTitle;
            set 
            {
                this.ytTitle = value;
                this.lastModified = DateTime.Now;
            }
        }
        public string YtDescription
        {
            get => ytDescription;
            set
            {
                ytDescription = value;
                this.lastModified = DateTime.Now;
            }
        }

        public List<string> Tags
        {
            get => tags;
            set
            {
                tags = value;
                this.lastModified = DateTime.Now;
            }
        }

        public YtVisibility YtVisibility
        {
            get => ytVisibility;
            set
            {
                ytVisibility = value;
                this.lastModified = DateTime.Now;
            }
        }
       
        public string GameTitle
        {
            get => gameTitle;
            set
            {
                gameTitle = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string PictureFilePathForRendering
        {
            get => getPictureFilePath();
        }

        public string PictureFilePathForEditing
        {
            get => this.pictureFilePath;
            set
            {
                pictureFilePath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string RootFolderPath
        {
            get => rootFolderPath;
            set
            {
                rootFolderPath = value;
                this.lastModified = DateTime.Now;
            }
        }

        public string ThumbnailFolderPath
        {
            get => thumbnailFolderPath;
            set
            {
                thumbnailFolderPath = value;
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
        private string getPictureFilePath()
        {
            if (string.IsNullOrWhiteSpace(this.pictureFilePath))
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/defaultupload.png");
            }

            return this.pictureFilePath;
        }

        public void AddUpload(Upload upload)
        {
            this.uploads.Add(upload);
        }
    }
}
