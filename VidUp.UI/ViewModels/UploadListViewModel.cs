using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Windows.Forms;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
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
    //todo: move ribbon properties to separate view model
    public class UploadListViewModel : INotifyPropertyChanged
    {
        private UploadList uploadList;

        private ObservableTemplateViewModels observableTemplateViewModelsInclAll;
        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;

        private GenericCommand deleteCommand;
        private ObservableUploadViewModels observableUploadViewModels;

        private GenericCommand addUploadCommand;
        private GenericCommand startUploadingCommand;

        private GenericCommand stopUploadingCommand;
        private GenericCommand recalculatePublishAtCommand;
        private GenericCommand resetRecalculatePublishAtStartDateCommand;
        private GenericCommand deleteUploadsCommand;
        private GenericCommand resetUploadsCommand;
        private GenericCommand resetAttributeCommand;

        private UploadStatus uploadStatus = UploadStatus.NotUploading;
        private Uploader uploader;
        private bool resumeUploads = true;
        private long maxUploadInBytesPerSecond = 0;

        private TemplateComboboxViewModel deleteSelectedTemplate;
        private string deleteSelectedUploadStatus = "Finished";

        private string resetToSelectedUploadStatus = "ReadyForUpload";
        private string resetWithSelectedUploadStatus = "Paused";
        private TemplateComboboxViewModel resetWithSelectedTemplate;

        private UploadTemplateAttribute resetAttributeSelectedAttribute = UploadTemplateAttribute.All;
        private TemplateComboboxViewModel resetAttributeSelectedTemplate;

        private TemplateComboboxViewModel recalculatePublishAtSelectedTemplate;
        private DateTime? recalculatePublishAtStartDate;

        private YoutubeAccount youtubeAccountForCreatingUploads;
        private YoutubeAccount youtubeAccountForFiltering;
        private object uploadingLock = new object();

        //todo: add to event aggregator maybe
        public event EventHandler<UploadStartedEventArgs> UploadStarted;
        public event EventHandler<UploadFinishedEventArgs> UploadFinished;
        public event PropertyChangedEventHandler PropertyChanged;

        public GenericCommand DeleteCommand
        {
            get
            {
                return this.deleteCommand;
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

        public GenericCommand StopUploadingCommand
        {
            get
            {
                return this.stopUploadingCommand;
            }
        }

        public GenericCommand RecalculatePublishAtCommand
        {
            get
            {
                return this.recalculatePublishAtCommand;
            }
        }

        public GenericCommand ResetRecalculatePublishAtStartDateCommand
        {
            get
            {
                return this.resetRecalculatePublishAtStartDateCommand;
            }
        }

        public GenericCommand DeleteUploadsCommand
        {
            get
            {
                return this.deleteUploadsCommand;
            }
        }

        public GenericCommand ResetUploadsCommand
        {
            get
            {
                return this.resetUploadsCommand;
            }
        }

        public GenericCommand ResetAttributeCommand
        {
            get
            {
                return this.resetAttributeCommand;
            }
        }

        

        public ObservableUploadViewModels ObservableUploadViewModels
        {
            get
            {
                return this.observableUploadViewModels;
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
                    this.observableUploadViewModels.ResumeUploads = value;
                    this.raisePropertyChanged("ResumeUploads");
                }
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

        public UploadListViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels, ObservableTemplateViewModels observableTemplateViewModelsInclAll, ObservableTemplateViewModels observableTemplateViewModelsInclAllNone,
            ObservablePlaylistViewModels observablePlaylistViewModels, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount youtubeAccountForCreatingUploads, YoutubeAccount youtubeAccountForFiltering)
        {
            this.uploadList = uploadList;

            this.observableTemplateViewModelsInclAllNone = observableTemplateViewModelsInclAllNone;
            this.observableTemplateViewModelsInclAll = observableTemplateViewModelsInclAll;
            this.observableTemplateViewModelsInclAllNone.CollectionChanged+= this.observableTemplateViewModelsInclAllNoneCollectionChanged;
            this.observableTemplateViewModelsInclAll.CollectionChanged += this.observableTemplateViewModelsInclAllCollectionChanged;
            this.recalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAll[0];
            this.deleteSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.resetWithSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.resetAttributeSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];

            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, observableTemplateViewModels, observablePlaylistViewModels, this.resumeUploads, observableYoutubeAccountViewModels);

            this.youtubeAccountForCreatingUploads = youtubeAccountForCreatingUploads;
            this.youtubeAccountForFiltering = youtubeAccountForFiltering;
            EventAggregator.Instance.Subscribe<BeforeYoutubeAccountDeleteMessage>(this.beforeYoutubeAccountDelete);
            EventAggregator.Instance.Subscribe<SelectedFilterYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);

            this.deleteCommand = new GenericCommand(this.DeleteUpload);
            this.addUploadCommand = new GenericCommand(this.openUploadDialog);
            this.startUploadingCommand = new GenericCommand(this.startUploadingAsync);
            this.stopUploadingCommand = new GenericCommand(this.stopUploading);
            this.recalculatePublishAtCommand = new GenericCommand(this.recalculatePublishAt);
            this.resetRecalculatePublishAtStartDateCommand = new GenericCommand(this.resetRecalculatePublishAtStartDate);
            this.deleteUploadsCommand = new GenericCommand(this.deleteUploadsAsync);
            this.resetUploadsCommand = new GenericCommand(this.resetUploadsAsync);
            this.resetAttributeCommand = new GenericCommand(this.resetAttributeAsync);
        }

        private void beforeYoutubeAccountDelete(BeforeYoutubeAccountDeleteMessage beforeYoutubeAccountDeleteMessage)
        {
            this.uploadList.DeleteUploads(upload => upload.YoutubeAccount == beforeYoutubeAccountDeleteMessage.AccountToBeDeleted);
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

            foreach (UploadViewModel observableUploadViewModel in this.observableUploadViewModels)
            {
                if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.IsDummy)
                {
                    if (selectedYoutubeAccountChangedMessage.NewYoutubeAccount.Name == "All")
                    {
                        if (observableUploadViewModel.Visible == false)
                        {
                            observableUploadViewModel.Visible = true;
                        }
                    }
                }
                else
                {
                    if (!selectedYoutubeAccountChangedMessage.NewYoutubeAccount.IsDummy)
                    {
                        if (observableUploadViewModel.SelectedYoutubeAccount.YoutubeAccount == selectedYoutubeAccountChangedMessage.NewYoutubeAccount)
                        {
                            observableUploadViewModel.Visible = true;
                        }
                        else
                        {
                            observableUploadViewModel.Visible = false;
                        }
                    }
                }
            }
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

        private void onUploadStarted(UploadStartedEventArgs e)
        {
            EventHandler<UploadStartedEventArgs> handler = this.UploadStarted;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void onUploadFinished(UploadFinishedEventArgs e)
        {
            EventHandler<UploadFinishedEventArgs> handler = this.UploadFinished;

            if (handler != null)
            {
                handler(this, e);
            }
        }

        private void onUploadBytesSent(Upload upload)
        {
            EventAggregator.Instance.Publish(new BytesSentMessage(upload));
        }

        private void onUploadStatusChanged(Upload upload)
        {
            EventAggregator.Instance.Publish(new UploadStatusChangedMessage(upload));
        }

        private void onErrorMessageChanged(Upload upload)
        {
            EventAggregator.Instance.Publish(new ErrorMessageChangedMessage(upload));
        }

        private void onResumableSessionUriSet(Upload upload)
        {
            EventAggregator.Instance.Publish(new ResumableSessionUriChangedMessage(upload));
        }

        private void onUploadStatsUpdated()
        { 
            EventAggregator.Instance.Publish(new UploadStatsChangedMessage());
        }

        public void ReOrder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            this.uploadList.ReOrder(uploadToMove, uploadAtTargetPosition);
            this.observableUploadViewModels.ReOrder(this.uploadList);
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
        }

        private void openUploadDialog(object obj)
        {
            Tracer.Write($"UploadListViewModel.openUploadDialog: Start.");
            OpenFileDialog fileDialog = new OpenFileDialog();
            fileDialog.Multiselect = true;

            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                Tracer.Write($"UploadListViewModel.openUploadDialog: DialogResult OK.");

                this.AddFiles(fileDialog.FileNames);

                Tracer.Write($"UploadListViewModel.openUploadDialog: Selected {fileDialog.FileNames.Length} files.");
            }
            else
            {
                Tracer.Write($"UploadListViewModel.openUploadDialog: DialogResult not OK.");
            }

            Tracer.Write($"UploadListViewModel.openUploadDialog: End.");
        }

        public void AddFiles(string[] files)
        {
            Tracer.Write($"UploadListViewModel.AddFiles: Start, add {files.Length} uploads.");
            Array.Sort(files, StringComparer.InvariantCulture);
            
            bool templateAutoAdded = this.uploadList.AddFiles(files, this.youtubeAccountForCreatingUploads);
            if (templateAutoAdded)
            {
                Tracer.Write($"UploadListViewModel.AddUploads: At least one template was auto added.");
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
            Tracer.Write($"UploadListViewModel.AddUploads: End.");
        }

        private async void startUploadingAsync(object obj)
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

            this.uploadStatus = UploadStatus.Uploading;
            bool resume = this.resumeUploads;

            //AutoResetEvent is used to prevent the UploadStats
            //from updating in the time between one upload
            //finished and the next upload has not yet started
            //as this can lead to unwanted behaviour in the UploadStats...
            AutoResetEvent resetEvent = new AutoResetEvent(true);
            UploadStats uploadStats = new UploadStats(resetEvent);
            this.onUploadStarted(new UploadStartedEventArgs(uploadStats));

            this.uploader = new Uploader(this.uploadList, this.maxUploadInBytesPerSecond);
            this.uploader.UploadBytesSent += (sender, upload) => this.onUploadBytesSent(upload);
            this.uploader.UploadStatusChanged += (sender, upload) => this.onUploadStatusChanged(upload);
            this.uploader.ResumableSessionUriSet += (sender, upload) => this.onResumableSessionUriSet(upload);
            this.uploader.UploadStatsUpdated += (sender) => this.onUploadStatsUpdated();
            this.uploader.ErrorMessageChanged += (sender, upload) => this.onErrorMessageChanged(upload);
            UploaderResult uploadResult = await uploader.UploadAsync(uploadStats, resume, resetEvent).ConfigureAwait(false);
            bool uploadStopped = uploader.UploadStopped;
            this.uploader = null;

            this.uploadStatus = UploadStatus.NotUploading;

            PowerSavingHelper.EnablePowerSaving();

            //needs to be after PowerSavingHelper.EnablePowerSaving(); so that StandBy is not prevented.
            this.onUploadFinished(new UploadFinishedEventArgs(uploadResult == UploaderResult.DataSent, uploadStopped));
            this.uploadStatus = UploadStatus.NotUploading;
        }

        private void stopUploading(object obj)
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
        private async void deleteUploadsAsync(object parameter)
        {
            Tracer.Write($"UploadListViewModel.deleteUploads(object parameter): Start.");
            bool skipDialog = (bool)parameter;
            bool remove = true;

            //skip dialog on testing
            if (!(bool)skipDialog)
            {
                if (this.deleteSelectedUploadStatus == "All" ||
                    (UplStatus) Enum.Parse(typeof(UplStatus), this.deleteSelectedUploadStatus) != UplStatus.Finished)
                {
                    ConfirmControl control = new ConfirmControl(
                        $"Do you really want to remove all uploads with template = '{this.deleteSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.deleteSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'?",
                        true);

                    remove = (bool) await DialogHost.Show(control, "RootDialog");
                }
            }

            if (remove)
            {
                Tracer.Write($"UploadListViewModel.deleteUploads: DialogResult OK.");
                this.deleteUploads();
            }
            else
            {
                Tracer.Write($"UploadListViewModel.deleteUploads: DialogResult not OK.");
            }

            Tracer.Write($"UploadListViewModel.deleteUploads(object parameter): End.");
        }

        //parameter skips dialog for testing
        private async void resetUploadsAsync(object parameter)
        {
            bool skipDialog = (bool)parameter;
            bool reset = true;

            //skip dialog on testing
            if (!skipDialog)
            {
                ConfirmControl control = new ConfirmControl(
                    $"Do you really want to reset all uploads with template = '{this.resetWithSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.resetWithSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}' to status '{new UplStatusStringValuesConverter().Convert(this.resetToSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'? Ready for Upload will restart begun uploads.",
                    true);

                reset = (bool)await DialogHost.Show(control, "RootDialog").ConfigureAwait(false);
            }

            if (reset)
            {
                this.resetUploads();
            }
        }

        private async void resetAttributeAsync(object parameter)
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

        private void recalculatePublishAt(object obj)
        {
            if (this.recalculatePublishAtSelectedTemplate.IsDummy)
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
        public void DeleteUpload(object parameter)
        {
            Tracer.Write($"UploadListViewModel.DeleteUpload: Start, Guid '{parameter}'.");
            Guid uploadGuid = Guid.Parse((string)parameter);
            this.deleteUploads(upload => upload.Guid == uploadGuid);
            Tracer.Write($"UploadListViewModel.DeleteUpload: End.");
        }

        private void deleteUploads()
        {
            Tracer.Write($"UploadListViewModel.deleteUploads: Start.");
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];

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

            Predicate<Upload> combinedPredicate = TinyHelpers.PredicateAnd(predicates);
            this.deleteUploads(combinedPredicate);
            Tracer.Write($"UploadListViewModel.deleteUploads: End.");
        }

        private void deleteUploads(Predicate<Upload> predicate)
        {
            Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): Start.");
            bool serializeTemplates = false;
            List<Upload> uploadsToDelete = this.uploadList.GetUploads(predicate);
            if (uploadsToDelete.Any(upl => upl.Template != null))
            {
                Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): At least one upload to delete has template.");
                serializeTemplates = true;
            }

            this.uploadList.DeleteUploads(predicate);

            if (serializeTemplates)
            {
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
            Tracer.Write($"UploadListViewModel.deleteUploads(Predicate<Upload> predicate): End.");
        }

        private void resetUploads()
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
