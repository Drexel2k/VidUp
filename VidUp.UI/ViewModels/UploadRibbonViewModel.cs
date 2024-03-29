﻿using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.Converters;
using Drexel.VidUp.UI.Definitions;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.UI.Events;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube;
using MaterialDesignThemes.Wpf;
using EnumConverter = Drexel.VidUp.UI.Converters.EnumConverter;

namespace Drexel.VidUp.UI.ViewModels
{
    public class UploadRibbonViewModel : INotifyPropertyChanged
    {
        private UploadList uploadList;
        private ObservableUploadViewModels observableUploadViewModels;

        private ObservableTemplateViewModels observableTemplateViewModelsInclAll;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;

        private GenericCommand parameterlessCommand;

        private UploadStatus uploadStatus = UploadStatus.NotUploading;
        private Uploader uploader;
        private bool resumeUploads = true;
        private long maxUploadInBytesPerSecond = 0;
        private bool keepLastUploadPerTemplateSide;
        private bool keepLastUploadPerTemplate;
        private int expandedExpander = 0;

        private TemplateComboboxViewModel deleteSelectedTemplate;
        private string deleteSelectedUploadStatus = "Finished";

        private string resetToSelectedUploadStatus = "ReadyForUpload";
        private string resetWithSelectedUploadStatus = "Paused";
        private TemplateComboboxViewModel resetWithSelectedTemplate;

        private UploadTemplateAttribute resetAttributeSelectedAttribute = UploadTemplateAttribute.All;
        private TemplateComboboxViewModel resetAttributeSelectedTemplate;

        private TemplateComboboxViewModel recalculatePublishAtSelectedTemplate;
        private DateTime? recalculatePublishAtStartDate;

        //if the filter account is a dummy account like all, a real account must be taken for upload creation.
        private YoutubeAccount youtubeAccountForCreatingUploads;
        private YoutubeAccount youtubeAccountForFiltering;
        private object uploadingLock = new object();

        private List<Upload> finishedUploads = new List<Upload>();

        private DateTime lastExecution = DateTime.MinValue;
        AutoResetEvent autoReset = new AutoResetEvent(true);

        //todo: add to event aggregator maybe
        public event EventHandler<UploadListStartedEventArgs> UploadStarted;
        public event EventHandler<UploadListFinishedEventArgs> UploadFinished;
        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableUploadViewModels ObservableUploadViewModels
        {
            get
            {
                return this.observableUploadViewModels;
            }
        }

        public GenericCommand ParameterlessCommand
        {
            get
            {
                return this.parameterlessCommand;
            }
        }

        public long MaxUploadInBytesPerSecond
        {
            get
            {
                return this.maxUploadInBytesPerSecond;
            }

            set
            {
                this.maxUploadInBytesPerSecond = value;

                Uploader uploader = this.uploader;
                if (uploader != null)
                {
                    uploader.MaxUploadInBytesPerSecond = this.maxUploadInBytesPerSecond;
                }
            }
        }

        public bool ResumeUploads
        {
            get
            {
                return this.resumeUploads;
            }

            set
            {
                if (this.resumeUploads != value)
                {
                    this.resumeUploads = value;
                    Uploader uploader = this.uploader;
                    if (uploader != null)
                    {
                        uploader.ResumeUploads = value;
                    }

                    this.raisePropertyChanged("ResumeUploads");
                }
            }
        }

        public bool KeepLastUploadPerTemplate
        {
            get
            {
                return this.keepLastUploadPerTemplate;
            }

            set
            {
                if (this.keepLastUploadPerTemplate != value)
                {
                    this.keepLastUploadPerTemplate = value;
                    Settings.Instance.UserSettings.KeepLastUploadPerTemplate = value;
                    JsonSerializationSettings.JsonSerializer.SerializeSettings();

                    this.raisePropertyChanged("KeepLastUploadPerTemplate");
                }
            }
        }

        public bool KeepLastUploadPerTemplateSide
        {
            get
            {
                return this.keepLastUploadPerTemplateSide;
            }

            set
            {
                if (this.keepLastUploadPerTemplateSide != value)
                {
                    this.keepLastUploadPerTemplateSide = value;
                    Settings.Instance.UserSettings.KeepLastUploadPerTemplateSide = value;
                    JsonSerializationSettings.JsonSerializer.SerializeSettings();

                    this.raisePropertyChanged("KeepLastUploadPerTemplateSide");
                }
            }
        }

        public int ContextExpanderIsExpanded
        {
            get => this.expandedExpander;
            set
            {
                this.expandedExpander = value;
                this.raisePropertyChanged("ContextExpanderIsExpanded");
            }
        }

        public ObservableTemplateViewModels ObservableTemplateViewModelsInclAllNone
        {
            get
            {
                return this.observableTemplateViewModelsInclAllNone;
            }
        }

        public ObservableTemplateViewModels ObservableTemplateViewModelsInclAll
        {
            get
            {
                return this.observableTemplateViewModelsInclAll;
            }
        }

