using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItemService;
using Drexel.VidUp.Youtube.PlaylistService;
using Drexel.VidUp.Youtube.VideoService;
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
                EventAggregator.Instance.Publish(new PlaylistDisplayPropertyChangedMessage("title"));
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

            this.youtubeAccountForRequestingPlaylists = selectedYoutubeAccountChangedMessage.NewAccount;
            if (selectedYoutubeAccountChangedMessage.NewAccount.IsDummy)
            {
                if (selectedYoutubeAccountChangedMessage.NewAccount.Name == "All")
                {
                    this.youtubeAccountForRequestingPlaylists = selectedYoutubeAccountChangedMessage.FirstNotAllAccount;
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
            this.playlistList.DeletePlaylists(pl => playlistsToRemove.Contains(pl));
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
                DataContext = await NewPlaylistViewModel.Create(
                    this.playlistList.GetReadOnlyPlaylistList().Select(playlist => playlist.PlaylistId).ToList(), this.observableYoutubeAccountViewModels, this.youtubeAccountForRequestingPlaylists)
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

            this.playlistList.DeletePlaylists(pl => pl == playlist);

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
            this.playlistList.AddPlaylist(playlist);
            JsonSerializationContent.JsonSerializer.SerializePlaylistList();

            this.SelectedPlaylist = this.observablePlaylistViewModels.GetViewModel(playlist);
        }

        private void autoSetPlaylistTimerElapsed(object sender, ElapsedEventArgs e)
        {
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: Start autosetting playlists.");
            this.autoSetPlaylistsAsync(null);
            Tracer.Write($"PlaylistViewModel.autoSetPlaylistTimerElapsed: End autosetting playlists.");
        }

        //todo: rework...
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
            List<StatusInformation> messages = new List<StatusInformation>();
            messages.Add(new StatusInformation($"{DateTime.Now} Starting autosetting playlists."));
            try
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Get uploads without playlists but with autoset templates.");

                //get potential uploads to add to playlists
                Dictionary<YoutubeAccount, Dictionary<Playlist, List<Upload>>> uploadsByPlaylistByAccount = this.getUploadsByPlaylistByAccount();

                if (uploadsByPlaylistByAccount.Count > 0)
                {
                    messages.Add(new StatusInformation($"{DateTime.Now} Try adding videos to playlists, potential uploads without playlist found."));

                    foreach (KeyValuePair<YoutubeAccount, Dictionary<Playlist, List<Upload>>> accountPlaylists in uploadsByPlaylistByAccount)
                    {
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Get uploads without playlists but with autoset templates.");
                        messages.Add(new StatusInformation($"{DateTime.Now} Processing account {accountPlaylists.Key.Name}."));
                        Dictionary<Playlist, List<Upload>> uploadsWithoutPlaylistByPlaylist = accountPlaylists.Value;

                        int originalPlaylistCount = uploadsWithoutPlaylistByPlaylist.Count;
                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if playlists exist on Youtube.");

                        //check if all needed playlists exist on youtube and if not mark them as not existing and remove from playlistUploadsWithoutPlaylistMap
                        messages.Add(new StatusInformation($"{DateTime.Now} Receiving current playlist content."));
                        GetPlaylistsAndRemoveNotExistingPlaylistsResult getPlaylistsAndRemoveNotExistingPlaylistsResult = await this.getPlaylistsAndRemoveNotExistingPlaylistsAsync(uploadsWithoutPlaylistByPlaylist).ConfigureAwait(false);
                        if (getPlaylistsAndRemoveNotExistingPlaylistsResult.StatusInformation != null)
                        {
                            success = false;
                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Could not receive playlist content:\n{getPlaylistsAndRemoveNotExistingPlaylistsResult.StatusInformation.Message}");
                            messages.Add(new StatusInformation($"{DateTime.Now} Could not receive playlist content:\n{getPlaylistsAndRemoveNotExistingPlaylistsResult.StatusInformation.Message}"));
                        }
                        else
                        {
                            StringBuilder tempStringBuilderForChangeLists = new StringBuilder();

                            if (originalPlaylistCount != uploadsWithoutPlaylistByPlaylist.Count)
                            {
                                success = false;

                                foreach (Playlist removedPlaylist in getPlaylistsAndRemoveNotExistingPlaylistsResult.RemovedPlaylists)
                                {
                                    tempStringBuilderForChangeLists.AppendLine($"{removedPlaylist.Title} ({removedPlaylist.PlaylistId})");
                                }

                                string removedPlaylistsString = tempStringBuilderForChangeLists.ToString();

                                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one playlist was removed on YoutTube:\n{removedPlaylistsString}");
                                messages.Add(new StatusInformation($"{DateTime.Now} These playlists were removed on Youtube:\n{removedPlaylistsString}"));
                            }

                            if (uploadsWithoutPlaylistByPlaylist.Count > 0)
                            {
                                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads are already in playlist.");

                                //clear uploadsWithoutPlaylistByPlaylist from uploads already and playlists on youtube and remove playlists where nothing is left to do.
                                messages.Add(new StatusInformation($"{DateTime.Now} Checking if videos are already in playlists."));
                                Dictionary<Playlist, List<Upload>> uploadsAlreadyInPlaylistByPlaylist = this.removeUploadsAlreadyInPlaylist(uploadsWithoutPlaylistByPlaylist, getPlaylistsAndRemoveNotExistingPlaylistsResult.PlaylistItemsByPlaylist);

                                if (uploadsAlreadyInPlaylistByPlaylist.Count > 0)
                                {
                                    success = false;

                                    tempStringBuilderForChangeLists.Clear();
                                    foreach (KeyValuePair<Playlist, List<Upload>> removedUploads in uploadsAlreadyInPlaylistByPlaylist)
                                    {
                                        tempStringBuilderForChangeLists.AppendLine($"{removedUploads.Key.Title} ({removedUploads.Key.PlaylistId})");
                                        foreach (Upload upload in removedUploads.Value)
                                        {
                                            tempStringBuilderForChangeLists.AppendLine($"   {upload.Title} ({upload.VideoId})");
                                        }
                                    }

                                    string removedUploadsAlreadyInPlaylistString = tempStringBuilderForChangeLists.ToString();

                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one upload was already in playlist on Youtube:\n{removedUploadsAlreadyInPlaylistString}");
                                    messages.Add(new StatusInformation($"{DateTime.Now} These videos were already in playlist on Youtube:\n{removedUploadsAlreadyInPlaylistString}"));
                                }

                                if (uploadsWithoutPlaylistByPlaylist.Count > 0)
                                {
                                    int originalVideoCount = uploadsWithoutPlaylistByPlaylist.Values.Sum(list => list.Count);
                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Check if uploads exist on Youtube.");

                                    //clear uploadsWithoutPlaylistByPlaylist also from uploads not on youtube anymore and remove playlists where nothing is left to do.
                                    messages.Add(new StatusInformation($"{DateTime.Now} Checking if videos are public."));
                                    GetPublicVideosAndRemoveNotExistingUploadsResult getPublicVideosAndRemoveNotExisingUploadsResult = await this.getPublicVideosAndRemoveNotExistingUploadsAsync(uploadsWithoutPlaylistByPlaylist).ConfigureAwait(false);
                                    if (getPublicVideosAndRemoveNotExisingUploadsResult.StatusInformation != null)
                                    {
                                        success = false;
                                        Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Video public check not successful:\n{getPublicVideosAndRemoveNotExisingUploadsResult.StatusInformation.Message}");
                                        messages.Add(new StatusInformation($"{DateTime.Now} Could not check if videos are public:\n{getPublicVideosAndRemoveNotExisingUploadsResult.StatusInformation.Message}"));
                                    }
                                    else
                                    {
                                        if (originalVideoCount != getPublicVideosAndRemoveNotExisingUploadsResult.PublicByVideo.Count)
                                        {
                                            success = false;

                                            tempStringBuilderForChangeLists.Clear();
                                            foreach (KeyValuePair<Playlist, List<Upload>> removedUploads in getPublicVideosAndRemoveNotExisingUploadsResult.UploadsNotExistOnYouTubeByPlaylist)
                                            {
                                                tempStringBuilderForChangeLists.AppendLine($"{removedUploads.Key.Title} ({removedUploads.Key.PlaylistId})");
                                                foreach (Upload upload in removedUploads.Value)
                                                {
                                                    tempStringBuilderForChangeLists.AppendLine($"   {upload.Title} ({upload.VideoId})");
                                                }
                                            }

                                            string removedUploadsDeletedOnYoutubeString = tempStringBuilderForChangeLists.ToString();

                                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one upload was removed on Youtube:\n{removedUploadsDeletedOnYoutubeString}");
                                            messages.Add(new StatusInformation($"{DateTime.Now} These videos were removed on Youtube.\n{removedUploadsDeletedOnYoutubeString}"));
                                        }

                                        if (uploadsWithoutPlaylistByPlaylist.Count > 0)
                                        {
                                            if (getPublicVideosAndRemoveNotExisingUploadsResult.PublicByVideo.All(upl => upl.Value == false))
                                            {
                                                messages.Add(new StatusInformation($"{DateTime.Now} All videos without playlist are still private."));
                                            }
                                            else
                                            {
                                                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Add public videos to playlist.");
                                                messages.Add(new StatusInformation($"{DateTime.Now} Adding videos to playlists."));
                                                AddUploadsToPlaylistIfPublicResult addUploadsToPlaylistIfPublicResult = await this.addUploadsToPlaylistIfPublicAsync(uploadsWithoutPlaylistByPlaylist, getPublicVideosAndRemoveNotExisingUploadsResult.PublicByVideo).ConfigureAwait(false);
                                                if (addUploadsToPlaylistIfPublicResult.StatusInformation != null)
                                                {
                                                    success = false;
                                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Could not add public videos to playlists:\n{addUploadsToPlaylistIfPublicResult.StatusInformation.Message}");
                                                    messages.Add(new StatusInformation($"{DateTime.Now} Could not add public videos to playlists:\n{addUploadsToPlaylistIfPublicResult.StatusInformation.Message}"));
                                                }

                                                if (addUploadsToPlaylistIfPublicResult.UploadsNotAddedByPlaylist.Count > 0)
                                                {
                                                    success = false;

                                                    tempStringBuilderForChangeLists.Clear();
                                                    foreach (KeyValuePair<Playlist, List<Upload>> notAddedUploads in addUploadsToPlaylistIfPublicResult.UploadsNotAddedByPlaylist)
                                                    {
                                                        tempStringBuilderForChangeLists.AppendLine($"{notAddedUploads.Key.Title} ({notAddedUploads.Key.PlaylistId})");
                                                        foreach (Upload upload in notAddedUploads.Value)
                                                        {
                                                            tempStringBuilderForChangeLists.AppendLine($"   {upload.Title} ({upload.VideoId}): {upload.UploadErrorMessage}");
                                                        }
                                                    }

                                                    string notAddedUploadsToPlaylistsString = tempStringBuilderForChangeLists.ToString();

                                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: At least one upload could not be added to playlist:\n{notAddedUploadsToPlaylistsString}");
                                                    messages.Add(new StatusInformation($"{DateTime.Now} These videos could not be added to playlist:\n{notAddedUploadsToPlaylistsString}"));
                                                }

                                                if (addUploadsToPlaylistIfPublicResult.UploadsAddedByPlaylist.Count > 0)
                                                {
                                                    tempStringBuilderForChangeLists.Clear();
                                                    foreach (KeyValuePair<Playlist, List<Upload>> addedUploads in addUploadsToPlaylistIfPublicResult.UploadsAddedByPlaylist)
                                                    {
                                                        tempStringBuilderForChangeLists.AppendLine($"{addedUploads.Key.Title} ({addedUploads.Key.PlaylistId})");
                                                        foreach (Upload upload in addedUploads.Value)
                                                        {
                                                            tempStringBuilderForChangeLists.AppendLine($"   {upload.Title} ({upload.VideoId})");
                                                        }
                                                    }

                                                    string addedUploadsToPlaylistsString = tempStringBuilderForChangeLists.ToString();

                                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Added these videos to playlists:\n{addedUploadsToPlaylistsString}");
                                                    messages.Add(new StatusInformation($"{DateTime.Now} Added these videos to playlists:\n{addedUploadsToPlaylistsString}"));
                                                }
                                            }
                                        }
                                        else
                                        {
                                            success = false;
                                            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No uploads left after public/exist check on Youtube.");
                                            messages.Add(new StatusInformation($"{DateTime.Now} All videos to add to playlists were removed on Youtube."));
                                        }
                                    }
                                }
                                else
                                {
                                    success = false;
                                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: All videos to add to playlists were already in playlist.");
                                    messages.Add(new StatusInformation($"{DateTime.Now} All videos to add to playlists were already in playlist."));
                                }

                                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                            }
                            else
                            {
                                success = false;
                                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No playlists left after availability check on Youtube.");
                                messages.Add(new StatusInformation($"{DateTime.Now} All playlists with videos to add were removed on Youtube."));
                            }
                        }
                    }
                }
                else
                {
                    Tracer.Write($"PlaylistViewModel.autoSetPlaylists: No videos for autosetting playlists without playlist found.");
                    messages.Add(new StatusInformation($"{DateTime.Now} No videos to add to playlists."));
                }

            }
            catch (Exception e)
            {
                success = false;
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Unexpected Exception: {e.ToString()}.");
                messages.Add(new StatusInformation($"{DateTime.Now} Something went wrong on autosetting playlists: {e.ToString()}."));
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
                messages.Add(new StatusInformation($"{DateTime.Now} Next auto set playlists: {DateTime.Now.AddMilliseconds(this.autoSetPlaylistTimer.Interval)}.")); 
                this.autoSetPlaylistTimer.Start();
            }
            else
            {
                Tracer.Write($"PlaylistViewModel.autoSetPlaylists: Autosetting playlists is disabled.");
                messages.Add(new StatusInformation($"{DateTime.Now} Autosetting playlists is disabled."));
            }

            this.autoSettingPlaylists = false;
            this.raisePropertyChanged("AutoSettingPlaylists");

            StringBuilder message = new StringBuilder();
            if (messages.Any(messageInternal => messageInternal.IsQuotaError))
            {
                message.AppendLine(TinyHelpers.QuotaExceededString);
            }

            if (messages.Any(messageInternal => messageInternal.IsApiAuthenticationError))
            {
                message.AppendLine(TinyHelpers.AuthenticationErrorString);
            }

            foreach (StatusInformation statusInformation in messages)
            {
                message.AppendLine(statusInformation.Message);
            }

            EventAggregator.Instance.Publish(new AutoSettingPlaylistsStateChangedMessage(success, TinyHelpers.TrimLineBreakAtEnd(message.ToString())));

            Tracer.Write($"PlaylistViewModel.autoSetPlaylists: End.");
        }


        //get uploads without playlists and where template has SetPlaylistAfterPublication active
        private Dictionary<YoutubeAccount, Dictionary<Playlist, List<Upload>>> getUploadsByPlaylistByAccount()
        {
            Dictionary<YoutubeAccount, Dictionary<Playlist, List<Upload>>> uploadsByPlaylistByAccount = new Dictionary<YoutubeAccount, Dictionary<Playlist, List<Upload>>>();
            foreach (Template template in this.templateList)
            {
                if (template.SetPlaylistAfterPublication && template.Playlist != null && !template.Playlist.NotExistsOnYoutube)
                {
                    Upload[] uploads = template.Uploads.Where(
                        upload1 => upload1.UploadStatus == UplStatus.Finished && !string.IsNullOrWhiteSpace(upload1.VideoId) &&
                                   upload1.Playlist == null && !upload1.NotExistsOnYoutube).ToArray();

                    if (uploads.Length >= 1)
                    {
                        Dictionary<Playlist, List<Upload>> uploadsByPlaylist;
                        if (!uploadsByPlaylistByAccount.TryGetValue(template.YoutubeAccount, out uploadsByPlaylist))
                        {
                            uploadsByPlaylist = new Dictionary<Playlist, List<Upload>>();
                            uploadsByPlaylistByAccount.Add(template.YoutubeAccount, uploadsByPlaylist);
                        }

                        List<Upload> playlistUploads;
                        if (!uploadsByPlaylist.TryGetValue(template.Playlist, out playlistUploads))
                        {
                            playlistUploads = new List<Upload>();
                            uploadsByPlaylist.Add(template.Playlist, playlistUploads);
                        }

                        playlistUploads.AddRange(uploads);
                    }
                }
            }

            return uploadsByPlaylistByAccount;
        }

        //gets all item/video ids of the playlists and checks if playlists exists on youtube
        //and removes playlists which don't exist on youtube
        private async Task<GetPlaylistsAndRemoveNotExistingPlaylistsResult> getPlaylistsAndRemoveNotExistingPlaylistsAsync(Dictionary<Playlist, List<Upload>> uploadsWithoutPlaylistByPlaylist)
        {
            GetPlaylistsAndRemoveNotExistingPlaylistsResult result = new GetPlaylistsAndRemoveNotExistingPlaylistsResult();

            GetPlaylistsPlaylistItemsResult getPlaylistsPlaylistItemsResult = await YoutubePlaylistItemService.GetPlaylistsPlaylistItemsAsync(uploadsWithoutPlaylistByPlaylist.Keys.ToList()).ConfigureAwait(false);
            if (getPlaylistsPlaylistItemsResult.StatusInformation != null)
            {
                result.StatusInformation = getPlaylistsPlaylistItemsResult.StatusInformation;
                return result;
            }

            //key are distinct, so no distinct is need here like in GetPlaylistsPlaylistItemsAsync
            if (uploadsWithoutPlaylistByPlaylist.Keys.Count != getPlaylistsPlaylistItemsResult.PlaylistItemsByPlaylist.Count)
            {
                KeyValuePair<Playlist, List<Upload>>[] missingPlaylists = uploadsWithoutPlaylistByPlaylist
                    .Where(kvp => !getPlaylistsPlaylistItemsResult.PlaylistItemsByPlaylist.ContainsKey(kvp.Key)).ToArray();

                List<Playlist> removedPlaylists = new List<Playlist>();
                foreach (KeyValuePair<Playlist, List<Upload>> playlistUploadsMap in missingPlaylists)
                {
                    playlistUploadsMap.Key.NotExistsOnYoutube = true;
                    uploadsWithoutPlaylistByPlaylist.Remove(playlistUploadsMap.Key);
                    removedPlaylists.Add(playlistUploadsMap.Key);
                }

                result.RemovedPlaylists = removedPlaylists;
                JsonSerializationContent.JsonSerializer.SerializePlaylistList();
            }

            result.PlaylistItemsByPlaylist = getPlaylistsPlaylistItemsResult.PlaylistItemsByPlaylist;
            return result;
        }


        //removes uploads without playlist already in playlists and removes playlists where are no uploads without playlist left
        private Dictionary<Playlist, List<Upload>> removeUploadsAlreadyInPlaylist(Dictionary<Playlist, List<Upload>> uploadsByPlaylist, Dictionary<Playlist, List<string>> playlistVideos)
        {
            Dictionary<Playlist, List<Upload>> uploadsAlreadyInPlaylistByPlaylist = new Dictionary<Playlist, List<Upload>>();
            //check if videos are already in playlist on YT.
            List<Playlist> noUploadsLeftPlaylists = new List<Playlist>();
            foreach (KeyValuePair<Playlist, List<Upload>> playlistVideosWithoutPlaylist in uploadsByPlaylist)
            {
                List<Upload> uploadsToRemove = new List<Upload>();
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    if (playlistVideos[playlistVideosWithoutPlaylist.Key].Contains(upload.VideoId))
                    {
                        upload.Playlist = playlistVideosWithoutPlaylist.Key;
                        uploadsToRemove.Add(upload);

                        List<Upload> uploads;
                        if (!uploadsAlreadyInPlaylistByPlaylist.TryGetValue(playlistVideosWithoutPlaylist.Key, out uploads))
                        {
                            uploads = new List<Upload>();
                            uploadsAlreadyInPlaylistByPlaylist.Add(playlistVideosWithoutPlaylist.Key, uploads);
                        }

                        uploads.Add(upload);
                    }
                }

                playlistVideosWithoutPlaylist.Value.RemoveAll(upload => uploadsToRemove.Contains(upload));
                if (playlistVideosWithoutPlaylist.Value.Count <= 0)
                {
                    noUploadsLeftPlaylists.Add(playlistVideosWithoutPlaylist.Key);
                }
            }

            foreach (Playlist playlist in noUploadsLeftPlaylists)
            {
                uploadsByPlaylist.Remove(playlist);
            }

            return uploadsAlreadyInPlaylistByPlaylist;
        }

        private async Task<GetPublicVideosAndRemoveNotExistingUploadsResult> getPublicVideosAndRemoveNotExistingUploadsAsync(Dictionary<Playlist, List<Upload>> uploadsWithoutPlaylistByPlaylist)
        {
            GetPublicVideosAndRemoveNotExistingUploadsResult result = new GetPublicVideosAndRemoveNotExistingUploadsResult();

            List<Playlist> noUploadsLeftPlaylists = new List<Playlist>();

            //transform into videos by account
            Dictionary<YoutubeAccount, List<string>> videoIdsByAccount = new Dictionary<YoutubeAccount, List<string>>();
            foreach (KeyValuePair<Playlist, List<Upload>> uploadsByPlaylist in uploadsWithoutPlaylistByPlaylist)
            {
                List<string> videoIds;
                if (!videoIdsByAccount.TryGetValue(uploadsByPlaylist.Key.YoutubeAccount, out videoIds))
                {
                    videoIds = new List<string>();
                    videoIdsByAccount.Add(uploadsByPlaylist.Key.YoutubeAccount, videoIds);
                }

                videoIds.AddRange(uploadsByPlaylist.Value.Select(upload => upload.VideoId));
            }

            foreach (KeyValuePair<YoutubeAccount, List<string>> accountVideoIds in videoIdsByAccount)
            {
                IsPublicResult isPublicResult = await YoutubeVideoService.IsPublicAsync(accountVideoIds.Key, accountVideoIds.Value).ConfigureAwait(false);
                if (isPublicResult.StatusInformation != null)
                {
                    result.StatusInformation = isPublicResult.StatusInformation;
                    return result;
                }

                foreach (KeyValuePair<Playlist, List<Upload>> playlistVideosWithoutPlaylist in uploadsWithoutPlaylistByPlaylist)
                {
                    foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                    {
                        //if video id is not included in response it was deleted on YT.
                        if (!isPublicResult.IsPublicByVideoId.ContainsKey(upload.VideoId))
                        {
                            upload.NotExistsOnYoutube = true;

                            List<Upload> playlistUploads;
                            if (!result.UploadsNotExistOnYouTubeByPlaylist.TryGetValue(playlistVideosWithoutPlaylist.Key, out playlistUploads))
                            {
                                playlistUploads = new List<Upload>();
                                result.UploadsNotExistOnYouTubeByPlaylist.Add(playlistVideosWithoutPlaylist.Key, playlistUploads);
                            }

                            playlistUploads.Add(upload);
                        }
                        else
                        {
                            result.PublicByVideo.Add(upload.VideoId, isPublicResult.IsPublicByVideoId[upload.VideoId]);
                        }
                    }

                    playlistVideosWithoutPlaylist.Value.RemoveAll(upload => upload.NotExistsOnYoutube);
                    if (playlistVideosWithoutPlaylist.Value.Count <= 0)
                    {
                        noUploadsLeftPlaylists.Add(playlistVideosWithoutPlaylist.Key);
                    }
                }

                foreach (Playlist playlist in noUploadsLeftPlaylists)
                {
                    uploadsWithoutPlaylistByPlaylist.Remove(playlist);
                }
            }

            return result;
        }

        private async Task<AddUploadsToPlaylistIfPublicResult> addUploadsToPlaylistIfPublicAsync(Dictionary<Playlist, List<Upload>> uploadsWithoutPlaylistByPlaylist, Dictionary<string, bool> videosPublicMap)
        {
            AddUploadsToPlaylistIfPublicResult result = new AddUploadsToPlaylistIfPublicResult();

            foreach (KeyValuePair<Playlist, List<Upload>> playlistVideosWithoutPlaylist in uploadsWithoutPlaylistByPlaylist)
            {
                foreach (Upload upload in playlistVideosWithoutPlaylist.Value)
                {
                    if (videosPublicMap[upload.VideoId])
                    {
                        upload.Playlist = playlistVideosWithoutPlaylist.Key;
                        AddToPlaylistResult addToPlaylistResult = await YoutubePlaylistItemService.AddToPlaylistAsync(upload).ConfigureAwait(false);
                        if (addToPlaylistResult.StatusInformation != null)
                        {
                            if (addToPlaylistResult.StatusInformation.IsQuotaError)
                            {
                                result.StatusInformation = addToPlaylistResult.StatusInformation;
                                return result;
                            }
                        }

                        if (!addToPlaylistResult.Success)
                        {
                            upload.Playlist = null;
                            List<Upload> notAddedUploads;
                            if (!result.UploadsNotAddedByPlaylist.TryGetValue(playlistVideosWithoutPlaylist.Key, out notAddedUploads))
                            {
                                notAddedUploads = new List<Upload>();
                                result.UploadsNotAddedByPlaylist.Add(playlistVideosWithoutPlaylist.Key, notAddedUploads);
                            }

                            notAddedUploads.Add(upload);
                        }
                        else
                        {
                            List<Upload> addedUploads;
                            if (!result.UploadsAddedByPlaylist.TryGetValue(playlistVideosWithoutPlaylist.Key, out addedUploads))
                            {
                                addedUploads = new List<Upload>();
                                result.UploadsAddedByPlaylist.Add(playlistVideosWithoutPlaylist.Key, addedUploads);
                            }

                            addedUploads.Add(upload);
                        }
                    }
                }
            }

            return result;
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
