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

        //raised only once even if multiple properties are changed
        //so serialization in this case takes only place once.
        public event EventHandler DataChanged;

        public Guid Guid { get => this.guid; }
        public DateTime Created { get => this.created; }

        public DateTime LastModified
        {
            get => this.lastModified;
            private set
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
                    this.copyTemplateValuesInternal(false);
                    this.autoSetThumbnail(false);
                    this.autoSetPublishAtDateTimeInteral(false);
                }
                else
                {
                    this.template.RemoveUpload(this);
                    this.template = value;
                }

                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                    this.uploadStart = null;
                    this.uploadEnd = null;
                    this.uploadErrorMessage = null;
                }

                if (value == UplStatus.Uploading)
                {
                    this.uploadStart = DateTime.Now;
                }

                if (value == UplStatus.Finished)
                {
                    this.uploadEnd = DateTime.Now;
                }

                if (value == UplStatus.Stopped)
                {
                    this.uploadErrorMessage = null;
                }

                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("UploadStatus");
            }
        }

        public DateTime? PublishAt
        {
            get { return this.publishAt; }
            set
            {
                this.publishAt = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("PublishAt");
            }
        }

        public List<string> Tags
        {
            get => this.tags;
            set
            {
                this.tags = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("ThumbnailFilePath", oldValue, value);
            }
        }

        public Playlist Playlist
        {
            get { return this.playlist; }
            set
            {
                this.playlist = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
                this.videoId = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("VideoId");
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
                this.notExistsOnYoutube = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("VideoId");
            }
        }

        public CultureInfo VideoLanguage
        {
            get => this.videoLanguage;
            set
            {
                this.videoLanguage = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
                this.raisePropertyChanged("VideoLanguage");
            }
        }

        public Category Category
        {
            get => this.category;
            set
            {
                this.category = value;
                this.LastModified = DateTime.Now;
                this.raiseDataChanged();
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
            this.title = title = Path.GetFileNameWithoutExtension(this.filePath);
            this.autoSetThumbnail(false);
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
            this.copyTemplateValuesInternal(true);
        }

        private void copyTemplateValuesInternal(bool raiseDataChanged)
        {
            this.copyTitleFromTemplateInternal(false);
            this.copyDescriptionFromTemplateInternal(false);
            this.copyTagsFromtemplateInternal(false);
            this.copyVisibilityFromTemplateInternal(false);
            this.copyPlaylistFromTemplateInternal(false);
            this.copyVideoLanguageFromTemplateInternal(false);
            this.copyCategoryFromTemplateInternal(false);

            if (raiseDataChanged)
            {
                this.raiseDataChanged();
            }
        }

        public void CopyVisibilityFromTemplate()
        {
            this.copyVisibilityFromTemplateInternal(true);
        }

        private void copyVisibilityFromTemplateInternal(bool raiseDataChanged)
        {
            if (this.template != null)
            {
                this.visibility = this.template.YtVisibility;
                this.LastModified = DateTime.Now;
                if (raiseDataChanged)
                {
                    this.raiseDataChanged();

                }

                this.raisePropertyChanged("Visibility");
            }
        }

        public void CopyTagsFromtemplate()
        {
            this.copyTagsFromtemplateInternal(true);
        }

        private void copyTagsFromtemplateInternal(bool raiseDataChanged)
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

                    this.LastModified = DateTime.Now;
                    if (raiseDataChanged)
                    {
                        this.raiseDataChanged();

                    }

                    this.raisePropertyChanged("Tags");
                }
            }
        }

        public void CopyDescriptionFromTemplate()
        {
            this.copyDescriptionFromTemplateInternal(true);
        }

        private void copyDescriptionFromTemplateInternal(bool raiseDataChanged)
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

                    this.LastModified = DateTime.Now;
                    if (raiseDataChanged)
                    {
                        this.raiseDataChanged();

                    }

                    this.raisePropertyChanged("Description");
                }
            }
        }

        public void CopyTitleFromTemplate()
        {
            this.copyTitleFromTemplateInternal(true);
        }

        private void copyTitleFromTemplateInternal(bool raiseDataChanged)
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
                    this.title = string.Empty;
                }

                this.LastModified = DateTime.Now;
                if (raiseDataChanged)
                {
                    this.raiseDataChanged();

                }

                this.raisePropertyChanged("Title");
            }
        }

        public void CopyPlaylistFromTemplate()
        {
            this.copyPlaylistFromTemplateInternal(true);
        }

        private void copyPlaylistFromTemplateInternal(bool raiseDataChanged)
        {
            if (this.template != null && !this.template.SetPlaylistAfterPublication)
            {
                this.playlist = this.template.Playlist;

                this.LastModified = DateTime.Now;
                if (raiseDataChanged)
                {
                    this.raiseDataChanged();

                }

                this.raisePropertyChanged("Playlist");
            }
        }

        public void CopyVideoLanguageFromTemplate()
        {
            this.copyVideoLanguageFromTemplateInternal(true);
        }

        private void copyVideoLanguageFromTemplateInternal(bool raiseDataChanged)
        {
            if (this.template != null )
            {
                this.videoLanguage = this.template.VideoLanguage;

                this.LastModified = DateTime.Now;
                if (raiseDataChanged)
                {
                    this.raiseDataChanged();

                }

                this.raisePropertyChanged("VideoLanguage");
            }
        }

        public void CopyCategoryFromTemplate()
        {
            this.copyCategoryFromTemplateInternal(true);
        }

        private void copyCategoryFromTemplateInternal(bool raiseDataChanged)
        {
            if (this.template != null )
            {
                this.category = this.template.Category;

                this.LastModified = DateTime.Now;
                if (raiseDataChanged)
                {
                    this.raiseDataChanged();

                }

                this.raisePropertyChanged("Category");
            }
        }

        public void AutoSetPublishAtDateTime()
        {
            this.autoSetPublishAtDateTimeInteral(true);
        }

        private void autoSetPublishAtDateTimeInteral(bool raiseDataChanged)
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
            DateTime nextPossibleDateTime = DateTime.MinValue;
            while (!isFree)
            {
                nextPossibleDateTime = this.template.PublishAtSchedule.GetNextDateTime(nextPossibleDateTime);

                IEnumerable<Upload> relevantUploads = this.Template.PublishAtSchedule.IgnoreUploadsBefore == null ?
                    this.template.Uploads.Where(upload => upload != this).ToArray() :
                    this.template.Uploads.Where(upload => upload != this && (upload.UploadStart == null || upload.UploadStart > upload.Template.PublishAtSchedule.IgnoreUploadsBefore)).ToArray();

                if (!relevantUploads.Any(upload => upload.PublishAt == nextPossibleDateTime))
                {
                    isFree = true;
                }
            }

            this.publishAt = nextPossibleDateTime;

            this.LastModified = DateTime.Now;
            if (raiseDataChanged)
            {
                this.raiseDataChanged();

            }

            this.raisePropertyChanged("Visibility");
            this.raisePropertyChanged("PublishAt");
        }

        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.publishAt = new DateTime(this.publishAt.Value.Year, this.publishAt.Value.Month, this.publishAt.Value.Day, quarterHour.Hours, quarterHour.Minutes, 0);

            this.LastModified = DateTime.Now;
            this.raiseDataChanged();
            this.raisePropertyChanged("PublishAt");
        }

        public void SetPublishAtDate(DateTime publishDate)
        {
            TimeSpan publishAtTime = this.publishAt != null ? this.publishAt.Value.TimeOfDay : new TimeSpan(0, 0, 0, 0);

            this.publishAt = new DateTime(publishDate.Year, publishDate.Month, publishDate.Day, publishAtTime.Hours, publishAtTime.Minutes, 0);

            this.LastModified = DateTime.Now;
            this.raiseDataChanged();
            this.raisePropertyChanged("PublishAt");
        }

        private void autoSetThumbnail(bool raiseDataChanged)
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
                            this.thumbnailFilePath = currentFile;
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
                            this.thumbnailFilePath = currentFile;
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

            this.LastModified = DateTime.Now;
            if (raiseDataChanged)
            {
                this.raiseDataChanged();
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
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void raisePropertyChanged(string propertyName, string oldValue, string newValue)
        {
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgsEx(propertyName, oldValue, newValue));
            }
        }

        private void raiseDataChanged()
        {
            EventHandler handler = this.DataChanged;
            if (handler != null)
            {
                handler(this, null);
            }
        }
    }
}