        public TemplateComboboxViewModel RecalculatePublishAtSelectedTemplate
        {
            get
            {
                return this.recalculatePublishAtSelectedTemplate;
            }
            set
            {
                if (this.recalculatePublishAtSelectedTemplate != value)
                {
                    this.recalculatePublishAtSelectedTemplate = value;
                    this.raisePropertyChanged("RecalculatePublishAtSelectedTemplate");
                }
            }
        }

        public DateTime? RecalculatePublishAtStartDate
        {
            get => this.recalculatePublishAtStartDate;
            set
            {
                this.recalculatePublishAtStartDate = value;
                this.raisePropertyChanged("RecalculatePublishAtStartDate");
            }
        }

        public DateTime RecalculatePublishAtFirstDate
        {
            get => DateTime.Now.AddDays(1).Date;
        }

        //Selected template for upload removal
        public TemplateComboboxViewModel DeleteSelectedTemplate
        {
            get
            {
                return this.deleteSelectedTemplate;
            }
            set
            {
                if (this.deleteSelectedTemplate != value)
                {
                    this.deleteSelectedTemplate = value;
                    this.raisePropertyChanged("DeleteSelectedTemplate");
                }
            }
        }

        public string[] StatusesInclAll
        {
            get
            {
                string[] values = Enum.GetNames(typeof(UplStatus));
                string[] newValues = new string[values.Length + 1];
                newValues[0] = "All";
                Array.Copy(values, 0, newValues, 1, values.Length);
                return newValues;
            }
        }

        public string DeleteSelectedUploadStatus
        {
            get
            {
                return this.deleteSelectedUploadStatus;
            }
            set
            {
                if (this.deleteSelectedUploadStatus != value)
                {
                    this.deleteSelectedUploadStatus = value;
                    this.raisePropertyChanged("DeleteSelectedUploadStatus");
                }
            }
        }

        public string[] ResetToUploadStatuses
        {
            get
            {
                List<UplStatus> filteredUplStatuses = Enum.GetValues(typeof(UplStatus)).Cast<UplStatus>().Where(
                    act => act == UplStatus.ReadyForUpload ||
                           act == UplStatus.Paused ||
                           act == UplStatus.Stopped).ToList();

                //conversion to string as the converter in xaml handles strings for entries like 'all'
                List<string> filteredUplStatusesStrings = filteredUplStatuses.Select(v => Enum.GetName(typeof(UplStatus), v)).ToList();
                return filteredUplStatusesStrings.ToArray();
            }
        }

        public string ResetToSelectedUploadStatus
        {
            get
            {
                return this.resetToSelectedUploadStatus;
            }
            set
            {
                if (this.resetToSelectedUploadStatus != value)
                {
                    this.resetToSelectedUploadStatus = value;
                    this.raisePropertyChanged("ResetToSelectedUploadStatus");
                }
            }
        }

        public Array ResetAttributeAttributes
        {
            get
            {
                return Enum.GetValues(typeof(UploadTemplateAttribute));
            }
        }

        public UploadTemplateAttribute ResetAttributeSelectedAttribute
        {
            get
            {
                return this.resetAttributeSelectedAttribute;
            }
            set
            {
                if (this.resetAttributeSelectedAttribute != value)
                {
                    this.resetAttributeSelectedAttribute = value;
                    this.raisePropertyChanged("ResetAttributeSelectedAttribute");
                }
            }
        }

        public TemplateComboboxViewModel ResetAttributeSelectedTemplate
        {
            get
            {
                return this.resetAttributeSelectedTemplate;
            }
            set
            {
                if (this.resetAttributeSelectedTemplate != value)
                {
                    this.resetAttributeSelectedTemplate = value;
                    this.raisePropertyChanged("ResetAttributeSelectedTemplate");
                }
            }
        }

        public string ResetWithSelectedUploadStatus
        {
            get
            {
                return this.resetWithSelectedUploadStatus;
            }
            set
            {
                if (this.resetWithSelectedUploadStatus != value)
                {
                    this.resetWithSelectedUploadStatus = value;
                    this.raisePropertyChanged("ResetWithSelectedUploadStatus");
                }
            }
        }

        public TemplateComboboxViewModel ResetWithSelectedTemplate
        {
            get
            {
                return this.resetWithSelectedTemplate;
            }
            set
            {
                if (this.resetWithSelectedTemplate != value)
                {
                    this.resetWithSelectedTemplate = value;
                    this.raisePropertyChanged("ResetWithSelectedTemplate");
                }
            }
        }

        public UploadRibbonViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels, ObservableTemplateViewModels observableTemplateViewModelsInclAll, ObservableTemplateViewModels observableTemplateViewModelsInclAllNone, 
            ObservablePlaylistViewModels observablePlaylistViewModels, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount youtubeAccountForCreatingUploads, YoutubeAccount youtubeAccountForFiltering)
        {
            if (uploadList == null)
            {
                throw new ArgumentException("UploadList must not be null.");
            }

            if (observableTemplateViewModelsInclAll == null)
            {
                throw new ArgumentException("ObservableTemplateViewModelsInclAll must not be null.");
            }

            if (observableTemplateViewModelsInclAllNone == null)
            {
                throw new ArgumentException("ObservableTemplateViewModelsInclAllNone must not be null.");
            }

            if (youtubeAccountForCreatingUploads == null)
            {
                throw new ArgumentException("YoutubeAccountForCreatingUploads must not be null.");
            }

            if (youtubeAccountForFiltering == null)
            {
                throw new ArgumentException("YoutubeAccountForFiltering must not be null.");
            }

            this.uploadList = uploadList;
            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, observableTemplateViewModels, observablePlaylistViewModels, true, observableYoutubeAccountViewModels);

