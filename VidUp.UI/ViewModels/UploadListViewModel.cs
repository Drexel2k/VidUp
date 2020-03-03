using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;

namespace Drexel.VidUp.UI.ViewModels
{
    class UploadListViewModel
    {
        private UploadList uploadList;
        private TemplateList templateList;
        private MainWindowViewModel mainWindowViewModel;

        private GenericCommand deleteCommand;
        private GenericCommand pauseCommand;
        private GenericCommand resetStateCommand;
        private GenericCommand noTemplateCommand;
        private GenericCommand openFileDialogCommand;

        private ObservableUploadViewModels observableUploadViewModels;

        public TemplateList TemplateList
        {
            get
            {
                return this.templateList;
            }
        }

        public GenericCommand DeleteCommand
        {
            get
            {
                return this.deleteCommand;
            }
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

        public ObservableUploadViewModels ObservableUploadViewModels
        {
            get
            {
                return this.observableUploadViewModels;
            }
        }

        public UploadListViewModel(UploadList uploadList, TemplateList templateList, MainWindowViewModel mainWindowViewModel)
        {
            this.uploadList = uploadList;
            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList);
            this.mainWindowViewModel = mainWindowViewModel;

            this.templateList = templateList;
            this.deleteCommand = new GenericCommand(deleteUpload);
            this.resetStateCommand = new GenericCommand(resetUploadState);
            this.pauseCommand = new GenericCommand(setPausedUploadState);
            this.noTemplateCommand = new GenericCommand(setTemplateToNull);
            this.openFileDialogCommand = new GenericCommand(openThumbnailDialog);
        }

        internal void AddUploads(List<Upload> uploads, TemplateList templateList)
        {
            this.uploadList.AddUploads(uploads, templateList);
            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeUploadList();
            JsonSerialization.SerializeTemplateList();
            this.observableUploadViewModels.AddUploads(uploads);

            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        internal void RemoveAllUploaded()
        {
            List<Upload> uploads = this.uploadList.GetUploads(upload => upload.UploadStatus == UplStatus.Finished);
            foreach (Upload upload in uploads)
            {
                this.uploadList.Remove(upload);
                this.ObservableUploadViewModels.Remove(upload);
            }

            JsonSerialization.SerializeUploadList();
        }

        private void deleteUpload(object parameter)
        {
            Upload upload = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter)).Upload;

            this.uploadList.Remove(upload);

            if(upload.UploadStatus != UplStatus.Finished && upload.Template != null)
            {
                upload.Template = null;
            }

            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeUploadList();
            JsonSerialization.SerializeTemplateList();
            this.observableUploadViewModels.Remove(upload);

            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        private void resetUploadState(object parameter)
        {
            UploadViewModel uploadViewModel = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter));
            uploadViewModel.UploadStatus = UplStatus.ReadyForUpload;
            JsonSerialization.SerializeAllUploads();

            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        private void setPausedUploadState(object parameter)
        {
            UploadViewModel uploadViewModel = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter));
            uploadViewModel.UploadStatus = UplStatus.Paused;
            JsonSerialization.SerializeAllUploads();

            this.mainWindowViewModel.SumTotalBytesToUpload();
        }

        private void setTemplateToNull(object parameter)
        {
            UploadViewModel uploadViewModel = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter));
            if(uploadViewModel.UploadStatus ==  UplStatus.Finished)
            {
                MessageBox.Show("Template cannot be removed if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            uploadViewModel.Template = null;
            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeTemplateList();
        }

        private void openThumbnailDialog(object parameter)
        {
            UploadViewModel uploadViewModel = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter));
            if (uploadViewModel.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            OpenFileDialog fileDialog = new OpenFileDialog();
            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                uploadViewModel.ThumbnailPath = fileDialog.FileName;
                JsonSerialization.SerializeAllUploads();
            }


        }

        public void RemoveTemplateFromUploads(Template template)
        {
            this.observableUploadViewModels.RemoveTemplateFromUploads(template);
        }

        public void SetUploadStatus(Guid guid, UplStatus uploadStatus)
        {
            UploadViewModel upload = this.observableUploadViewModels.GetUploadByGuid(guid);
            upload.UploadStatus = uploadStatus;
        }
    }
}
