using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.UI.Controls;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class PlaylistViewModel : INotifyPropertyChanged
    {
        private PlaylistList playlistList;
        private Playlist playlist;
        private ObservablePlaylistViewModels observablePlaylistViewModels;
        private PlaylistComboboxViewModel selectedPlaylist;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newPlaylistCommand;
        private GenericCommand removePlaylistCommand;

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

        public ObservablePlaylistViewModels ObservablePlaylistViewModels
        {
            get
            {
                return this.observablePlaylistViewModels;
            }
        }

        public PlaylistComboboxViewModel SelectedPlaylist
        {
            get
            {
                return this.selectedPlaylist;
            }
            set
            {
                if (this.selectedPlaylist != value)
                {
                    this.selectedPlaylist = value;
                    if (value != null)
                    {
                        this.Playlist = value.Playlist;
                    }
                    else
                    {
                        this.Playlist = null;
                    }

                    this.raisePropertyChanged("SelectedPlaylist");
                }
            }
        }

        public GenericCommand NewPlaylistCommand
        {
            get
            {
                return this.newPlaylistCommand;
            }
        }

        public GenericCommand RemovePlaylistCommand
        {
            get
            {
                return this.removePlaylistCommand;
            }
        }

        public string PlaylistId
        { 
            get => this.playlist != null ? this.playlist.PlaylistId : string.Empty;
            set
            {
                this.playlist.PlaylistId = value;
                this.raisePropertyChangedAndSerializePlaylistList("PlaylistId");
            }
        }

        public string Name
        {
            get => this.playlist != null ? this.playlist.Name : null;
            set
            {
                this.playlist.Name = value;
                this.raisePropertyChangedAndSerializePlaylistList("Name");
            }
        }

        public DateTime Created
        { 
            get => this.playlist != null ? this.playlist.Created : DateTime.MinValue; 
        }

        public DateTime LastModified 
        { 
            get => this.playlist != null ? this.playlist.LastModified : DateTime.MinValue; 
        }


        #endregion properties

        public PlaylistViewModel(PlaylistList playlistList, ObservablePlaylistViewModels observablePlaylistViewModels)
        {
            if(playlistList == null)
            {
                throw new ArgumentException("PlaylistList must not be null.");
            }

            if(observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels action must not be null.");
            }

            this.playlistList = playlistList;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.SelectedPlaylist = this.observablePlaylistViewModels.PlaylistCount > 0 ? this.observablePlaylistViewModels[0] : null;

            this.newPlaylistCommand = new GenericCommand(this.OpenNewPlaylistDialog);
            this.removePlaylistCommand = new GenericCommand(this.RemovePlaylist);
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

        public async void OpenNewPlaylistDialog(object obj)
        {
            var view = new NewPlaylistControl
            {
                DataContext = new NewPlaylistViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewPlaylistViewModel data = (NewPlaylistViewModel)view.DataContext;
                Playlist playlist = new Playlist(data.Name, data.PlaylistId);
                this.AddPlaylist(playlist);
            }
        }

        public void RemovePlaylist(object playlistId)
        {       
            Playlist playlist = this.playlistList.GetPlaylist((string)playlistId);

            //Needs to set before deleting the ViewModel in ObservableTemplateViewModels, otherwise the RaiseNotifyCollectionChanged
            //will set the SelectedTemplate to null which causes problems if there are templates left
            if (this.observablePlaylistViewModels.PlaylistCount > 1)
            {
                if (this.observablePlaylistViewModels[0].Playlist == playlist)
                {
                    this.SelectedPlaylist = this.observablePlaylistViewModels[1];
                }
                else
                {
                    this.SelectedPlaylist = this.observablePlaylistViewModels[0];
                }
            }
            else
            {
                this.SelectedPlaylist = null;
            }

            this.playlistList.Remove(playlist);

            JsonSerialization.JsonSerializer.SerializePlaylistList();
            JsonSerialization.JsonSerializer.SerializeAllUploads();
            JsonSerialization.JsonSerializer.SerializeTemplateList();
        }

        private void raisePropertyChangedAndSerializePlaylistList(string propertyName)
        {
            this.raisePropertyChanged(propertyName);
            JsonSerialization.JsonSerializer.SerializePlaylistList();
        }

        //exposed for testing
        public void AddPlaylist(Playlist playlist)
        {
            List<Playlist> list = new List<Playlist>();
            list.Add(playlist);
            this.playlistList.AddPlaylists(list);
            JsonSerialization.JsonSerializer.SerializePlaylistList();

            this.SelectedPlaylist = new PlaylistComboboxViewModel(playlist);
        }
    }
}
