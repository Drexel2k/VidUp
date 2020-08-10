using System.ComponentModel;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{

    public class PlaylistComboboxViewModel : INotifyPropertyChanged
    {
        private Playlist playlist;
        public event PropertyChangedEventHandler PropertyChanged;

        public Playlist Playlist
        {
            get
            {
                return this.playlist;
            }
            set
            {
                if (this.playlist != null)
                {
                    this.playlist.PropertyChanged -= playlistPropertyChanged;
                }

                this.playlist = value;

                if (this.playlist != null)
                {
                    this.playlist.PropertyChanged += playlistPropertyChanged;
                }

                this.raisePropertyChanged(string.Empty);
            }
        }

        public string PlaylistId
        {
            get => this.playlist != null ? this.playlist.PlaylistId : string.Empty;
        }

        public string Name
        {
            get => this.playlist != null ? this.playlist.Name : string.Empty;
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
            if(e.PropertyName == "Name")
            {
                this.raisePropertyChanged("Name");
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
