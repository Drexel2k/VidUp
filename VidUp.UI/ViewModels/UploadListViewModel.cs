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
        //needed for template combobox in upload control
        private ObservableTemplateViewModels observableTemplateViewModels;

        private GenericCommand deleteCommand;
        private ObservableUploadViewModels observableUploadViewModels;

        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels;
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

        public UploadListViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels)
        {
            this.uploadList = uploadList;
            this.observableTemplateViewModels = observableTemplateViewModels;
            
            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, this.observableTemplateViewModels);
            this.deleteCommand = new GenericCommand(RemoveUpload);
        }

        public void RemoveAllUploaded()
        {
            this.uploadList.RemoveUploads(upload => upload.UploadStatus == UplStatus.Finished);
            JsonSerialization.SerializeUploadList();
        }

        //exposed for testing
        public void RemoveUpload(object parameter)
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
        }
    }
}
