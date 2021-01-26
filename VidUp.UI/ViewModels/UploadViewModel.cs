using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.UI.ViewModels
{
    public class UploadViewModel : INotifyPropertyChanged
    {
        private Upload upload;
        private QuarterHourViewModels quarterHourViewModels;

        private GenericCommand pauseCommand;
        private GenericCommand resetStateCommand;
        private GenericCommand removeComboBoxValueCommand;
        private GenericCommand openFileDialogCommand;
        private GenericCommand resetThumbnailCommand;
        private GenericCommand resetToTemplateValueCommand;

        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        //need for determination of upload status color
        private bool resumeUploads;

        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels;
            }
        }

        public ObservablePlaylistViewModels ObservablePlaylistViewModels
        {
            get
            {
                return this.observablePlaylistViewModels;
            }
        }

        public string Guid
        {
            get => this.upload.Guid.ToString();
        }

        public GenericCommand PauseCommand
        {
            get
            {
                return this.pauseCommand;
            }
        }

        public GenericCommand ResetStateCommand
        {
            get
            {
                return this.resetStateCommand;
            }
        }

        public GenericCommand RemoveComboBoxValueCommand
        {
            get
            {
                return this.removeComboBoxValueCommand;
            }
        }

        public GenericCommand OpenFileDialogCommand
        {
            get
            {
                return this.openFileDialogCommand;
            }
        }

        public GenericCommand ResetThumbnailCommand
        {
            get
            {
                return this.resetThumbnailCommand;
            }
        }

        public GenericCommand ResetToTemplateValueCommand
        {
            get
            {
                return this.resetToTemplateValueCommand;
            }
        }

        public UplStatus UploadStatus
        {
            get => this.upload.UploadStatus;
        }

        public SolidColorBrush UploadStatusColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.UploadStatus == UplStatus.Finished || this.upload.UploadStatus == UplStatus.Uploading)
                {
                    color = Colors.Lime;
                }

                if (this.upload.UploadStatus == UplStatus.ReadyForUpload)
                {
                    color = Colors.Gold;
                }

                if (this.upload.UploadStatus == UplStatus.Failed || this.upload.UploadStatus == UplStatus.Stopped)
                {
                    if (this.resumeUploads)
                    {
                        color = Colors.Gold;
                    }
                    else
                    {
                        color = Colors.Transparent;
                    }
                }

                if (this.upload.UploadStatus == UplStatus.Paused)
                {
                    color = Colors.Transparent;
                }

                return new SolidColorBrush(color);
            }
        }

        public bool UploadStatusColorAnimation
        {
            get
            {
                if ( this.upload.UploadStatus == UplStatus.Uploading)
                {
                    return true;
                }

                return false;
            }
        }


        public string FilePath
        {
            get => this.upload.FilePath;
        }

        public bool StateCommandsEnabled
        {
            get
            {
                if (this.UploadStatus != UplStatus.Uploading)
                {
                    return true;
                }

                return false;
            }
        }

        public bool ResetStateCommandEnabled
        {
            get
            {
                return this.StateCommandsEnabled && this.upload.VerifyForUpload();
            }
        }

        public TemplateComboboxViewModel SelectedTemplate
        {
            get => this.observableTemplateViewModels.GetViewModel(this.upload.Template);
            set
            {
                if (value == null)
                {
                    this.upload.Template = null;
                }
                else
                {
                    this.upload.Template = value.Template;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                //if template changes all values are set to template values
                this.raisePropertyChanged(null);
            }
        }

        public DateTime Created
        {
            get => this.upload.Created;
        }

        public DateTime LastModified
        {
            get => this.upload.LastModified;
        }

        public string UploadStart
        {
            get
            {
                if (this.upload.UploadStart != null)
                {
                    return this.upload.UploadStart.Value.ToString("g");
                }

                return null;
            }
        }

        public string UploadEnd
        {
            get
            {
                if (this.upload.UploadEnd != null)
                {
                    return this.upload.UploadEnd.Value.ToString("g");
                }

                return null;
            }
        }

        public BitmapImage ImageBitmap
        {
            get
            {
                if (File.Exists(this.upload.ImageFilePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(this.upload.ImageFilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    return bitmap;
                }

                return null;
            }
        }

        public string Title
        {
            get => this.upload.Title;
            set
            {
                this.upload.Title = value;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("Title");
                this.raisePropertyChanged("YtTitle");
                this.raisePropertyChanged("TitleColor");
                this.raisePropertyChanged("TitleCharacterCount");
            }
        }

        public string YtTitle
        {
            get
            {
                if (this.upload.Title.Length <= YoutubeLimits.TitleLimit)
                {
                    return this.upload.Title;
                }
                else
                {
                    return this.upload.Title.Substring(0, YoutubeLimits.TitleLimit);
                }
            }
        }

        public string TitleCharacterCount
        {
            get
            {
                return this.upload.Title.Length.ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        public SolidColorBrush TitleColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.Title.Length > YoutubeLimits.TitleLimit)
                {
                    color = Colors.Red;
                }
                
                return new SolidColorBrush(color);
            }
        }


        public string Description
        {
            get => this.upload.Description;
            set
            {
                this.upload.Description = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("Description");
                this.raisePropertyChanged("DescriptionColor");
                this.raisePropertyChanged("DescriptionCharacterCount");
            }
        }

        public string DescriptionCharacterCount
        {
            get
            {
                return this.upload.Description != null ? 
                    this.upload.Description.Length.ToString("N0", CultureInfo.CurrentCulture) : 0.ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        public SolidColorBrush DescriptionColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.Description != null && this.upload.Description.Length > YoutubeLimits.DescriptionLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public string MaxDescriptionCharacters
        {
            get
            {
                return $"/ {YoutubeLimits.DescriptionLimit.ToString("N0", CultureInfo.CurrentCulture)}";
            }
        }


        public string TagsAsString
        {
            get => string.Join(",", this.upload.Tags);
            set
            {
                this.upload.Tags.Clear();
                this.upload.Tags.AddRange(value.Split(','));
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("TagsAsString");
                this.raisePropertyChanged("TagsColor");
                this.raisePropertyChanged("TagsCharacterCount");
            }
        }

        public string TagsCharacterCount
        {
            get
            {
                return this.upload.TagsCharacterCount.ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        public SolidColorBrush TagsColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.TagsCharacterCount > YoutubeLimits.TagsLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
        }

        public Visibility SelectedVisibility
        {
            get => this.upload.Visibility;
            set
            {
                this.setSelectedVisibilityInternal(value, true);
            }
        }

        private void setSelectedVisibilityInternal(Visibility value, bool serialize)
        {
            if (value != Visibility.Private && this.SelectedVisibility == Visibility.Private)
            {
                if (this.PublishAt)
                {
                    this.setPublishAtInternal(false, false);
                }
            }

            this.upload.Visibility = value;

            if (serialize)
            {
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            }

            this.raisePropertyChanged("SelectedVisibility");
        }

        public PlaylistComboboxViewModel SelectedPlaylist
        {
            get => this.observablePlaylistViewModels.GetViewModel(this.upload.Playlist);
            set
            {
                if (value == null)
                {
                    this.upload.Playlist = null;
                }
                else
                {
                    this.upload.Playlist = value.Playlist;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("ShowPlaylistHint");
                this.raisePropertyChanged("SelectedPlaylist");
            }
        }

        public string FileSizeInMegaByte
        {
            get => $"{((float)this.upload.FileLength / Constants.ByteMegaByteFactor).ToString("N0", CultureInfo.CurrentCulture)} MB";
        }

        public SolidColorBrush FileSizeColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.FileLength > YoutubeLimits.FileSizeLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public string UploadedInMegaByte
        {
            get => $"{((float)this.upload.BytesSent / Constants.ByteMegaByteFactor).ToString("N0", CultureInfo.CurrentCulture)} MB";
        }

        public string UploadErrorMessage
        {
            get
            {
                return this.upload.UploadErrorMessage;
            }
        }

        public bool ControlsEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.upload.ResumableSessionUri))
                {
                    return true;
                }

                return false;
            }
        }


        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.upload.SetPublishAtTime(quarterHour);
            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            this.raisePropertyChanged("PublishAtTime");
        }

        public QuarterHourViewModels QuarterHourViewModels
        {
            get
            {
                return this.quarterHourViewModels;
            }
        }
        public bool PublishAt
        {
            get
            {
                if (this.upload.PublishAt == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            set
            {
                this.setPublishAtInternal(value, true);
            }
        }

        private void setPublishAtInternal(bool value, bool serialize)
        {
            if (value && !this.PublishAt)
            {
                if (this.SelectedVisibility != Visibility.Private)
                {
                    this.SelectedVisibility = Visibility.Private;
                }
            }

            if (value)
            {
                //default publish at is 24 hour in the future
                this.upload.PublishAt = QuarterHourCalculator.GetRoundedToNextQuarterHour
                (new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, DateTime.Now.Hour,
                    DateTime.Now.Minute, 0).AddHours(24));
            }
            else
            {
                this.upload.PublishAt = null;
            }

            if (serialize)
            {
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            }

            this.raisePropertyChanged("PublishAt");
            this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
            this.raisePropertyChanged("PublishAtTime");
            this.raisePropertyChanged("PublishAtDate");
        }

        public QuarterHourViewModel PublishAtTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.upload.PublishAt != null ? this.upload.PublishAt.Value.TimeOfDay : TimeSpan.MinValue);
            }
            set
            {
                if (this.Upload.PublishAt.Value.TimeOfDay != value.QuarterHour)
                {
                    this.SetPublishAtTime(value.QuarterHour.Value);
                }
            }
        }

        public DateTime? PublishAtDate
        {
            get
            {
                return this.upload.PublishAt;
            }
            set
            {
                this.upload.SetPublishAtDate(value.Value);
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("PublishAtDate");
            }
        }

        public bool PublishAtDateTimeControlsEnabled
        {
            get
            {
                if (!this.ControlsEnabled)
                {
                    return false;
                }
                else
                {
                    return this.upload.PublishAt != null;
                }
            }
        }

        public Upload Upload { get => this.upload;  }
        public string ThumbnailFilePath
        {
            get
            {
                return this.upload.ThumbnailFilePath;
            }
            set
            {
                string oldFilePath = this.upload.ThumbnailFilePath;
                this.upload.ThumbnailFilePath = value;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("ThumbnailFilePath");
            }
        }

        public bool ResumeUploads
        {
            set
            {
                this.resumeUploads = value; 
                this.raisePropertyChanged("UploadStatusColor");
            }
        }

        public List<CultureInfo> VideoLanguages
        {
            get => Cultures.RelevantCultureInfos;
        }

        public CultureInfo SelectedVideoLanguage
        {
            get => this.upload.VideoLanguage;
            set
            {
                this.upload.VideoLanguage = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("SelectedVideoLanguage");
            }
        }

        public Category[] Categories
        {
            get => Category.Categories;
        }

        public Category SelectedCategory
        {
            get => this.upload.Category;
            set
            {
                this.upload.Category = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("SelectedCategory");
            }
        }

        public string ShowPlaylistHint
        {
            get
            {
                if (this.upload.Playlist == null && this.upload.Template != null && this.upload.Template.SetPlaylistAfterPublication && this.upload.Template.Playlist != null)
                {
                    return "Visible";
                }

                return "Collapsed";
            }
        }

        public UploadViewModel (Upload upload, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels, bool resumeUploads)
        {
            if (observableTemplateViewModels == null)
            {
                throw new ArgumentException("ObservableTemplateViewModels must not be null.");
            }

            if (observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels must not be null.");
            }

            this.upload = upload;
            this.upload.PropertyChanged += this.uploadPropertyChanged;

            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;
            this.resumeUploads = resumeUploads;

            this.quarterHourViewModels = new QuarterHourViewModels(false);

            this.resetStateCommand = new GenericCommand(this.resetUploadStateCommand);
            this.pauseCommand = new GenericCommand(this.setPausedUploadState);
            this.removeComboBoxValueCommand = new GenericCommand(this.removeComboBoxValue);
            this.openFileDialogCommand = new GenericCommand(this.openThumbnailDialog);
            this.resetThumbnailCommand = new GenericCommand(this.resetThumbnail);
            this.resetToTemplateValueCommand = new GenericCommand(this.resetToTemplateValue);
        }

        private void removeComboBoxValue(object parameter)
        {
            if (!this.ControlsEnabled)
            {
                MessageBox.Show("Upload cannot be modified if upload is started.");
                return;
            }

            switch (parameter)
            {
                case "template":
                    this.SelectedTemplate = null;
                    break;
                case "playlist":
                    this.SelectedPlaylist = null;
                    break;
                case "videolanguage":
                    this.SelectedVideoLanguage = null;
                    break;
                case "category":
                    this.SelectedCategory = null;
                    break;
                default:
                    throw new InvalidOperationException("No parameter for removeComboBoxValue specified.");
                    break;
            }
        }

        private void uploadPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UploadStatus")
            {
                this.raisePropertyChanged("UploadErrorMessage");
                this.raisePropertyChanged("ShowUploadErrorIcon");
                this.raisePropertyChanged("UploadStatus");
                this.raisePropertyChanged("UploadStatusColor");
                this.raisePropertyChanged("UploadStatusColorAnimation");
                this.raisePropertyChanged("UploadStart");
                this.raisePropertyChanged("UploadEnd");
                this.raisePropertyChanged("StateCommandsEnabled");
                this.raisePropertyChanged("ResetStateCommandEnabled");
            }

            if (e.PropertyName == "ResumableSessionUri")
            {
                this.raisePropertyChanged("ControlsEnabled");
            }

            if (e.PropertyName == "BytesSent")
            {
                this.raisePropertyChanged("UploadedInMegaByte");
            }

            if (e.PropertyName == "PublishAt")
            {
                this.raisePropertyChanged("PublishAt");
                this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
                this.raisePropertyChanged("PublishAtTime");
                this.raisePropertyChanged("PublishAtDate");
            }

            if (e.PropertyName == "Title")
            {
                this.raisePropertyChanged("ResetStateCommandEnabled");
            }

            if (e.PropertyName == "Description")
            {
                this.raisePropertyChanged("ResetStateCommandEnabled");
            }

            if (e.PropertyName == "Tags")
            {
                this.raisePropertyChanged("ResetStateCommandEnabled");
            }
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

        private void resetUploadStateCommand(object parameter)
        {
            this.resetUploadState();
        }

        private bool resetUploadState()
        {
            if (!this.upload.VerifyForUpload())
            {
                return false;
            }

            if (this.upload.BytesSent > 0 && this.upload.UploadStatus != UplStatus.Stopped &&
                this.upload.UploadStatus != UplStatus.Finished)
            {
                this.upload.UploadStatus = UplStatus.Stopped;
            }
            else
            {
                this.upload.UploadStatus = UplStatus.ReadyForUpload;
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            return true;
        }

        private void setPausedUploadState(object parameter)
        {
            this.upload.UploadStatus = UplStatus.Paused;
            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
        }

        private void openThumbnailDialog(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            OpenFileDialog fileDialog = new OpenFileDialog();

            if (!string.IsNullOrWhiteSpace(this.ThumbnailFilePath))
            {
                fileDialog.InitialDirectory = Path.GetDirectoryName(this.ThumbnailFilePath);
            }
            else
            {
                if (this.upload.Template != null)
                {
                    if (!string.IsNullOrWhiteSpace(this.upload.Template.ThumbnailFolderPath))
                    {
                        fileDialog.InitialDirectory = this.upload.Template.ThumbnailFolderPath;
                    }
                    else 
                    {
                        if (!string.IsNullOrWhiteSpace(this.upload.Template.RootFolderPath))
                        {
                            fileDialog.InitialDirectory = this.upload.Template.RootFolderPath;
                        }
                    }
                }
            }

            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                this.ThumbnailFilePath = fileDialog.FileName;
            }
        }

        private void resetThumbnail(object obj)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            this.ThumbnailFilePath = null;
        }

        private void resetToTemplateValue(object parameter)
        {
            if (this.upload.Template != null)
            { 
                switch (parameter)
                {
                    case "title":
                        this.upload.CopyTitleFromTemplate();
                        this.raisePropertyChanged("Title");
                        this.raisePropertyChanged("YtTitle");
                        break;
                    case "description":
                        this.upload.CopyDescriptionFromTemplate();
                        this.raisePropertyChanged("Description");
                        break;
                    case "tags":
                        this.upload.CopyTagsFromtemplate();
                        this.raisePropertyChanged("TagsAsString");
                        break;
                    case "visibility":
                        this.upload.CopyVisibilityFromTemplate();
                        this.raisePropertyChanged("Visibility");
                        break;
                    case "playlist":
                        this.upload.CopyPlaylistFromTemplate();
                        this.raisePropertyChanged("PlaylistId");
                        break;
                    case "videolanguage":
                        this.upload.CopyVideoLanguageFromTemplate();
                        this.raisePropertyChanged("VideoLanguage");
                        break;
                    case "category":
                        this.upload.CopyCategoryFromTemplate();
                        this.raisePropertyChanged("Category");
                        break;
                    case "all":
                        this.upload.CopyTemplateValues();
                        this.raisePropertyChanged(null);
                        break;
                    default:
                        throw new InvalidOperationException("No parameter for resetToTemplateValue specified.");
                        break;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            }
        }
    }
}
