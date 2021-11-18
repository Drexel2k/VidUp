using System;
using System.ComponentModel;
using System.DirectoryServices.ActiveDirectory;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{

    public class YoutubeAccountComboboxViewModel : INotifyPropertyChanged
    {
        private YoutubeAccount youtubeAccount;
        public event PropertyChangedEventHandler PropertyChanged;

        public YoutubeAccount YoutubeAccount
        {
            get => this.youtubeAccount;
        }

        public string YoutubeAccountName
        {
            get => this.youtubeAccount.Name;
        }

        public YoutubeAccountComboboxViewModel(YoutubeAccount youtubeAccount)
        {
            if (youtubeAccount == null)
            {
                throw new ArgumentException("YouToubeAccount must not be null");
            }

            this.youtubeAccount = youtubeAccount;
            this.youtubeAccount.PropertyChanged += youtubeAccountPropertyChanged;
        }

        private void youtubeAccountPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                this.raisePropertyChanged("YoutubeAccountName");
            }

            if(e.PropertyName == "FilePath")
            {
                this.raisePropertyChanged("YoutubeAccountFilePath");
            }
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
