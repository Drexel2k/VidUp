using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class ObservablePlaylistViewModels : INotifyCollectionChanged, IEnumerable<PlaylistComboboxViewModel>
    {
        private PlaylistList playlistList;
        private List<PlaylistComboboxViewModel> playlistComboboxViewModels;

        public int PlaylistCount { get => this.playlistComboboxViewModels.Count;  }

        public event NotifyCollectionChangedEventHandler CollectionChanged;

        public ObservablePlaylistViewModels(PlaylistList playlistList)
        {
            this.playlistList = playlistList;

            this.playlistComboboxViewModels = new List<PlaylistComboboxViewModel>();
            foreach (Playlist playlist in playlistList)
            {
                PlaylistComboboxViewModel playlistComboboxViewModel = new PlaylistComboboxViewModel(playlist);
                this.playlistComboboxViewModels.Add(playlistComboboxViewModel);
            }

            this.playlistList.CollectionChanged += playlistListCollectionChanged;
        }

        private void playlistListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Add)
            {
                List<PlaylistComboboxViewModel> newViewModels = new List<PlaylistComboboxViewModel>();
                foreach (Playlist playlist in e.NewItems)
                {
                    newViewModels.Add(new PlaylistComboboxViewModel(playlist));
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
