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

        private MainWindowViewModel mainWindowViewModel;

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
            set
            {
                this.upload.UploadStatus = value;
                this.RaisePropertyChanged("UploadErrorMessage");
                this.RaisePropertyChanged("ShowUploadErrorIcon");
                this.RaisePropertyChanged("UploadStatus");
                this.RaisePropertyChanged("UploadStart");
                this.RaisePropertyChanged("UploadEnd");
                this.RaisePropertyChanged("ControlsEnabled");
            }
        }

        public string FilePath
        {
            get => this.upload.FilePath;
        }

        public Template Template
        {
            get => this.upload.Template;
            set
            {
                this.upload.Template = value;

                this.SerializeAllUploads();
                this.SerializeTemplateList();

                this.RaisePropertyChanged(null);
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

        public DateTime UploadStart
        {
            get => this.upload.UploadStart;
        }

        public DateTime UploadEnd
        {
            get => this.upload.UploadEnd;
        }

        public string PictureFilePath
        {
            get => this.upload.PictureFilePath;
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

                RaisePropertyChanged("YtTitle");
                RaisePropertyChanged("Title");
            }
        }

        public string Description
        {
            get => this.upload.Description;
            set
            {
                this.upload.Description = value;
                this.SerializeAllUploads();

                RaisePropertyChanged("Description");
            }
        }

        public string TagsAsString
        {
            get => string.Join(',', this.upload.Tags);
            set
            {
                this.upload.Tags = new List<string>(value.Split(','));
                this.SerializeAllUploads();

                RaisePropertyChanged("TagsAsString");
            }
        }

        public List<string> Tags
        {
            get => this.upload.Tags;
            set
            {
                this.upload.Tags = value;
                this.SerializeAllUploads();

                RaisePropertyChanged("TagsAsString");
            }
        }

        public Array YtVisibilities
        {
            get
            {
                return Enum.GetValues(typeof(YtVisibility));
            }
        }

        public YtVisibility Visibility
        {
            get => this.upload.Visibility;
            set
            {
                this.upload.Visibility = value;
                this.SerializeAllUploads();

                RaisePropertyChanged("Visibility");
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
                if (string.IsNullOrWhiteSpace(this.upload.ThumbnailPath) || File.Exists(this.upload.ThumbnailPath))
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

        public string ShowUploadErrorIcon
        {
            get
            {
                if (!(this.upload.UploadStatus == UplStatus.Failed))
                {
                    return "Collapsed";
                }

                return "Visible";
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

            RaisePropertyChanged("PublishAtTime");
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
                    return true; ;
                }
            }

            set
            {
                if (this.upload.PublishAt.Date == DateTime.MinValue)
                {
                    this.upload.SetPublishAtDate(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day + 1));
                }
                else
                {
                    this.upload.SetPublishAtDate(DateTime.MinValue);
                    this.upload.SetPublishAtTime(DateTime.MinValue);
                }

                this.SerializeAllUploads();

                RaisePropertyChanged("PublishAt");
                RaisePropertyChanged("PublishAtTime");
                RaisePropertyChanged("PublishAtDate");
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

                RaisePropertyChanged("PublishAtDate");
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
        public string ThumbnailPath
        {
            get
            {
                return this.upload.ThumbnailPath;
            }
            set
            {
                this.upload.ThumbnailPath = value;
                RaisePropertyChanged("ThumbnailPath");
            }
        }

        public UploadViewModel (Upload upload, MainWindowViewModel mainWindowViewModel)
        {
            this.upload = upload;
            this.mainWindowViewModel = mainWindowViewModel;

            this.quarterHourViewModels = new QuarterHourViewModels();

            this.resetStateCommand = new GenericCommand(resetUploadState);
            this.pauseCommand = new GenericCommand(setPausedUploadState);
            this.noTemplateCommand = new GenericCommand(setTemplateToNull);
            this.openFileDialogCommand = new GenericCommand(openThumbnailDialog);
            this.resetToTemplateValueCommand = new GenericCommand(resetToTemplateVuale);
        } 

        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
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
            this.UploadStatus = UplStatus.ReadyForUpload;
            JsonSerialization.SerializeAllUploads();
            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        private void setPausedUploadState(object parameter)
        {
            this.UploadStatus = UplStatus.Paused;
            JsonSerialization.SerializeAllUploads();

            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        private void setTemplateToNull(object parameter)
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Template cannot be removed if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            this.Template = null;
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
                this.ThumbnailPath = fileDialog.FileName;
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
                        RaisePropertyChanged("Title");
                        RaisePropertyChanged("YtTitle");
                        break;
                    case "description":
                        this.upload.CopyDescriptionFromTemplate();
                        RaisePropertyChanged("Description");
                        break;
                    case "tags":
                        this.upload.CopyTagsFromtemplate();
                        RaisePropertyChanged("TagsAsString");
                        break;
                    case "visibility":
                        this.upload.CopyVisbilityFromTemplate();
                        RaisePropertyChanged("Visibility");
                        break;
                    default:
                        this.upload.CopyTemplateValues();
                        RaisePropertyChanged(null);
                        break;
                }
            }
        }
    }
}
