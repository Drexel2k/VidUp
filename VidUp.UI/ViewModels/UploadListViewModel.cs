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
            this.mainWindowViewModel = mainWindowViewModel;
            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, this.mainWindowViewModel);

            this.templateList = templateList;
            this.deleteCommand = new GenericCommand(deleteUpload);
        }

        public void AddUploads(List<Upload> uploads, TemplateList templateList)
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
