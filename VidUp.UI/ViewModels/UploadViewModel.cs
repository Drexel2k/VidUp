using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Media.Imaging;

namespace Drexel.VidUp.UI.ViewModels
{
    class UploadViewModel : INotifyPropertyChanged
    {
        private Upload upload;
        private QuarterHourViewModels quarterHourViewModels;

        private GenericCommand pauseCommand;
        private GenericCommand resetStateCommand;
        private GenericCommand noTemplateCommand;
        private GenericCommand openFileDialogCommand;
        private GenericCommand resetToTemplateValueCommand;

        private ObservableTemplateViewModels observableTemplateViewModels;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public GenericCommand OpenFileDialogCommand
        {
            get
            {
                return this.openFileDialogCommand;
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
                //combobox handling initiated in code behind
                this.upload.Template = value.Template;
                
                this.SerializeAllUploads();
                this.SerializeTemplateList();

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


        public void SetPublishAtTime(DateTime quarterHour)
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
                    this.upload.SetPublishAtTime(DateTime.MinValue);
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
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.upload.PublishAt);
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

        public UploadViewModel (Upload upload, ObservableTemplateViewModels observableTemplateViewModels)
        {
            this.upload = upload;
            this.observableTemplateViewModels = observableTemplateViewModels;
            this.upload.PropertyChanged += uploadPropertyChanged;

            this.quarterHourViewModels = new QuarterHourViewModels();

            this.resetStateCommand = new GenericCommand(resetUploadState);
            this.pauseCommand = new GenericCommand(setPausedUploadState);
            this.noTemplateCommand = new GenericCommand(setTemplateToNull);
            this.openFileDialogCommand = new GenericCommand(openThumbnailDialog);
            this.resetToTemplateValueCommand = new GenericCommand(resetToTemplateVuale);
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
            JsonSerialization.SerializeAllUploads();
        }

        public void SerializeTemplateList()
        {
            JsonSerialization.SerializeTemplateList();
        }

        private void resetUploadState(object parameter)
        {
            this.upload.UploadStatus = UplStatus.ReadyForUpload;
            JsonSerialization.SerializeAllUploads();
        }

        private void setPausedUploadState(object parameter)
        {
            this.upload.UploadStatus = UplStatus.Paused;
            JsonSerialization.SerializeAllUploads();
        }

        private void setTemplateToNull(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Template cannot be removed if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            this.SelectedTemplate = null;
            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeTemplateList();
        }

        private void openThumbnailDialog(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            OpenFileDialog fileDialog = new OpenFileDialog();
            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                this.ThumbnailFilePath = fileDialog.FileName;
                JsonSerialization.SerializeAllUploads();
            }
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
                    default:
                        this.upload.CopyTemplateValues();
                        raisePropertyChanged(null);
                        break;
                }

                JsonSerialization.SerializeAllUploads();
            }
        }
    }
}
