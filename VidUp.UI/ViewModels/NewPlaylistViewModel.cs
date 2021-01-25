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
        private List<PlaylistSelectionViewModel> allPayPlaylistSelectionViewModels;
        private bool showPlaylistReceiveError = false;
        private List<string> selectedPlaylists;
        private string searchText = string.Empty;

        public bool ShowPlaylistReceiveError
        {
            get => this.showPlaylistReceiveError;
        }

        public ObservableCollection<PlaylistSelectionViewModel> ObservablePlaylistSelectionViewModels
        {
            get => this.observablePlaylistSelectionViewModels;
        }

        public string SearchText
        {
            get => this.searchText;
            set
            {
                this.searchText = value;
                this.filterObservablePlaylistSelectionViewmodels();
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewPlaylistViewModel(List<string> selectedPlaylists)
        {
            this.selectedPlaylists = selectedPlaylists == null ? new List<string>() : selectedPlaylists;
            this.observablePlaylistSelectionViewModels = new ObservableCollection<PlaylistSelectionViewModel>();
            this.allPayPlaylistSelectionViewModels = new List<PlaylistSelectionViewModel>();
            this.initializePlaylists();
        }

        private async Task initializePlaylists()
        {
            try
            {
                List<Playlist> playlists = await YoutubePlaylistService.GetPlaylists();

                foreach (Playlist playlist in playlists)
                {
                    if (!this.selectedPlaylists.Contains(playlist.Id))
                    {
                        PlaylistSelectionViewModel playlistSelectionViewModel = new PlaylistSelectionViewModel(playlist.Id, playlist.Title, false);
                        playlistSelectionViewModel.PropertyChanged += playlistViewModelPropertyChanged;
                        this.allPayPlaylistSelectionViewModels.Add(playlistSelectionViewModel); }
                }

                this.filterObservablePlaylistSelectionViewmodels();
            }
            catch (Exception e)
            {
                this.showPlaylistReceiveError = true;
                this.raisePropertyChanged("ShowPlaylistReceiveError");
            }
        }

        private void filterObservablePlaylistSelectionViewmodels()
        {
            this.observablePlaylistSelectionViewModels.Clear();

            string searchText = this.searchText.ToLower();
            List<PlaylistSelectionViewModel> selectedViewModels = new List<PlaylistSelectionViewModel>();
            List<PlaylistSelectionViewModel> notSelectedViewModels = new List<PlaylistSelectionViewModel>();
            foreach (PlaylistSelectionViewModel playlistSelectionViewModel in this.allPayPlaylistSelectionViewModels)
            {
                if (playlistSelectionViewModel.IsChecked)
                {
                    selectedViewModels.Add(playlistSelectionViewModel);
                }
                else
                {
                    if (playlistSelectionViewModel.Title.ToLower().Contains(searchText))
                    {
                        notSelectedViewModels.Add(playlistSelectionViewModel);
                    }
                    
                }
            }

            selectedViewModels.Sort((vm1,vm2) => vm1.Title.CompareTo(vm2.Title));
            notSelectedViewModels.Sort((vm1, vm2) => vm1.Title.CompareTo(vm2.Title));

            foreach (PlaylistSelectionViewModel selectedViewModel in selectedViewModels)
            {
                this.observablePlaylistSelectionViewModels.Add(selectedViewModel);
            }

            foreach (PlaylistSelectionViewModel notSelectedViewModel in notSelectedViewModels)
            {
                this.observablePlaylistSelectionViewModels.Add(notSelectedViewModel);
            }
        }

        private void playlistViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                PlaylistSelectionViewModel playlistSelectionViewModel = (PlaylistSelectionViewModel)sender;
                int currentIndex = this.observablePlaylistSelectionViewModels.IndexOf(playlistSelectionViewModel);
                this.observablePlaylistSelectionViewModels.RemoveAt(currentIndex);

                if (playlistSelectionViewModel.IsChecked)
                {
                    List<PlaylistSelectionViewModel> checkedPlaylistSelectionViewModels = this.observablePlaylistSelectionViewModels.Where(playlistViewModel => playlistViewModel.IsChecked).ToList();
                    if (checkedPlaylistSelectionViewModels.Count == 0)
                    {
                        this.observablePlaylistSelectionViewModels.Insert(0, playlistSelectionViewModel);
                    }
                    else
                    {
                        checkedPlaylistSelectionViewModels.Add(playlistSelectionViewModel);
                        checkedPlaylistSelectionViewModels.Sort((cultureViewModel1, cultureViewModel2) => cultureViewModel1.Title.CompareTo(cultureViewModel2.Title));

                        for (int index = 0; index < checkedPlaylistSelectionViewModels.Count; index++)
                        {
                            if (this.observablePlaylistSelectionViewModels[index] != checkedPlaylistSelectionViewModels[index])
                            {
                                this.observablePlaylistSelectionViewModels.Insert(index, checkedPlaylistSelectionViewModels[index]);
                            }
                        }
                    }
                }
                else
                {
                    bool inserted = false;
                    for (int index = 0; index < this.observablePlaylistSelectionViewModels.Count; index++)
                    {
                        if (!this.observablePlaylistSelectionViewModels[index].IsChecked)
                        {
                            if (this.observablePlaylistSelectionViewModels[index].Title.CompareTo(playlistSelectionViewModel.Title) > 0)
                            {
                                this.observablePlaylistSelectionViewModels.Insert(index, playlistSelectionViewModel);
                                inserted = true;
                                break;
                            }
                        }
                    }

                    //if unchecked playlistSelectionViewModel is the last position in order
                    if (!inserted)
                    {
                        this.observablePlaylistSelectionViewModels.Add(playlistSelectionViewModel);
                    }
                }

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