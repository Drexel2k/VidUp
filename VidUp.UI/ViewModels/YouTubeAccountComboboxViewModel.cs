using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{

    public class YoutubeAccountComboboxViewModel : INotifyPropertyChanged, IDisposable
    {
        private YoutubeAccount youtubeAccount;
        private Subscription youtubeAccountDisplayPropertyChangedSubscription;

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

            this.youtubeAccountDisplayPropertyChangedSubscription = EventAggregator.Instance.Subscribe<YoutubeAccountDisplayPropertyChangedMessage>(this.onYoutubeAccountDisplayPropertyChanged);
        }

        private void onYoutubeAccountDisplayPropertyChanged(YoutubeAccountDisplayPropertyChangedMessage obj)
        {
            this.raisePropertyChanged("YoutubeAccountName");
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

        public void Dispose()
        {
            if (this.youtubeAccountDisplayPropertyChangedSubscription != null)
            {
                this.youtubeAccountDisplayPropertyChangedSubscription.Dispose();
            }
        }
    }
}
