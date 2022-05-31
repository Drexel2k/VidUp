using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using System.Windows.Controls;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservableUploadViewModels : INotifyCollectionChanged, IEnumerable<UploadViewModel>
    {
        private List<UploadViewModel> uploadViewModels;
        public event NotifyCollectionChangedEventHandler CollectionChanged;

        private ObservableTemplateViewModels observableTemplateViewModels;

        private bool resumeUploads;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        public bool ResumeUploads
        {
            set
            {
                this.resumeUploads = value;
                foreach (UploadViewModel uploadViewModel in this.uploadViewModels)
                {
                    uploadViewModel.ResumeUploads = this.resumeUploads;
                }
            }
        }

        public ObservableUploadViewModels(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels, bool resumeUploads, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels)
        {
            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;
            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.resumeUploads = resumeUploads;

            this.uploadViewModels = new List<UploadViewModel>();
            
            if (uploadList != null)
            {
                foreach (Upload upload in uploadList)
                {
                    this.uploadViewModels.Add(new UploadViewModel(upload, this.observableTemplateViewModels, this.observablePlaylistViewModels, this.resumeUploads, this.observableYoutubeAccountViewModels));
                }

                uploadList.CollectionChanged += uploadListCollectionChanged;
            }
        }

        //for testing
        public UploadViewModel this [int index]
        {
            get => this.uploadViewModels[index];
        }

        private void uploadListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Add)
            {
                List<UploadViewModel> newViewModels = new List<UploadViewModel>();
                foreach (Upload upload in e.NewItems)
                {
                    newViewModels.Add(new UploadViewModel(upload, this.observableTemplateViewModels, this.observablePlaylistViewModels, this.resumeUploads, this.observableYoutubeAccountViewModels));
                }

                this.uploadViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (Upload upload in e.OldItems)
                {
                    UploadViewModel oldViewModel = this.uploadViewModels.Find(viewModel => viewModel.Upload == upload);
                    int index = this.uploadViewModels.IndexOf(oldViewModel);
                    oldViewModel.Dispose();
                    this.uploadViewModels.Remove(oldViewModel);
                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservableTemplateViewModels supports only adding and removing.");
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged; 
            if (handler != null)
            {
                handler(this, args);
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

        public UploadViewModel GetUploadByGuid(Guid guid)
        {
            return this.uploadViewModels.Find(uploadviewModel => uploadviewModel.Guid == guid.ToString());
        }

        public void Reorder(UploadList uploadList)
        {
            List <UploadViewModel> reOrderedViewModels = new List<UploadViewModel>();
            foreach (Upload upload in uploadList)
            {
                reOrderedViewModels.Add(uploadViewModels.Find(vm => vm.Upload.Guid == upload.Guid));
            }

            this.uploadViewModels = reOrderedViewModels;

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
    }
}
