using System.ComponentModel;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{

    public class PlaylistComboboxViewModel : INotifyPropertyChanged
    {
        private Playlist playlist;
        private bool visible = true;
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

        public string TitleWithYoutubeAccountInfo
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

            if (playlist != null)
            {
                this.playlist.PropertyChanged += playlistPropertyChanged;
            }
        }

        private void playlistPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if(e.PropertyName == "Title")
            {
                this.raisePropertyChanged("Title");
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
