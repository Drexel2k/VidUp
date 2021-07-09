using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Shell;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Definitions;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.UI.Events;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube;
using Drexel.VidUp.Youtube.Authentication;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int tabNo;
        private List<object> viewModels = new List<object>(new object[5]);

        private AppStatus appStatus = AppStatus.Idle;
        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAll;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        private PostUploadAction postUploadAction;
        private UploadStats uploadStats;

        private TaskbarState taskbarState = TaskbarState.Normal;

        private TemplateList templateList;
        private UploadList uploadList;
        private PlaylistList playlistList;
        private string autoSettingPlaylistsText;
        private Color autoSettingPlaylistsColor = MainWindowViewModel.blueColor;
        private static Color blueColor = (Color) ColorConverter.ConvertFromString("#03a9f4");

        public event PropertyChangedEventHandler PropertyChanged;

        public List<object> ViewModels
        {
            get => this.viewModels;
        }

        //is bound to grid row 1 (main window content) MainWindow.Xaml
        public object CurrentViewModel
        {
            get
            {
                return this.viewModels[this.tabNo];
            }
        }

        public AppStatus AppStatus
        {
            get => this.appStatus;
        }

        private TaskbarState taskbarStateInternal
        {
            set
            {
                this.taskbarState = value;
                this.raisePropertyChanged("TotalProgressPercentage");
                this.raisePropertyChanged("TaskbarItemProgressState");
            }
        }

        public PostUploadAction PostUploadAction
        {
            get => this.postUploadAction;
            set
            {
                this.postUploadAction = value;
                this.raisePropertyChanged("PostUploadAction");
            }
        }

        public Array PostUploadActions
        {
            get
            {
                return Enum.GetValues(typeof(PostUploadAction));
            }
        }

        public string AppTitle
        {
            get
            {
                Assembly assembly = System.Reflection.Assembly.GetExecutingAssembly();
                string product = ((AssemblyProductAttribute)Attribute.GetCustomAttribute(assembly, typeof(AssemblyProductAttribute), false)).Product;
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
                return string.Format("{0} {1}", product, version);
            }
        }

        public float TotalProgressPercentage
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return this.uploadStats.TotalProgressPercentage;
                }
                else
                {
                    if (this.taskbarState == TaskbarState.Normal)
                    {
                        return 0;
                    }
                    else
                    {
                        return 1;
                    }
                }
            }
        }

        public TaskbarItemProgressState TaskbarItemProgressState
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return TaskbarItemProgressState.Normal;
                }
                else
                {
                    if (this.taskbarState == TaskbarState.Normal)
                    {
                        return TaskbarItemProgressState.Normal;
                    }
                    else
                    {
                        return TaskbarItemProgressState.Paused;
                    }
                }
            }
        }

        public string CurrentFilePercent
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return string.Format("{0}%", this.uploadStats.CurrentFilePercent);
                }

                return "n/a";
            }
        }
        public string CurrentFileTimeLeft
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading && this.uploadStats.CurrentFileTimeLeft != null)
                {
                    return string.Format("{0}h {1}m {2}s",
                        (int)this.uploadStats.CurrentFileTimeLeft.Value.TotalHours,
                        this.uploadStats.CurrentFileTimeLeft.Value.Minutes,
                        this.uploadStats.CurrentFileTimeLeft.Value.Seconds);
                }

                return "n/a";
            }
        }

        public string CurrentFileMbLeft
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return this.uploadStats.CurrentFileMbLeft.ToString("N0", CultureInfo.CurrentCulture);
                }

                return "n/a";
            }
        }

        public string CurrentUploadSpeedInKiloBytesPerSecond
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return this.uploadStats.CurrentSpeedInKiloBytesPerSecond.ToString("N0", CultureInfo.CurrentCulture);
                }

                return "n/a";
            }
        }

        public string TotalTimeLeft
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading && this.uploadStats.TotalTimeLeft != null)
                {
                    return string.Format("{0}h {1}m {2}s",
                        (int)this.uploadStats.TotalTimeLeft.Value.TotalHours,
                        this.uploadStats.TotalTimeLeft.Value.Minutes,
                        this.uploadStats.TotalTimeLeft.Value.Seconds);
                }

                return "n/a";
            }
        }

        public string TotalMbLeft
        {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return this.uploadStats.TotalMbLeft.ToString("N0", CultureInfo.CurrentCulture);
                }

                if (((UploadListViewModel)this.viewModels[0]).ResumeUploads)
                {
                    return ((int)((float)this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable /
                                   Constants.ByteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
                }
                else
                {
                    return ((int)((float)this.uploadList.RemainingBytesOfFilesToUpload /
                                  Constants.ByteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
                }
            }
        }

        public string MaxUploadInKiloBytesPerSecond
        {
            get
            {
                return (((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond / 1024).ToString("N0", CultureInfo.CurrentCulture); ;
            }

            set
            {
                value = value.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, string.Empty);
                if (long.TryParse(value, out long kiloBytesPerSecond))
                {
                    long newUploadInBytesPerSecond = kiloBytesPerSecond * 1024;

                    // minimum 32 KiloBytes per second
                    if (newUploadInBytesPerSecond < 32768)
                    {
                        newUploadInBytesPerSecond = 0;
                    }

                    ((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond = newUploadInBytesPerSecond;
                }
                else
                {
                    ((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond = 0;
                }

                this.raisePropertyChanged("MaxUploadInKiloBytesPerSecond");
            }
        }

        public int TabNo
        {
            get
            {
                return this.tabNo;
            }
            set
            {
                if (this.tabNo != value)
                {
                    this.tabNo = value;
                    this.raisePropertyChanged("TabNo");
                    this.raisePropertyChanged("CurrentViewModel");
                }
            }
        }

        public int? WindowTop
        {
            get => Settings.SettingsInstance.UserSettings.WindowTop;
            set
            {
                Settings.SettingsInstance.UserSettings.WindowTop = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowLeft
        {
            get => Settings.SettingsInstance.UserSettings.WindowLeft;
            set
            {
                Settings.SettingsInstance.UserSettings.WindowLeft = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowHeight
        {
            get => Settings.SettingsInstance.UserSettings.WindowHeight;
            set
            {
                Settings.SettingsInstance.UserSettings.WindowHeight = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowWidth
        {
            get => Settings.SettingsInstance.UserSettings.WindowWidth;
            set
            {
                Settings.SettingsInstance.UserSettings.WindowWidth = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public bool AutoSettingPlaylists
        {
            get => ((PlaylistViewModel)this.viewModels[2]).AutoSettingPlaylists;
        }

        public string AutoSettingPlaylistsText
        {
            get => this.autoSettingPlaylistsText;
        }

        public SolidColorBrush AutoSettingPlaylistsColor
        {
            get
            {
                return new SolidColorBrush(this.autoSettingPlaylistsColor);
            }
        }

        public MainWindowViewModel()
        {
            this.initialize(null ,null, out _, out _, out _);
        }

        //for testing purposes
        public MainWindowViewModel(string user, string subFolder, out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            this.initialize(user, subFolder, out uploadList, out templateList, out playlistList);
        }

        private void initialize(string user, string subfolder, out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            if (string.IsNullOrWhiteSpace(user))
            {
                user = JsonDeserializationSettings.DeserializeUser();
            }

            Settings.SettingsInstance = new Settings(user, subfolder);

            this.checkAppDataFolder();

            this.deserializeSettings();
            ReSerialize reSerialize = this.deserializeContent();

            JsonSerializationContent.JsonSerializer = new JsonSerializationContent(Settings.SettingsInstance.StorageFolder, this.uploadList, this.templateList, this.playlistList);
            JsonSerializationSettings.JsonSerializer = new JsonSerializationSettings(Settings.SettingsInstance.StorageFolder, Settings.SettingsInstance.UserSettings);

            this.reSerialize(reSerialize);

            uploadList = this.uploadList;
            templateList = this.templateList;
            playlistList = this.playlistList;

            EventAggregator.Instance.Subscribe<UploadStatusChangedMessage>(this.uploadStatusChanged);
            EventAggregator.Instance.Subscribe<UploadStatsChangedMessage>(this.uploadStatsChanged);
            EventAggregator.Instance.Subscribe<AutoSettingPlaylistsStateChangedMessage>(this.autoSettingPlaylistsStateChanged);

            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList, false, false);
            this.observableTemplateViewModelsInclAllNone = new ObservableTemplateViewModels(this.templateList, true, true);
            this.observableTemplateViewModelsInclAll = new ObservableTemplateViewModels(this.templateList, true, false);
            this.observablePlaylistViewModels = new ObservablePlaylistViewModels(this.playlistList);

            UploadListViewModel uploadListViewModel = new UploadListViewModel(this.uploadList, this.observableTemplateViewModels, this.observableTemplateViewModelsInclAll, this.observableTemplateViewModelsInclAllNone, this.observablePlaylistViewModels);
            this.viewModels[0] = uploadListViewModel;
            uploadListViewModel.PropertyChanged += this.uploadListViewModelOnPropertyChanged;
            uploadListViewModel.UploadStarted += this.uploadListViewModelOnUploadStarted;
            uploadListViewModel.UploadFinished += this.uploadListViewModelOnUploadFinished;

            this.viewModels[1] = new TemplateViewModel(this.templateList, this.observableTemplateViewModels, this.observablePlaylistViewModels);
            this.viewModels[2] = new PlaylistViewModel(this.playlistList, this.observablePlaylistViewModels, templateList);
            this.viewModels[3] = new SettingsViewModel();
            this.viewModels[4] = new VidUpViewModel();
        }

        private void uploadListViewModelOnPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "ResumeUploads")
            {
                this.raisePropertyChanged("TotalMbLeft");
            }
        }

        private void uploadListViewModelOnUploadStarted(object sender, UploadStartedEventArgs e)
        {
            this.appStatus = AppStatus.Uploading;
            this.uploadStats = e.UploadStats;
            this.raisePropertyChanged("AppStatus");
        }

        private void uploadListViewModelOnUploadFinished(object sender, UploadFinishedEventArgs e)
        {
            this.appStatus = AppStatus.Idle;
            this.uploadStats = null;
            this.raisePropertyChanged("AppStatus");

            if (e.OneUploadFinished && !e.UploadStopped)
            {
                switch (this.postUploadAction)
                {
                    case PostUploadAction.SleepMode:
                        Application.SetSuspendState(PowerState.Suspend, false, false);
                        break;
                    case PostUploadAction.Hibernate:
                        Application.SetSuspendState(PowerState.Hibernate, false, false);
                        break;
                    case PostUploadAction.Shutdown:
                        ShutDownHelper.ExitWin(ExitWindows.ShutDown, ShutdownReason.MajorOther | ShutdownReason.MinorOther);
                        break;
                    case PostUploadAction.FlashTaskbar:
                        this.notifyTaskbarItemInfo();
                        break;
                    default:
                        break;
                }
            }

            System.Timers.Timer timer = new System.Timers.Timer(10000d);
            timer.Elapsed += resetTaskBarAndStats;
            timer.AutoReset = false;
            timer.Start();
        }

        private ReSerialize deserializeContent()
        {
            Tracer.Write($"MainWindowViewModel.deserializeContent: Start.");
            JsonDeserializationContent deserializer = new JsonDeserializationContent(
                Settings.SettingsInstance.StorageFolder, Settings.SettingsInstance.ThumbnailFallbackImageFolder);
            YoutubeAuthentication.SerializationFolder = Settings.SettingsInstance.StorageFolder;
            ReSerialize reSerialize = deserializer.Deserialize();
            this.templateList = DeserializationRepositoryContent.TemplateList;
            this.templateList.CollectionChanged += this.templateListCollectionChanged;
            
            this.uploadList = DeserializationRepositoryContent.UploadList;
            this.uploadList.PropertyChanged += this.uploadListPropertyChanged;

            this.playlistList = DeserializationRepositoryContent.PlaylistList;
            this.playlistList.CollectionChanged += this.playlistListCollectionChanged;

            this.uploadList.CheckFileUsage = this.templateList.TemplateContainsFallbackThumbnail;
            this.templateList.CheckFileUsage = this.uploadList.UploadContainsFallbackThumbnail;

            DeserializationRepositoryContent.ClearRepositories();
            Tracer.Write($"MainWindowViewModel.deserializeContent: End.");

            return reSerialize;
        }

        private void deserializeSettings()
        {
            JsonDeserializationSettings deserializer = new JsonDeserializationSettings(Settings.SettingsInstance.StorageFolder);
            deserializer.DeserializeSettings();
            Settings.SettingsInstance.UserSettings = DeserializationRepositorySettings.UserSettings;

            DeserializationRepositorySettings.ClearRepositories();
        }

        private void reSerialize(ReSerialize reSerialize)
        {
            if (reSerialize.AllUploads)
            {
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            }

            if (reSerialize.UploadList)
            {
                JsonSerializationContent.JsonSerializer.SerializeUploadList();
            }

            if (reSerialize.TemplateList)
            {
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            if (reSerialize.PlaylistList)
            {
                JsonSerializationContent.JsonSerializer.SerializePlaylistList();
            }
        }

        private void templateListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if(e.Action == NotifyCollectionChangedAction.Remove)
            {
                this.uploadList.RemoveTemplate((Template)e.OldItems[0]);
            }
        }

        private void playlistListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                this.uploadList.RemovePlaylist((Playlist)e.OldItems[0]);
                this.templateList.RemovePlaylist((Playlist)e.OldItems[0]);
            }
        }

        private void checkAppDataFolder()
        {
            if(!Directory.Exists(Settings.SettingsInstance.StorageFolder))
            {
                Directory.CreateDirectory(Settings.SettingsInstance.StorageFolder);
            }

            if (!Directory.Exists(Settings.SettingsInstance.TemplateImageFolder))
            {
                Directory.CreateDirectory(Settings.SettingsInstance.TemplateImageFolder);
            }

            if (!Directory.Exists(Settings.SettingsInstance.ThumbnailFallbackImageFolder))
            {
                Directory.CreateDirectory(Settings.SettingsInstance.ThumbnailFallbackImageFolder);
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

        private void resetTaskBarAndStats(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.resetTaskbarItemInfo();
            this.updateStats();
        }

        private void uploadListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalBytesToUpload")
            {
                this.updateStats();
            }
        }

        private void autoSettingPlaylistsStateChanged(AutoSettingPlaylistsStateChangedMessage autoSettingPlaylistsStateChangedMessage)
        {
            this.autoSettingPlaylistsColor = autoSettingPlaylistsStateChangedMessage.Success ? MainWindowViewModel.blueColor : Colors.Red;
            this.autoSettingPlaylistsText = autoSettingPlaylistsStateChangedMessage.Message;

            this.raisePropertyChanged("AutoSettingPlaylists");
            this.raisePropertyChanged("AutoSettingPlaylistsText");
            this.raisePropertyChanged("AutoSettingPlaylistsColor");
        }

        private void uploadStatsChanged(UploadStatsChangedMessage uploadStatsChangedMessage)
        {
            this.updateStats();
        }

        private void uploadStatusChanged(UploadStatusChangedMessage uploadStatusChangedMessage)
        {
            this.updateStats();
        }

        private void updateStats()
        {
            this.raisePropertyChanged("TotalProgressPercentage");
            this.raisePropertyChanged("CurrentFilePercent");
            this.raisePropertyChanged("CurrentFileTimeLeft");
            this.raisePropertyChanged("CurrentFileMbLeft");
            this.raisePropertyChanged("TotalMbLeft");
            this.raisePropertyChanged("TotalTimeLeft");
            this.raisePropertyChanged("CurrentUploadSpeedInKiloBytesPerSecond");
        }

        private void notifyTaskbarItemInfo()
        {
            this.taskbarStateInternal = TaskbarState.Notification;
        }

        private void resetTaskbarItemInfo()
        {
            this.taskbarStateInternal = TaskbarState.Normal;
        }

        public void Close()
        {
            Tracer.Close();
        }
    }
}
