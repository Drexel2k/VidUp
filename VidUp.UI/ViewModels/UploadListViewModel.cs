#region

using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Globalization;
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

        private GenericCommand deleteCommand;
        private ObservableUploadViewModels observableUploadViewModels;

        private GenericCommand addUploadCommand;
        private GenericCommand startUploadingCommand;
        private GenericCommand stopUploadingCommand;
        private GenericCommand removeUploadsCommand;

        private UploadStatus uploadStatus = UploadStatus.NotUploading;
        private Uploader uploader;
        private bool resumeUploads = true;
        private long maxUploadInBytesPerSecond = 0;

        //todo: convert to observable collection to remove dependency on observableTemplateViewModels
        private List<TemplateComboboxViewModel> removeTemplateViewModels;
        private TemplateComboboxViewModel removeSelectedTemplate;
        private string removeUploadStatus = "Finished";

        public event EventHandler<UploadStartedEventArgs> UploadStarted;
        public event EventHandler<UploadFinishedEventArgs> UploadFinished;
        public event EventHandler UploadStatsUpdated;

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

        public List<TemplateComboboxViewModel> RemoveTemplateViewModels
        {
            get
            {
                return this.removeTemplateViewModels;
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

        public UploadListViewModel(UploadList uploadList, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels)
        {
            this.uploadList = uploadList;

            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.observableUploadViewModels = new ObservableUploadViewModels(this.uploadList, this.observableTemplateViewModels, this.observablePlaylistViewModels);
            this.observableTemplateViewModels.CollectionChanged += observableTemplateViewModelsCollectionChanged;
            this.removeRefreshTemplateFilter();

            this.deleteCommand = new GenericCommand(this.RemoveUpload);
            this.addUploadCommand = new GenericCommand(this.openUploadDialog);
            this.startUploadingCommand = new GenericCommand(this.startUploading);
            this.stopUploadingCommand = new GenericCommand(this.stopUploading);
            this.removeUploadsCommand = new GenericCommand(this.removeUploads);
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
                this.RemoveUploads();
            }
        }

        //exposed for testing
        public void RemoveUploads()
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

        //reinitializes filters for upload removals
        private void removeRefreshTemplateFilter()
        {
            Template allTemplate = new Template();
            allTemplate.Name = "All";
            TemplateComboboxViewModel allViewModel = new TemplateComboboxViewModel(allTemplate);

            List<TemplateComboboxViewModel> viewModels = new List<TemplateComboboxViewModel>();
            viewModels.Add(allViewModel);

            Template noTemplate = new Template();
            noTemplate.Name = "None";
            TemplateComboboxViewModel noViewModel = new TemplateComboboxViewModel(noTemplate);

            viewModels.Add(noViewModel);

            bool selectedFilterStillExits = false;
            foreach (TemplateComboboxViewModel templateComboboxViewModel in this.observableTemplateViewModels)
            {
                if (this.removeSelectedTemplate == templateComboboxViewModel)
                {
                    selectedFilterStillExits = true;
                }

                viewModels.Add(templateComboboxViewModel);
            }

            if (!selectedFilterStillExits)
            {
                this.removeSelectedTemplate = allViewModel;
            }

            this.removeTemplateViewModels = viewModels;

            this.raisePropertyChanged("RemoveSelectedTemplate");
            this.raisePropertyChanged("RemoveTemplateViewModels");
        }

        //Template filter for upload removal is based on observableTemplateViewModels
        //so the template list for this filter must be updated after observableTemplateViewModels
        //is updated. templateListCollectionChanged may be triggered before observableTemplateViewModels
        //is updated.
        private void observableTemplateViewModelsCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            this.RemoveSelectedTemplate = null;
            this.removeRefreshTemplateFilter();
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
    }
}
