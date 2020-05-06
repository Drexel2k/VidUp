using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using Drexel.VidUp.JSON;
using System.IO;
using Drexel.VidUp.UI.Controls;
using MaterialDesignThemes.Wpf;
using Drexel.VidUp.Youtube;
using System.Reflection;
using System.Globalization;
using Drexel.VidUp.UI.DllImport;
using System.Windows.Shell;
using System.Windows.Forms;
using Drexel.VidUp.UI.Definitions;
using System.Security.Policy;

//todo: fallbackthumbnail integrieren, verschiebender browse button wenn thumbnail file fehlt, Wix Framework überprüfung

namespace Drexel.VidUp.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int byteMegaByteFactor = 1048567;

        private int tabNo;
        private UploadListViewModel uploadListViewModel;

        //todo: rename delete command remove commands
        private TemplateViewModel templateViewModel;
        private GenericCommand addUploadCommand;
        private GenericCommand startUploadingCommand;
        private GenericCommand newTemplateCommand;
        private GenericCommand deleteTemplateCommand;
        private GenericCommand aboutCommand;
        private GenericCommand removeAllUploadedCommand;
        private GenericCommand donateCommand;

        private AppStatus appStatus;
        private PostUploadAction postUploadAction;

        //contains sum of upload with status ReadyForUpload or Uploading
        private long currentTotalBytesToUpload = 0;
        private long maxUploadInBytesPerSecond = 0;

        private Uploader uploader;
        private UploadStats uploadStats;

        private TaskbarItemProgressState taskbarItemProgressState = TaskbarItemProgressState.Normal;

        private bool windowActive = true;

        private TemplateList templateList;
        private ObservableTemplateViewModels observableTemplateViewModels;
        private UploadList uploadList;

        private TemplateComboboxViewModel selectedTemplate;

        private object currentView;
        

        public MainWindowViewModel()
        {
            TemplateList templatelist = null;
            this.initialize(out templatelist);
        }

        //for testing purposes
        public MainWindowViewModel(string userSuffix, string storageFolder, string templateImageFolder, string thumbnailFallbackImageFolder, out TemplateList templateList)
        {
            Settings.UserSuffix = userSuffix;
            Settings.StorageFolder = storageFolder;
            Settings.TemplateImageFolder = templateImageFolder;
            Settings.ThumbnailFallbackImageFolder = thumbnailFallbackImageFolder;

            this.initialize(out templateList);
        }

        private void initialize(out TemplateList templateList)
        {
            this.appStatus = AppStatus.Idle;
            this.checkAppDataFolder();

            this.addUploadCommand = new GenericCommand(openUploadDialog);
            this.startUploadingCommand = new GenericCommand(startUploading);
            this.newTemplateCommand = new GenericCommand(openNewTemplateDialog);
            this.deleteTemplateCommand = new GenericCommand(RemoveTemplate);
            this.aboutCommand = new GenericCommand(openAboutDialog);
            this.removeAllUploadedCommand = new GenericCommand(removeAllUploaded);
            this.donateCommand = new GenericCommand(openDonateDialog);

            this.deserialize();

            templateList = this.templateList;

            //todo: harmoinze template list and uplaod list viewmodel architecture
            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList);
            this.uploadListViewModel = new UploadListViewModel(this.uploadList, this.observableTemplateViewModels);

            this.templateViewModel = new TemplateViewModel();

            this.SelectedTemplate = this.observableTemplateViewModels.TemplateCount > 0 ? this.observableTemplateViewModels[0] : null;

            currentView = uploadListViewModel;
        }

        private void deserialize()
        {
            TemplateList templateList;
            JsonSerialization.SerializationFolder = Settings.StorageFolder;
            YoutubeAuthentication.SerializationFolder = JsonSerialization.SerializationFolder;
            JsonSerialization.Initialize();
            JsonSerialization.Deserialize();
            this.templateList = new TemplateList(DeSerializationRepository.Templates, Settings.TemplateImageFolder, Settings.ThumbnailFallbackImageFolder);
            this.templateList.ThumbnailFallbackImageFolder = Settings.ThumbnailFallbackImageFolder;
            templateList = this.templateList;
            //for serialization
            JsonSerialization.TemplateList = this.templateList;

            this.uploadList = DeSerializationRepository.UploadList != null ? DeSerializationRepository.UploadList : new UploadList();
            this.uploadList.PropertyChanged += uploadListPropertyChanged;
            this.uploadList.CheckFileUsage = this.templateList.TemplateContainsFallbackThumbnail;
            this.uploadList.ThumbnailFallbackImageFolder = Settings.ThumbnailFallbackImageFolder;

            this.templateList.CheckFileUsage = this.uploadList.UploadContainsFallbackThumbnail;
            //for serialization
            JsonSerialization.UploadList = this.uploadList;

            DeSerializationRepository.ClearRepositories();
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

        //is bound to grid row 1 (main window content) MainWindow.Xaml
        public object CurrentView
        {
            get
            {
                return currentView;
            }
            set
            {
                if (currentView != value)
                {
                    currentView = value;
                    this.raisePropertyChanged("CurrentView");
                    if(currentView is TemplateViewModel)
                    {
                        if(this.templateList.TemplateCount == 0)
                        {
                            openNewTemplateDialog(null);
                        }
                    }
                }
            }
        }

        public GenericCommand AddUploadCommand
        {
            get
            {
                return this.addUploadCommand;
            }
        }

        public GenericCommand StartUploadingCommand
        {
            get
            {
                return this.startUploadingCommand;
            }
        }

        public GenericCommand NewTemplateCommand
        {
            get
            {
                return this.newTemplateCommand;
            }
        }

        public GenericCommand AboutCommand
        {
            get
            {
                return this.aboutCommand;
            }
        }

        public GenericCommand DonateCommand
        {
            get
            {
                return this.donateCommand;
            }
        }

        public GenericCommand DeleteTemplateCommand
        {
            get
            {
                return this.deleteTemplateCommand;
            }
        }

        public GenericCommand RemoveAllUploadedCommand
        {
            get
            {
                return this.removeAllUploadedCommand;
            }
        }

        

        public AppStatus AppStatus
        {
            get => this.appStatus;
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

                return 0;
            }
       }

        public TaskbarItemProgressState TaskbarItemProgressState
        {
            get
            {
                return this.taskbarItemProgressState;
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
                    
                    if (this.uploadStats.CurrentFileTimeLeft > TimeSpan.MinValue)
                    {
                        return string.Format("{0}h {1}m {2}s",
                            (int)this.uploadStats.CurrentFileTimeLeft.TotalHours,
                            this.uploadStats.CurrentFileTimeLeft.Minutes,
                            this.uploadStats.CurrentFileTimeLeft.Seconds);
                    }

                    return "calclulating...";
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
                    if (this.uploadStats.TotalTimeLeft > TimeSpan.MinValue)
                    { 
                        return string.Format("{0}h {1}m {2}s",
                            (int)this.uploadStats.TotalTimeLeft.TotalHours,
                            this.uploadStats.TotalTimeLeft.Minutes,
                            this.uploadStats.TotalTimeLeft.Seconds);
                    }

                    return "calclulating...";
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

                return ((int)((float)this.uploadList.TotalBytesToUpload / MainWindowViewModel.byteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
            }
        }

        public string MaxUploadInKiloBytesPerSecond
        {
            get
            {
                return (this.maxUploadInBytesPerSecond / 1024).ToString("N0", CultureInfo.CurrentCulture); ;
            }

            set
            {
                long kiloBytesPerSecong;       
                value = value.Replace(CultureInfo.CurrentCulture.NumberFormat.NumberGroupSeparator, string.Empty);
                if (long.TryParse(value, out kiloBytesPerSecong))
                {
                    this.maxUploadInBytesPerSecond = kiloBytesPerSecong * 1024;
                    if (this.maxUploadInBytesPerSecond < 262144) // minimum 256 KiloBytes per second
                    {
                        this.maxUploadInBytesPerSecond = 0;
                    }
                }
                else
                {
                    this.maxUploadInBytesPerSecond = 0;
                }


                //todo: set to uploader
                //YoutubeUpload.MaxUploadInBytesPerSecond = this.maxUploadInBytesPerSecond;
                this.raisePropertyChanged("MaxUploadInKiloBytesPerSecond");
            }
        }
        
        public TemplateViewModel TemplateViewModel
        {
            get
            {
                return this.templateViewModel;
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
                    switch(this.tabNo)
                    {
                        case 0:
                            CurrentView = uploadListViewModel;
                            break;
                        case 1:
                            CurrentView = templateViewModel;
                            break;
                        case 2:
                            CurrentView = null;
                            break;
                        default:
                            throw new Exception("View not implemented");
                    }

                    this.raisePropertyChanged("TabNo");
                }
            }
        }

        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels;
            }
        }

        public TemplateComboboxViewModel SelectedTemplate
        {
            get
            {
                return this.selectedTemplate;
            }
            set
            {
                if (this.selectedTemplate != value)
                {
                    this.selectedTemplate = value;
                    if (value != null)
                    {
                        this.TemplateViewModel.Template = value.Template;
                    }
                    else
                    {
                        this.TemplateViewModel.Template = null;
                    }

                    this.raisePropertyChanged("SelectedTemplate");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void openUploadDialog(object obj)
        {
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;

            DialogResult result = fileDialog.ShowDialog();
            

            if (result == DialogResult.OK)
            {
                List<Upload> uploads = new List<Upload>();
                foreach (string fileName in fileDialog.FileNames)
                {
                    uploads.Add(new Upload(fileName));
                }

                this.AddUploads(uploads);
            }
        }

        public void AddUploads(List<Upload> uploads)
        {
            this.uploadList.AddUploads(uploads, this.templateList);

            JsonSerialization.SerializeAllUploads();
            JsonSerialization.SerializeUploadList();
            JsonSerialization.SerializeTemplateList();
        }

        private void removeAllUploaded(object obj)
        {
            this.uploadListViewModel.RemoveAllUploaded();
        }

        private async void startUploading(object obj)
        {
            //button alle finished aus UploadList entfernen.
            if (this.appStatus == AppStatus.Idle)
            {
                this.appStatus = AppStatus.Uploading;
                //prevent sleep mode
                PowerSavingHelper.DisablePowerSaving();
                this.raisePropertyChanged("AppStatus");

                bool oneUploadFinished = false;
                this.uploader = new Uploader(this.uploadList);
                this.uploadStats = new UploadStats();
                oneUploadFinished = await uploader.Upload(null, null, this.updateUploadProgress, this.uploadStats, this.maxUploadInBytesPerSecond);
                this.uploader = null;
                this.uploadStats = null;

                this.appStatus = AppStatus.Idle;

                PowerSavingHelper.EnablePowerSaving();
                this.raisePropertyChanged("AppStatus");

                this.raisePropertyChanged("CurrentFilePercent");
                this.raisePropertyChanged("CurrentFileTimeLeft");
                this.raisePropertyChanged("CurrentFileMbLeft");
                this.raisePropertyChanged("TotalMbLeft");
                this.raisePropertyChanged("TotalTimeLeft");
                this.raisePropertyChanged("CurrentUploadSpeedInKiloBytesPerSecond");

                if (oneUploadFinished)
                {
                    switch(this.postUploadAction)
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
                        default:
                            this.taskbarItemProgressState = TaskbarItemProgressState.Indeterminate;
                            this.raisePropertyChanged("TaskbarItemProgressState");
                            if (this.windowActive)
                            {
                                System.Timers.Timer timer = new System.Timers.Timer(5000d);
                                timer.Elapsed += stopFlashing;
                                timer.AutoReset = false;
                                timer.Start();
                            }
                            break;
                    }
                }
            }
        }

        private void stopFlashing(object sender, System.Timers.ElapsedEventArgs e)
        {
            this.resetTaskbarItemInfo();
        }

        private async void openNewTemplateDialog(object obj)
        {
            var view = new NewTemplateControl
            {
                DataContext = new NewTemplateViewModel(Settings.TemplateImageFolder)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if(result)
            { 
                NewTemplateViewModel data = (NewTemplateViewModel)view.DataContext;
                Template template = new Template(data.Name, data.ImageFilePath, data.RootFolderPath, this.templateList);
                this.AddTemplate(template);
            }

            if(!result && this.templateList.TemplateCount == 0)
            {
                this.CurrentView = this.uploadListViewModel;
                this.TabNo = 0;
            }
        }

        //exposed for testing purposes
        public void AddTemplate(Template template)
        {
            List<Template> list = new List<Template>();
            list.Add(template);
            this.templateList.AddTemplates(list);
            this.templateViewModel.SerializeTemplateList();

            this.SelectedTemplate = new TemplateComboboxViewModel(template);
        }

        private async void openAboutDialog(object obj)
        {
            var view = new AboutControl
            {
                DataContext = new AboutViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
        }

        private async void openDonateDialog(object obj)
        {
            var view = new DonateControl
            {
               // DataContext = new DonateViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
        }

        //exposed for testing
        public void RemoveTemplate(Object guid)
        {
            Template template = this.templateList.GetTemplate(Guid.Parse((string)guid));

            //Needs to set before deleting the ViewModel in ObservableTemplateViewModels, otherwise the RaiseNotifyCollectionChanged
            //will set the SelectedTemplate to null which causes problems if there are templates left
            if (this.observableTemplateViewModels.TemplateCount > 1)
            {
                if (this.observableTemplateViewModels[0].Template == template)
                {
                    this.SelectedTemplate = this.observableTemplateViewModels[1];
                }
                else
                {
                    this.SelectedTemplate = this.observableTemplateViewModels[0];
                }
            }
            else
            {
                this.SelectedTemplate = null;
            }

            this.uploadList.RemoveTemplate(template);
            this.templateList.Remove(template);

            JsonSerialization.SerializeTemplateList();
            JsonSerialization.SerializeAllUploads();
            if (this.ObservableTemplateViewModels.TemplateCount == 0)
            {
                this.NewTemplateCommand.Execute(null);
            }
        }

        private void uploadListPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalBytesToUpload")
            {
                this.raisePropertyChanged("TotalMbLeft");
            }
        }
        void updateUploadProgress()
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

            if (this.taskbarItemProgressState != TaskbarItemProgressState.Normal)
            {
                this.resetTaskbarItemInfo();
            }
        }

        private void resetTaskbarItemInfo()
        {
            this.taskbarItemProgressState = TaskbarItemProgressState.Normal;
            this.raisePropertyChanged("ProgressPercentage");
            this.raisePropertyChanged("TaskbarItemProgressState");
        }

        public void WindowDeactivated()
        {
            this.windowActive = false;
        }

        //exposed for testing
        public void RemoveUpload(string guid)
        {
            this.uploadListViewModel.RemoveUpload(guid);
        }
    }
}
