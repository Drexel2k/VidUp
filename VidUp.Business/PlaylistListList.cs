using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class PlaylistList : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<Playlist>
    {
        [JsonProperty]
        private List<Playlist> playlists;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public int PlaylistCount { get => this.playlists.Count; }

        [JsonConstructor]
        private PlaylistList()
        {

        }

        public PlaylistList(List<Playlist> playlists)
        {
            this.playlists = new List<Playlist>();

            if (playlists != null)
            {
                this.playlists = playlists;
            }
        }

        public Playlist this[int index]
        {
            get
            {
                return this.playlists[index];
            }
        }

        public void AddPlaylists(List<Playlist> playlists)
        {
            this.playlists.AddRange(playlists);

            this.raiseNotifyPropertyChanged("PlaylistCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, playlists));
        }

        public int FindIndex(Predicate<Playlist> predicate)
        {
            return this.playlists.FindIndex(predicate);
        }

        public void Remove(Playlist playlist)
        {
            this.playlists.Remove(playlist);

            this.raiseNotifyPropertyChanged("PlaylistCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, playlist));
        }

        public Playlist GetPlaylist(int index)
        {
            return this.playlists[index];
        }

        public Playlist GetPlaylist(string playlistId)
        {
            return this.playlists.Find(playlist => playlist.PlaylistId == playlistId);
        }

        public ReadOnlyCollection<Playlist> GetReadOnlyPlaylistList()
        {
            return this.playlists.AsReadOnly();
        }

        public Playlist Find(Predicate<Playlist> match)
        {
            return this.playlists.Find(match);
        }

        public IEnumerator<Playlist> GetEnumerator()
        {
            return this.playlists.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
