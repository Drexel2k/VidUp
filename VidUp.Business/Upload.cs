using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text.RegularExpressions;
using Newtonsoft.Json;

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
        private DateTime? uploadStart;
        [JsonProperty]
        private DateTime? uploadEnd;
        [JsonProperty]
        private string filePath;
        [JsonProperty]
        private string thumbnailFilePath;
        [JsonProperty]
        private Playlist playlist;
        [JsonProperty]
        private Template template;
        [JsonProperty]
        private UplStatus uploadStatus;
        [JsonProperty]
        private DateTime? publishAt;
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
        [JsonProperty]
        private string resumableSessionUri;
        [JsonProperty]
        private long bytesSent;
        [JsonProperty]
        private string videoId;
        [JsonProperty]
        private bool notExistsOnYoutube;
        [JsonProperty]
        private CultureInfo videoLanguage;
        [JsonProperty]
        private Category category;

        private long fileLength;

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

        public DateTime? UploadStart { get => this.uploadStart; }
        public DateTime? UploadEnd { get => this.uploadEnd; }
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
                        this.template.RemoveUpload(this);
                    }

                    value.AddUpload(this);

                    //must be set before AutoSetTemplate but AutoSetTemplate
                    //shall no be called when template is set to null
                    this.template = value;
                    this.CopyTemplateValues();
                    this.autoSetThumbnail();
                    this.AutoSetPublishAtDateTime();
                }
                else
                {
                    this.template.RemoveUpload(this);
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
                if (value == UplStatus.ReadyForUpload)
                {
                    this.ResumableSessionUri = null;
                    this.BytesSent = 0;
                }

                if (value == UplStatus.Uploading)
                {
                    this.uploadStart = DateTime.Now;
                }

                if (value == UplStatus.Finished)
                {
                    this.uploadEnd = DateTime.Now;
                }

                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("UploadStatus");
            }
        }

        public DateTime? PublishAt
        {
            get { return this.publishAt; }
            set
            {
                this.publishAt = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("PublishAt");
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

        public Playlist Playlist
        {
            get { return this.playlist; }
            set
            {
                this.playlist = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Playlist");
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
                this.lastModifiedInternal = DateTime.Now;
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
                if (this.fileLength <= 0)
                {
                    FileInfo fileInfo = new FileInfo(this.filePath);
                    if (fileInfo.Exists)
                    {
                        this.fileLength = fileInfo.Length;
                    }
                }

                return this.fileLength;
            }
        }

        public string ResumableSessionUri
        {
            get
            {
                return this.resumableSessionUri;
            }
            set
            {
                this.resumableSessionUri = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("ResumableSessionUri");
            }
        }
        public long BytesSent
        {
            get
            {
                return this.bytesSent;
            }
            set
            {
                this.bytesSent = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("BytesSent");
            }
        }

        public string VideoId
        {
            get
            {
                return this.videoId;
            }
            set
            {
                this.lastModifiedInternal = DateTime.Now;
                this.videoId = value;
            }
        }

        public bool NotExistsOnYoutube
        {
            get
            {
                return this.notExistsOnYoutube;
            }
            set
            {
                this.lastModifiedInternal = DateTime.Now;
                this.notExistsOnYoutube = value;
            }
        }

        public CultureInfo VideoLanguage
        {
            get => this.videoLanguage;
            set
            {
                this.videoLanguage = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("VideoLanguage");
            }
        }

        public Category Category
        {
            get => this.category;
            set
            {
                this.category = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Category");
            }
        }

        [JsonConstructor]
        private Upload()
        {
            
        }

        public Upload(string filePath)
        {
            this.guid = Guid.NewGuid();
            this.filePath = filePath;
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.uploadStatus = UplStatus.ReadyForUpload;
            this.tags = new List<string>();
            this.visibility = Visibility.Private;

            //to ensure at least file name is set as title.
            this.Title = string.Empty;
            this.autoSetThumbnail();
        }

        [OnDeserialized()]
        private void OnDeserializingMethod(StreamingContext context)
        {
            if (this.uploadStatus == UplStatus.Uploading)
            {
                this.uploadStatus = UplStatus.Stopped;
            }
        }

        public void CopyTemplateValues()
        {
            this.CopyTitleFromTemplate();
            this.CopyDescriptionFromTemplate();
            this.CopyTagsFromtemplate();
            this.CopyVisibilityFromTemplate();
            this.CopyPlaylistFromTemplate();
            this.CopyVideoLanguageFromTemplate();
            this.CopyCategoryFromTemplate();
        }

        public void CopyVisibilityFromTemplate()
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

                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(Path.GetFileName(this.FilePath)))
                    {
                        for (int tagIndex = 0; tagIndex < this.tags.Count; tagIndex++)
                        {
                            this.tags[tagIndex] = this.tags[tagIndex].Replace("#" + matchIndex + "#", match.Groups[1].Value);
                        }

                        matchIndex++;
                    }

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
                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(Path.GetFileName(this.FilePath)))
                    {
                        this.description = this.description.Replace("#" + matchIndex + "#", match.Groups[1].Value);

                        matchIndex++;
                    }

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
                    foreach (Match match in regex.Matches(Path.GetFileName(this.FilePath)))
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

        public void CopyPlaylistFromTemplate()
        {
            if (this.template != null && !this.template.SetPlaylistAfterPublication)
            {
                this.Playlist = this.template.Playlist;
            }
        }

        public void CopyVideoLanguageFromTemplate()
        {
            if (this.template != null )
            {
                this.VideoLanguage = this.template.VideoLanguage;
            }
        }

        public void CopyCategoryFromTemplate()
        {
            if (this.template != null )
            {
                this.Category = this.template.Category;
            }
        }

        public void AutoSetPublishAtDateTime()
        {
            if (this.template == null)
            {
                return;
            }

            if (!this.template.UsePublishAtSchedule)
            {
                return;
            }

            this.visibility = Visibility.Private;

            bool isFree = false;
            DateTime plannedUntil = DateTime.MinValue;
            DateTime nextPossibleDateTime = DateTime.MinValue;
            while (!isFree)
            {
                nextPossibleDateTime = this.template.PublishAtSchedule.GetNextDateTime(plannedUntil);
                plannedUntil = nextPossibleDateTime;

                IEnumerable<Upload> relevantUploads;
                relevantUploads = this.Template.PublishAtSchedule.IgnoreUploadsBefore == null ? 
                    this.template.Uploads.Where(upload => upload != this).ToArray() : 
                    this.template.Uploads.Where(upload => upload != this && (upload.UploadStart == null || upload.UploadStart > upload.Template.PublishAtSchedule.IgnoreUploadsBefore)).ToArray();
                
                if (!relevantUploads.Any(upload => (upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading ||
                                                    upload.UploadStatus == UplStatus.Stopped || upload.UploadStatus == UplStatus.Finished) 
                                                   && upload.PublishAt == nextPossibleDateTime))
                {
                    isFree = true;
                }
            }

            this.PublishAt = nextPossibleDateTime;

            this.raisePropertyChanged("Visibility");
            this.raisePropertyChanged("PublishAt");
        }

        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.publishAt = new DateTime(this.publishAt.Value.Year, this.publishAt.Value.Month, this.publishAt.Value.Day, quarterHour.Hours, quarterHour.Minutes, 0);
            this.lastModifiedInternal = DateTime.Now;
            this.raisePropertyChanged("PublishAt");
        }

        public void SetPublishAtDate(DateTime publishDate)
        {
            TimeSpan publishAtTime = this.publishAt != null ? this.publishAt.Value.TimeOfDay : new TimeSpan(0, 0, 0, 0);

            this.publishAt = new DateTime(publishDate.Year, publishDate.Month, publishDate.Day, publishAtTime.Hours, publishAtTime.Minutes, 0);
            this.lastModifiedInternal = DateTime.Now;
            this.raisePropertyChanged("PublishAt");
        }

        private void autoSetThumbnail()
        {
            string[] extensions = { ".png", ".jpg", ".jpeg" };
            string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.filePath).ToLower();
            string oldFilePath = this.thumbnailFilePath;

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

            this.raisePropertyChanged("ThumbnailFilePath", oldFilePath, this.thumbnailFilePath);
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
