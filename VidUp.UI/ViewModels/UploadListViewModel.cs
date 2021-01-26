using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.Converters;
using Drexel.VidUp.UI.Definitions;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.Events;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class UploadListViewModel : INotifyPropertyChanged
    {
        private UploadList uploadList;

        //needed for template and playlist combobox in upload control
        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

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

        private UploadStatus uploadStatus = UploadStatus.NotUploading;
        private Uploader uploader;
        private bool resumeUploads = true;
        private long maxUploadInBytesPerSecond = 0;

        private TemplateComboboxViewModel deleteSelectedTemplate;
        private string deleteSelectedUploadStatus = "Finished";

        private string resetToSelectedUploadStatus = "ReadyForUpload";
        private string resetWithSelectedUploadStatus = "Paused";
        private TemplateComboboxViewModel resetWithSelectedTemplate;

        private TemplateComboboxViewModel recalculatePublishAtSelectedTemplate;
        private DateTime? recalculatePublishAtStartDate;
        

        public event EventHandler<UploadStartedEventArgs> UploadStarted;
        public event EventHandler<UploadFinishedEventArgs> UploadFinished;
        public event EventHandler UploadStatsUpdated;
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

        public UploadListViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels, ObservableTemplateViewModels observableTemplateViewModelsInclAll,
            ObservableTemplateViewModels observableTemplateViewModelsInclAllNone, ObservablePlaylistViewModels observablePlaylistViewModels)
        {
            this.uploadList = uploadList;
            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.observableTemplateViewModelsInclAllNone = observableTemplateViewModelsInclAllNone;
            this.observableTemplateViewModelsInclAll = observableTemplateViewModelsInclAll;
            this.observableTemplateViewModelsInclAllNone.CollectionChanged+= this.observableTemplateViewModelsInclAllNoneCollectionChanged;
            this.observableTemplateViewModelsInclAll.CollectionChanged += this.observableTemplateViewModelsInclAllCollectionChanged;
            this.recalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAll[0];
            this.deleteSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
            this.resetWithSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];

            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, this.observableTemplateViewModels, this.observablePlaylistViewModels, this.resumeUploads);

            this.deleteCommand = new GenericCommand(this.DeleteUpload);
            this.addUploadCommand = new GenericCommand(this.openUploadDialog);
            this.startUploadingCommand = new GenericCommand(this.startUploading);
            this.stopUploadingCommand = new GenericCommand(this.stopUploading);
            this.recalculatePublishAtCommand = new GenericCommand(this.recalculatePublishAt);
            this.resetRecalculatePublishAtStartDateCommand = new GenericCommand(this.resetRecalculatePublishAtStartDate);
            this.deleteUploadsCommand = new GenericCommand(this.deleteUploads);
            this.resetUploadsCommand = new GenericCommand(this.resetUploads);
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

        private void onUploadStatsUpdated()
        {
            EventHandler handler = this.UploadStatsUpdated;

            if (handler != null)
            {
                handler(this, null);
            }
        }

        public void ReOrder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            this.uploadList.ReOrder(uploadToMove, uploadAtTargetPosition);
            this.observableUploadViewModels.ReOrder(this.uploadList);
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
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
            this.uploadList.AddUploads(uploads);

            if (uploads.Any(upl => upl.Template != null))
            {
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
        }

        private async void startUploading(object obj)
        {
            if (this.uploadStatus == UploadStatus.NotUploading)
            {
                //prevent sleep mode
                PowerSavingHelper.DisablePowerSaving();

                this.uploadStatus = UploadStatus.Uploading;
                UploadStats uploadStats = new UploadStats();
                this.onUploadStarted(new UploadStartedEventArgs(uploadStats));

                this.uploader = new Uploader(this.uploadList);
                this.uploader.UploadStatsUpdated += (sender, args) => this.onUploadStatsUpdated();
                UploaderResult uploadResult = await uploader.Upload(uploadStats, this.resumeUploads, this.maxUploadInBytesPerSecond);
                bool uploadStopped = uploader.UploadStopped;
                this.uploader = null;

                this.uploadStatus = UploadStatus.NotUploading;

                if (uploadResult != UploaderResult.NothingDone)
                {
                    this.onUploadStatsUpdated();
                }

                PowerSavingHelper.EnablePowerSaving();

                //needs to be after PowerSavingHelper.EnablePowerSaving(); so that StandBy is not prevented.
                this.onUploadFinished(new UploadFinishedEventArgs(uploadResult == UploaderResult.OneUploadFinished, uploadStopped));
            }
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
        private async void deleteUploads(object parameter)
        {
            bool skipDialog = (bool)parameter;
            bool remove = true;

            //skip dialog on testing
            if (!(bool)skipDialog)
            {
                if (this.deleteSelectedUploadStatus == "All" ||
                    (UplStatus) Enum.Parse(typeof(UplStatus), this.deleteSelectedUploadStatus) != UplStatus.Finished)
                {
                    ConfirmControl control = new ConfirmControl(
                        $"Do you really want to remove all uploads with template = '{this.deleteSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.deleteSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'?");

                    remove = (bool) await DialogHost.Show(control, "RootDialog");
                }
            }

            if (remove)
            {
                this.deleteUploads();
            }
        }

        //parameter skips dialog for testing
        private async void resetUploads(object parameter)
        {
            bool skipDialog = (bool)parameter;
            bool reset = true;

            //skip dialog on testing
            if (!(bool)skipDialog)
            {
                ConfirmControl control = new ConfirmControl(
                    $"Do you really want to reset all uploads with template = '{this.resetWithSelectedTemplate.Template.Name}' and status = '{new UplStatusStringValuesConverter().Convert(this.resetWithSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}' to status '{new UplStatusStringValuesConverter().Convert(this.resetToSelectedUploadStatus, typeof(string), null, CultureInfo.CurrentCulture)}'? Ready for Upload will restart begun uploads.");

                reset = (bool)await DialogHost.Show(control, "RootDialog");
            }

            if (reset)
            {
                this.resetUploads();
            }
        }

        private void recalculatePublishAt(object obj)
        {
            if (this.recalculatePublishAtSelectedTemplate.Name == "All")
            {
                if (this.recalculatePublishAtStartDate != null)
                {
                    this.uploadList.SetStartDateOnAllTemplateSchedules(this.recalculatePublishAtStartDate.Value);
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                    {
                        upload.PublishAt = null;
                    }
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template.UsePublishAtSchedule && upload.UploadStatus == UplStatus.ReadyForUpload)
                    {
                        upload.AutoSetPublishAtDateTime();
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
                    }
                }
            }
        }

        private void resetRecalculatePublishAtStartDate(object obj)
        {
            this.RecalculatePublishAtStartDate = null;
        }

        //exposed for testing
        public void DeleteUpload(object parameter)
        {
            Guid uploadGuid = Guid.Parse((string)parameter);
            this.deleteUploads(upload => upload.Guid == uploadGuid);
        }

        private void deleteUploads()
        {
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];

            if (this.deleteSelectedUploadStatus == "All")
            {
                predicates[0] = upload => true;
            }
            else
            {
                UplStatus status = (UplStatus)Enum.Parse(typeof(UplStatus), this.deleteSelectedUploadStatus);
                predicates[0] = upload => upload.UploadStatus == status;
            }

            if (this.deleteSelectedTemplate.Template.Name == "All")
            {
                predicates[1] = upload => true;
            }
            else if (this.deleteSelectedTemplate.Template.Name == "None")
            {
                predicates[1] = upload => upload.Template == null;
            }
            else
            {
                predicates[1] = upload => upload.Template == this.deleteSelectedTemplate.Template;
            }

            Predicate<Upload> combinedPredicate = PredicateCombiner.And(predicates);
            this.deleteUploads(combinedPredicate);
            
        }

        private void deleteUploads(Predicate<Upload> predicate)
        {
            bool serializeTemplates = false;
            List<Upload> uploadsToDelete = this.uploadList.GetUploads(predicate);
            if (uploadsToDelete.Any(upl => upl.Template != null))
            {
                serializeTemplates = true;
            }

            this.uploadList.RemoveUploads(predicate);

            if (serializeTemplates)
            {
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeUploadList();
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

            if (this.resetWithSelectedTemplate.Template.Name == "All")
            {
                predicates[1] = upload => true;
            }
            else if (this.resetWithSelectedTemplate.Template.Name == "None")
            {
                predicates[1] = upload => upload.Template == null;
            }
            else
            {
                predicates[1] = upload => upload.Template == this.resetWithSelectedTemplate.Template;
            }

            List<Upload> uploads = this.uploadList.Uploads.Where(upload => PredicateCombiner.And(predicates)(upload)).ToList();

            UplStatus resetToStatus = (UplStatus) Enum.Parse(typeof(UplStatus), this.resetToSelectedUploadStatus);
            foreach (Upload upload in uploads)
            {
                if (upload.VerifyForUpload())
                {
                    if (resetToStatus == UplStatus.Stopped)
                    {
                        //uplaod cannot be stopped if it hasn't been started
                        if (string.IsNullOrWhiteSpace(upload.ResumableSessionUri))
                        {
                            continue;
                        }
                    }

                    upload.UploadStatus = resetToStatus;
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
