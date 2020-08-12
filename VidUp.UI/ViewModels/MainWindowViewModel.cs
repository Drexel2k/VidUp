using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Forms;
using System.Windows.Shell;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.JSON;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.Definitions;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.Events;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube;
using Drexel.VidUp.Youtube.Service;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private int tabNo;
        private List<object> viewModels = new List<object>(new object[4]);

        private AppStatus appStatus = AppStatus.Idle;
        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        private PostUploadAction postUploadAction;
        private UploadStats uploadStats;

        private TaskbarState taskbarState = TaskbarState.Normal;

        private bool windowActive = true;

        private TemplateList templateList;
        private UploadList uploadList;
        private PlaylistList playlistList;

        public event PropertyChangedEventHandler PropertyChanged;

        public MainWindowViewModel()
        {
            this.initialize(out _, out _, out _);
        }

        //for testing purposes
        public MainWindowViewModel(string userSuffix, string storageFolder, string templateImageFolder, string thumbnailFallbackImageFolder, out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            Settings.UserSuffix = userSuffix;
            Settings.StorageFolder = storageFolder;
            Settings.TemplateImageFolder = templateImageFolder;
            Settings.ThumbnailFallbackImageFolder = thumbnailFallbackImageFolder;

            this.initialize(out uploadList, out templateList, out playlistList);
        }

        private void initialize(out UploadList uploadList, out TemplateList templateList, out PlaylistList playlistList)
        {
            this.checkAppDataFolder();
            this.deserialize();
            JsonSerialization.JsonSerializer = new JsonSerialization(Settings.StorageFolder, this.uploadList, this.templateList, this.playlistList);

            uploadList = this.uploadList;
            templateList = this.templateList;
            playlistList = this.playlistList;

            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList, false);
            this.observableTemplateViewModelsInclAllNone = new ObservableTemplateViewModels(this.templateList, true);
            this.observablePlaylistViewModels = new ObservablePlaylistViewModels(this.playlistList);

            UploadListViewModel uploadListViewModel = new UploadListViewModel(this.uploadList, this.observableTemplateViewModels, this.observableTemplateViewModelsInclAllNone, this.observablePlaylistViewModels);
            this.viewModels[0] = uploadListViewModel;
            uploadListViewModel.PropertyChanged += uploadListViewModelOnPropertyChanged;
            uploadListViewModel.UploadStarted += uploadListViewModelOnUploadStarted;
            uploadListViewModel.UploadFinished += uploadListViewModelOnUploadFinished;
            uploadListViewModel.UploadStatsUpdated += uploadListViewModelOnUploadStatsUpdated;

            this.viewModels[1] = new TemplateViewModel(this.templateList, this.observableTemplateViewModels, this.observablePlaylistViewModels);
            this.viewModels[2] = new PlaylistViewModel(this.playlistList, this.observablePlaylistViewModels);
            this.viewModels[3] = new VidUpViewModel();
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

            if (e.OneUploadFinished)
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
                        if (this.windowActive)
                        {
                            System.Timers.Timer timer = new System.Timers.Timer(5000d);
                            timer.Elapsed += stopFlashing;
                            timer.AutoReset = false;
                            timer.Start();
                        }
                        break;
                    default:
                        break;
                }
            }
        }

        private void deserialize()
        {
            JsonDeserialization deserializer = new JsonDeserialization(Settings.StorageFolder, Settings.TemplateImageFolder, Settings.ThumbnailFallbackImageFolder);
            YoutubeAuthentication.SerializationFolder = Settings.StorageFolder;
            deserializer.Deserialize();
            this.templateList = DeserializationRepository.TemplateList;
            this.templateList.CollectionChanged += this.templateListCollectionChanged;
            
            this.uploadList = DeserializationRepository.UploadList;
            this.uploadList.PropertyChanged += this.uploadListPropertyChanged;

            this.playlistList = DeserializationRepository.PlaylistList;
            this.playlistList.CollectionChanged += playlistListCollectionChanged;

            this.uploadList.CheckFileUsage = this.templateList.TemplateContainsFallbackThumbnail;
            this.templateList.CheckFileUsage = this.uploadList.UploadContainsFallbackThumbnail;

            DeserializationRepository.ClearRepositories();
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
            if(!Directory.Exists(Settings.StorageFolder))
            {
                Directory.CreateDirectory(Settings.StorageFolder);
            }

            if (!Directory.Exists(Settings.TemplateImageFolder))
            {
                Directory.CreateDirectory(Settings.TemplateImageFolder);
            }

            if (!Directory.Exists(Settings.ThumbnailFallbackImageFolder))
            {
                Directory.CreateDirectory(Settings.ThumbnailFallbackImageFolder);
            }
        }

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
                this.raisePropertyChanged("ProgressPercentage");
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
                string version = Assembly.GetExecutingAssembly().GetName().Version.ToString();
                return string.Format("{0} {1}", product, version);
            }
        }

       public float ProgressPercentage
       {
            get
            {
                if (this.appStatus == AppStatus.Uploading)
                {
                    return this.uploadStats.ProgressPercentage;
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
                if (this.appStatus == AppStatus.Uploading)
                {
                    return string.Format("{0}h {1}m {2}s",
                        (int)this.uploadStats.CurrentFileTimeLeft.TotalHours,
                        this.uploadStats.CurrentFileTimeLeft.Minutes,
                        this.uploadStats.CurrentFileTimeLeft.Seconds);
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
                if (this.appStatus == AppStatus.Uploading)
                {
                    return string.Format("{0}h {1}m {2}s",
                        (int)this.uploadStats.TotalTimeLeft.TotalHours,
                        this.uploadStats.TotalTimeLeft.Minutes,
                        this.uploadStats.TotalTimeLeft.Seconds);
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
                    return this.uploadStats.TotalMBLeft.ToString("N0", CultureInfo.CurrentCulture);
                }

                if (((UploadListViewModel)this.viewModels[0]).ResumeUploads)
                {
                    return ((int) ((float) this.uploadList.TotalBytesToUploadIncludingResumableRemaining /
                                   Constants.ByteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
                }
                else
                {
                    return ((int)((float)this.uploadList.TotalBytesToUploadRemaining /
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
                    ((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond = kiloBytesPerSecond * 1024;
                    if (((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond < 262144) // minimum 256 KiloBytes per second
                    {
                        ((UploadListViewModel)this.viewModels[0]).MaxUploadInBytesPerSecond = 0;
                    }
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

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void stopFlashing(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.resetTaskbarItemInfo();
        }

        private void uploadListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalBytesToUpload")
            {
                this.raisePropertyChanged("TotalMbLeft");
            }

            if (e.PropertyName == "TotalBytesToUploadIncludingResumableRemaining" || e.PropertyName == "TotalBytesToUploadRemaining")
            {
                JsonSerialization.JsonSerializer.SerializeAllUploads();
            }
        }

        private void uploadListViewModelOnUploadStatsUpdated(object sender, EventArgs e)
        {
            this.raisePropertyChanged("ProgressPercentage");
            this.raisePropertyChanged("CurrentFilePercent");
            this.raisePropertyChanged("CurrentFileTimeLeft");
            this.raisePropertyChanged("CurrentFileMbLeft");
            this.raisePropertyChanged("TotalMbLeft");
            this.raisePropertyChanged("TotalTimeLeft");
            this.raisePropertyChanged("CurrentUploadSpeedInKiloBytesPerSecond");
        }

        public void WindowActivated()
        {
            this.windowActive = true;

            if (this.taskbarState != TaskbarState.Normal)
            {
                this.resetTaskbarItemInfo();
            }
        }

        private void notifyTaskbarItemInfo()
        {
            this.taskbarStateInternal = TaskbarState.Notification;
        }

        private void resetTaskbarItemInfo()
        {
            this.taskbarStateInternal = TaskbarState.Normal;
        }

        public void WindowDeactivated()
        {
            this.windowActive = false;
        }
    }
}
