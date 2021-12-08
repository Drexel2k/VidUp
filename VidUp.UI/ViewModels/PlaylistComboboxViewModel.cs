using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{

    public class PlaylistComboboxViewModel : INotifyPropertyChanged, IDisposable
    {
        private Playlist playlist;
        private bool visible = true;
        private Subscription playlistDisplayPropertyChangedSubscription;
        public event PropertyChangedEventHandler PropertyChanged;

        public Playlist Playlist
        {
            get
            {
                return this.playlist;
            }
        }

        public string PlaylistId
        {
            get => this.playlist != null ? this.playlist.PlaylistId : string.Empty;
        }

        public string Title
        {
            get => this.playlist != null ? this.playlist.Title : string.Empty;
        }

        public string TitleWithYoutubeAccountName
        {
            get => this.playlist != null ? $"{this.playlist.Title} [{this.playlist.YoutubeAccount.Name}]" : string.Empty;
        }

        public string YoutubeAccountName
        {
            get => this.playlist != null ? this.playlist.YoutubeAccount.Name : string.Empty;
        }

        public bool Visible
        {
            get => this.visible;
            set
            {
                this.visible = value;
                this.raisePropertyChanged("Visible");
            }
        }

        public PlaylistComboboxViewModel(Playlist playlist)
        {
            this.playlist = playlist;

            this.playlistDisplayPropertyChangedSubscription = EventAggregator.Instance.Subscribe<PlaylistDisplayPropertyChangedMessage>(this.onPlaylistDisplayPropertyChanged);
        }

        private void onPlaylistDisplayPropertyChanged(PlaylistDisplayPropertyChangedMessage obj)
        {
            this.raisePropertyChanged("TitleWithYoutubeAccountName");
            this.raisePropertyChanged("Title");
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
            if (this.playlistDisplayPropertyChangedSubscription != null)
            {
                this.playlistDisplayPropertyChangedSubscription.Dispose();
            }
        }
    }
}
