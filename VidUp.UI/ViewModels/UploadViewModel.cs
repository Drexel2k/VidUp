using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Drexel.VidUp.UI.ViewModels
{
    class UploadViewModel : INotifyPropertyChanged
    {
        private Upload upload;

        public event PropertyChangedEventHandler PropertyChanged;

        public string Guid
        {
            get => this.upload.Guid.ToString();
        }

        public UplStatus UploadStatus
        {
            get => this.upload.UploadStatus;
            set
            {
                this.upload.UploadStatus = value;
                this.RaisePropertyChanged("UploadStatus");
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
        public Upload Upload { get => this.upload;  }

        public UploadViewModel (Upload upload)
        {
            this.upload = upload;
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
    }
}
