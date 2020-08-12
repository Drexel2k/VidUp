#region

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.Converters;
using Drexel.VidUp.UI.Definitions;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.Events;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube;
using MaterialDesignThemes.Wpf;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    public class UploadListViewModel : INotifyPropertyChanged
    {
        private UploadList uploadList;

        //needed for template and playlist combobox in upload control
        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        private ObservableTemplateViewModels observableTemplateViewModelsInclAllNone;

        private GenericCommand deleteCommand;
        private ObservableUploadViewModels observableUploadViewModels;

        private GenericCommand addUploadCommand;
        private GenericCommand startUploadingCommand;
        private GenericCommand stopUploadingCommand;
        private GenericCommand recalculatePublishAtCommand;
        private GenericCommand removeUploadsCommand;

        private UploadStatus uploadStatus = UploadStatus.NotUploading;
        private Uploader uploader;
        private bool resumeUploads = true;
        private long maxUploadInBytesPerSecond = 0;

        private TemplateComboboxViewModel removeSelectedTemplate;
        private string removeUploadStatus = "Finished";

        private TemplateComboboxViewModel recalculatePublishAtSelectedTemplate;


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

        public GenericCommand RemoveUploadsCommand
        {
            get
            {
                return this.removeUploadsCommand;
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

        //Selected template for upload removal
        public TemplateComboboxViewModel RemoveSelectedTemplate
        {
            get
            {
                return this.removeSelectedTemplate;
            }
            set
            {
                if (this.removeSelectedTemplate != value)
                {
                    this.removeSelectedTemplate = value;
                    this.raisePropertyChanged("RemoveSelectedTemplate");
                }
            }
        }

        public string[] RemoveUploadStatuses
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

        public string RemoveUploadStatus
        {
            get
            {
                return this.removeUploadStatus;
            }
            set
            {
                if (this.removeUploadStatus != value)
                {
                    this.removeUploadStatus = value;
                    this.raisePropertyChanged("RemoveUploadStatus");
                }
            }
        }

        public UploadListViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels,
            ObservableTemplateViewModels observableTemplateViewModelsInclAllNone, ObservablePlaylistViewModels observablePlaylistViewModels)
        {
            this.uploadList = uploadList;

            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.observableTemplateViewModelsInclAllNone = observableTemplateViewModelsInclAllNone;
            this.observableTemplateViewModelsInclAllNone.CollectionChanged+= this.observableTemplateViewModelsInclAllNoneCollectionChanged;
            this.recalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAllNone[1];
            this.removeSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];

            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, this.observableTemplateViewModels, this.observablePlaylistViewModels);

            this.deleteCommand = new GenericCommand(this.RemoveUpload);
            this.addUploadCommand = new GenericCommand(this.openUploadDialog);
            this.startUploadingCommand = new GenericCommand(this.startUploading);
            this.stopUploadingCommand = new GenericCommand(this.stopUploading);
            this.recalculatePublishAtCommand = new GenericCommand(this.recalculatePublishAt);
            this.removeUploadsCommand = new GenericCommand(this.removeUploads);
        }

        //need to change the remove uploads template filter combobox selected item, if this template is deleted. So the selected
        //item isn't set to null by the GUI which prevents updating it to an existing item in the same call.
        private void observableTemplateViewModelsInclAllNoneCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                if ((TemplateComboboxViewModel) e.OldItems[0] == this.removeSelectedTemplate)
                {
                    this.RemoveSelectedTemplate = this.observableTemplateViewModelsInclAllNone[0];
                }

                if ((TemplateComboboxViewModel)e.OldItems[0] == this.recalculatePublishAtSelectedTemplate)
                {
                    this.RecalculatePublishAtSelectedTemplate = this.observableTemplateViewModelsInclAllNone[1];
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

        //exposed for testing
        public void RemoveUpload(object parameter)
        {
            Upload upload = this.observableUploadViewModels.GetUploadByGuid(Guid.Parse((string)parameter)).Upload;
            this.uploadList.RemoveUploads(upload2 => upload2 == upload);

            JsonSerialization.JsonSerializer.SerializeAllUploads();
            JsonSerialization.JsonSerializer.SerializeUploadList();
            JsonSerialization.JsonSerializer.SerializeTemplateList();
        }

        public void ReOrder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            this.uploadList.ReOrder(uploadToMove, uploadAtTargetPosition);
            this.observableUploadViewModels.ReOrder(this.uploadList);
            JsonSerialization.JsonSerializer.SerializeUploadList();
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

            JsonSerialization.JsonSerializer.SerializeAllUploads();
            JsonSerialization.JsonSerializer.SerializeUploadList();
            JsonSerialization.JsonSerializer.SerializeTemplateList();
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

                bool oneUploadFinished = false;
                this.uploader = new Uploader(this.uploadList);
                this.uploader.UploadStatsUpdated += (sender, args) => this.onUploadStatsUpdated();
                oneUploadFinished = await uploader.Upload(uploadStats, this.resumeUploads, this.maxUploadInBytesPerSecond);
                this.uploader = null;

                this.uploadStatus = UploadStatus.NotUploading;
                this.onUploadFinished(new UploadFinishedEventArgs(oneUploadFinished));

                this.onUploadStatsUpdated();

                PowerSavingHelper.EnablePowerSaving();
            }
        }

        private void stopUploading(object obj)
        {
            if (this.uploadStatus == UploadStatus.Uploading)
            {
                Uploader uploader = this.uploader;
                if (uploader != null)
                {
                    uploader.StopUpload = true;
                }
            }
        }

        private async void removeUploads(object obj)
        {
            bool remove = true;
            if (this.removeUploadStatus == "All" || (UplStatus)Enum.Parse(typeof(UplStatus), this.removeUploadStatus) != UplStatus.Finished)
            {
                ConfirmControl control = new ConfirmControl(string.Format(
                    "Do you really want to remove all uploads with template = {0} and status = {1}?",
                    this.removeSelectedTemplate.Template.Name,
                    new UplStatusStringValuesConverter().Convert(this.removeUploadStatus, typeof(string), null,
                        CultureInfo.CurrentCulture)));

                remove = (bool)await DialogHost.Show(control, "RootDialog");
            }

            if (remove)
            {
                this.removeUploads();
            }
        }

        private void recalculatePublishAt(object obj)
        {
            if (this.recalculatePublishAtSelectedTemplate.Name == "None")
            {
                return;
            }

            if (this.recalculatePublishAtSelectedTemplate.Name == "All")
            {
                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template.UsePublishAtSchedule)
                    {
                        if (upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            upload.PublishAt = null;
                        }
                    }
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null && upload.Template.UsePublishAtSchedule)
                    {
                        if (upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            upload.AutoSetPublishAtDateTime();
                        }
                    }
                }
            }
            else
            {
                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null &&
                        upload.Template == this.recalculatePublishAtSelectedTemplate.Template &&  upload.Template.UsePublishAtSchedule)
                    {
                        if (upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            upload.PublishAt = null;
                        }
                    }
                }

                foreach (Upload upload in this.uploadList)
                {
                    if (upload.Template != null &&
                        upload.Template == this.recalculatePublishAtSelectedTemplate.Template && upload.Template.UsePublishAtSchedule)
                    {
                        if (upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            upload.AutoSetPublishAtDateTime();
                        }
                    }
                }
            }

            JsonSerialization.JsonSerializer.SerializeAllUploads();
        }

        private void removeUploads()
        {
            Predicate<Upload>[] predicates = new Predicate<Upload>[2];

            if (this.removeUploadStatus == "All")
            {
                predicates[0] = upload => true;
            }
            else
            {
                UplStatus status = (UplStatus)Enum.Parse(typeof(UplStatus), this.removeUploadStatus);
                predicates[0] = upload => upload.UploadStatus == status;
            }

            if (this.removeSelectedTemplate.Template.Name == "All")
            {
                predicates[1] = upload => true;
            }
            else if (this.removeSelectedTemplate.Template.Name == "None")
            {
                predicates[1] = upload => upload.Template == null;
            }
            else
            {
                predicates[1] = upload => upload.Template == this.removeSelectedTemplate.Template;
            }

            this.uploadList.RemoveUploads(PredicateCombiner.And(predicates));

            JsonSerialization.JsonSerializer.SerializeAllUploads();
            JsonSerialization.JsonSerializer.SerializeUploadList();
            JsonSerialization.JsonSerializer.SerializeTemplateList();
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
