using System;
using System.ComponentModel;
using System.Windows.Forms;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    class CopyTemplateViewModel : INotifyPropertyChanged
    {
        private string name;

        private bool formVaild = true;

        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccountComboboxViewModel selectedYoutubeAccount;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    if(!string.IsNullOrWhiteSpace(this.name))
                    {
                        this.FormValid = true;
                    }
                    else
                    {
                        this.FormValid = false;
                    }

                    this.raisePropertyChanged("Name");
                }
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get => this.observableYoutubeAccountViewModels;
        }

        public YoutubeAccountComboboxViewModel SelectedYouTubeAccount
        {
            get => this.selectedYoutubeAccount;
            set => this.selectedYoutubeAccount = value;
        }

        public bool FormValid
        {
            get => this.formVaild;
            private set
            {
                this.formVaild = value;
                this.raisePropertyChanged("FormValid");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public CopyTemplateViewModel(string orirginalTemplateName, string templateImageFolder, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount selectedYoutubeAccount)
        {
            if (observableYoutubeAccountViewModels == null || observableYoutubeAccountViewModels.YoutubeAccountCount <= 0)
            {
                throw new ArgumentException("observableYoutubeAccountViewModels must not be null and must contain accounts.");
            }

            if (selectedYoutubeAccount == null)
            {
                throw new ArgumentException("selectedYoutubeAccount must not be null.");
            }

            if(string.IsNullOrWhiteSpace(orirginalTemplateName))
            {
                throw new ArgumentException("orirginalTemplateName must not be null or empty.");
            }

            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.selectedYoutubeAccount = this.observableYoutubeAccountViewModels.GetViewModel(selectedYoutubeAccount);

            this.name = $"Copy of {orirginalTemplateName}";
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