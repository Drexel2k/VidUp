using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItem;
using Drexel.VidUp.Youtube.Video;
using MaterialDesignThemes.Wpf;
using Timer = System.Timers.Timer;

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
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccount youtubeAccountForRequestingPlaylists;

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

        public string Title
        {
            get => this.playlist != null ? this.playlist.Title : null;
            set
            {
                this.playlist.Title = value;
                this.raisePropertyChangedAndSerializePlaylistList("Title");
            }
        }

        public string YouTubeAccountName
        {
            get => this.playlist != null ? this.playlist.YoutubeAccount.Name : null;
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
            get => Settings.Instance.UserSettings.AutoSetPlaylists;
            set
            {
                Settings.Instance.UserSettings.AutoSetPlaylists = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.setAutoSetPlaylistsTimer();
            }
        }

        #endregion properties

        public PlaylistViewModel(PlaylistList playlistList, ObservablePlaylistViewModels observablePlaylistViewModels, TemplateList templateList,
            ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount youtubeAccountForRequestingPlaylists)
        {
            if(playlistList == null)
            {
                throw new ArgumentException("PlaylistList must not be null.");
            }

            if(observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels must not be null.");
            }

            if (templateList == null)
            {
                throw new ArgumentException("TemplateList must not be null.");
            }

            if (observableYoutubeAccountViewModels == null || observableYoutubeAccountViewModels.YoutubeAccountCount <= 0)
            {
                throw new ArgumentException("observableYoutubeAccountViewModels action must not be null and contain at least one account.");
            }

            if (youtubeAccountForRequestingPlaylists == null)
            {
                throw new ArgumentException("selectedAccount must not be null.");
            }

            this.playlistList = playlistList;
            this.observablePlaylistViewModels = observablePlaylistViewModels;
            this.templateList = templateList;

            this.SelectedPlaylist = this.observablePlaylistViewModels.PlaylistCount > 0 ? this.observablePlaylistViewModels[0] : null;

            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.youtubeAccountForRequestingPlaylists = youtubeAccountForRequestingPlaylists;
            EventAggregator.Instance.Subscribe<SelectedYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);
            EventAggregator.Instance.Subscribe<BeforeYoutubeAccountDeleteMessage>(this.beforeYoutubeAccountDelete);

            this.newPlaylistCommand = new GenericCommand(this.OpenNewPlaylistDialogAsync);
            this.deletePlaylistCommand = new GenericCommand(this.DeletePlaylist);
            this.autoSetPlaylistsCommand = new GenericCommand(this.autoSetPlaylistsAsync);

            this.autoSetPlaylistTimer = new Timer();
            this.autoSetPlaylistTimer.AutoReset = false;
            this.autoSetPlaylistTimer.Elapsed += autoSetPlaylistTimerElapsed;

            if (Settings.Instance.UserSettings.AutoSetPlaylists)
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Autosetting playlists is enabled, setting up timer.");
                this.calculateTimeAndSetAutoSetPlaylistsTimer();
            }
            else
            {
                EventAggregator.Instance.Publish(new AutoSettingPlaylistsStateChangedMessage(true, "Auto setting playlists is disabled."));
            }
        }

        private void selectedYoutubeAccountChanged(SelectedYoutubeAccountChangedMessage selectedYoutubeAccountChangedMessage)
        {
            if (selectedYoutubeAccountChangedMessage.NewAccount == null)
            {
                throw new ArgumentException("Changed Youtube account must not be null.");
            }

            this.youtubeAccountForRequestingPlaylists = selectedYoutubeAccountChangedMessage.NewAccount.Name == "All" ?
                selectedYoutubeAccountChangedMessage.FirstNotAllAccount : selectedYoutubeAccountChangedMessage.NewAccount;

            foreach (PlaylistComboboxViewModel playlistComboboxViewModel in this.observablePlaylistViewModels)
            {
                if (selectedYoutubeAccountChangedMessage.NewAccount.Name == "All")
                {
                    if (playlistComboboxViewModel.Visible == false)
                    {
                        playlistComboboxViewModel.Visible = true;
                    }
                }
                else
                {
                    if (playlistComboboxViewModel.YoutubeAccountName == selectedYoutubeAccountChangedMessage.NewAccount.Name)
                    {
                        playlistComboboxViewModel.Visible = true;
                    }
                    else
                    {
                        playlistComboboxViewModel.Visible = false;
                    }
                }
            }

            //is null if there is no playlist in previously selected account
            if (this.selectedPlaylist == null || this.selectedPlaylist.Visible == false)
            {
                PlaylistComboboxViewModel viewModel = this.observablePlaylistViewModels.GetFirstViewModel(vm => vm.Visible == true);
                this.SelectedPlaylist = viewModel;
                this.raisePropertyChanged("SelectedPlaylist");
            }
        }

        private void beforeYoutubeAccountDelete(BeforeYoutubeAccountDeleteMessage beforeYoutubeAccountDeleteMessage)
        {
            List<Playlist> playlistsToRemove = this.playlistList.FindAll(playlist => playlist.YoutubeAccount == beforeYoutubeAccountDeleteMessage.AccountToBeDeleted);
            foreach (Playlist playlist in playlistsToRemove)
            {
                this.playlistList.Remove(playlist);
            }
            JsonSerializationContent.JsonSerializer.SerializePlaylistList();
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

        public async void OpenNewPlaylistDialogAsync(object obj)
        {
            var view = new NewPlaylistControl
            {
                DataContext = new NewPlaylistViewModel(this.playlistList.GetReadOnlyPlaylistList().Select(playlist => playlist.PlaylistId).ToList(), this.observableYoutubeAccountViewModels, this.youtubeAccountForRequestingPlaylists)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewPlaylistViewModel data = (NewPlaylistViewModel)view.DataContext;
                foreach (PlaylistSelectionViewModel playlistSelectionViewModel in data.ObservablePlaylistSelectionViewModels)
                {
                    if (playlistSelectionViewModel.IsChecked)
                    {
                        Playlist playlist = new Playlist(playlistSelectionViewModel.Id, playlistSelectionViewModel.Title, data.SelectedYoutubeAccount.YoutubeAccount);
                        this.AddPlaylist(playlist);
                    }
                }
            }
        }

        public void DeletePlaylist(object playlistId)
        {       
            Playlist playlist = this.playlistList.GetPlaylist((string)playlistId);

            //Needs to set before deleting the ViewModel in ObservableTemplateViewModels, otherwise the RaiseNotifyCollectionChanged
            //will set the SelectedTemplate to null which causes problems if there are templates left
            PlaylistComboboxViewModel viewModel = this.observablePlaylistViewModels.GetFirstViewModel(vm => vm.Playlist != playlist && vm.Visible == true);
            this.SelectedPlaylist = viewModel;

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

            this.SelectedPlaylist = this.observablePlaylistViewModels.GetViewModel(playlist);
        }

        private void autoSetPlaylistTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: Start autosetting playlists.");
            this.autoSetPlaylistsAsync(null);
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: End autosetting playlists.");
        }

        private async void autoSetPlaylistsAsync(object obj)
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
                EventAggregator.Instance.Publish(new AutoSettingPlaylistsStateChangedMessage(true, "Auto setting playlists is running..."));
                this.raisePropertyChanged("AutoSettingPlaylists");
            }

            bool success = true;
            StringBuilder message = new StringBuilder();
            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Get uploads without playlists but with autoset templates.");
            Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap = this.getPlaylistUploadsWithoutPlaylistMap();

            if (playlistUploadsWithoutPlaylistMap.Count > 0)
            {
                message.AppendLine($"{DateTime.Now} adding videos to playlists.");
                int originalPlaylistCount = playlistUploadsWithoutPlaylistMap.Count;
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if playlists exist on Youtube.");
                //check if all needed playlists exist on youtube and if not mark them as not existing and remove from playlistUploadsWithoutPlaylistMap
                Dictionary<string, List<string>> playlistVideos = await this.getPlaylistsAndRemoveNotExistingPlaylistsAsync(playlistUploadsWithoutPlaylistMap).ConfigureAwait(false);

                if (playlistUploadsWithoutPlaylistMap.Count > 0)
                {
                    if (originalPlaylistCount != playlistUploadsWithoutPlaylistMap.Count)
                    {
                        success = false;
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one playlist was removed on YoutTube.");
                        message.AppendLine("At least one playlist was removed on Youtube.");
                    }

                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads are already in playlist.");
                    bool uploadAlreadyInPlaylist = !this.removeUploadsAlreadyInPlaylist(playlistUploadsWithoutPlaylistMap, playlistVideos);

                    if (playlistUploadsWithoutPlaylistMap.Count > 0)
                    {
                        if (uploadAlreadyInPlaylist)
                        {
                            success = false;
                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one upload was already in playlist on Youtube.");
                            message.AppendLine("At least one video to add to playlist was already in playlist on Youtube.");
                        }

                        int originalVideoCount = playlistUploadsWithoutPlaylistMap.Values.Sum(list => list.Count);
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads exist on Youtube.");
                        var videosPublicMap = await this.getPublicVideosAndRemoveNotExisingUploadsAsync(playlistUploadsWithoutPlaylistMap).ConfigureAwait(false);

                        if (playlistUploadsWithoutPlaylistMap.Count > 0)
                        {
                            if (originalVideoCount != videosPublicMap.Count)
                            {
                                success = false;
                                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one upload was removed on Youtube.");
                                message.AppendLine("At least one video to add to playlist was removed on Youtube.");
                            }

                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads is public and add to playlist.");
                            await this.addUploadsToPlaylistIfPublicAsync(playlistUploadsWithoutPlaylistMap, videosPublicMap).ConfigureAwait(false);
                        }
                        else
                        {
                            success = false;
                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No uploads left after public/exist check on Youtube.");
                            message.AppendLine("All videos to add to playlists were removed on Youtube.");
                        }
                    }
                    else
                    {
                        success = false;
                        message.AppendLine("All videos to add to playlists were already in playlist.");
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after uploads already in playlist check on Youtube.");
                    }

                    JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                }
                else
                {
                    success = false;
                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after availability check on Youtube.");
                    message.AppendLine("All playlists with videos to add were removed on Youtube. ");
                }
            }
            else
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Nothing to do.");
                message.AppendLine($"{DateTime.Now} no videos to add to playlists.");
            }

            Settings.Instance.UserSettings.LastAutoAddToPlaylist = DateTime.Now;
            JsonSerializationSettings.JsonSerializer.SerializeSettings();

            if (Settings.Instance.UserSettings.AutoSetPlaylists)
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Autosetting playlists is enabled, setting timer to {this.interval.Days * 24 + this.interval.Hours}:{this.interval.Minutes}:{this.interval.Seconds} hours:minutes:seconds.");
                //stop timer if triggered manually and restart
                this.autoSetPlaylistTimer.Stop();
                //first call after constructor the timer can be less than interval.
                this.autoSetPlaylistTimer.Interval = this.intervalInSeconds * 1000;
                message.AppendLine($"Next auto set playlists: {DateTime.Now.AddMilliseconds(this.autoSetPlaylistTimer.Interval)}."); 
                this.autoSetPlaylistTimer.Start();
            }

            this.autoSettingPlaylists = false;
            this.raisePropertyChanged("AutoSettingPlaylists");
            EventAggregator.Instance.Publish(new AutoSettingPlaylistsStateChangedMessage(success, message.ToString().TrimEnd('\r', '\n')));

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

        private async Task<Dictionary<string, List<string>>> getPlaylistsAndRemoveNotExistingPlaylistsAsync(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap)
        {
            Dictionary<string, List<string>> playlistVideos = await YoutubePlaylistItemService.GetPlaylistsContentAsync(playlistUploadsWithoutPlaylistMap.Keys.ToList(), "default").ConfigureAwait(false);
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

        private bool removeUploadsAlreadyInPlaylist(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap, Dictionary<string, List<string>> playlistVideos)
        {
            bool allVideosNotInPlaylist = true;
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

                if (uploadsToRemove.Count > 0)
                {
                    allVideosNotInPlaylist = false;
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

            return allVideosNotInPlaylist;
        }

        private async Task<Dictionary<string, bool>> getPublicVideosAndRemoveNotExisingUploadsAsync(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap)
        {
            List<string> noUploadsLeftPlaylists = new List<string>();
            Dictionary<string, bool> videosPublicMap = await YoutubeVideoService.IsPublicAsync(playlistUploadsWithoutPlaylistMap
                .SelectMany(kvp => kvp.Value).Select(upload => upload.VideoId).ToList(), "default").ConfigureAwait(false);

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

        private async Task addUploadsToPlaylistIfPublicAsync(Dictionary<string, List<Upload>> playlistUploadsWithoutPlaylistMap, Dictionary<string, bool> videosPublicMap)
        {

            foreach (KeyValuePair<string, List<Upload>> playlistVideosWithoutPlaylist in playlistUploadsWithoutPlaylistMap)
            {
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    if (videosPublicMap[upload.VideoId])
                    {
                        upload.Playlist = this.playlistList.GetPlaylist(playlistVideosWithoutPlaylist.Key);
                        if (!await YoutubePlaylistItemService.AddToPlaylistAsync(upload).ConfigureAwait(false))
                        {
                            upload.Playlist = null;
                        }
                    }
                }
            }
        }

        private void setAutoSetPlaylistsTimer()
        {
            if (Settings.Instance.UserSettings.AutoSetPlaylists)
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
            TimeSpan span = DateTime.Now - Settings.Instance.UserSettings.LastAutoAddToPlaylist;
            Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Last autoset is {span.Days * 24 + span.Hours}:{span.Minutes}:{span.Seconds} hours:minutes:seconds ago.");

            if (span.TotalSeconds > this.intervalInSeconds)
            {
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Starting autosetting playlists immediately.");
                this.autoSetPlaylistsAsync(null);
            }
            else
            {
                double nextAutoSetPlaylists = ((this.intervalInSeconds - span.TotalSeconds) * 1000);
                span = TimeSpan.FromMilliseconds(nextAutoSetPlaylists);
                Tracer.Write($"PlaylistViewModel.PlaylistViewModel: Scheduling autosetting playlists in {span.Days * 24 + span.Hours}:{span.Minutes}:{span.Seconds} hours:minutes:seconds.");

                this.autoSetPlaylistTimer.Interval = nextAutoSetPlaylists;
                EventAggregator.Instance.Publish(new AutoSettingPlaylistsStateChangedMessage(true, $"Next auto setting playlists: {DateTime.Now.AddMilliseconds(this.autoSetPlaylistTimer.Interval)}."));
                this.autoSetPlaylistTimer.Start();
            }
        }
    }
}
