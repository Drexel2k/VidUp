using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.Youtube.Service;
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

        private TemplateList templateList;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newPlaylistCommand;
        private GenericCommand removePlaylistCommand;
        private GenericCommand autoSetPlaylistsCommand;

        private bool autoSettingPlaylists;


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

        public GenericCommand AutoSetPlaylistsCommand
        {
            get
            {
                return this.autoSetPlaylistsCommand;
            }
        }

        public bool AutoSettingPlaylists
        {
            get
            {
                return this.autoSettingPlaylists;
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

        public PlaylistViewModel(PlaylistList playlistList, ObservablePlaylistViewModels observablePlaylistViewModels, TemplateList templateList)
        {
            if(playlistList == null)
            {
                throw new ArgumentException("PlaylistList must not be null.");
            }

            if(observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels action must not be null.");
            }

            if (templateList == null)
            {
                throw new ArgumentException("TemplateList action must not be null.");
            }

            this.playlistList = playlistList;
            this.observablePlaylistViewModels = observablePlaylistViewModels;
            this.templateList = templateList;

            this.SelectedPlaylist = this.observablePlaylistViewModels.PlaylistCount > 0 ? this.observablePlaylistViewModels[0] : null;

            this.newPlaylistCommand = new GenericCommand(this.OpenNewPlaylistDialog);
            this.removePlaylistCommand = new GenericCommand(this.RemovePlaylist);
            this.autoSetPlaylistsCommand = new GenericCommand(this.autoSetPlaylists);
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

        private async void autoSetPlaylists(object obj)
        {
            this.autoSettingPlaylists = true;
            this.raisePropertyChanged("AutoSettingPlaylists");

            Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap = new Dictionary<string, List<Upload>>();
            foreach (Template template in this.templateList)
            {
                if (template.SetPlaylistAfterPublication && template.Playlist != null && !template.Playlist.NotExistsOnYoutube)
                {
                    Upload[] uploads = template.Uploads.Where(upload1 => upload1.UploadStatus == UplStatus.Finished && 
                        !string.IsNullOrWhiteSpace(upload1.VideoId) && upload1.Playlist == null && !upload1.NotExistsOnYoutube).ToArray();

                    if (uploads.Length >= 1)
                    {
                        List<Upload> uploadsFromMap;
                        if (!playlistUploadsWithoutPlaylistMap.TryGetValue(template.Playlist.PlaylistId, out uploadsFromMap))
                        {
                            uploadsFromMap = new List<Upload>();
                            playlistUploadsWithoutPlaylistMap.Add(template.Playlist.PlaylistId, uploadsFromMap);
                        }

                        uploadsFromMap.AddRange(uploads);
                    }
                }
            }

            Dictionary<string, List<string>> playlistVideos = await YoutubePlaylistService.GetPlaylists(playlistUploadsWithoutPlaylistMap.Keys);
            if (playlistUploadsWithoutPlaylistMap.Count != playlistVideos.Count)
            {
                IEnumerable<KeyValuePair<string, List<Upload>>> missingPlaylistsTemp =
                    playlistUploadsWithoutPlaylistMap.Where(kvp => !playlistVideos.ContainsKey(kvp.Key));
                KeyValuePair<string, List<Upload>>[] missingPlaylists =
                    missingPlaylistsTemp as KeyValuePair<string, List<Upload>>[] ?? missingPlaylistsTemp.ToArray();

                foreach (KeyValuePair<string, List<Upload>> playlistUploadsMap in missingPlaylists)
                {
                    this.playlistList.GetPlaylist(playlistUploadsMap.Key).NotExistsOnYoutube = true;
                    playlistUploadsWithoutPlaylistMap.Remove(playlistUploadsMap.Key);
                }

                JsonSerialization.JsonSerializer.SerializePlaylistList();
            }


            //check if videos are already in playlist on YT.
            foreach (KeyValuePair<string, List<Upload>> playlistVideosWithoutPlaylist in playlistUploadsWithoutPlaylistMap)
            {
                List<Upload> uploadsToRemove = new List<Upload>();
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    if (playlistVideos[playlistVideosWithoutPlaylist.Key].Contains(upload.VideoId))
                    {
                        upload.Playlist = this.playlistList.GetPlaylist(playlistVideosWithoutPlaylist.Key);
                        uploadsToRemove.Add(upload);
                    }
                }

                playlistVideosWithoutPlaylist.Value.RemoveAll(upload => uploadsToRemove.Contains(upload));
            }

            Dictionary<string, bool> videosPublicMap = await YoutubeVideoService.IsPublic(
                playlistUploadsWithoutPlaylistMap.SelectMany(kvp => kvp.Value).Select(upload => upload.VideoId).ToList());

            //set playlist on YT if video is public.
            foreach (KeyValuePair<string, List<Upload>> playlistVideosWithoutPlaylist in playlistUploadsWithoutPlaylistMap)
            {
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    //if video id is not included in response it was deleted on YT.
                    if (videosPublicMap.ContainsKey(upload.VideoId))
                    {
                        if (videosPublicMap[upload.VideoId])
                        {
                            upload.Playlist = this.playlistList.GetPlaylist(playlistVideosWithoutPlaylist.Key);
                            await YoutubePlaylistService.AddToPlaylist(upload);
                        }
                    }
                    else
                    {
                        upload.NotExistsOnYoutube = true;
                    }
                }
            }

            JsonSerialization.JsonSerializer.SerializeAllUploads();

            this.autoSettingPlaylists = false;
            this.raisePropertyChanged("AutoSettingPlaylists");
        }
    }
}
