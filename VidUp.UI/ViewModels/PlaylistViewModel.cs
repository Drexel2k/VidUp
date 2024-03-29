﻿using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        private Playlist playlist;
        public event PropertyChangedEventHandler PropertyChanged;
        private GenericCommand deletePlaylistCommand;

        #region properties
        public Playlist Playlist
        {
            get
            {
                return this.playlist;
            }
            set
            {
                if (this.playlist != value)
                {
                    this.playlist = value;
                    //all properties changed
                    this.raisePropertyChanged(null);
                }
            }
        }

        public bool PlaylistSet
        {
            get => this.playlist != null;
        }

        public GenericCommand DeletePlaylistCommand
        {
            get
            {
                return this.deletePlaylistCommand;
            }
        }

        public string PlaylistId
        { 
            get => this.playlist != null ? this.playlist.PlaylistId : string.Empty;
        }

        public string Title
        {
            get => this.playlist != null ? this.playlist.Title : null;
            set
            {
                this.playlist.Title = value;
                JsonSerializationContent.JsonSerializer.SerializePlaylistList();

                EventAggregator.Instance.Publish(new PlaylistDisplayPropertyChangedMessage("title"));
                this.raisePropertyChanged("Title");
            }
        }

        public string YouTubeAccountName
        {
            get => this.playlist != null ? this.playlist.YoutubeAccount.Name : null;
        }

        #endregion properties

        public PlaylistViewModel(Playlist playlist)
        {
            this.playlist = playlist;
            this.deletePlaylistCommand = new GenericCommand(this.deletePlaylist);

            EventAggregator.Instance.Subscribe<SelectedPlaylistChangedMessage>(this.selectedPlaylistChanged);
        }

        private void selectedPlaylistChanged(SelectedPlaylistChangedMessage selectedPlaylistChangedMessage)
        {
            //raises all properties changed
            this.Playlist = selectedPlaylistChangedMessage.NewPlaylist;
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

        private void deletePlaylist(object paramter)
        {
            EventAggregator.Instance.Publish(new PlaylistDeleteMessage(this.playlist));
        }
    }
}
