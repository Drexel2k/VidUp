using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Text;
using System.Windows.Forms;
using System.Windows.Input;
using Drexel.VidUp.JSON;
using System.IO;
using Drexel.VidUp.UI.Controls;
using MaterialDesignThemes.Wpf;
using System.Collections.ObjectModel;
using Drexel.VidUp.YouTube;
using Google.Apis.Upload;
using Google.Apis.YouTube.v3.Data;
using System.Reflection;
using System.Globalization;
using System.Runtime.InteropServices;
using Drexel.VidUp.UI.DllImport;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MainWindowViewModel : INotifyPropertyChanged
    {
        private const int byteMegaByteFactor = 1048567;

        private int tabNo;
        private UploadListViewModel uploadListViewModel;

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

        //no upload session tracking currently, this would require also consider added or removed videos, status changes of videos
        //and to recalculate the session bytes. Therefore total time left is only calculated by the average of the current upload
        //and not of the whole session
        private long currentUploadBytes;
        private long currentUploadBytesSent;
        private DateTime currentUploadStart;

        private long totalBytesToUpload;

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

            JsonSerialization.SerializationFolder = string.Format("{0}{1}", Settings.SerializationFolder, Settings.UserSuffix);
            JsonSerialization.Initialize();
            JsonSerialization.Deserialize();
            this.templateList = new TemplateList(DeSerializationRepository.Templates);
            //for serialization
            JsonSerialization.TemplateList = this.templateList;

            this.uploadList = DeSerializationRepository.UploadList != null ? DeSerializationRepository.UploadList : new UploadList();
            //for serialization
            JsonSerialization.UploadList = this.uploadList;

            DeSerializationRepository.ClearRepositories();

            this.observableTemplateViewModels = new ObservableTemplateViewModels(this.templateList);

            uploadListViewModel = new UploadListViewModel(this.uploadList, this.templateList, this);            
            templateViewModel = new TemplateViewModel(this);

            this.SelectedTemplate = this.observableTemplateViewModels.TemplateCount > 0 ? this.observableTemplateViewModels[0] : null;
            this.SumTotalBytesToUpload();

            currentView = uploadListViewModel;
        }

        private void checkAppDataFolder()
        {
            if(!Directory.Exists(string.Format("{0}{1}", Settings.SerializationFolder, Settings.UserSuffix)))
            {
                Directory.CreateDirectory(string.Format("{0}{1}", Settings.SerializationFolder, Settings.UserSuffix));
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
                    RaisePropertyChanged("CurrentView");
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
                RaisePropertyChanged("PostUploadAction");
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
                        TimeSpan totalDuration = duration / factor;
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
                    return ((int)((float)(this.currentUploadBytes - this.currentUploadBytesSent) / byteMegaByteFactor)).ToString("#,#", CultureInfo.CurrentCulture);
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
                        TimeSpan totalDuration = duration / factor;
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
                return ((int)((float)(totalBytesToUpload - currentUploadBytesSent) / byteMegaByteFactor)).ToString("#,#", CultureInfo.CurrentCulture);
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

                    RaisePropertyChanged("TabNo");
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

                    RaisePropertyChanged("SelectedTemplate");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        private void RaisePropertyChanged(string propertyName)
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
                RaisePropertyChanged("AppStatus");


                bool oneUploadFinished = false;
                Upload upload = this.uploadList.GetUpload(upload => upload.UploadStatus == UplStatus.ReadyForUpload && File.Exists(upload.FilePath));
                while (upload != null)
                {
                    this.currentUploadBytes = new FileInfo(upload.FilePath).Length;
                    this.currentUploadStart = DateTime.Now;

                    this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Uploading);
                    RaisePropertyChanged("CurrentFilePercent");
                    RaisePropertyChanged("CurrentFileTimeLeft");
                    RaisePropertyChanged("CurrentFileMbLeft");
                    RaisePropertyChanged("TotalMbLeft");
                    RaisePropertyChanged("TotalTimeLeft");

                    string videoId = await Youtube.Upload(upload, videosInsertRequest_ProgressChanged, Settings.UserSuffix);

                    if (!string.IsNullOrWhiteSpace(videoId))
                    {
                        oneUploadFinished = true;

                        if (!string.IsNullOrWhiteSpace(videoId) && !string.IsNullOrWhiteSpace(upload.ThumbnailPath) && File.Exists(upload.ThumbnailPath))
                        {
                            await Youtube.AddThumbnail(videoId, upload.ThumbnailPath, Settings.UserSuffix);
                        }

                        this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Finished);
                    }
                    else
                    {
                        this.uploadListViewModel.SetUploadStatus(upload.Guid, UplStatus.Failed);
                    }

                    this.currentUploadBytes = 0;
                    this.currentUploadBytesSent = 0;
                    this.SumTotalBytesToUpload();
                    JsonSerialization.SerializeAllUploads();

                    upload = this.uploadList.GetUpload(upload => upload.UploadStatus == UplStatus.ReadyForUpload);
                }

                this.appStatus = AppStatus.Idle;

                PowerSavingHelper.EnablePowerSaving();
                RaisePropertyChanged("AppStatus");

                RaisePropertyChanged("CurrentFilePercent");
                RaisePropertyChanged("CurrentFileTimeLeft");
                RaisePropertyChanged("CurrentFileMbLeft");
                RaisePropertyChanged("TotalMbLeft");
                RaisePropertyChanged("TotalTimeLeft");

                if (oneUploadFinished)
                {
                    switch(this.postUploadAction)
                    {
                        case PostUploadAction.Shutdown:
                            ShutDownHelper.ExitWin(ExitWindows.ShutDown, ShutdownReason.MajorOther | ShutdownReason.MinorOther);
                            break;
                        case PostUploadAction.SleepMode:
                            Application.SetSuspendState(PowerState.Suspend, false, false);
                            break;
                        case PostUploadAction.Hibernate:
                            Application.SetSuspendState(PowerState.Hibernate, false, false);
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private async void openNewTemplateDialog(object obj)
        {
            var view = new NewTemplateControl
            {
                DataContext = new NewTemplateViewModel()
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

                RaisePropertyChanged("templateList");                
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
            this.templateList.Remove(template);

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

            JsonSerialization.SerializeTemplateList();
            JsonSerialization.SerializeAllUploads();
            if (this.ObservableTemplateViewModels.TemplateCount == 0)
            {
                this.NewTemplateCommand.Execute(null);
            }
        }

        public void SumTotalBytesToUpload()
        {
            long length = 0;
            foreach(Upload upload in this.uploadList.GetUploads(upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading))
            {
                FileInfo fileInfo = new FileInfo(upload.FilePath);
                if (fileInfo.Exists)
                {
                    length += fileInfo.Length;
                }
            }

            this.totalBytesToUpload = length;
            RaisePropertyChanged("TotalMbLeft");
        }

        void videosInsertRequest_ProgressChanged(IUploadProgress progress)
        {
            if (progress.Status == Google.Apis.Upload.UploadStatus.Uploading)
            {
                this.currentUploadBytesSent = progress.BytesSent;

                RaisePropertyChanged("CurrentFilePercent");
                RaisePropertyChanged("CurrentFileTimeLeft");
                RaisePropertyChanged("CurrentFileMbLeft");
                RaisePropertyChanged("TotalMbLeft");
                RaisePropertyChanged("TotalTimeLeft");
            }
        }
    }
}
