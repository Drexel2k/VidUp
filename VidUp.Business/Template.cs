﻿using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

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
        private TemplateMode templateMode;
        [JsonProperty]
        private string rootFolderPath;
        [JsonProperty]
        private string partOfFileName;
        [JsonProperty]
        private string thumbnailFolderPath;
        [JsonProperty]
        private string thumbnailFallbackFilePath;
        [JsonProperty]
        private bool isDefault;
        [JsonProperty]
        private string placeholderFolderPath;
        [JsonProperty]
        private Playlist playlist;
        [JsonProperty]
        private bool usePublishAtSchedule;
        [JsonProperty]
        private Schedule publishAtSchedule;
        [JsonProperty] 
        private bool setPlaylistAfterPublication;
        [JsonProperty]
        private CultureInfo videoLanguage;
        [JsonProperty]
        private CultureInfo descriptionLanguage;
        [JsonProperty]
        private Category category;
        [JsonProperty]
        private YoutubeAccount youtubeAccount;
        [JsonProperty]
        private bool enableAutomation;
        [JsonProperty]
        private AutomationSettings automationSettings;

        private bool isDummy = false;

        //for dummy templates "All", "None"...
        public static YoutubeAccount DummyAccount;

        //public event PropertyChangedEventHandler PropertyChanged;
        public event OldValueHandler ThumbnailFallbackFilePathChanged;
        public event OldValueHandler ImageFilePathForEditingChanged;
        public event EventHandler IsDefaultChanged;

        #region properties
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

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                this.setName(value);
                this.LastModified = DateTime.Now;
            }
        }

        public string Title
        {
            get => this.title;
            set 
            {
                this.title = value;
                this.LastModified = DateTime.Now;
            }
        }
        public string Description
        {
            get => this.description;
            set
            {
                this.description = value;
                this.LastModified = DateTime.Now;
            }
        }

        public List<string> Tags
        {
            get => this.tags;
            set
            {
                this.tags = value;
                this.LastModified = DateTime.Now;
            }
        }

        public Visibility YtVisibility
        {
            get => this.visibility;
            set
            {
                this.visibility = value;
                this.LastModified = DateTime.Now;
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
                string newFilePath = TemplateList.CopyTemplateImageToStorageFolder(value);

                string oldFilePath = this.imageFilePath;
                this.imageFilePath = newFilePath;
                
                this.LastModified = DateTime.Now;
                this.onImageFilePathChanged(oldFilePath);
            }
        }

        private void setImageFilePath(string imageFilePath)
        {}

        public TemplateMode TemplateMode
        {
            get => this.templateMode;
            set
            {
                this.templateMode = value;
                this.LastModified = DateTime.Now;
            }
        }

        public string RootFolderPath
        {
            get => this.rootFolderPath;
            set
            {
                this.rootFolderPath = value;
                this.LastModified = DateTime.Now;
            }
        }

        public string PartOfFileName
        {
            get => this.partOfFileName;
            set
            {
                this.partOfFileName = value;
                this.LastModified = DateTime.Now;
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.thumbnailFolderPath;
            set
            {
                this.thumbnailFolderPath = value;
                this.LastModified = DateTime.Now;
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.thumbnailFallbackFilePath;
            set
            {
                string oldValue = this.thumbnailFallbackFilePath;
                string filePath = TemplateList.CopyThumbnailFallbackToStorageFolder(value);
                this.thumbnailFallbackFilePath = filePath;
                this.LastModified = DateTime.Now;
                this.onThumbnailFallbackChanged(oldValue);
            }
        }



        public bool IsDefault
        {
            get => this.isDefault;
            set
            {
                this.isDefault = value;
                this.LastModified = DateTime.Now;
                this.onIsDefaultChanged();
            }
        }

        public string PlaceholderFolderPath
        {
            get => this.placeholderFolderPath;
            set
            {
                this.placeholderFolderPath = value;
                this.LastModified = DateTime.Now;
            }
        }

        public ReadOnlyCollection<Upload> Uploads { get => this.uploads.AsReadOnly(); }

        public Playlist Playlist
        {
            get => this.playlist;
            set
            {
                this.playlist = value;
                this.LastModified = DateTime.Now;
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
                this.LastModified = DateTime.Now;
            }
        }

        public Schedule PublishAtSchedule
        {
            get => this.publishAtSchedule;
            set
            {
                this.publishAtSchedule = value;
                this.LastModified = DateTime.Now;
            }
        }

        public bool SetPlaylistAfterPublication
        {
            get => this.setPlaylistAfterPublication;
            set
            {
                this.setPlaylistAfterPublication = value;
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

        public CultureInfo DescriptionLanguage
        {
            get => this.descriptionLanguage;
            set
            {
                this.descriptionLanguage = value;
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
                    throw new ArgumentException("Template's YouTube account must not be null.");
                }

                if (value != this.youtubeAccount)
                {
                    //otherwise collection was modified exception as upload is removed...
                    foreach (Upload upload in this.uploads.ToList())
                    {
                        upload.Template = null;
                    }
                }

                this.youtubeAccount = value;
                this.LastModified = DateTime.Now;
            }
        }

        public bool EnableAutomation
        {
            get => this.enableAutomation;
            set
            {
                if (value)
                {
                    if (this.automationSettings == null)
                    {
                        this.AutomationSettings = new AutomationSettings();
                    }
                }

                this.enableAutomation = value;
                this.LastModified = DateTime.Now;
            }
        }

        public AutomationSettings AutomationSettings
        {
            get => this.automationSettings;
            set
            {
                this.automationSettings = value;
                this.LastModified = DateTime.Now;
            }
        }

        #endregion properties

        [JsonConstructor]
        private Template()
        {
        }

        //for fake templates(e.g. All and None Templates for filter)
        public Template(string name)
        {
            this.name = name;
            this.isDummy = true;
            this.youtubeAccount = Template.DummyAccount;
        }

        public bool IsDummy
        {
            get => this.isDummy;
        }

        public Template(string name, string imagefilePath, TemplateMode templateMode, string rootFolderPath, string partOfFileName, TemplateList templateList, YoutubeAccount youtubeAccount)
        {
            if (templateList == null)
            {
                throw new ArgumentException("templateList must not be null.");
            }

            if (youtubeAccount == null)
            {
                throw new ArgumentException("youtubeAccount must not be null.");
            }

            this.guid = Guid.NewGuid();
            this.setName(name);
            this.imageFilePath = TemplateList.CopyTemplateImageToStorageFolder(imagefilePath);
            this.templateMode = templateMode;
            this.rootFolderPath = rootFolderPath;
            this.partOfFileName = partOfFileName;
            this.uploads = new List<Upload>();
            this.tags = new List<string>();
            this.created = DateTime.Now;
            this.lastModified = this.created;
            this.visibility = Visibility.Private;
            this.youtubeAccount = youtubeAccount;
        }

        public Template(Template template, string name, TemplateList templateList):
            this(name, template.ImageFilePathForEditing, template.TemplateMode, template.RootFolderPath, template.PartOfFileName, templateList, template.YoutubeAccount)
        {
            this.thumbnailFolderPath = template.ThumbnailFolderPath;
            this.thumbnailFallbackFilePath = template.ThumbnailFallbackFilePath;
            this.placeholderFolderPath = template.PlaceholderFolderPath;
            this.title = template.Title;
            this.description = template.Description;
            this.tags.AddRange(template.Tags);
            this.visibility = template.YtVisibility;

            this.usePublishAtSchedule = template.UsePublishAtSchedule;
            if (template.PublishAtSchedule != null)
            {
                this.publishAtSchedule = new Schedule(template.publishAtSchedule);
            }

            this.videoLanguage = template.VideoLanguage;
            this.category = template.Category;
            this.descriptionLanguage = template.DescriptionLanguage;
            this.playlist = template.Playlist;
            this.setPlaylistAfterPublication = template.SetPlaylistAfterPublication;

            this.enableAutomation = template.EnableAutomation;
            if(template.AutomationSettings != null)
            {
                this.automationSettings = new AutomationSettings(template.AutomationSettings);
            }
        }

        private void setName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                throw new ArgumentException("Template name must not be null or empty.", "name");
            }

            this.name = name;
        }

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
            if (upload == null)
            {
                this.uploads.RemoveAll(upl => upl == null);
            }
            else
            {
                this.uploads.Remove(upload);
            }
        }

        private void onThumbnailFallbackChanged(string oldValue)
        {
            OldValueHandler handler = this.ThumbnailFallbackFilePathChanged;
            if (handler != null)
            {
                handler(this, new OldValueArgs(oldValue));
            }
        }

        private void onImageFilePathChanged(string oldValue)
        {
            OldValueHandler handler = this.ImageFilePathForEditingChanged;
            if (handler != null)
            {
                handler(this, new OldValueArgs(oldValue));
            }
        }

        private void onIsDefaultChanged()
        {
            EventHandler handler = this.IsDefaultChanged;
            if (handler != null)
            {
                handler(this, new EventArgs());
            }
        }

        //sets new daily/weekly/monthly UploadedUntil on schedule
        public void SetScheduleProgress()
        {
            if (this.UsePublishAtSchedule && this.uploads.Count > 0 &&
                this.publishAtSchedule != null && this.publishAtSchedule.ScheduleFrequency != ScheduleFrequency.SpecificDate)
            {
                List<Upload> relevantUploads = this.uploads.FindAll(upload =>
                    (upload.UploadStart == null || upload.UploadStart > this.PublishAtSchedule.IgnoreUploadsBefore) &&
                    upload.UploadStatus == UplStatus.Finished);

                relevantUploads = relevantUploads.FindAll(upload => upload.PublishAt > this.publishAtSchedule.UploadedUntil);
                relevantUploads.Sort(Template.compareUploadPublishAtDates);

                DateTime nextDate = this.publishAtSchedule.GetNextDateTime(DateTime.MinValue, false);
                foreach (Upload upload in relevantUploads)
                {
                    if (upload.PublishAt == nextDate)
                    {
                        this.publishAtSchedule.UploadedUntil = nextDate;
                        nextDate = this.publishAtSchedule.GetNextDateTime(DateTime.MinValue, false);
                    }

                    if (upload.PublishAt > nextDate)
                    {
                        return;
                    }
                }
            }
        }

        public void SetStartDateOnTemplateSchedule(DateTime startDate)
        {
            if (this.PublishAtSchedule != null)
            {
                this.PublishAtSchedule.IgnoreUploadsBefore = DateTime.Now;
                this.PublishAtSchedule.StartDate = startDate;
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
