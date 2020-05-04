using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using Newtonsoft.Json;
using System.Text.RegularExpressions;
using System.Linq;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization=MemberSerialization.OptIn)]
    public class Upload : INotifyPropertyChanged
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
        private string thumbnailFilePath;
        [JsonProperty]
        private Template template;
        [JsonProperty]
        private UplStatus uploadStatus;
        [JsonProperty]
        private DateTime publishAt;
        [JsonProperty]
        private string uploadErrorMessage;

        [JsonProperty]
        private string title;
        [JsonProperty]
        private string description;
        [JsonProperty]
        private List<string> tags;
        [JsonProperty]
        private Visibility visibility;

        public event PropertyChangedEventHandler PropertyChanged;

        public Guid Guid { get => this.guid; }
        public DateTime Created { get => this.created; }

        public DateTime LastModified
        {
            get => this.lastModified;
        }
        private DateTime lastModifiedInternal
        {
            set
            {
                this.lastModified = value;
                this.raisePropertyChanged("LastModified");
            }
        }

        public DateTime UploadStart { get => this.uploadStart; }
        public DateTime UploadEnd { get => this.uploadEnd; }
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

                    //must be set before AutoSetTemplate but AutoSetTemplate
                    //shall no be called when template is set to null
                    this.template = value;
                    this.CopyTemplateValues();
                    this.autoSetThumbnail();
                }
                else
                {
                    this.template.Uploads.Remove(this);
                    this.template = value;
                }

                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Template");
            }
        }

        public UplStatus UploadStatus
        {
            get { return this.uploadStatus; }
            set
            {
                this.uploadStatus = value;

                if (value == UplStatus.Uploading)
                {
                    this.uploadStart = DateTime.Now;
                    this.uploadEnd = DateTime.MinValue;
                }

                if (value == UplStatus.Finished)
                {
                    this.uploadEnd = DateTime.Now;
                }

                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("UploadStatus");
            }
        }

        public DateTime PublishAt
        {
            get { return this.publishAt; }
        }

        public List<string> Tags
        {
            get => this.tags;
            set
            {
                this.tags = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Tags");
            }
        }
        public string ImageFilePath { get => this.getImagePath(this.template); }

        public string YtTitle
        {
            get
            {
                if (this.title.Length <= 100)
                {
                    return this.title;
                }
                else
                {
                    return title.Substring(0, 100);
                }
            }
        }

        public DateTime PublishAtTime
        {
            get
            {
                return new DateTime(1, 1, 1, this.publishAt.Hour, this.publishAt.Minute, 0);
            }
        }

        public string ThumbnailFilePath
        {
            get { return this.thumbnailFilePath; }
            set
            {
                string oldValue = this.thumbnailFilePath;
                this.thumbnailFilePath = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("ThumbnailFilePath", oldValue, value);
            }
        }

        public string UploadErrorMessage
        { 
            get
            {
                return this.uploadErrorMessage;
            }
            set
            {
                this.uploadErrorMessage = value;
                this.raisePropertyChanged("UploadErrorMessage");
            }
        }

        public string Title
        {
            get
            {
                return this.title;
            }
            set
            {
                string title = Path.GetFileNameWithoutExtension(this.filePath);
                if (!string.IsNullOrWhiteSpace(value))
                {
                    title = value;
                }

                this.title = title;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Title");
            }
        }

        public string Description
        {
            get
            {
                return this.description;
            }
            set
            {
                this.description = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Description");
            }
        }

        public Visibility Visibility
        {
            get
            {
                return this.visibility;
            }
            set
            {
                this.visibility = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Visibility");
            }
        }

        public long FileLength
        {
            get
            {
                FileInfo fileInfo = new FileInfo(this.filePath);
                if (fileInfo.Exists)
                {
                    return fileInfo.Length;
                }

                return 0;
            }
        }

        public Upload()
        {
            this.tags = new List<string>();
        }

        public Upload(string filePath)
        {
            this.guid = Guid.NewGuid();
            this.filePath = filePath;
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.uploadStatus = UplStatus.ReadyForUpload;
            this.tags = new List<string>();

            //to ensure at least file name is set as title.
            this.Title = string.Empty;
            this.autoSetThumbnail();
        }

        public void CopyTemplateValues()
        {
            this.CopyTitleFromTemplate();
            this.CopyDescriptionFromTemplate();
            this.CopyTagsFromtemplate();
            this.CopyVisbilityFromTemplate();
        }

        public void CopyVisbilityFromTemplate()
        {
            if (this.template != null)
            {
                this.visibility = this.template.YtVisibility;
                this.raisePropertyChanged("Visibility");
            }
        }

        public void CopyTagsFromtemplate()
        {
            if (this.template != null)
            {
                if (this.template.Tags != null && this.template.Tags.Count > 0)
                {
                    this.tags.Clear();
                    this.tags.AddRange(this.template.Tags);
                    this.raisePropertyChanged("Tags");
                }
            }
        }

        public void CopyDescriptionFromTemplate()
        {
            if (this.template != null)
            {
                if (!string.IsNullOrWhiteSpace(this.template.Description))
                {
                    this.description = this.template.Description;
                    this.raisePropertyChanged("Description");
                }
            }
        }

        public void CopyTitleFromTemplate()
        {
            if (this.template != null)
            {
                if (!string.IsNullOrWhiteSpace(this.template.Title))
                {
                    this.title = this.Template.Title;
                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(this.FilePath))
                    {
                        this.title = this.title.Replace("#" + matchIndex + "#", match.Groups[1].Value);

                        matchIndex++;
                    }
                }
                else
                {
                    this.Title = string.Empty;
                }

                this.raisePropertyChanged("Title");
            }
        }

        public void SetPublishAtTime(DateTime quarterHour)
        {
            this.publishAt = new DateTime(this.publishAt.Year, this.publishAt.Month, this.publishAt.Day, quarterHour.Hour, quarterHour.Minute, 0);
            this.lastModifiedInternal = DateTime.Now;
            this.raisePropertyChanged("PublishAtTime");
        }

        public void SetPublishAtDate(DateTime publishDate)
        {
            if (this.publishAt.Date == DateTime.MinValue.Date)
            {
                if (this.template != null)
                {
                    this.publishAt = new DateTime(1, 1, 1, this.template.DefaultPublishAtTime.Hour, this.template.DefaultPublishAtTime.Minute, 0);
                }
            }

            this.publishAt = new DateTime(publishDate.Year, publishDate.Month, publishDate.Day, this.publishAt.Hour, this.publishAt.Minute, 0);
            this.lastModifiedInternal = DateTime.Now;
            this.raisePropertyChanged("PublishAtDate");
        }

        private void autoSetThumbnail()
        {
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.filePath).ToLower();

            bool found = false;
            if(this.template!=null && !string.IsNullOrWhiteSpace(this.template.ThumbnailFolderPath) && Directory.Exists(this.template.ThumbnailFolderPath))
            {
                foreach(string currentFile in Directory.GetFiles(this.template.ThumbnailFolderPath))
                {
                    if(fileNameWithoutExtension == Path.GetFileNameWithoutExtension(currentFile).ToLower())
                    {
                        if(extensions.Contains(Path.GetExtension(currentFile).ToLower()))
                        {
                            this.ThumbnailFilePath = currentFile;
                            found = true;
                            break;
                        }
                    }
                }
            }

            if(!found)
            {
                foreach (string currentFile in Directory.GetFiles(Path.GetDirectoryName(this.filePath)))
                {
                    if (fileNameWithoutExtension == Path.GetFileNameWithoutExtension(currentFile).ToLower() && Path.GetFullPath(currentFile).ToLower() != Path.GetFullPath(this.filePath).ToLower())
                    {
                        if (extensions.Contains(Path.GetExtension(currentFile).ToLower()))
                        {
                            this.ThumbnailFilePath = currentFile;
                            found = true;
                            break;
                        }
                    }
                }
            }

            if(!found)
            {
                if(this.template != null && !string.IsNullOrWhiteSpace(this.template.ThumbnailFallbackFilePath))
                {
                    this.thumbnailFilePath = this.template.ThumbnailFallbackFilePath;
                }
            }

            this.raisePropertyChanged("ThumbnailFilePath");
        }

        private string getImagePath(Template template)
        {
            if (this.template == null)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/defaultupload.png");
            }

           return this.template.ImageFilePathForRendering;
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void raisePropertyChanged(string propertyName, string oldValue, string newValue)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
            }
        }
    }
}
