#region

using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;

#endregion

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
            this.deleteCommand = new GenericCommand(this.RemoveUpload);
        }

        //exposed for testing
        public void RemoveUpload(object parameter)
        {
            Upload upload = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter)).Upload;
            this.uploadList.RemoveUploads(upload2 => upload2 == upload);

            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeUploadList();
            JsonSerialization.SerializeTemplateList();
        }

        public void ReOrder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            this.uploadList.ReOrder(uploadToMove, uploadAtTargetPosition);
            this.observableUploadViewModels.ReOrder(this.uploadList);
            JsonSerialization.SerializeUploadList();
        }
    }
}