            this.observableTemplateViewModelsInclAllNone = observableTemplateViewModelsInclAllNone;
            this.observableTemplateViewModelsInclAll = observableTemplateViewModelsInclAll;
            this.observableTemplateViewModelsInclAllNone.CollectionChanged+= this.observableTemplateViewModelsInclAllNoneCollectionChanged;
            this.observableTemplateViewModelsInclAll.CollectionChanged += this.observableTemplateViewModelsInclAllCollectionChanged;
            this.recalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAll[0];
            this.deleteSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.resetWithSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.resetAttributeSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];

            this.youtubeAccountForCreatingUploads = youtubeAccountForCreatingUploads;
            this.youtubeAccountForFiltering = youtubeAccountForFiltering;
            EventAggregator.Instance.Subscribe<BeforeYoutubeAccountDeleteMessage>(this.beforeYoutubeAccountDelete);
            EventAggregator.Instance.Subscribe<SelectedFilterYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);

            this.parameterlessCommand = new GenericCommand(this.parameterlessCommandAction);
            this.keepLastUploadPerTemplate = Settings.Instance.UserSettings.KeepLastUploadPerTemplate;

            EventAggregator.Instance.Subscribe<UploadDeleteMessage>(this.DeleteUpload);
            EventAggregator.Instance.Subscribe<UploadListReorderMessage>(this.reorderUplods);
        }

        private void parameterlessCommandAction(object target)
        {
            switch (target)
            {
                case "addfiles":
                    this.openAddFilesDialog();
                    break;
                case "startupload":
                    this.startUploadingAsync();
                    break;
                case "stopupload":
                    this.stopUploading();
                    break;
                case "recalculatepublishat":
                    this.recalculatePublishAt();
                    break;
                case "delete":
                    this.deleteUploadsAsync(true);
                    break;
                case "deleteside":
                    this.deleteUploadsAsync(false);
                    break;
                case "resetstatus":
                    this.resetUploadStatusAsync();
                    break;
                case "resetattribute":
                    this.resetAttributesAsync();
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for parameterlessCommandAction.");
                    break;
            }
        }

        private void reorderUplods(UploadListReorderMessage uploadListReorderMessage)
        {
            this.uploadList.Reorder(uploadListReorderMessage.UploadToMove, uploadListReorderMessage.UploadAtTargetPosition);
            this.observableUploadViewModels.Reorder(this.uploadList);
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
        }

        private void beforeYoutubeAccountDelete(BeforeYoutubeAccountDeleteMessage beforeYoutubeAccountDeleteMessage)
        {
            this.uploadList.DeleteUploads(upload => upload.YoutubeAccount == beforeYoutubeAccountDeleteMessage.AccountToBeDeleted, false);
        }

        private void selectedYoutubeAccountChanged(SelectedFilterYoutubeAccountChangedMessage selectedYoutubeAccountChangedMessage)
        {
            if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount == null)
            {
                throw new ArgumentException("Changed Youtube account must not be null.");
            }

            this.youtubeAccountForCreatingUploads = selectedYoutubeAccountChangedMessage.NewYoutubeAccount;
            if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.IsDummy)
            {
                if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.Name == "All")
                {
                    if (selectedYoutubeAccountChangedMessage.FirstNotAllYoutubeAccount == null)
                    {
                        throw new ArgumentException("FirstNotAllAccount Youtube account must not be null.");
                    }

                    this.youtubeAccountForCreatingUploads = selectedYoutubeAccountChangedMessage.FirstNotAllYoutubeAccount;
                }
            }

            this.youtubeAccountForFiltering = selectedYoutubeAccountChangedMessage.NewYoutubeAccount;
            this.DeleteSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.DeleteSelectedUploadStatus = "Finished";
            this.RecalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAll[0];
            this.RecalculatePublishAtStartDate = null;
            this.ResetToSelectedUploadStatus = "ReadyForUpload";
            this.ResetWithSelectedUploadStatus = "Paused";
            this.ResetWithSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.ResetAttributeSelectedAttribute = UploadTemplateAttribute.All;
            this.ResetAttributeSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
        }

        //need to change the template filter combobox selected item, if this template is deleted. So the selected
        //item isn't set to null by the GUI which prevents updating it to an existing item in the same call.
        private void observableTemplateViewModelsInclAllCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if ((TemplateComboboxViewModel)e.OldItems[0] == this.recalculatePublishAtSelectedTemplate)
                {
                    this.RecalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAllNone[1];
                }
            }
        }

        //need to change the template filter combobox selected item, if this template is deleted. So the selected
        //item isn't set to null by the GUI which prevents updating it to an existing item in the same call.
        private void observableTemplateViewModelsInclAllNoneCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if ((TemplateComboboxViewModel) e.OldItems[0] == this.deleteSelectedTemplate)
                {
                    this.DeleteSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
                }
            }
        }

        private void onUploadStarted(UploadListStartedEventArgs e)
        {
            EventHandler<UploadListStartedEventArgs> handler = this.UploadStarted;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void onUploadFinished(UploadListFinishedEventArgs e)
        {
            EventHandler<UploadListFinishedEventArgs> handler = this.UploadFinished;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void uploadBytesSent(Upload upload)
        {
            EventAggregator.Instance.Publish(new BytesSentMessage(upload));
        }

        private void resumableSessionUriSet(Upload upload)
        {
            EventAggregator.Instance.Publish(new ResumableSessionUriChangedMessage(upload));
        }

        private void uploadStarting(Upload upload)
        {
            EventAggregator.Instance.Publish(new UploadStartingMessage(upload));
        }

        private void uploadFinished(Upload upload)
        {
            this.finishedUploads.Add(upload);
            EventAggregator.Instance.Publish(new UploadFinishedMessage(upload));

            bool executed = false;
            if (upload.Template != null && upload.Template.EnableAutomation && !string.IsNullOrWhiteSpace(upload.Template.AutomationSettings.ExecuteAfterEachPath))
            {
                UploadResultAutomationInfo uploadResultAutomationInfo = new UploadResultAutomationInfo();
                uploadResultAutomationInfo.UploadedFiles.Add(new FileInfo(upload.FilePath).FullName, upload.UploadStatus.ToString());
                this.serializeandExecute(upload.Template.AutomationSettings.ExecuteAfterEachPath, "each", uploadResultAutomationInfo);
                executed = true;
            }

            if (upload.Template != null && upload.Template.EnableAutomation && !string.IsNullOrWhiteSpace(upload.Template.AutomationSettings.ExecuteAfterTemplatePath))
            {              
                if (upload.Template.Uploads.All(upl => upl.UploadStatus != UplStatus.ReadyForUpload))
                {
                    List<Upload> templateUploads = new List<Upload>();
                    templateUploads.AddRange(this.finishedUploads.Where(upl => upl.Template == upload.Template));

                    UploadResultAutomationInfo uploadResultAutomationInfo = new UploadResultAutomationInfo();
                    uploadResultAutomationInfo.TemplateName = upload.Template.Name;

                    foreach (Upload templateUpload in templateUploads)
                    {
                        uploadResultAutomationInfo.UploadedFiles.Add(new FileInfo(templateUpload.FilePath).FullName, templateUpload.UploadStatus.ToString());
                    }

                    this.serializeandExecute(upload.Template.AutomationSettings.ExecuteAfterTemplatePath, "template", uploadResultAutomationInfo);
                }
            }
        }

        private async void serializeandExecute(string executeFile, string midText, UploadResultAutomationInfo uploadResultAutomationInfo)
        {
            await Task.Run(async () =>
                {
                    this.autoReset.WaitOne();
                    double delay = (DateTime.Now - this.lastExecution).TotalMilliseconds;
                    if (delay < Settings.Instance.UserSettings.AutomationExecutionDelayInSeconds * 1000)
                    {
                        delay = Settings.Instance.UserSettings.AutomationExecutionDelayInSeconds * 1000 - delay;
                        await Task.Delay((int)delay).ConfigureAwait(false);
                    }
                }
            );            

            string fileName = $"automationinfo_uploadfinished_{midText}_{DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss-fff")}.json";
            JsonSerializationUploadResultAutomationInfo.JsonSerializer.SerializeUploadResultAutomationInfo(fileName, uploadResultAutomationInfo);
            Process proc = new Process();
            proc.StartInfo.FileName = new FileInfo(executeFile).FullName;
            proc.StartInfo.WorkingDirectory = new DirectoryInfo(executeFile).Parent.FullName;
            proc.StartInfo.Arguments = Path.Combine(JsonSerializationUploadResultAutomationInfo.JsonSerializer.SerializationFolder, fileName);
            this.lastExecution = DateTime.Now;
            proc.Start();
            this.autoReset.Set();
        }

        private void uploadStatsUpdated()
        { 
            EventAggregator.Instance.Publish(new UploadStatsChangedMessage());
        }

        private void openAddFilesDialog()
        {
            Tracer.Write($"UploadListViewModel.openUploadDialog: Start.");
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;

            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                Tracer.Write($"UploadListViewModel.openUploadDialog: DialogResult OK.");

                this.AddFiles(fileDialog.FileNames, false);

                Tracer.Write($"UploadListViewModel.openUploadDialog: Selected {fileDialog.FileNames.Length} files.");
            }
            else
            {
                Tracer.Write($"UploadListViewModel.openUploadDialog: DialogResult not OK.");
            }

            Tracer.Write($"UploadListViewModel.openUploadDialog: End.");
        }

        public void AddFiles(string[] files, bool considerAutomationDirectoy)
        {
            Tracer.Write($"UploadListViewModel.AddFiles: Start, add {files.Length} uploads, considerAutomationDirectoy {considerAutomationDirectoy}.");
            Array.Sort(files, StringComparer.InvariantCulture);
            
            AddFilesResult addFilesResult = this.uploadList.AddFiles(files, this.youtubeAccountForCreatingUploads, considerAutomationDirectoy);
            if (addFilesResult.TemplateAutoAdded)
            {
                Tracer.Write($"UploadListViewModel.AddUploads: At least one template was auto added.");
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();

            if (addFilesResult.StartAutoUpload)
            {
                Tracer.Write($"UploadListViewModel.AddUploads: Starting auto upload.");
                this.startUploadingAsync();
            }

            Tracer.Write($"UploadListViewModel.AddUploads: End.");
        }

        private async void startUploadingAsync()
        {
            lock (this.uploadingLock)
            {
                if (this.uploadStatus == UploadStatus.Uploading)
                {
                    return;
                }

                this.uploadStatus = UploadStatus.Uploading;
            }

            //prevent sleep mode
            PowerSavingHelper.DisablePowerSaving();

            this.finishedUploads.Clear();

            this.uploadStatus = UploadStatus.Uploading;
            bool resume = this.resumeUploads;

            //AutoResetEvent is used to prevent the UploadStats
            //from updating in the time between one upload
            //finished and the next upload has not yet started
            //as this can lead to unwanted behaviour in the UploadStats...
            AutoResetEvent resetEvent = new AutoResetEvent(true);
            UploadStats uploadStats = new UploadStats(resetEvent);
            this.onUploadStarted(new UploadListStartedEventArgs(uploadStats));

            this.uploader = new Uploader(this.uploadList, this.maxUploadInBytesPerSecond);
            this.uploader.UploadBytesSent += (sender, upload) => this.uploadBytesSent(upload);
            this.uploader.ResumableSessionUriSet += (sender, upload) => this.resumableSessionUriSet(upload);
            this.uploader.UploadStatsUpdated += (sender) => this.uploadStatsUpdated();
            this.uploader.UploadStarting += (sender, upload) => this.uploadStarting(upload);
            this.uploader.UploadFinished += (sender, upload) => this.uploadFinished(upload);
            UploaderResult uploadResult = await uploader.UploadAsync(uploadStats, resume, resetEvent).ConfigureAwait(false);
            bool uploadStopped = uploader.UploadStopped;
            this.uploader = null;

            this.uploadStatus = UploadStatus.NotUploading;

            //needs to be after PowerSavingHelper.EnablePowerSaving(); so that StandBy is not prevented.
            this.onUploadFinished(new UploadListFinishedEventArgs(uploadResult == UploaderResult.DataSent, uploadStopped));
            this.uploadStatus = UploadStatus.NotUploading;

            if(uploadResult != UploaderResult.NothingDone && !uploadStopped)
            {
                this.executelFinalAutomationExecutions();
            }

            this.finishedUploads.Clear();

            PowerSavingHelper.EnablePowerSaving();
        }

        private void executelFinalAutomationExecutions()
        {
            //all templates with uploads
            List<Template> templatesWithUploads = this.finishedUploads.Select(upl => upl.Template).Distinct().ToList();
            templatesWithUploads.RemoveAll(template => template == null);

            //check if aborted and if last template execution needs be done for automation
            if (this.uploadList.Any(upl => upl.UploadStatus == UplStatus.ReadyForUpload)) //aborted
            {
                foreach(Template templateWithUpload in templatesWithUploads)
                {
                    if (templateWithUpload.Uploads.Any(upl => upl.UploadStatus == UplStatus.ReadyForUpload))
                    {
                        List<Upload> templateUploads = new List<Upload>();
                        templateUploads.AddRange(this.finishedUploads.Where(upl => upl.Template == templateWithUpload));

                        UploadResultAutomationInfo uploadResultAutomationInfoTemplate = new UploadResultAutomationInfo();
                        uploadResultAutomationInfoTemplate.TemplateName = templateWithUpload.Name;

                        foreach (Upload templateUpload in templateUploads)
                        {
                            uploadResultAutomationInfoTemplate.UploadedFiles.Add(new FileInfo(templateUpload.FilePath).FullName, templateUpload.UploadStatus.ToString());
                        }

                        this.serializeandExecute(templateWithUpload.AutomationSettings.ExecuteAfterTemplatePath, "template", uploadResultAutomationInfoTemplate);
                    }
                }
            }

            //prevent double execution of execute after all file, if the same file is configured on multiple templates
            List<FileInfo> filesToExecute = new List<FileInfo>();
            foreach (Template templateWithUpload in templatesWithUploads)
            {
                if (templateWithUpload.EnableAutomation && !string.IsNullOrWhiteSpace(templateWithUpload.AutomationSettings.ExecuteAfterAllPath))
                {
                    FileInfo file = new FileInfo(templateWithUpload.AutomationSettings.ExecuteAfterAllPath);
                    if (!filesToExecute.Any(fileInternal => fileInternal.FullName == file.FullName))
                    {
                        filesToExecute.Add(file);
                    }
                }
            }

            UploadResultAutomationInfo uploadResultAutomationInfo = new UploadResultAutomationInfo();
            foreach (Upload uploadFinished in this.finishedUploads)
            {
                uploadResultAutomationInfo.UploadedFiles.Add(new FileInfo(uploadFinished.FilePath).FullName, uploadFinished.UploadStatus.ToString());
            }

            foreach (FileInfo file in filesToExecute)
            {
                this.serializeandExecute(file.FullName, "all", uploadResultAutomationInfo);
            }
        }

        private void stopUploading()
        {
            if (this.uploadStatus == UploadStatus.Uploading)
            {
                Uploader uploader = this.uploader;
                if (uploader != null)
                {
                    uploader.StopUpload();
                }
            }
        }

        //parameter skips dialog for testing
        private async void deleteUploadsAsync(bool quick)
        {
            Tracer.Write($"UploadListViewModel.deleteUploads(quick={quick}): Start.");
            bool remove = true;

            if (quick || this.deleteSelectedUploadStatus == "All" ||
                (UplStatus) Enum.Parse(typeof(UplStatus), this.deleteSelectedUploadStatus) != UplStatus.Finished)
            {
                if (!quick)
                {
                    ConfirmControl control = new ConfirmControl(
                        $"Do you really want to remove all uploads with template = '{this.deleteSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.deleteSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'?",
                        true);

                    remove = (bool)await DialogHost.Show(control, "RootDialog");
                }
            }
            
            if (remove)
            {
                Tracer.Write($"UploadListViewModel.deleteUploads: DialogResult OK.");
                this.DeleteUploads(quick);
            }
            else
            {
                Tracer.Write($"UploadListViewModel.deleteUploads: DialogResult not OK.");
            }

            Tracer.Write($"UploadListViewModel.deleteUploads(quick={quick}): End.");
        }

        //parameter skips dialog for testing
        private async void resetUploadStatusAsync()
        {
            bool reset = true;
            ConfirmControl control = new ConfirmControl(
                $"Do you really want to reset all uploads with template = '{this.resetWithSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.resetWithSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}' to status '{new UplStatusStringValuesConverter().Convert(this.resetToSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'? Ready for Upload will restart begun uploads.",
                true);

            reset = (bool)await DialogHost.Show(control, "RootDialog").ConfigureAwait(false);
            
            if (reset)
            {
                this.resetUploadStatus();
            }
        }

        private async void resetAttributesAsync()
        {
            ConfirmControl control = new ConfirmControl(
                $"Do you really want to reset attributes to template value with attribute = '{new EnumConverter().Convert(this.resetAttributeSelectedAttribute, typeof(string), null, CultureInfo.CurrentCulture)}' on all uploads with template = '{this.resetAttributeSelectedTemplate.Template.Name}' ?",
                true);

            bool reset = (bool)await DialogHost.Show(control, "RootDialog").ConfigureAwait(false);
            

            if (reset)
            {
                this.resetAttributes();
            }
        }

        private void resetAttributes()
        {
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];


            predicates[0] = upload => string.IsNullOrWhiteSpace(upload.ResumableSessionUri);
            if (this.resetAttributeSelectedTemplate.Template.IsDummy)
            {
                if (this.resetAttributeSelectedTemplate.Template.Name == "All")
                {
                    if (this.youtubeAccountForFiltering.IsDummy)
                    {
                        if (this.youtubeAccountForFiltering.Name == "All")
                        {
                            predicates[1] = upload => true;
                        }
                    }
                    else
                    {
                        predicates[1] = upload => upload.YoutubeAccount == this.youtubeAccountForFiltering;
                    }
                }
                else if (this.resetAttributeSelectedTemplate.Template.Name == "None")
                {
                    if (this.youtubeAccountForFiltering.IsDummy)
                    {
                        if (this.youtubeAccountForFiltering.Name == "All")
                        {
                            predicates[1] = upload => upload.Template == null;
                        }
                    }
                    else
                    {
                        predicates[1] = upload => upload.Template == null && upload.YoutubeAccount == this.youtubeAccountForFiltering;
                    }
                }
            }
            else
            {
                predicates[1] = upload => upload.Template == this.resetAttributeSelectedTemplate.Template;
            }

            List<Upload> uploads = this.uploadList.Uploads.Where(upload => TinyHelpers.PredicateAnd(predicates)(upload)).ToList();

            //set all puplish at dates to null so that existing values don't block potential dates
            if (this.resetAttributeSelectedAttribute == UploadTemplateAttribute.All ||
                this.resetAttributeSelectedAttribute == UploadTemplateAttribute.PublishAt)
            {
                foreach (Upload upload in uploads)
                {
                    upload.PublishAt = null;
                }
            }

            foreach (Upload upload in uploads)
            {
                switch (this.resetAttributeSelectedAttribute)
                {
                    case UploadTemplateAttribute.All:
                        upload.CopyTemplateValues();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "all"));
                        break;
                    case UploadTemplateAttribute.Title:
                        upload.CopyTitleFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "title"));
                        break;
                    case UploadTemplateAttribute.Description:
                        upload.CopyDescriptionFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "description"));
                        break;
                    case UploadTemplateAttribute.Tags:
                        upload.CopyTagsFromtemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "tags"));
                        break;
                    case UploadTemplateAttribute.Visibility:
                        upload.CopyVisibilityFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "visibility"));
                        break;
                    case UploadTemplateAttribute.VideoLanguage:
                        upload.CopyVideoLanguageFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "videoLanguage"));
                        break;
                    case UploadTemplateAttribute.DescriptionLanguage:
                        upload.CopyDescriptionLanguageFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "descriptionLanguage"));
                        break;
                    case UploadTemplateAttribute.PublishAt:
                        upload.AutoSetPublishAtDateTime();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "publishAt"));
                        break;
                    case UploadTemplateAttribute.Playlist:
                        upload.CopyPlaylistFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "playlist"));
                        break;
                    case UploadTemplateAttribute.Category:
                        upload.CopyCategoryFromTemplate();
                        EventAggregator.Instance.Publish(new AttributeResetMessage(upload, "category"));
                        break;
                    default:
                        throw new InvalidOperationException();
                        break;
                }
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
        }

        private void recalculatePublishAt()
        {
            if (this.recalculatePublishAtSelectedTemplate.Template.IsDummy)
            {
                if(this.recalculatePublishAtSelectedTemplate.Name == "All")
                {
                    if (this.recalculatePublishAtStartDate != null)
                    {
                        this.uploadList.SetStartDateOnAllTemplateSchedules(this.recalculatePublishAtStartDate.Value, this.youtubeAccountForFiltering);
                    }

                    foreach (Upload upload in this.uploadList)
                    {
                        if (upload.Template != null  && upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            if (this.youtubeAccountForFiltering.IsDummy)
                            {
                                if (this.youtubeAccountForFiltering.Name == "All")
                                {
                                    upload.PublishAt = null;
                                }
                            }
                            else
                            {
                                if (upload.Template.YoutubeAccount == this.youtubeAccountForFiltering)
                                {
                                    upload.PublishAt = null;
                                }
                            }
                        }
                    }

                    foreach (Upload upload in this.uploadList)
                    {
                        if (upload.Template != null && upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            if (this.youtubeAccountForFiltering.IsDummy)
                            {
                                if (this.youtubeAccountForFiltering.Name == "All")
                                {
                                    upload.AutoSetPublishAtDateTime();
                                    EventAggregator.Instance.Publish(new PublishAtChangedMessage(upload));
                                }
                            }
                            else
                            {
                                if (upload.Template.YoutubeAccount == this.youtubeAccountForFiltering)
                                {
                                    upload.AutoSetPublishAtDateTime();
                                    EventAggregator.Instance.Publish(new PublishAtChangedMessage(upload));
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (this.recalculatePublishAtStartDate != null)
                {
                    this.recalculatePublishAtSelectedTemplate.Template.SetStartDateOnTemplateSchedule(this.recalculatePublishAtStartDate.Value);
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template == this.recalculatePublishAtSelectedTemplate.Template &&
                        upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                    {
                        upload.PublishAt = null;
                    }
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template == this.recalculatePublishAtSelectedTemplate.Template &&
                        upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                    {
                        upload.AutoSetPublishAtDateTime();
                        EventAggregator.Instance.Publish(new PublishAtChangedMessage(upload));
                    }
                }
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeTemplateList();
        }

        private void resetRecalculatePublishAtStartDate(object obj)
        {
            this.RecalculatePublishAtStartDate = null;
        }

        //exposed for testing
        public void DeleteUpload(UploadDeleteMessage uploadDeleteMessage)
        {
            Tracer.Write($"UploadListViewModel.DeleteUpload: Start, Guid '{uploadDeleteMessage.UploadGuid}'.");
            this.deleteUploads(upload => upload.Guid == uploadDeleteMessage.UploadGuid, true, false);
            Tracer.Write($"UploadListViewModel.DeleteUpload: End.");
        }

        //exposed for testing
        public void DeleteUploads(bool quick)
        {
            Tracer.Write($"UploadListViewModel.deleteUploads(quick={quick}): Start.");
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];

            if (quick)
            {
                Tracer.Write($"UploadListViewModel.deleteUploads status with quick=true.");
                predicates[0] = upload => upload.UploadStatus == UplStatus.Finished;
            }
            else
            {
                if (this.deleteSelectedUploadStatus == "All")
                {
                    Tracer.Write($"UploadListViewModel.deleteUploads: with status All.");
                    predicates[0] = upload => true;
                }
                else
                {
                    UplStatus status = (UplStatus)Enum.Parse(typeof(UplStatus), this.deleteSelectedUploadStatus);
                    Tracer.Write($"UploadListViewModel.deleteUploads: with status {this.deleteSelectedUploadStatus}.");
                    predicates[0] = upload => upload.UploadStatus == status;
                }
            }

            if (quick)
            {
                Tracer.Write($"UploadListViewModel.deleteUploads template with quick=true.");
                if (this.youtubeAccountForFiltering.IsDummy == true)
                {
                    if (youtubeAccountForFiltering.Name == "All")
                    {
                        Tracer.Write($"UploadListViewModel.deleteUploads with quick=true and dummy account all.");
                        predicates[1] = upload => true;
                    }
                }
                else
                {
                    Tracer.Write($"UploadListViewModel.deleteUploads with quick=true and filtered account.");
                    predicates[1] = upload => upload.YoutubeAccount == this.youtubeAccountForFiltering;
                }
            }
            else
            {
                if (this.deleteSelectedTemplate.Template.IsDummy)
                {
                    if (this.deleteSelectedTemplate.Template.Name == "All")
                    {
                        if (this.youtubeAccountForFiltering.IsDummy == true)
                        {
                            if (youtubeAccountForFiltering.Name == "All")
                            {
                                Tracer.Write($"UploadListViewModel.deleteUploads: with dummy template All and dummy account all.");
                                predicates[1] = upload => true;
                            }
                        }
                        else
                        {
                            Tracer.Write($"UploadListViewModel.deleteUploads: with dummy template All and filtered account.");
                            predicates[1] = upload => upload.YoutubeAccount == this.youtubeAccountForFiltering;
                        }
                    }
                    else if (this.deleteSelectedTemplate.Template.Name == "None")
                    {
                        if (this.youtubeAccountForFiltering.IsDummy == true)
                        {
                            if (youtubeAccountForFiltering.Name == "All")
                            {
                                Tracer.Write($"UploadListViewModel.deleteUploads: with dummy template None and dummy account all.");
                                predicates[1] = upload => upload.Template == null;
                            }
                        }
                        else
                        {
                            Tracer.Write($"UploadListViewModel.deleteUploads: with dummy template None and filtered.");
                            predicates[1] = upload => upload.Template == null && upload.YoutubeAccount == this.youtubeAccountForFiltering;
                        }
                    }
                }
                else
                {
                    Tracer.Write($"UploadListViewModel.deleteUploads: with template {this.deleteSelectedTemplate.Template.Name}.");
                    predicates[1] = upload => upload.Template == this.deleteSelectedTemplate.Template;
                }
            }

            Predicate<Upload> combinedPredicate = TinyHelpers.PredicateAnd(predicates);
            this.deleteUploads(combinedPredicate, false, quick);
            Tracer.Write($"UploadListViewModel.deleteUploads(quick={quick}): End.");
        }

        private void deleteUploads(Predicate<Upload> predicate, bool ignoreKeepLastPerTemplate, bool quick)
        {
            Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): Start.");

            bool serializeTemplates = false;
            List<Upload> uploadsToDelete = this.uploadList.GetUploads(predicate);
            if (uploadsToDelete.Any(upl => upl.Template != null))
            {
                Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): At least one upload to delete has template.");
                serializeTemplates = true;
            }

            bool keepLastUploadPerTemplateInternal = quick ? this.keepLastUploadPerTemplate : this.keepLastUploadPerTemplateSide;
            this.uploadList.DeleteUploads(predicate, !ignoreKeepLastPerTemplate && keepLastUploadPerTemplateInternal);

            if (serializeTemplates)
            {
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();

            Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): End.");
        }

        private void resetUploadStatus()
        {
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];

            if (this.resetWithSelectedUploadStatus == "All")
            {
                predicates[0] = upload => true;
            }
            else
            {
                UplStatus status = (UplStatus)Enum.Parse(typeof(UplStatus), this.resetWithSelectedUploadStatus);
                predicates[0] = upload => upload.UploadStatus == status;
            }

            if (this.resetWithSelectedTemplate.Template.IsDummy)
            {
                if (this.resetWithSelectedTemplate.Template.Name == "All")
                {
                    if (this.youtubeAccountForFiltering.IsDummy)
                    {
                        if (this.youtubeAccountForFiltering.Name == "All")
                        {
                            predicates[1] = upload => true;
                        }
                    }
                    else
                    {
                        predicates[1] = upload => upload.YoutubeAccount == this.youtubeAccountForFiltering;
                    }
                }
                else if (this.resetWithSelectedTemplate.Template.Name == "None")
                {
                    if (this.youtubeAccountForFiltering.IsDummy)
                    {
                        if (this.youtubeAccountForFiltering.Name == "All")
                        {
                            predicates[1] = upload => upload.Template == null; ;
                        }
                    }
                    else
                    {
                        predicates[1] = upload => upload.Template == null && upload.YoutubeAccount == this.youtubeAccountForFiltering;
                    }
                }
            }
            else
            {
                predicates[1] = upload => upload.Template == this.resetWithSelectedTemplate.Template;
            }

            List<Upload> uploads = this.uploadList.Uploads.Where(upload => TinyHelpers.PredicateAnd(predicates)(upload)).ToList();

            UplStatus resetToStatus = (UplStatus) Enum.Parse(typeof(UplStatus), this.resetToSelectedUploadStatus);
            foreach (Upload upload in uploads)
            {
                if (upload.VerifyForUpload())
                {
                    if (resetToStatus == UplStatus.Stopped)
                    {
                        //upload cannot be stopped if it hasn't been started
                        if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                        {
                            continue;
                        }
                    }

                    upload.UploadStatus = resetToStatus;
                    EventAggregator.Instance.Publish(new UploadStatusChangedMessage(upload));
                }
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
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
    }
}
