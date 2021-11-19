using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security;
using System.Text.RegularExpressions;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;

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
        private CultureInfo descriptionLanguage;
        [JsonProperty]
        private Category category;
        [JsonProperty]
        private YoutubeAccount youtubeAccount;

        private long fileLength;

        public event OldValueHandler ThumbnailChanged;

        public Guid Guid { get => this.guid; }
        public DateTime Created { get => this.created; }

        public DateTime LastModified
        {
            get => this.lastModified;
            private set
            {
                this.lastModified = value;
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
                    this.copyTemplateValuesInternal();
                    this.autoSetThumbnail();
                    this.autoSetPublishAtDateTimeInternal();
                }
                else
                {
                    this.template.RemoveUpload(this);
                    this.template = value;
                }

                this.LastModified = DateTime.Now;
            }
        }

        public UplStatus UploadStatus
        {
            get { return this.uploadStatus; }
            set
            {
                if (!this.VerifyForUpload())
                {
                    if (value != UplStatus.Paused)
                    {
                        throw new InvalidOperationException("Upload can only be in paused state due to Youtube limitations.");
                    }
                }

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
                    this.uploadErrorMessage = null;
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
            }
        }

        public DateTime? PublishAt
        {
            get { return this.publishAt; }
            set
            {
                if (value != null)
                {
                    this.visibility = Visibility.Private;
                    this.publishAt = QuarterHourCalculator.GetRoundedToNextQuarterHour(value.Value);
                }
                else
                {
                    this.publishAt = null;
                }

                this.LastModified = DateTime.Now;
            }
        }

        public ReadOnlyCollection<string> Tags
        {
            get => this.tags.AsReadOnly();
        }

        public int TagsCharacterCount
        {
            get
            {
                int length = 0;
                foreach (string tag in this.tags)
                {
                    length += tag.Length;
                    if (tag.Contains(' '))
                    {
                        //quotation marks
                        length += 2;
                    }
                }

                //commas
                if (this.tags.Count > 1)
                {
                    length += this.tags.Count - 1;
                }

                return length;
            }
        }

        public string ImageFilePath { get => this.getImagePath(); }

        public string ThumbnailFilePath
        {
            get { return this.thumbnailFilePath; }
            set
            {
                string oldFilePath = this.thumbnailFilePath;
                this.thumbnailFilePath = value;
                this.LastModified = DateTime.Now;
                this.onThumbnailChanged(oldFilePath);
            }
        }

        private void onThumbnailChanged(string oldFilePath)
        {
            OldValueHandler handler = this.ThumbnailChanged;
            if (handler != null)
            {
                handler(this, new OldValueArgs(oldFilePath));
            }
        }

        public Playlist Playlist
        {
            get { return this.playlist; }
            set
            {
                this.playlist = value;
                this.LastModified = DateTime.Now;
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
                this.verifyAndSetUploadStatus();
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
                this.verifyAndSetUploadStatus();
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
                if (value != Visibility.Private)
                {
                    this.publishAt = null;
                }

                this.visibility = value;

                this.LastModified = DateTime.Now;
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
            }
        }

        public CultureInfo DescriptionLanguage
        {
            get => this.descriptionLanguage;
            set
            {
                this.descriptionLanguage = value;
                this.LastModified = DateTime.Now;
            }
        }

        public CultureInfo VideoLanguage
        {
            get => this.videoLanguage;
            set
            {
                this.videoLanguage = value;
                this.LastModified = DateTime.Now;
            }
        }

        public Category Category
        {
            get => this.category;
            set
            {
                this.category = value;
                this.LastModified = DateTime.Now;
            }
        }

        public YoutubeAccount YoutubeAccount
        {
            get => this.youtubeAccount;
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("youtubeAccount must not be null.");
                }

                this.youtubeAccount = value;
                this.LastModified = DateTime.Now;
            }
        }

        [JsonConstructor]
        private Upload()
        {
            
        }

        public Upload(string filePath, YoutubeAccount youtubeAccount)
        {
            if (string.IsNullOrWhiteSpace(filePath))
            {
                throw new ArgumentException("filePath must not be null or white space.");
            }

            if (youtubeAccount == null)
            {
                throw new ArgumentException("youtubeAccount must not be null.");
            }

            this.guid = Guid.NewGuid();
            this.filePath = filePath;
            this.youtubeAccount = youtubeAccount;
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.uploadStatus = UplStatus.ReadyForUpload;
            this.tags = new List<string>();
            this.visibility = Visibility.Private;

            //to ensure at least file name is set as title.
            this.title = title = Path.GetFileNameWithoutExtension(this.filePath);
            this.autoSetThumbnail();
            this.verifyAndSetUploadStatus();
        }

        [OnDeserialized()]
        private void OnDeserializingMethod(StreamingContext context)
        {
            if (this.uploadStatus == UplStatus.Uploading)
            {
                this.uploadStatus = UplStatus.Stopped;
            }
        }

        public void SetTags(IEnumerable<string> tags)
        {
            this.tags.Clear();
            this.tags.AddRange(tags);
            this.LastModified = DateTime.Now;
            this.verifyAndSetUploadStatus();
        }

        public void CopyTemplateValues()
        {
            this.copyTemplateValuesInternal();
        }

        private void copyTemplateValuesInternal()
        {
            this.copyTitleFromTemplateInternal();
            this.copyDescriptionFromTemplateInternal();
            this.copyTagsFromtemplateInternal();
            this.copyVisibilityFromTemplateInternal();
            this.copyPlaylistFromTemplateInternal();
            this.copyVideoLanguageFromTemplateInternal();
            this.copyDescriptionLanguageFromTemplateInternal();
            this.copyCategoryFromTemplateInternal();
            this.copyYoutubeAccountFromTemplateInternal();
            this.autoSetPublishAtDateTimeInternal();
        }

        public void CopyVisibilityFromTemplate()
        {
            this.copyVisibilityFromTemplateInternal();
        }

        private void copyVisibilityFromTemplateInternal()
        {
            if (this.template != null)
            {
                this.visibility = this.template.YtVisibility;
                this.LastModified = DateTime.Now;
            }
        }

        public void CopyTagsFromtemplate()
        {
            this.copyTagsFromtemplateInternal();
        }

        private void copyTagsFromtemplateInternal()
        {
            if (this.template != null)
            {
                if (this.template.Tags != null && this.template.Tags.Count > 0)
                {
                    this.tags.Clear();
                    this.tags.AddRange(this.template.Tags);

                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(this.getPlaceholderContents()))
                    {
                        for (int tagIndex = 0; tagIndex < this.tags.Count; tagIndex++)
                        {
                            this.tags[tagIndex] = this.tags[tagIndex].Replace("#" + matchIndex + "#", match.Groups[1].Value);
                        }

                        matchIndex++;
                    }

                    this.LastModified = DateTime.Now;
                }
            }
        }

        public void CopyDescriptionFromTemplate()
        {
            this.copyDescriptionFromTemplateInternal();
        }

        private void copyDescriptionFromTemplateInternal()
        {
            if (this.template != null)
            {
                if (!string.IsNullOrWhiteSpace(this.template.Description))
                {
                    this.description = this.template.Description;
                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(this.getPlaceholderContents()))
                    {
                        this.description = this.description.Replace("#" + matchIndex + "#", match.Groups[1].Value);

                        matchIndex++;
                    }

                    this.LastModified = DateTime.Now;
                }
            }
        }

        public void CopyTitleFromTemplate()
        {
            this.copyTitleFromTemplateInternal();
        }

        private void copyTitleFromTemplateInternal()
        {
            if (this.template != null)
            {
                if (!string.IsNullOrWhiteSpace(this.template.Title))
                {
                    this.title = this.Template.Title;
                    Regex regex = new Regex(@"#([^#]+)#");
                    int matchIndex = 0;
                    foreach (Match match in regex.Matches(this.getPlaceholderContents()))
                    {
                        this.title = this.title.Replace("#" + matchIndex + "#", match.Groups[1].Value);

                        matchIndex++;
                    }
                }

                this.LastModified = DateTime.Now;
            }
        }

        public void CopyPlaylistFromTemplate()
        {
            this.copyPlaylistFromTemplateInternal();
        }

        private void copyPlaylistFromTemplateInternal()
        {
            if (this.template != null)
            {
                if (!this.template.SetPlaylistAfterPublication)
                {
                    this.playlist = this.template.Playlist;
                }
                else
                {
                    this.playlist = null;
                }

                this.LastModified = DateTime.Now;
            }
        }

        public void CopyVideoLanguageFromTemplate()
        {
            this.copyVideoLanguageFromTemplateInternal();
        }

        private void copyVideoLanguageFromTemplateInternal()
        {
            if (this.template != null )
            {
                this.videoLanguage = this.template.VideoLanguage;

                this.LastModified = DateTime.Now;
            }
        }

        public void CopyDescriptionLanguageFromTemplate()
        {
            this.copyDescriptionLanguageFromTemplateInternal();
        }

        private void copyDescriptionLanguageFromTemplateInternal()
        {
            if (this.template != null)
            {
                this.descriptionLanguage = this.template.DescriptionLanguage;

                this.LastModified = DateTime.Now;
            }
        }

        public void CopyCategoryFromTemplate()
        {
            this.copyCategoryFromTemplateInternal();
        }

        public void CopyYoutubeAccountFromTemplate()
        {
            this.copyYoutubeAccountFromTemplateInternal();
        }

        private void copyCategoryFromTemplateInternal()
        {
            if (this.template != null )
            {
                this.category = this.template.Category;

                this.LastModified = DateTime.Now;
            }
        }

        private void copyYoutubeAccountFromTemplateInternal()
        {
            if (this.template != null)
            {
                this.youtubeAccount = this.template.YoutubeAccount;

                this.LastModified = DateTime.Now;
            }
        }

        public void AutoSetPublishAtDateTime()
        {
            this.autoSetPublishAtDateTimeInternal();
        }

        private void autoSetPublishAtDateTimeInternal()
        {
            if (this.template == null)
            {
                this.publishAt = null;
            }
            else
            {
                if (!this.template.UsePublishAtSchedule)
                {
                    this.publishAt = null;
                }
                else
                {
                    this.visibility = Visibility.Private;

                    DateTime nextPossibleDateTime = DateTime.MinValue;
                    if (this.template.PublishAtSchedule.ScheduleFrequency == ScheduleFrequency.SpecificDate)
                    {
                        nextPossibleDateTime = this.template.PublishAtSchedule.GetNextDateTime(nextPossibleDateTime);
                    }
                    else
                    {
                        bool isFree = false;
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
                    }

                    this.publishAt = nextPossibleDateTime;
                }
            }

            this.LastModified = DateTime.Now;
        }

        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.publishAt = new DateTime(this.publishAt.Value.Year, this.publishAt.Value.Month, this.publishAt.Value.Day, quarterHour.Hours, quarterHour.Minutes, 0);

            this.LastModified = DateTime.Now;
        }

        public void SetPublishAtDate(DateTime publishDate)
        {
            TimeSpan publishAtTime = this.publishAt != null ? this.publishAt.Value.TimeOfDay : new TimeSpan(0, 0, 0, 0);

            this.publishAt = new DateTime(publishDate.Year, publishDate.Month, publishDate.Day, publishAtTime.Hours, publishAtTime.Minutes, 0);

            this.LastModified = DateTime.Now;
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
        }

        private string getPlaceholderContents()
        {
            // since we already know there's a template if this code is called, no need to null check the template
            if (this.template.UsePlaceholderFile)
            {
                string extension = ".txt";
                string fileNameWithoutExtension = Path.GetFileNameWithoutExtension(this.filePath).ToLower();

                if (!string.IsNullOrWhiteSpace(this.template.PlaceholderFolderPath) && Directory.Exists(this.template.PlaceholderFolderPath))
                {
                    foreach (string currentFile in Directory.GetFiles(this.template.PlaceholderFolderPath))
                    {
                        if (fileNameWithoutExtension == Path.GetFileNameWithoutExtension(currentFile).ToLower())
                        {
                            if (Path.GetExtension(currentFile).Equals(extension))
                            {
                                return File.ReadAllText(currentFile);
                            }
                        }
                    }
                }

                foreach (string currentFile in Directory.GetFiles(Path.GetDirectoryName(this.filePath)))
                {
                    if (fileNameWithoutExtension == Path.GetFileNameWithoutExtension(currentFile).ToLower() && Path.GetFullPath(currentFile).ToLower() != Path.GetFullPath(this.filePath).ToLower())
                    {
                        if (Path.GetExtension(currentFile).Equals(extension))
                        {
                            return File.ReadAllText(currentFile);
                        }
                    }
                }
            }

            return Path.GetFileName(this.FilePath);
        }

        public void ResetPublishAt()
        {
            //default publish at is 24 hour in the future
            this.publishAt = QuarterHourCalculator.GetRoundedToNextQuarterHour(
                new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 0).AddHours(24));
            this.visibility = Visibility.Private;
        }

        private string getImagePath()
        {
            if (this.template == null)
            {
                return Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "images/defaultupload.png");
            }

            return this.template.ImageFilePathForRendering;
        }

        public bool VerifyForUpload()
        {
            if (this.title != null && this.title.Length > YoutubeLimits.TitleLimit || 
                this.description != null && this.description.Length > YoutubeLimits.DescriptionLimit ||
                this.TagsCharacterCount > YoutubeLimits.TagsLimit || 
                this.fileLength > YoutubeLimits.FileSizeLimit)
            {
                return false;
            }

            return true;
        }

        private void verifyAndSetUploadStatus()
        {
            if (!this.VerifyForUpload())
            {
                this.uploadStatus = UplStatus.Paused;
            }
        }
    }
}
