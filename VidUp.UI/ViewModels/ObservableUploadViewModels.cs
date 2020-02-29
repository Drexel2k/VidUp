using Drexel.VidUp.Business;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace Drexel.VidUp.UI.ViewModels
{
    class ObservableUploadViewModels : INotifyCollectionChanged, IEnumerable<UploadViewModel>
    {
        private List<UploadViewModel> uploadViewModels;

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservableUploadViewModels(UploadList uploadList)
        {
            this.uploadViewModels = new List<UploadViewModel>();

            if (uploadList != null)
            {
                foreach (Upload upload in uploadList)
                {
                    this.uploadViewModels.Add(new UploadViewModel(upload));
                }
            }
        }


        private void RaiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            if (this.CollectionChanged != null)
            {
                this.CollectionChanged(this, args);
            }
        }

        public IEnumerator<UploadViewModel> GetEnumerator()
        {
            return this.uploadViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public void AddUploads(List<Upload> uploads)
        {
            List<UploadViewModel> newViewModels = new List<UploadViewModel>();
            foreach(Upload upload in uploads)
            {
                newViewModels.Add(new UploadViewModel(upload));
            }

            this.uploadViewModels.AddRange(newViewModels);
            this.RaiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
        }

        public void Remove(Upload upload)
        {
            int position = this.uploadViewModels.FindIndex(uvm => uvm.Guid == upload.Guid.ToString());
            UploadViewModel uploadViewModel = this.uploadViewModels[position];
            this.uploadViewModels.Remove(uploadViewModel);
            this.RaiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, uploadViewModel, position));
        }

        public UploadViewModel GetUploadByGuid(Guid guid)
        {
            return this.uploadViewModels.Find(uploadviewModel => uploadviewModel.Guid == guid.ToString());
        }

        public void RemoveTemplateFromUploads(Template template)
        {
            foreach (UploadViewModel uploadViewModel in this.uploadViewModels)
            {
                if (uploadViewModel.Template == template)
                {
                    uploadViewModel.Template = null;
                }
            }
        }
    }
}
