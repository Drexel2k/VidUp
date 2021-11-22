using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Runtime.CompilerServices;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservablePlaylistViewModels : INotifyCollectionChanged, IEnumerable<PlaylistComboboxViewModel>
    {
        private PlaylistList playlistList;
        private List<PlaylistComboboxViewModel> playlistComboboxViewModels;

        private Dictionary<YoutubeAccount, PlaylistList> playlistListsByAccount;
        private Dictionary<YoutubeAccount, ObservablePlaylistViewModels> observablePlaylistViewModelsByAccount;

        public int PlaylistCount { get => this.playlistComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservablePlaylistViewModels(PlaylistList playlistList, bool createByAccount)
        {
            this.playlistList = playlistList;

            this.playlistComboboxViewModels = new List<PlaylistComboboxViewModel>();
            foreach (Playlist playlist in playlistList)
            {
                PlaylistComboboxViewModel playlistComboboxViewModel = new PlaylistComboboxViewModel(playlist);
                this.playlistComboboxViewModels.Add(playlistComboboxViewModel);
            }

            if(createByAccount)
            {
                this.playlistListsByAccount = new Dictionary<YoutubeAccount, PlaylistList>();
                this.observablePlaylistViewModelsByAccount = new Dictionary<YoutubeAccount, ObservablePlaylistViewModels>();

                Dictionary<YoutubeAccount, List<Playlist>> playlistsByAccount = new Dictionary<YoutubeAccount, List<Playlist>>();
                foreach (Playlist playlist in this.playlistList)
                {
                    List<Playlist> playlists;
                    if (!playlistsByAccount.TryGetValue(playlist.YoutubeAccount, out playlists))
                    {
                        playlists = new List<Playlist>();
                        playlistsByAccount.Add(playlist.YoutubeAccount, playlists);
                    }

                    playlists.Add(playlist);
                }

                foreach (KeyValuePair<YoutubeAccount, List<Playlist>> accountPlaylists in playlistsByAccount)
                {
                    PlaylistList accountPlaylistList = new PlaylistList(accountPlaylists.Value);
                    this.playlistListsByAccount.Add(accountPlaylists.Key, accountPlaylistList);
                    this.observablePlaylistViewModelsByAccount.Add(accountPlaylists.Key, new ObservablePlaylistViewModels(accountPlaylistList, false));
                }
            }

            this.playlistList.CollectionChanged += this.playlistListCollectionChanged;
        }

        private void playlistListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<PlaylistComboboxViewModel> newViewModels = new List<PlaylistComboboxViewModel>();
                foreach (Playlist playlist in e.NewItems)
                {
                    newViewModels.Add(new PlaylistComboboxViewModel(playlist));

                    if (this.playlistListsByAccount != null)
                    {
                        PlaylistList accountPlaylistList;
                        if (!this.playlistListsByAccount.TryGetValue(playlist.YoutubeAccount, out accountPlaylistList))
                        {
                            List<Playlist> playlists = new List<Playlist>();
                            playlists.Add(playlist);
                            accountPlaylistList = new PlaylistList(playlists);
                            this.playlistListsByAccount.Add(playlist.YoutubeAccount, accountPlaylistList);
                            this.observablePlaylistViewModelsByAccount.Add(playlist.YoutubeAccount, new ObservablePlaylistViewModels(accountPlaylistList, false));
                        }
                        else
                        {
                            this.playlistListsByAccount[playlist.YoutubeAccount].AddPlaylist(playlist);
                        }
                    }
                }

                this.playlistComboboxViewModels.AddRange(newViewModels);

                this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newViewModels));
                return;
            }

            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                //if multiple view models removed, remove every view model with a single call, as WPF/MVVM only supports
                //multiple deletes in one call when they are all in direct sequence in the collection
                foreach (Playlist playlist in e.OldItems)
                {
                    PlaylistComboboxViewModel oldViewModel = this.playlistComboboxViewModels.Find(viewModel => viewModel.Playlist == playlist);
                    int index = this.playlistComboboxViewModels.IndexOf(oldViewModel);
                    this.playlistComboboxViewModels.Remove(oldViewModel);

                    if (this.playlistListsByAccount != null)
                    {
                        this.playlistListsByAccount[playlist.YoutubeAccount].DeletePlaylists(pl => pl == playlist);
                    }

                    this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldViewModel, index));
                }

                return;
            }

            throw new InvalidOperationException("ObservablePlaylistViewModels supports only adding and removing.");
        }

        public PlaylistComboboxViewModel GetViewModel(Playlist playlist)
        {
            if (playlist != null)
            {
                foreach (PlaylistComboboxViewModel playlistComboboxViewModel in this.playlistComboboxViewModels)
                {
                    if (playlistComboboxViewModel.Playlist == playlist)
                    {
                        return playlistComboboxViewModel;
                    }
                }
            }

            return null;
        }

        public PlaylistComboboxViewModel GetFirstViewModel(Predicate<PlaylistComboboxViewModel> match)
        {
            if (match != null)
            {
                foreach (PlaylistComboboxViewModel playlistComboboxViewModel in this.playlistComboboxViewModels)
                {
                    if (match(playlistComboboxViewModel))
                    {
                        return playlistComboboxViewModel;
                    }
                }
            }

            return null;
        }

        public PlaylistComboboxViewModel this[int index]
        {
            get => this.playlistComboboxViewModels[index];
        }

        public ObservablePlaylistViewModels this[YoutubeAccount youtubeAccount]
        {
            get
            {
                if (!this.observablePlaylistViewModelsByAccount.ContainsKey(youtubeAccount))
                {
                    return null;
                }

                return this.observablePlaylistViewModelsByAccount[youtubeAccount];
            }
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        public IEnumerator<PlaylistComboboxViewModel> GetEnumerator()
        {
            return this.playlistComboboxViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }
    }
}
