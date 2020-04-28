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

        //todo: refactor uploading in seperate class
        //will be updated after every finished upload to the current total bytes left
        private long totalBytesToUpload = 0;
        //will be updated during upload when files are added or removed
        private long sessionTotalBytesToUplopad = 0;
        private long uploadedLength = 0;

        //total time left is only calculated by the average of the current upload and not of the whole session
        private long currentUploadBytes;
        private long currentUploadBytesSent;
        private DateTime currentUploadStart;

        private long currentUploadSpeedInBytesPerSecond;
        private long maxUploadInBytesPerSecond = 0;

        private float progressPercentage;
        private TaskbarItemProgressState taskbarItemProgressState = TaskbarItemProgressState.Normal;

        private bool windowActive = true;

        private TemplateList templateList;
        private ObservableTemplateViewModels observableTemplateViewModels;
        private UploadList uploadList;

        private TemplateComboboxViewModel selectedTemplate;

        private object currentView;
        

        public MainWindowViewModel()
        {
            this.appStatus = AppStatus.Idle;
            checkAppDataFolder();

            this.addUploadCommand = new GenericCommand(openUploadDialog);
            this.startUploadingCommand = new GenericCommand(startUploading);
            this.newTemplateCommand = new GenericCommand(openNewTemplateDialog);
            this.deleteTemplateCommand = new GenericCommand(deleteTemplate);
            this.aboutCommand = new GenericCommand(openAboutDialog);
            this.removeAllUploadedCommand = new GenericCommand(removeAllUploaded);
            this.donateCommand = new GenericCommand(openDonateDialog);

            JsonSerialization.SerializationFolder = Settings.StorageFolder;
            YoutubeAuthentication.SerializationFolder = JsonSerialization.SerializationFolder;
            JsonSerialization.Initialize();
            JsonSerialization.Deserialize();
            this.templateList = new TemplateList(DeSerializationRepository.Templates, Settings.TemplateImageFolder);
            //for serialization
            JsonSerialization.TemplateList = this.templateList;

            this.uploadList = DeSerializationRepository.UploadList != null ? DeSerializationRepository.UploadList : new UploadList();
            //for serialization
            JsonSerialization.UploadList = this.uploadList;

            DeSerializationRepository.ClearRepositories();

            //todo: harmoinze template list and uplaod list viewmodel architecture
            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList);
            this.uploadListViewModel = new UploadListViewModel(this.uploadList, this.templateList, this);  
            
            this.templateViewModel = new TemplateViewModel(this);

            this.SelectedTemplate = this.observableTemplateViewModels.TemplateCount > 0 ? this.observableTemplateViewModels[0] : null;
            this.SumTotalBytesToUploadAndRefreshTotalMbLeft();

            currentView = uploadListViewModel;
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

        public void SetDefaultTemplate(Template template, bool isDefault)
        {
            if (isDefault)
            {
                Template templateInternal = this.templateList.Find(templateInternal2 => templateInternal2.IsDefault);
                if (templateInternal != null)
                {
                    templateInternal.IsDefault = false;
                    this.observableTemplateViewModels.RaiseNameChange(templateInternal);
                }
            }

            template.IsDefault = isDefault;
            this.observableTemplateViewModels.RaiseNameChange(template);


            JsonSerialization.SerializeTemplateList();
        }

        internal string CopyThumbnailFallbackImageToStorageFolder(string filePath)
        {
            return this.templateList.CopyPictureToStorageFolder(filePath, Settings.ThumbnailFallbackImageFolder);
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

       public double ProgressPercentage
       {
            get
            {
                return this.progressPercentage;
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
                    return string.Format("{0}%", (int)((float)this.currentUploadBytesSent / this.currentUploadBytes * 100));
                }

                return "n/a";
            }
        }
        public string CurrentFileTimeLeft
        {
            get
            {
                TimeSpan timeLeft = TimeSpan.Zero;
                if (this.appStatus == AppStatus.Uploading)
                {
                    TimeSpan duration = DateTime.Now - this.currentUploadStart;
                    if (this.currentUploadBytesSent > 0)
                    {
                        float factor = (float)this.currentUploadBytesSent / this.currentUploadBytes;
                        TimeSpan totalDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / factor);
                        timeLeft = totalDuration - duration;
                        return string.Format("{0}h {1}m {2}s", (int)timeLeft.TotalHours, timeLeft.Minutes, timeLeft.Seconds);
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
                    return ((int)((float)(this.currentUploadBytes - this.currentUploadBytesSent) / byteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
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
                    return (this.currentUploadSpeedInBytesPerSecond / 1024f ).ToString("N0", CultureInfo.CurrentCulture);
                }

                return "n/a";
            }
        }

        public string TotalTimeLeft
        {
            get
            {
                TimeSpan timeLeft = TimeSpan.Zero;
                if (this.appStatus == AppStatus.Uploading)
                {
                    TimeSpan duration = DateTime.Now - this.currentUploadStart;
                    if (this.currentUploadBytesSent > 0)
                    { 
                        float factor = (float)this.currentUploadBytesSent / this.totalBytesToUpload;
                        TimeSpan totalDuration = TimeSpan.FromMilliseconds (duration.TotalMilliseconds / factor);
                        timeLeft = totalDuration - duration;
                        return string.Format("{0}h {1}m {2}s", (int)timeLeft.TotalHours, timeLeft.Minutes, timeLeft.Seconds);
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
                return ((int)((float)(totalBytesToUpload - currentUploadBytesSent) / byteMegaByteFactor)).ToString("N0", CultureInfo.CurrentCulture);
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

                YoutubeUpload.MaxUploadInBytesPerSecond = this.maxUploadInBytesPerSecond;
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
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
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

                this.uploadListViewModel.AddUploads(uploads, this.templateList);
            }
        }

        public void AddUploads(List<Upload> uploads)
        {
            this.uploadListViewModel.AddUploads(uploads, this.templateList);
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
                Upload upload = this.uploadList.GetUpload(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload && File.Exists(upload2.FilePath));

                this.sessionTotalBytesToUplopad = this.totalBytesToUpload;
                this.uploadedLength = 0;

                while (upload != null)
                {
                    upload.UploadErrorMessage = null;
                    this.currentUploadBytes = this.getUploadLength(upload);
                    this.currentUploadStart = DateTime.Now;

                    this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Uploading);
                    this.raisePropertyChanged("CurrentFilePercent");
                    this.raisePropertyChanged("CurrentFileTimeLeft");
                    this.raisePropertyChanged("CurrentFileMbLeft");
                    this.raisePropertyChanged("TotalMbLeft");
                    this.raisePropertyChanged("TotalTimeLeft");
                    this.raisePropertyChanged("CurrentUploadSpeedInKiloBytesPerSecond");

                    UploadResult result = await YoutubeUpload.Upload(upload, this.maxUploadInBytesPerSecond, updateUploadProgress);

                    if (!string.IsNullOrWhiteSpace(result.VideoId))
                    {
                        oneUploadFinished = true;
                        this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Finished);
                    }
                    else
                    {
                        this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Failed);
                    }

                    this.uploadedLength += this.getUploadLength(upload);
                    this.currentUploadBytes = 0;
                    this.currentUploadBytesSent = 0;
                    this.SumTotalBytesToUploadAndRefreshTotalMbLeft();
                    JsonSerialization.SerializeAllUploads();

                    uploadedLength += this.currentUploadBytes;
                    upload = this.uploadList.GetUpload(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload);
                }

                this.progressPercentage = 1f;
                this.raisePropertyChanged("ProgressPercentage");

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
                Template template = new Template(data.Name, data.PictureFilePath, data.RootFolderPath);
                List<Template> list = new List<Template>();
                list.Add(template);
                this.templateList.AddTemplates(list);
                this.templateViewModel.SerializeTemplateList();

                this.observableTemplateViewModels.AddTemplates(list);
                this.SelectedTemplate = this.observableTemplateViewModels.GetTemplateByGuid(template.Guid);               
            }

            if(!result && this.templateList.TemplateCount == 0)
            {
                this.CurrentView = this.uploadListViewModel;
                this.TabNo = 0;
            }
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

        private void deleteTemplate(Object guid)
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

            this.uploadListViewModel.RemoveTemplateFromUploads(template);
            this.observableTemplateViewModels.DeleteTemplate(template);

            this.templateList.Remove(template);
            this.DeleteThumbnailIfPossible(template.ThumbnailFallbackFilePath);

            JsonSerialization.SerializeTemplateList();
            JsonSerialization.SerializeAllUploads();
            if (this.ObservableTemplateViewModels.TemplateCount == 0)
            {
                this.NewTemplateCommand.Execute(null);
            }
        }

        public void DeleteThumbnailIfPossible(string thumbnailFilePath)
        {
            if (thumbnailFilePath != null)
            {
                string thumbnailFileFolder = Path.GetDirectoryName(thumbnailFilePath);
                if (String.Compare(Path.GetFullPath(Settings.ThumbnailFallbackImageFolder).TrimEnd('\\'), thumbnailFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }

                bool found = false;
                foreach (Template template in this.templateList)
                {
                    if (template.ThumbnailFallbackFilePath != null)
                    {
                        if (String.Compare(Path.GetFullPath(thumbnailFilePath), Path.GetFullPath(template.ThumbnailFallbackFilePath), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            found = true;
                            break;
                        }
                    }
                }

                if (!found)
                {
                    foreach (Upload upload in this.uploadList)
                    {
                        if (upload.ThumbnailFilePath != null)
                        {
                            if (String.Compare(Path.GetFullPath(thumbnailFilePath), Path.GetFullPath(upload.ThumbnailFilePath), StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                found = true;
                                break;
                            }
                        }
                    }
                }

                if (!found)
                {
                    if (File.Exists(thumbnailFilePath))
                    {
                        File.Delete(thumbnailFilePath);
                    }
                }
            }
        }

        public long sumTotalBytesToUpload()
        {
            long length = 0;
            foreach (Upload upload in this.uploadList.GetUploads(upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading))
            {
                length += this.getUploadLength(upload);
            }

            return length;
        }

        public void SumTotalBytesToUploadAndRefreshTotalMbLeft()
        {
            long length = this.sumTotalBytesToUpload();
            this.totalBytesToUpload = length;
            this.raisePropertyChanged("TotalMbLeft");
        }

        private long getUploadLength(Upload upload)
        {
            FileInfo fileInfo = new FileInfo(upload.FilePath);
            if (fileInfo.Exists)
            {
                return fileInfo.Length;
            }

            return 0;
        }

        void updateUploadProgress(YoutubeUploadStats stats)
        {
            this.currentUploadBytesSent = stats.BytesSent;
            this.currentUploadSpeedInBytesPerSecond = stats.CurrentSpeedInBytesPerSecond;

            //upload has been added or removed
            long currentTotalMbLeft = this.sumTotalBytesToUpload();
            long delta = this.sessionTotalBytesToUplopad - (currentTotalMbLeft + this.uploadedLength);
            if (delta != 0)
            {
                //delta is negative when files have been added
                this.sessionTotalBytesToUplopad -= delta;
            }

            this.progressPercentage = (this.uploadedLength + this.currentUploadBytesSent) / (float)this.sessionTotalBytesToUplopad;

            this.raisePropertyChanged("ProgressPercentage");
            this.raisePropertyChanged("CurrentFilePercent");
            this.raisePropertyChanged("CurrentFileTimeLeft");
            this.raisePropertyChanged("CurrentFileMbLeft");
            this.raisePropertyChanged("CurrentFileOverallSpeed");
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
            this.progressPercentage = 0f;
            this.taskbarItemProgressState = TaskbarItemProgressState.Normal;
            this.raisePropertyChanged("ProgressPercentage");
            this.raisePropertyChanged("TaskbarItemProgressState");
        }

        internal void WindowDeactivated()
        {
            this.windowActive = false;
        }
    }
}
