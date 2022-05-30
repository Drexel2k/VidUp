using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
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
using TraceLevel = Drexel.VidUp.Utils.TraceLevel;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int tabNo;
        private List<object> viewModels = new List<object>(new object[5]);
        private List<object> ribbonViewModels = new List<object>(new object[5]);

        private AppStatus appStatus = AppStatus.Idle;
        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAll;
        private ObservablePlaylistViewModels observablePlaylistViewModels;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModelsInclAll;

        private PostUploadAction postUploadAction;
        private UploadStats uploadStats;

        private TaskbarState taskbarState = TaskbarState.Normal;

        private TemplateList templateList;
        private UploadList uploadList;
        private PlaylistList playlistList;
        private string autoSettingPlaylistsText;
        private Color autoSettingPlaylistsColor = MainWindowViewModel.blueColor;
        private static Color blueColor = (Color) ColorConverter.ConvertFromString("#03a9f4");

        private bool postponePostUploadAction;
        
        private object postUploadActionLock = new object();
        private CancellationTokenSource postUploadActionCancellationTokenSource;
        private YoutubeAccountList youtubeAccountList;

        private YoutubeAccountComboboxViewModel selectedYoutubeAccount;

        public event PropertyChangedEventHandler PropertyChanged;

        public List<object> ViewModels
        {
            get => this.viewModels;
        }

        public List<object> RibbonViewModels
        {
            get => this.ribbonViewModels;
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
                    return this.uploadStats.CurrentFileMbLeft.ToString("N0");
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
                    return this.uploadStats.CurrentSpeedInKiloBytesPerSecond.ToString("N0");
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
                    return this.uploadStats.TotalMbLeft.ToString("N0");
                }

                if (((UploadListViewModel)this.viewModels[0]).ResumeUploads)
                {
                    return ((int)((float)this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(null) /
                                   Constants.ByteMegaByteFactor)).ToString("N0");
                }
                else
                {
                    return ((int)((float)this.uploadList.GetRemainingBytesOfFilesToUpload(null) /
                                  Constants.ByteMegaByteFactor)).ToString("N0");
                }
            }
        }

        public string MaxUploadInKiloBytesPerSecond
        {
            get
            {
                return (((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond / 1024).ToString("N0"); ;
            }

            set
            {
                if (long.TryParse(value, NumberStyles.AllowLeadingWhite | NumberStyles.AllowTrailingWhite | NumberStyles.AllowThousands, CultureInfo.CurrentCulture, out long kiloBytesPerSecond))
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
            get => Settings.Instance.UserSettings.WindowTop;
            set
            {
                Settings.Instance.UserSettings.WindowTop = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowLeft
        {
            get => Settings.Instance.UserSettings.WindowLeft;
            set
            {
                Settings.Instance.UserSettings.WindowLeft = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowHeight
        {
            get => Settings.Instance.UserSettings.WindowHeight;
            set
            {
                Settings.Instance.UserSettings.WindowHeight = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public int? WindowWidth
        {
            get => Settings.Instance.UserSettings.WindowWidth;
            set
            {
                Settings.Instance.UserSettings.WindowWidth = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public bool AutoSettingPlaylists
        {
            get => ((PlaylistRibbonViewModel)this.ribbonViewModels[2]).AutoSettingPlaylists;
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

        public bool PostponePostUploadAction
        {
            get => this.postponePostUploadAction;
            set
            {
                if (!value)
                {
                    this.cancelPostUploadAction();
                }

                this.postponePostUploadAction = value;
                this.raisePropertyChanged("PostponePostUploadAction");
            }
        }

        public string PostponePostUploadActionProcessName
        {
            get => Settings.Instance.UserSettings.PostponePostUploadActionProcessName;
            set
            {
                Settings.Instance.UserSettings.PostponePostUploadActionProcessName = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get => this.observableYoutubeAccountViewModels;
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModelsInclAll
        {
            get => this.observableYoutubeAccountViewModelsInclAll;
        }

        public YoutubeAccountComboboxViewModel SelectedYoutubeAccount
        {
            get => this.selectedYoutubeAccount;
            set
            {
                YoutubeAccount oldAccount = this.selectedYoutubeAccount.YoutubeAccount;
                this.selectedYoutubeAccount = value;

                this.selectedYoutubeAccountChanged();

                EventAggregator.Instance.Publish(new SelectedYoutubeAccountChangedMessage(oldAccount, this.selectedYoutubeAccount.YoutubeAccount, this.youtubeAccountList[0]));
                EventAggregator.Instance.Publish(new TemplateDisplayPropertyChangedMessage("youtubeaccount"));

                this.raisePropertyChanged("SelectedYoutubeAccount");
            }
        }

        public MainWindowViewModel() : this(null, null, out _, out _, out _)
        {
        }

        //for testing purposes
        public MainWindowViewModel(string user, string subFolder, out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            this.initialize(user, subFolder, out uploadList, out templateList, out playlistList);
        }

        private void initialize(string folderSuffix, string subfolder, out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            if (string.IsNullOrWhiteSpace(folderSuffix))
            {
                folderSuffix = JsonDeserializationSettings.DeserializeFolderSuffix();
            }

            Settings.Instance = new Settings(folderSuffix, subfolder);

            this.checkAppDataFolder();

            this.deserializeSettings();
            ReSerialize reSerialize = this.deserializeContent();

            JsonSerializationContent.JsonSerializer = new JsonSerializationContent(Settings.Instance.StorageFolder, this.uploadList, this.templateList, this.playlistList, this.youtubeAccountList);
            JsonSerializationSettings.JsonSerializer = new JsonSerializationSettings(Settings.Instance.StorageFolder, Settings.Instance.UserSettings);

            this.reSerialize(reSerialize);

            uploadList = this.uploadList;
            templateList = this.templateList;
            playlistList = this.playlistList;

            EventAggregator.Instance.Subscribe<UploadStatusChangedMessage>(this.uploadStatusChanged);
            EventAggregator.Instance.Subscribe<UploadStatsChangedMessage>(this.uploadStatsChanged);
            EventAggregator.Instance.Subscribe<AutoSettingPlaylistsStateChangedMessage>(this.autoSettingPlaylistsStateChanged);
            EventAggregator.Instance.Subscribe<YoutubeAccountStatusChangedMessage>(this.youtubeAccountStatusChangedChanged);

            Template.DummyAccount = new YoutubeAccount("Dummy", this.getYoutubeAccountName);
            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList, true, false, false);
            this.observableTemplateViewModelsInclAllNone = new ObservableTemplateViewModels(this.templateList, false, true, true);
            this.observableTemplateViewModelsInclAll = new ObservableTemplateViewModels(this.templateList, false, true, false);
            this.observablePlaylistViewModels = new ObservablePlaylistViewModels(this.playlistList, true);

            this.observableYoutubeAccountViewModels = new ObservableYoutubeAccountViewModels(this.youtubeAccountList, false);
            this.observableYoutubeAccountViewModelsInclAll = new ObservableYoutubeAccountViewModels(this.youtubeAccountList, true);
            this.selectedYoutubeAccount = this.observableYoutubeAccountViewModelsInclAll[0];

            EventAggregator.Instance.Subscribe<BeforeYoutubeAccountDeleteMessage>(this.beforeYoutubeAccountDelete);

            UploadListViewModel uploadListViewModel = new UploadListViewModel(this.uploadList, this.observableTemplateViewModels, this.observableTemplateViewModelsInclAll,
                this.observableTemplateViewModelsInclAllNone, this.observablePlaylistViewModels, this.observableYoutubeAccountViewModels, this.youtubeAccountList[0], this.selectedYoutubeAccount.YoutubeAccount);
            this.viewModels[0] = uploadListViewModel;
            uploadListViewModel.PropertyChanged += this.uploadListViewModelOnPropertyChanged;
            uploadListViewModel.UploadStarted += this.uploadListViewModelOnUploadStarted;
            uploadListViewModel.UploadFinished += this.uploadListViewModelOnUploadFinished;

            this.ribbonViewModels[2] = new PlaylistRibbonViewModel(this.playlistList, this.observablePlaylistViewModels, this.templateList, this.ObservableYoutubeAccountViewModels, this.youtubeAccountList[0]);

            this.viewModels[1] = new TemplateViewModel(this.templateList, this.observableTemplateViewModels, this.observablePlaylistViewModels, this.observableYoutubeAccountViewModels,this.selectedYoutubeAccount.YoutubeAccount, this.youtubeAccountList[0]);
            PlaylistComboboxViewModel selectedPlaylistViewModel = ((PlaylistRibbonViewModel)this.ribbonViewModels[2]).SelectedPlaylist;
            this.viewModels[2] = new PlaylistViewModel(selectedPlaylistViewModel != null ? selectedPlaylistViewModel.Playlist : null);
            this.viewModels[3] = new SettingsViewModel(this.youtubeAccountList, this.observableYoutubeAccountViewModels);
            this.viewModels[4] = new VidUpViewModel();
        }

        private string getYoutubeAccountName()
        {
            return this.selectedYoutubeAccount.YoutubeAccountName;
        }

        private void selectedYoutubeAccountChanged()
        {
            foreach (TemplateComboboxViewModel templateComboboxViewModel in this.observableTemplateViewModelsInclAll)
            {
                if (this.selectedYoutubeAccount.YoutubeAccount.IsDummy)
                {
                    if (this.selectedYoutubeAccount.YoutubeAccountName == "All")
                    {
                        if (templateComboboxViewModel.Visible == false)
                        {
                            templateComboboxViewModel.Visible = true;
                        }
                    }
                }
                else
                {
                    if (!templateComboboxViewModel.Template.IsDummy)
                    {
                        if (templateComboboxViewModel.YoutubeAccountName == this.selectedYoutubeAccount.YoutubeAccountName)
                        {
                            templateComboboxViewModel.Visible = true;
                        }
                        else
                        {
                            templateComboboxViewModel.Visible = false;
                        }
                    }
                }
            }

            foreach (TemplateComboboxViewModel templateComboboxViewModel in this.observableTemplateViewModelsInclAllNone)
            {
                if (this.selectedYoutubeAccount.YoutubeAccount.IsDummy)
                {
                    if (this.selectedYoutubeAccount.YoutubeAccountName == "All")
                    {
                        if (templateComboboxViewModel.Visible == false)
                        {
                            templateComboboxViewModel.Visible = true;
                        }
                    }
                }
                else
                {
                    if (!templateComboboxViewModel.Template.IsDummy)
                    {
                        if (templateComboboxViewModel.YoutubeAccountName == this.selectedYoutubeAccount.YoutubeAccountName)
                        {
                            templateComboboxViewModel.Visible = true;
                        }
                        else
                        {
                            templateComboboxViewModel.Visible = false;
                        }
                    }
                }
            }

            foreach (PlaylistComboboxViewModel playlistComboboxViewModel in this.observablePlaylistViewModels)
            {
                if (selectedYoutubeAccount.YoutubeAccount.IsDummy)
                {
                    if (selectedYoutubeAccount.YoutubeAccount.Name == "All")
                    {
                        if (playlistComboboxViewModel.Visible == false)
                        {
                            playlistComboboxViewModel.Visible = true;
                        }
                    }
                }
                else
                {
                    if (!selectedYoutubeAccount.YoutubeAccount.IsDummy)
                    {
                        if (playlistComboboxViewModel.YoutubeAccountName == selectedYoutubeAccount.YoutubeAccount.Name)
                        {
                            playlistComboboxViewModel.Visible = true;
                        }
                        else
                        {
                            playlistComboboxViewModel.Visible = false;
                        }
                    }
                }
            }
        }

        private void beforeYoutubeAccountDelete(BeforeYoutubeAccountDeleteMessage beforeYoutubeAccountDeleteMessage)
        {
            if (this.selectedYoutubeAccount.YoutubeAccount == beforeYoutubeAccountDeleteMessage.AccountToBeDeleted)
            {
                this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModelsInclAll[0];
            }
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

            this.cancelPostUploadAction();

            this.raisePropertyChanged("AppStatus");
        }

        private void cancelPostUploadAction()
        {
            lock (this.postUploadActionLock)
            {
                if (this.postUploadActionCancellationTokenSource != null)
                {
                    this.postUploadActionCancellationTokenSource.Cancel();
                    this.postUploadActionCancellationTokenSource.Dispose();
                    this.postUploadActionCancellationTokenSource = null;
                }
            }
        }

        private void uploadListViewModelOnUploadFinished(object sender, UploadFinishedEventArgs e)
        {
            this.appStatus = AppStatus.Idle;
            this.uploadStats = null;
            this.raisePropertyChanged("AppStatus");

            CancellationToken cancellationToken;
            lock (this.postUploadActionLock)
            {
                //this.postUploadActionCancellationTokenSource is cancelled and nulled on start of a new upload session,
                //it should be always null here, if it is not the case, it would mean quick start of at least 2 upload sessions
                //which call the finished event nearly simultaneously, then only one doPostUploadTasks task is created.
                if (this.postUploadActionCancellationTokenSource == null)
                {
                    this.postUploadActionCancellationTokenSource = new CancellationTokenSource();
                    cancellationToken = this.postUploadActionCancellationTokenSource.Token;
                }
                else
                {
                    return;
                }
            }

            this.doPostUploadTasks(e, cancellationToken);
        }

        private async Task doPostUploadTasks(UploadFinishedEventArgs e, CancellationToken cancellationToken)
        {
            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Start.");

            bool postponed = false;
            PostUploadAction postUploadActionInternal = this.postUploadAction;
            AppStatus appStatusInternal = this.appStatus;

            if (postUploadActionInternal != PostUploadAction.None && e.DataSent && !e.UploadStopped && appStatusInternal == AppStatus.Idle)
            {
                Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Performing post upload action.");

                if (this.postponePostUploadAction && !string.IsNullOrWhiteSpace(Settings.Instance.UserSettings.PostponePostUploadActionProcessName))
                {
                    postponed = true;
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Postponing post upload action if necessary.");
                    string processName = Path.GetFileNameWithoutExtension(Settings.Instance.UserSettings.PostponePostUploadActionProcessName);
                    bool processRunning = true;
                    while (processRunning)
                    {
                        Process[] processes = Process.GetProcessesByName(processName);
                        if (processes.Length > 0)
                        {
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Postponing process found, delaying 5 minutes.", TraceLevel.Detailed);
                            try
                            {
                                //await Task.Delay(10000, cancellationToken).ConfigureAwait(false); //10 seconds
                                await Task.Delay(300000, cancellationToken).ConfigureAwait(false); //5 minutes
                            }
                            catch (TaskCanceledException)
                            {
                                Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Postponing canceled.");
                                return;
                            }
                        }
                        else
                        {
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Postponing process '{processName}' not found.");
                            processRunning = false;
                        }
                    }
                }

                //Setting could be changed while waiting for postponing process to finish
                postUploadActionInternal = this.postUploadAction;
                appStatusInternal = this.appStatus;

                if (postUploadActionInternal != PostUploadAction.None && appStatusInternal == AppStatus.Idle)
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Performing post upload action {this.postUploadAction}.");

                    switch (this.postUploadAction)
                    {
                        case PostUploadAction.SleepMode:
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Sleep.");
                            Application.SetSuspendState(PowerState.Suspend, false, false);
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Sleep ended.");
                            break;
                        case PostUploadAction.Hibernate:
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Hibernate.");
                            Application.SetSuspendState(PowerState.Hibernate, false, false);
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Hibernate ended.");
                            break;
                        case PostUploadAction.Shutdown:
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: End, shutdown system.");
                            ShutDownHelper.ExitWin(ExitWindows.ShutDown, ShutdownReason.MajorOther | ShutdownReason.MinorOther);
                            break;
                        case PostUploadAction.FlashTaskbar:
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Flash taskbar.");
                            this.notifyTaskbarItemInfo();
                            break;
                        case PostUploadAction.Close:
                            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: End, exit app.");
                            System.Windows.Application.Current.Dispatcher.Invoke(() => System.Windows.Application.Current.Shutdown());
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: No post upload action to perform after postponing.");

                    if (postUploadActionInternal == PostUploadAction.None)
                    {
                        Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Post upload action was none.");
                    }

                    if (postUploadActionInternal == PostUploadAction.None && postponed)
                    {
                        //post upload action was set to none by user while postponing was running
                        Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Post upload action was set to none by user while waiting for postponing process.");
                    }

                    if (appStatusInternal != AppStatus.Idle)
                    {
                        Tracer.Write($"MainWindowViewModel.doPostUploadTasks: App status was not idle.");
                    }
                }
            }
            else
            {
                Tracer.Write($"MainWindowViewModel.doPostUploadTasks: No post upload action to perform.");

                if (postUploadActionInternal == PostUploadAction.None)
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Post upload action was none.");
                }

                if (appStatusInternal != AppStatus.Idle)
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: App status was not idle.");
                }

                if (e.DataSent != true)
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: No data was sent during upload session.");
                }

                if (e.UploadStopped)
                {
                    Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Upload was stopped manually.");
                }
            }

            //for all post upload actions which don't close VidUp the result shall stay 10 seconds.
            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Resetting taskbar and upload stats in 10 seconds.");
            await Task.Delay(10000);
            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: Resetting taskbar and upload stats.");
            this.resetTaskbarItemInfo();
            this.updateStats();

            Tracer.Write($"MainWindowViewModel.doPostUploadTasks: End.");
        }

        private ReSerialize deserializeContent()
        {
            Tracer.Write($"MainWindowViewModel.deserializeContent: Start.");

            ReSerialize reSerialize = new ReSerialize();

            JsonDeserializationContent deserializer = new JsonDeserializationContent(Settings.Instance.StorageFolder, Settings.Instance.ThumbnailFallbackImageFolder);
            deserializer.Deserialize(reSerialize);

            this.youtubeAccountList = DeserializationRepositoryContent.YoutubeAccountList;
            this.templateList = DeserializationRepositoryContent.TemplateList;

            this.uploadList = DeserializationRepositoryContent.UploadList;
            this.uploadList.PropertyChanged += this.uploadListPropertyChanged;

            this.playlistList = DeserializationRepositoryContent.PlaylistList;

            this.uploadList.CheckFileUsage = this.templateList.TemplateContainsFallbackThumbnail;
            this.templateList.CheckFileUsage = this.uploadList.UploadContainsFallbackThumbnail;

            DeserializationRepositoryContent.ClearRepositories();
            Tracer.Write($"MainWindowViewModel.deserializeContent: End.");

            return reSerialize;
        }

        private void deserializeSettings()
        {
            JsonDeserializationSettings deserializer = new JsonDeserializationSettings(Settings.Instance.StorageFolder);
            deserializer.DeserializeSettings();
            Settings.Instance.UserSettings = DeserializationRepositorySettings.UserSettings;

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

            if (reSerialize.YoutubeAccountList)
            {
                JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();
            }
        }

        private void checkAppDataFolder()
        {
            if(!Directory.Exists(Settings.Instance.StorageFolder))
            {
                Directory.CreateDirectory(Settings.Instance.StorageFolder);
            }

            if (!Directory.Exists(Settings.Instance.TemplateImageFolder))
            {
                Directory.CreateDirectory(Settings.Instance.TemplateImageFolder);
            }

            if (!Directory.Exists(Settings.Instance.ThumbnailFallbackImageFolder))
            {
                Directory.CreateDirectory(Settings.Instance.ThumbnailFallbackImageFolder);
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

        private void youtubeAccountStatusChangedChanged(YoutubeAccountStatusChangedMessage obj)
        {
            this.updateStats();
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
