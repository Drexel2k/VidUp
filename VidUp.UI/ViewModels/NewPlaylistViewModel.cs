using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Drexel.VidUp.Youtube.Playlist;


namespace Drexel.VidUp.UI.ViewModels
{
    class NewPlaylistViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<PlaylistSelectionViewModel> observablePlaylistSelectionViewModels;
        private bool showPlaylistReceiveError = false;
        private List<string> selectedPlaylists;

        public bool ShowPlaylistReceiveError
        {
            get => this.showPlaylistReceiveError;
        }

        public ObservableCollection<PlaylistSelectionViewModel> ObservablePlaylistSelectionViewModels
        {
            get => this.observablePlaylistSelectionViewModels;
        }

        public bool ShowPlaylistRemoveNotification
        {
            get
            {
                foreach (string selectedPlaylist in this.selectedPlaylists)
                {
                    PlaylistSelectionViewModel playlistViewModel = this.observablePlaylistSelectionViewModels.FirstOrDefault(playlist => playlist.Id == selectedPlaylist);
                    if (playlistViewModel != null)
                    {
                        if (!playlistViewModel.IsChecked)
                        {
                            return true;
                        }
                    }
                }

                return false;
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewPlaylistViewModel(List<string> selectedPlaylists)
        {
            this.selectedPlaylists = selectedPlaylists == null ? new List<string>() : selectedPlaylists;
            this.observablePlaylistSelectionViewModels = new ObservableCollection<PlaylistSelectionViewModel>();

            this.refreshObservablePlaylistSelectionViewmodels();
        }

        private async Task refreshObservablePlaylistSelectionViewmodels()
        {
            try
            {
                List<Playlist> playlists = await YoutubePlaylistService.GetPlaylists();
                playlists.Sort((p1,p2) => p1.Title.CompareTo(p2.Title));

                foreach (Playlist playlist in playlists)
                {
                    PlaylistSelectionViewModel playlistViewModel = new PlaylistSelectionViewModel(playlist.Id, playlist.Title, this.selectedPlaylists.Contains(playlist.Id));
                    playlistViewModel.PropertyChanged += playlistViewModelPropertyChanged;
                    this.observablePlaylistSelectionViewModels.Add(playlistViewModel);
                }
            }
            catch (Exception e)
            {
                this.showPlaylistReceiveError = true;
                this.raisePropertyChanged("ShowPlaylistReceiveError");
            }
        }

        private void playlistViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                this.raisePropertyChanged("ShowPlaylistRemoveNotification");
            }
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