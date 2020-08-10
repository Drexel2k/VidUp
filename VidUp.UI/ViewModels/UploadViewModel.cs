#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Runtime.Remoting.Messaging;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.Utils;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    class UploadViewModel : INotifyPropertyChanged
    {
        private Upload upload;
        private QuarterHourViewModels quarterHourViewModels;

        private GenericCommand pauseCommand;
        private GenericCommand resetStateCommand;
        private GenericCommand noTemplateCommand;
        private GenericCommand noPlaylistCommand;
        private GenericCommand openFileDialogCommand;
        private GenericCommand resetThumbnailCommand;
        private GenericCommand resetToTemplateValueCommand;

        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

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

        public GenericCommand NoTemplateCommand
        {
            get
            {
                return this.noTemplateCommand;
            }
        }

        public GenericCommand NoPlaylistCommand
        {
            get
            {
                return this.noPlaylistCommand;
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

        public string FilePath
        {
            get => this.upload.FilePath;
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

                this.SerializeAllUploads();
                JsonSerialization.JsonSerializer.SerializeTemplateList();

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
                if (this.upload.UploadStart > DateTime.MinValue)
                {
                    return this.upload.UploadStart.ToString("g");
                }

                return null;
            }
        }

        public string UploadEnd
        {
            get
            {
                if (this.upload.UploadEnd > DateTime.MinValue)
                {
                    return this.upload.UploadEnd.ToString("g");
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

        public string YtTitle
        {
            get => this.upload.YtTitle;
        }

        public string Title
        {
            get => this.upload.Title;
            set
            {
                this.upload.Title = value;
                this.SerializeAllUploads();

                this.raisePropertyChanged("YtTitle");
                this.raisePropertyChanged("Title");
            }
        }

        public string Description
        {
            get => this.upload.Description;
            set
            {
                this.upload.Description = value;
                this.SerializeAllUploads();

                this.raisePropertyChanged("Description");
            }
        }

        public string TagsAsString
        {
            get => string.Join(",", this.upload.Tags);
            set
            {
                this.upload.Tags = new List<string>(value.Split(','));
                this.SerializeAllUploads();

                this.raisePropertyChanged("TagsAsString");
            }
        }

        public List<string> Tags
        {
            get => this.upload.Tags;
            set
            {
                this.upload.Tags = value;
                this.SerializeAllUploads();

                this.raisePropertyChanged("TagsAsString");
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
        }

        public Visibility Visibility
        {
            get => this.upload.Visibility;
            set
            {
                if (value != Visibility.Private && this.Visibility == Visibility.Private)
                {
                    if (this.PublishAt)
                    {
                        this.PublishAt = false;
                    }
                }

                this.upload.Visibility = value;

                this.SerializeAllUploads();

                this.raisePropertyChanged("Visibility");
            }
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

                this.SerializeAllUploads();
                this.raisePropertyChanged("SelectedPlaylist");
            }
        }

        public string FileSizeInMegaByte
        {
            get => $"{((float)this.upload.FileLength / Constants.ByteMegaByteFactor).ToString("N0", CultureInfo.CurrentCulture)} MB";
        }

        public string UploadedInMegaByte
        {
            get => $"{((float)this.upload.BytesSent / Constants.ByteMegaByteFactor).ToString("N0", CultureInfo.CurrentCulture)} MB";
        }

        public string ShowFileNotExistsIcon
        {
            get
            {
                if (File.Exists(this.upload.FilePath))
                {
                    return "Collapsed";
                }

                return "Visible";
            }
        }

        public string ShowThumbnailNotExistsIcon
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.upload.ThumbnailFilePath) || File.Exists(this.upload.ThumbnailFilePath))
                {
                    return "Collapsed";
                }

                return "Visible";
            }
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
                if (!(this.upload.UploadStatus == UplStatus.Finished))
                {
                    return true;
                }

                return false;
            }
        }


        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.upload.SetPublishAtTime(quarterHour);

            this.SerializeAllUploads();
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
                if (this.upload.PublishAt.Date == DateTime.MinValue)
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
                if (value && !this.PublishAt)
                {
                    if (this.Visibility != Visibility.Private)
                    {
                        this.Visibility = Visibility.Private;
                    }
                }

                if (this.upload.PublishAt.Date == DateTime.MinValue)
                {
                    this.upload.SetPublishAtDate(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day).AddDays(1));
                }
                else
                {
                    this.upload.SetPublishAtDate(DateTime.MinValue);
                    this.upload.SetPublishAtTime(new TimeSpan());
                }

                this.SerializeAllUploads();

                this.raisePropertyChanged("PublishAt");
                this.raisePropertyChanged("PublishAtTime");
                this.raisePropertyChanged("PublishAtDate");
            }
        }

        public QuarterHourViewModel PublishAtTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.upload.PublishAt.TimeOfDay);
            }
            set
            {
                if (this.Upload.PublishAtTime != value.QuarterHour)
                {
                    this.SetPublishAtTime(value.QuarterHour.Value);
                }
            }
        }

        public DateTime PublishAtDate
        {
            get
            {
                if (this.upload.PublishAt.Date == DateTime.MinValue)
                {
                    return DateTime.Now.AddDays(1);
                }
                else
                {
                    return this.upload.PublishAt;
                }
            }
            set
            {
                this.upload.SetPublishAtDate(value);
                this.SerializeAllUploads();

                this.raisePropertyChanged("PublishAtDate");
            }
        }

        public DateTime PublishAtFirstDate
        {
            get
            {
                return DateTime.Now.AddDays(1);
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
                this.raisePropertyChanged("ThumbnailFilePath");
            }
        }

        public UploadViewModel (Upload upload, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels)
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

            this.quarterHourViewModels = new QuarterHourViewModels(false);

            this.resetStateCommand = new GenericCommand(this.resetUploadState);
            this.pauseCommand = new GenericCommand(this.setPausedUploadState);
            this.noTemplateCommand = new GenericCommand(this.setTemplateToNull);
            this.noPlaylistCommand = new GenericCommand(this.setPlaylistToNull);
            this.openFileDialogCommand = new GenericCommand(this.openThumbnailDialog);
            this.resetThumbnailCommand = new GenericCommand(this.resetThumbnail);
            this.resetToTemplateValueCommand = new GenericCommand(this.resetToTemplateVuale);
        }
        private void uploadPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UploadStatus")
            {
                this.raisePropertyChanged("UploadErrorMessage");
                this.raisePropertyChanged("ShowUploadErrorIcon");
                this.raisePropertyChanged("UploadStatus");
                this.raisePropertyChanged("UploadStart");
                this.raisePropertyChanged("UploadEnd");
                this.raisePropertyChanged("ControlsEnabled");

                return;
            }

            if (e.PropertyName == "BytesSent")
            {
                this.raisePropertyChanged("UploadedInMegaByte");
            }
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

        public void SerializeAllUploads()
        {
            JsonSerialization.JsonSerializer.SerializeAllUploads();
        }

        private void resetUploadState(object parameter)
        {
            this.upload.UploadStatus = UplStatus.ReadyForUpload;
            JsonSerialization.JsonSerializer.SerializeAllUploads();
        }

        private void setPausedUploadState(object parameter)
        {
            this.upload.UploadStatus = UplStatus.Paused;
            JsonSerialization.JsonSerializer.SerializeAllUploads();
        }

        private void setTemplateToNull(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Template cannot be removed if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            this.SelectedTemplate = null;
            JsonSerialization.JsonSerializer.SerializeAllUploads();
            JsonSerialization.JsonSerializer.SerializeTemplateList();
        }

        private void setPlaylistToNull(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Template cannot be removed if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            this.SelectedPlaylist = null;
            JsonSerialization.JsonSerializer.SerializeAllUploads();
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
                JsonSerialization.JsonSerializer.SerializeAllUploads();
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


        private void resetToTemplateVuale(object parameter)
        {
            if (this.upload.Template != null)
            { 
                switch (parameter)
                {
                    case "title":
                        this.upload.CopyTitleFromTemplate();
                        raisePropertyChanged("Title");
                        raisePropertyChanged("YtTitle");
                        break;
                    case "description":
                        this.upload.CopyDescriptionFromTemplate();
                        raisePropertyChanged("Description");
                        break;
                    case "tags":
                        this.upload.CopyTagsFromtemplate();
                        raisePropertyChanged("TagsAsString");
                        break;
                    case "visibility":
                        this.upload.CopyVisbilityFromTemplate();
                        raisePropertyChanged("Visibility");
                        break;
                    case "playlist":
                        this.upload.CopyPlaylistFromTemplate();
                        raisePropertyChanged("PlaylistId");
                        break;
                    default:
                        this.upload.CopyTemplateValues();
                        raisePropertyChanged(null);
                        break;
                }

                JsonSerialization.JsonSerializer.SerializeAllUploads();
            }
        }
    }
}
