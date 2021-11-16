using System;
using System.ComponentModel;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{

    public class YouTubeAccountComboboxViewModel : INotifyPropertyChanged
    {
        private YouTubeAccount youTubeAccount;
        public event PropertyChangedEventHandler PropertyChanged;

        public string YouTubeAccountFilePath
        {
            get => this.youTubeAccount.FilePath;
        }

        public string YouTubeAccountName
        {
            get => this.youTubeAccount.Name;
            set => this.youTubeAccount.Name = value;
        }

        public YouTubeAccountComboboxViewModel(YouTubeAccount youTubeAccount)
        {
            if (youTubeAccount == null)
            {
                throw new ArgumentException("YouToubeAccount must not be null");
            }

            this.youTubeAccount = youTubeAccount;
            this.youTubeAccount.PropertyChanged += youTubeAccountPropertyChanged;
        }

        private void youTubeAccountPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Name")
            {
                this.raisePropertyChanged("YouTubeAccountName");
            }

            if(e.PropertyName == "FilePath")
            {
                this.raisePropertyChanged("YouTubeAccountFilePath");
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
