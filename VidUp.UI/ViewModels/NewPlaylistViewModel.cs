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

        public ReadOnlyCollection<PlaylistSelectionViewModel> AllPaylists
        {
            get => this.allPayPlaylistSelectionViewModels.AsReadOnly();
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
                    PlaylistSelectionViewModel playlistSelectionViewModel = new PlaylistSelectionViewModel(playlist.Id, playlist.Title, this.selectedPlaylists.Contains(playlist.Id));
                    playlistSelectionViewModel.PropertyChanged += playlistViewModelPropertyChanged;
                    this.allPayPlaylistSelectionViewModels.Add(playlistSelectionViewModel);
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
            List<PlaylistSelectionViewModel> toBeDeletedViewModels = new List<PlaylistSelectionViewModel>();
            List<PlaylistSelectionViewModel> notSelectedViewModels = new List<PlaylistSelectionViewModel>();
            foreach (PlaylistSelectionViewModel playlistSelectionViewModel in this.allPayPlaylistSelectionViewModels)
            {
                if (playlistSelectionViewModel.IsChecked)
                {
                    selectedViewModels.Add(playlistSelectionViewModel);
                }
                else
                {
                    if (playlistSelectionViewModel.ToBeDeleted)
                    {
                        toBeDeletedViewModels.Add(playlistSelectionViewModel);
                    }
                    else
                    {
                        if (playlistSelectionViewModel.Title.ToLower().Contains(searchText))
                        {

                            notSelectedViewModels.Add(playlistSelectionViewModel);
                        }
                    }
                }
            }

            selectedViewModels.Sort((vm1,vm2) => vm1.Title.CompareTo(vm2.Title));
            toBeDeletedViewModels.Sort((vm1, vm2) => vm1.Title.CompareTo(vm2.Title));
            notSelectedViewModels.Sort((vm1, vm2) => vm1.Title.CompareTo(vm2.Title));

            foreach (PlaylistSelectionViewModel selectedViewModel in selectedViewModels)
            {
                this.observablePlaylistSelectionViewModels.Add(selectedViewModel);
            }

            foreach (PlaylistSelectionViewModel toBeDeletedViewModel in toBeDeletedViewModels)
            {
                this.observablePlaylistSelectionViewModels.Add(toBeDeletedViewModel);
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
                    if (playlistSelectionViewModel.ToBeDeleted)
                    {
                        List<PlaylistSelectionViewModel> toBeDeletedViewModels = this.observablePlaylistSelectionViewModels.Where(playlistViewModel => playlistViewModel.ToBeDeleted).ToList();
                        if (toBeDeletedViewModels.Count == 0)
                        {
                            PlaylistSelectionViewModel firstUncheckedViewModel = this.observablePlaylistSelectionViewModels.FirstOrDefault(playlistViewModel => !playlistViewModel.IsChecked);
                            if (firstUncheckedViewModel == null)
                            {
                                this.observablePlaylistSelectionViewModels.Add(playlistSelectionViewModel);
                            }
                            else
                            {
                                this.observablePlaylistSelectionViewModels.Insert(this.observablePlaylistSelectionViewModels.IndexOf(firstUncheckedViewModel), playlistSelectionViewModel);
                            }
                        }
                        else
                        {
                            toBeDeletedViewModels.Add(playlistSelectionViewModel);
                            toBeDeletedViewModels.Sort((cultureViewModel1, cultureViewModel2) => cultureViewModel1.Title.CompareTo(cultureViewModel2.Title));

                            int toBeDeletedStartIndex = this.observablePlaylistSelectionViewModels.IndexOf(
                                this.observablePlaylistSelectionViewModels.FirstOrDefault(playlistViewModel => playlistViewModel.ToBeDeleted));

                            for (int index = 0; index < toBeDeletedViewModels.Count; index++)
                            {
                                if (this.observablePlaylistSelectionViewModels[index + toBeDeletedStartIndex] != toBeDeletedViewModels[index])
                                {
                                    this.observablePlaylistSelectionViewModels.Insert(index + toBeDeletedStartIndex, toBeDeletedViewModels[index]);
                                }
                            }
                        }
                    }
                    else
                    {
                        bool inserted = false;
                        for (int index = 0; index < this.observablePlaylistSelectionViewModels.Count; index++)
                        {
                            if (!this.observablePlaylistSelectionViewModels[index].IsChecked && !this.observablePlaylistSelectionViewModels[index].ToBeDeleted)
                            {
                                if (this.observablePlaylistSelectionViewModels[index].Title.CompareTo(playlistSelectionViewModel.Title) > 0)
                                {
                                    this.observablePlaylistSelectionViewModels.Insert(index, playlistSelectionViewModel);
                                    inserted = true;
                                    break;
                                }
                            }
                        }

                        //if unchecked cultureViewModel is the last position in order
                        if (!inserted)
                        {
                            this.observablePlaylistSelectionViewModels.Add(playlistSelectionViewModel);
                        }
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