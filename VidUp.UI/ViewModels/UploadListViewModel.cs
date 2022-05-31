using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;


namespace Drexel.VidUp.UI.ViewModels
{
    public class UploadListViewModel : INotifyPropertyChanged
    {
        private GenericCommand deleteCommand;
        private ObservableUploadViewModels observableUploadViewModels;

        public event PropertyChangedEventHandler PropertyChanged;

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

        public UploadListViewModel(ObservableUploadViewModels observableUploadViewModels, YoutubeAccount youtubeAccountForFiltering)
        {
            if(observableUploadViewModels == null)
            {
                throw new ArgumentException("ObservableUploadViewModels must not be null.");
            }

            if (youtubeAccountForFiltering == null)
            {
                throw new ArgumentException("YoutubeAccountForFiltering must not be null.");
            }

            this.observableUploadViewModels = observableUploadViewModels;

            EventAggregator.Instance.Subscribe<SelectedFilterYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);

            this.deleteCommand = new GenericCommand(this.DeleteUpload);
        }

        private void selectedYoutubeAccountChanged(SelectedFilterYoutubeAccountChangedMessage selectedYoutubeAccountChangedMessage)
        {
            if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount == null)
            {
                throw new ArgumentException("Changed Youtube account must not be null.");
            }

            foreach (UploadViewModel observableUploadViewModel in this.observableUploadViewModels)
            {
                if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.IsDummy)
                {
                    if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.Name == "All")
                    {
                        if (observableUploadViewModel.Visible == false)
                        {
                            observableUploadViewModel.Visible = true;
                        }
                    }
                }
                else
                {
                    if (observableUploadViewModel.SelectedYoutubeAccount.YoutubeAccount == selectedYoutubeAccountChangedMessage.NewYoutubeAccount)
                    {
                        observableUploadViewModel.Visible = true;
                    }
                    else
                    {
                        observableUploadViewModel.Visible = false;
                    }
                }
            }
        }

        public void Reorder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            EventAggregator.Instance.Publish(new UploadListReorderMessage(uploadToMove, uploadAtTargetPosition));
        }

        //exposed for testing
        public void DeleteUpload(object parameter)
        {
            Tracer.Write($"UploadListViewModel.DeleteUpload: Start, Guid '{parameter}'.");
            Guid uploadGuid = Guid.Parse((string)parameter);
            EventAggregator.Instance.Publish(new UploadDeleteMessage(uploadGuid));
            Tracer.Write($"UploadListViewModel.DeleteUpload: End.");
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
