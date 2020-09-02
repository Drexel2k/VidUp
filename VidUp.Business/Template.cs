#region

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Runtime.Serialization;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Template : INotifyPropertyChanged
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
        private string thumbnailFolderPath;
        [JsonProperty]
        private string thumbnailFallbackFilePath;
        [JsonProperty]
        private bool isDefault;
        [JsonProperty]
        private Playlist playlist;
        [JsonProperty]
        private bool usePublishAtSchedule;
        [JsonProperty]
        private Schedule publishAtSchedule;

        private TemplateList templateList;

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonConstructor]
        private Template()
        {
        }

        //for fake templates(e.g. All and None Templates for filter)
        public Template(string name)
        {
            this.name = name;
        }

        public Template(string name, string imagefilePath, string rootFolderPath, TemplateList templateList)
        {
            if(templateList == null)
            {
                throw new ArgumentException("templateList must not be null.");
            }

            this.guid = Guid.NewGuid();
            this.Name = name;
            this.templateList = templateList;
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
        public Guid Guid { get => this.guid; }
        public DateTime Created { get => this.created; }
        public DateTime LastModified { get => this.lastModified; }

        private DateTime lastModifiedInternal
        {
            set
            {
                this.lastModified = value;
                this.raisePropertyChanged("LastModified");
            }
        }
        public string Name
        {
            get
            {
                return this.name;
            }
            set
             {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("Template name must not be null or empty.", "name");
                }

                this.name = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Name");
            }
        }
        public string Title
        {
            get => this.title;
            set 
            {
                this.title = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Title");
            }
        }
        public string Description
        {
            get => this.description;
            set
            {
                this.description = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Description");
            }
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

        public Visibility YtVisibility
        {
            get => this.visibility;
            set
            {
                this.visibility = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("YtVisibility");
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
                string newFilePath = this.templateList.CopyTemplateImageToStorageFolder(value);

                string oldFilePath = this.imageFilePath;
                this.imageFilePath = newFilePath;
                
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("ImageFilePathForEditing", oldFilePath, value);
            }
        }

        public string RootFolderPath
        {
            get => this.rootFolderPath;
            set
            {
                this.rootFolderPath = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("RootFolderPath");
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.thumbnailFolderPath;
            set
            {
                this.thumbnailFolderPath = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("ThumbnailFolderPath");
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.thumbnailFallbackFilePath;
            set
            {
                string oldValue = this.thumbnailFallbackFilePath;
                string filePath = this.templateList.CopyThumbnailFallbackToStorageFolder(value);
                this.thumbnailFallbackFilePath = filePath;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("ThumbnailFallbackFilePath", oldValue, value);
            }
        }

        public bool IsDefault
        {
            get => this.isDefault;
            set
            {
                this.isDefault = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("IsDefault");
            }
        }

        public ReadOnlyCollection<Upload> Uploads { get => this.uploads.AsReadOnly(); }

        public Playlist Playlist
        {
            get => this.playlist;
            set
            {
                this.playlist = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Playlist");
            }
        }

        public bool UsePublishAtSchedule
        {
            get => this.usePublishAtSchedule;
            set
            {
                if (value)
                {
                    if (this.publishAtSchedule == null)
                    {
                        this.PublishAtSchedule = new Schedule();
                    }
                }

                this.usePublishAtSchedule = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("UsePublishAtSchedule");
            }
        }

        public Schedule PublishAtSchedule
        {
            get => this.publishAtSchedule;
            set
            {
                this.publishAtSchedule = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("PublishAtSchedule");
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

        public void RemoveUpload(Upload upload)
        {
            this.uploads.Remove(upload);
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

        //sets new daily/weekly/monthly UploadedUntil on schedule
        public void SetScheduleProgress()
        {
            if (this.UsePublishAtSchedule && this.uploads.Count > 0)
            {
                List<Upload> relevantUploads = this.uploads.FindAll(upload =>
                    (upload.UploadStart == null || upload.UploadStart > this.PublishAtSchedule.IgnoreUploadsBefore) &&
                    upload.UploadStatus == UplStatus.Finished);

                relevantUploads = relevantUploads.FindAll(upload => upload.PublishAt > this.publishAtSchedule.UploadedUntil);
                relevantUploads.Sort(Template.compareUploadPublishAtDates);

                DateTime nextDate = this.publishAtSchedule.GetNextDateTime(DateTime.MinValue);
                foreach (Upload upload in relevantUploads)
                {
                    if (upload.PublishAt == nextDate)
                    {
                        this.publishAtSchedule.UploadedUntil = nextDate;
                        nextDate = this.publishAtSchedule.GetNextDateTime(DateTime.MinValue);
                    }

                    if (upload.PublishAt > nextDate)
                    {
                        return;
                    }
                }
            }
        }

        private static int compareUploadPublishAtDates(Upload upload1, Upload upload2)
        {
            if (upload1.PublishAt > upload2.PublishAt)
            {
                return 1;
            }

            if (upload1.PublishAt < upload2.PublishAt)
            {
                return -1;
            }

            return 0;
        }
    }
}
