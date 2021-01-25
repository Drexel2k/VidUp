using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItem;
using Drexel.VidUp.Youtube.Video;
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
        private GenericCommand deletePlaylistCommand;
        private GenericCommand autoSetPlaylistsCommand;

        private bool autoSettingPlaylists;
        private int intervalInSeconds = 12 * 60 * 60;

        private readonly object autoSetPlaylistsLock = new object();
        private Timer autoSetPlaylistTimer;

        #region properties

        private TimeSpan interval
        {
            get
            {
                return TimeSpan.FromSeconds(this.intervalInSeconds);
            }
        }

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

        public GenericCommand DeletePlaylistCommand
        {
            get
            {
                return this.deletePlaylistCommand;
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
        }

        public string Name
        {
            get => this.playlist != null ? this.playlist.Title : null;
            set
            {
                this.playlist.Title = value;
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

        public bool AutoSetPlaylists
        {
            get => Settings.SettingsInstance.UserSettings.AutoSetPlaylists;
            set
            {
                Settings.SettingsInstance.UserSettings.AutoSetPlaylists = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.setAutoSetPlaylistsTimer();
            }
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
            this.deletePlaylistCommand = new GenericCommand(this.DeletePlaylist);
            this.autoSetPlaylistsCommand = new GenericCommand(this.autoSetPlaylists);


            this.autoSetPlaylistTimer = new Timer();
            this.autoSetPlaylistTimer.AutoReset = false;
            this.autoSetPlaylistTimer.Elapsed += autoSetPlaylistTimerElapsed;

            if (Settings.SettingsInstance.UserSettings.AutoSetPlaylists)
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Autosetting playlists is enabled, setting up timer.");
                this.calculateTimeAndSetAutoSetPlaylistsTimer();
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

        public async void OpenNewPlaylistDialog(object obj)
        {
            var view = new NewPlaylistControl
            {
                DataContext = new NewPlaylistViewModel(this.playlistList.GetReadOnlyPlaylistList().Select(playlist => playlist.PlaylistId).ToList())
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewPlaylistViewModel data = (NewPlaylistViewModel)view.DataContext;
                Playlist playlistCheck;
                foreach (PlaylistSelectionViewModel playlistSelectionViewModel in data.ObservablePlaylistSelectionViewModels)
                {
                    if (playlistSelectionViewModel.IsChecked)
                    {
                        if (this.playlistList.FirstOrDefault(playlist => playlist.PlaylistId == playlistSelectionViewModel.Id) == null)
                        {
                            Playlist playlist = new Playlist(playlistSelectionViewModel.Id, playlistSelectionViewModel.Title);
                            this.AddPlaylist(playlist);
                        }
                    }
                    else
                    {
                        playlistCheck = this.playlistList.FirstOrDefault(playlist => playlist.PlaylistId == playlistSelectionViewModel.Id);
                        if (playlistCheck != null)
                        {
                            this.DeletePlaylist(playlistCheck.PlaylistId);
                        }
                    }
                }
            }
        }

        public void DeletePlaylist(object playlistId)
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

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            JsonSerializationContent.JsonSerializer.SerializePlaylistList();
        }

        private void raisePropertyChangedAndSerializePlaylistList(string propertyName)
        {
            this.raisePropertyChanged(propertyName);
            JsonSerializationContent.JsonSerializer.SerializePlaylistList();
        }

        //exposed for testing
        public void AddPlaylist(Playlist playlist)
        {
            List<Playlist> list = new List<Playlist>();
            list.Add(playlist);
            this.playlistList.AddPlaylists(list);
            JsonSerializationContent.JsonSerializer.SerializePlaylistList();

            this.SelectedPlaylist = new PlaylistComboboxViewModel(playlist);
        }

        private void autoSetPlaylistTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: Start autosetting playlists.");
            this.autoSetPlaylists(null);
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: End autosetting playlists.");
        }

        private async void autoSetPlaylists(object obj)
        {
            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Start.");
            lock (this.autoSetPlaylistsLock)
            {
                if (this.autoSettingPlaylists)
                {
                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: End, utosetting playlists currently in progress.");
                    return;
                }

                this.autoSettingPlaylists = true;
                this.raisePropertyChanged("AutoSettingPlaylists");
            }

            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Get uploads without playlists but with autoset templates.");
            Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap = this.getPlaylistUploadsWithoutPlaylistMap();

            if (playlistUploadsWithoutPlaylistMap.Count > 0)
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if playlists exist on Youtube.");
                //check if all needed playlists exist on youtube and if not mark them as not existing and remove from playlistUploadsWithoutPlaylistMap
                Dictionary<string, List<string>> playlistVideos = await this.getPlaylistsAndRemoveNotExistingPlaylists(playlistUploadsWithoutPlaylistMap);

                if (playlistUploadsWithoutPlaylistMap.Count > 0)
                {
                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads are already in playlist.");
                    this.removeUploadsAlreadyInPlaylist(playlistUploadsWithoutPlaylistMap, playlistVideos);


                    if (playlistUploadsWithoutPlaylistMap.Count > 0)
                    {
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads exist on Youtube.");
                        var videosPublicMap = await this.getPublicVideosAndRemoveNotExisingUploads(playlistUploadsWithoutPlaylistMap);

                        if (playlistUploadsWithoutPlaylistMap.Count > 0)
                        {
                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads is public and add to playlist.");
                            await this.addUploadsToPlaylistIfPublic(playlistUploadsWithoutPlaylistMap, videosPublicMap);
                        }
                        else
                        {
                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after uploads exist check on Youtube.");
                        }
                    }
                    else
                    {
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after uploads already in playlist check on Youtube.");
                    }

                    JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                }
                else
                {
                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after availability check on Youtube.");
                }
            }
            else
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Nothing to do.");
            }

            Settings.SettingsInstance.UserSettings.LastAutoAddToPlaylist = DateTime.Now;
            JsonSerializationSettings.JsonSerializer.SerializeSettings();

            if (Settings.SettingsInstance.UserSettings.AutoSetPlaylists)
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Autosetting playlists is enabled, setting timer to {this.interval.Days * 24 + this.interval.Hours}:{this.interval.Minutes}:{this.interval.Seconds} hours:minutes:seconds.");
                //stop timer if triggered manually and restart
                this.autoSetPlaylistTimer.Stop();
                //first call after constructor the timer can be less than interval.
                this.autoSetPlaylistTimer.Interval = this.intervalInSeconds * 1000;
                this.autoSetPlaylistTimer.Start();
            }

            this.autoSettingPlaylists = false;
            this.raisePropertyChanged("AutoSettingPlaylists");
            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: End.");
        }

        private Dictionary<string, List<Upload>> getPlaylistUploadsWithoutPlaylistMap()
        {
            Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap = new Dictionary<string, List<Upload>>();
            foreach (Template template in this.templateList)
            {
                if (template.SetPlaylistAfterPublication && template.Playlist != null && !template.Playlist.NotExistsOnYoutube)
                {
                    Upload[] uploads = template.Uploads.Where(
                        upload1 => upload1.UploadStatus == UplStatus.Finished && !string.IsNullOrWhiteSpace(upload1.VideoId) &&
                                   upload1.Playlist == null && !upload1.NotExistsOnYoutube).ToArray();

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

            return playlistUploadsWithoutPlaylistMap;
        }

        private async Task<Dictionary<string, List<string>>> getPlaylistsAndRemoveNotExistingPlaylists(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap)
        {
            Dictionary<string, List<string>> playlistVideos = await YoutubePlaylistItemService.GetPlaylists(playlistUploadsWithoutPlaylistMap.Keys);
            if (playlistUploadsWithoutPlaylistMap.Count != playlistVideos.Count)
            {
                KeyValuePair<string, List<Upload>>[] missingPlaylists = playlistUploadsWithoutPlaylistMap
                    .Where(kvp => !playlistVideos.ContainsKey(kvp.Key)).ToArray();
                foreach (KeyValuePair<string, List<Upload>> playlistUploadsMap in missingPlaylists)
                {
                    this.playlistList.GetPlaylist(playlistUploadsMap.Key).NotExistsOnYoutube = true;
                    playlistUploadsWithoutPlaylistMap.Remove(playlistUploadsMap.Key);
                }

                JsonSerializationContent.JsonSerializer.SerializePlaylistList();
            }

            return playlistVideos;
        }

        private void removeUploadsAlreadyInPlaylist(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap, Dictionary<string, List<string>> playlistVideos)
        {
            //check if videos are already in playlist on YT.
            List<string> noUploadsLeftPlaylists = new List<string>();
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
                if (playlistVideosWithoutPlaylist.Value.Count <= 0)
                {
                    noUploadsLeftPlaylists.Add(playlistVideosWithoutPlaylist.Key);
                }
            }

            foreach (string playlistId in noUploadsLeftPlaylists)
            {
                playlistUploadsWithoutPlaylistMap.Remove(playlistId);
            }
        }

        private async Task<Dictionary<string, bool>> getPublicVideosAndRemoveNotExisingUploads(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap)
        {
            List<string> noUploadsLeftPlaylists = new List<string>();
            Dictionary<string, bool> videosPublicMap = await YoutubeVideoService.IsPublic(playlistUploadsWithoutPlaylistMap
                .SelectMany(kvp => kvp.Value).Select(upload => upload.VideoId).ToList());

            foreach (KeyValuePair<string, List<Upload>> playlistVideosWithoutPlaylist in playlistUploadsWithoutPlaylistMap)
            {
                List<Upload> notExistingUploadsOnYoutube = new List<Upload>();
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    //if video id is not included in response it was deleted on YT.
                    if (!videosPublicMap.ContainsKey(upload.VideoId))
                    {
                        upload.NotExistsOnYoutube = true;
                        notExistingUploadsOnYoutube.Add(upload);
                    }
                }

                playlistVideosWithoutPlaylist.Value.RemoveAll(upload => upload.NotExistsOnYoutube);
                if (playlistVideosWithoutPlaylist.Value.Count <= 0)
                {
                    noUploadsLeftPlaylists.Add(playlistVideosWithoutPlaylist.Key);
                }
            }

            foreach (string playlistId in noUploadsLeftPlaylists)
            {
                playlistUploadsWithoutPlaylistMap.Remove(playlistId);
            }

            return videosPublicMap;
        }

        private async Task addUploadsToPlaylistIfPublic(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap, Dictionary<string, bool> videosPublicMap)
        {

            foreach (KeyValuePair<string, List<Upload>> playlistVideosWithoutPlaylist in playlistUploadsWithoutPlaylistMap)
            {
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    if (videosPublicMap[upload.VideoId])
                    {
                        upload.Playlist = this.playlistList.GetPlaylist(playlistVideosWithoutPlaylist.Key);
                        if (!await YoutubePlaylistItemService.AddToPlaylist(upload))
                        {
                            upload.Playlist = null;
                        }
                    }
                }
            }
        }

        private void setAutoSetPlaylistsTimer()
        {
            if (Settings.SettingsInstance.UserSettings.AutoSetPlaylists)
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Autosetting playlists enabled by user, setting up timer.");
                this.calculateTimeAndSetAutoSetPlaylistsTimer();
            }
            else
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Autosetting playlists disabled by user, stopping timer.");
                this.autoSetPlaylistTimer.Stop();
            }
        }

        private void calculateTimeAndSetAutoSetPlaylistsTimer()
        {
            TimeSpan span = DateTime.Now - Settings.SettingsInstance.UserSettings.LastAutoAddToPlaylist;
            Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Last autoset is {span.Days * 24 + span.Hours}:{span.Minutes}:{span.Seconds} hours:minutes:seconds ago.");

            if (span.TotalSeconds > this.intervalInSeconds)
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Starting autosetting playlists immediately.");
                this.autoSetPlaylists(null);
            }
            else
            {
                double nextAutoSetPlaylists = ((this.intervalInSeconds - span.TotalSeconds) * 1000);
                span = TimeSpan.FromMilliseconds(nextAutoSetPlaylists);
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Scheduling autosetting playlists in {span.Days * 24 + span.Hours}:{span.Minutes}:{span.Seconds} hours:minutes:seconds.");

                this.autoSetPlaylistTimer.Interval = nextAutoSetPlaylists;
                this.autoSetPlaylistTimer.Start();
            }
        }
    }
}
