#region

using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using Newtonsoft.Json;

#endregion

namespace Drexel.VidUp.Business
{
    public delegate bool CheckFileUsage(string filePath);

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UploadList : INotifyCollectionChanged, INotifyPropertyChanged, IEnumerable<Upload>
    {
        [JsonProperty]
        private List<Upload> uploads;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private CheckFileUsage checkFileUsage;
        private string thumbnailFallbackImageFolder;
        private TemplateList templateList;

        public int UploadCount { get => this.uploads.Count; }

        public long TotalBytesOfFilesToUpload
        {
            get
            {
                long length = 0;
                foreach (Upload upload in this.uploads.FindAll(upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading))
                {
                    length += upload.FileLength;
                }

                return length;
            }
        }

        public long RemainingBytesOfFilesToUpload
        {
            get
            {
                long length = 0;
                foreach (Upload upload in this.uploads.FindAll(upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading))
                {
                    if (upload.UploadStatus == UplStatus.ReadyForUpload)
                    {
                        length += upload.FileLength;
                    }
                    else
                    {
                        length += upload.FileLength - upload.BytesSent;
                    }

                }

                return length;
            }
        }

        public long TotalBytesOfFilesToUploadIncludingResumable
        {
            get
            {
                long length = 0;
                foreach (Upload upload in this.uploads.FindAll(
                    upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading ||
                              upload.UploadStatus == UplStatus.Stopped || upload.UploadStatus == UplStatus.Failed))
                {
                    length += upload.FileLength;
                }

                return length;
            }
        }

        public long RemainingBytesOfFilesToUploadIncludingResumable
        {
            get
            {
                long length = 0;
                foreach (Upload upload in this.uploads.FindAll(
                    upload => upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading ||
                    upload.UploadStatus == UplStatus.Stopped ||upload.UploadStatus== UplStatus.Failed))
                {
                    if (File.Exists(upload.FilePath))
                    {
                        if (upload.UploadStatus == UplStatus.ReadyForUpload)
                        {
                            length += upload.FileLength;
                        }
                        else
                        {
                            length += upload.FileLength - upload.BytesSent;
                        }
                    }
                }

                return length;
            }
        }

        public CheckFileUsage CheckFileUsage
        {
            set
            {
                this.checkFileUsage = value;
            }
        }

        public ReadOnlyCollection<Upload> Uploads { get => this.uploads.AsReadOnly(); }

        public UploadList(List<Upload> uploads, TemplateList templateList, string thumbnailFallbackImageFolder)
        {
            this.templateList = templateList;

            this.uploads = new List<Upload>();
            if (uploads != null)
            {
                this.uploads = uploads;
                foreach (Upload upload in this.uploads)
                {
                    upload.PropertyChanged += uploadPropertyChanged;
                }
            }

            this.thumbnailFallbackImageFolder = thumbnailFallbackImageFolder;
        }

        public void AddUploads(List<Upload> uploads)
        {
            foreach(Upload upload in uploads)
            {
                Template template = this.templateList.GetTemplateForUpload(upload);
                if (template != null)
                {
                    upload.Template = template;
                }
                else
                {
                    template = this.templateList.GetDefaultTemplate();
                    if (template != null)
                    {
                        upload.Template = template;
                    }
                }

                upload.PropertyChanged += uploadPropertyChanged;
            }

            this.uploads.AddRange(uploads);

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, uploads));
        }

        //after deserialiatzion
        public void SetEventListeners()
        {
            foreach (Upload upload in uploads)
            {
                upload.PropertyChanged += uploadPropertyChanged;
            }
        }

        public void RemoveUploads(Predicate<Upload> predicate)
        {
            List<Upload> oldUploads = this.uploads.FindAll(predicate);
            this.uploads.RemoveAll(predicate);

            foreach (Upload upload in oldUploads)
            {
                if (upload.UploadStatus != UplStatus.Finished && upload.Template != null)
                {
                    upload.Template = null;
                }

                upload.PropertyChanged -= this.uploadPropertyChanged;

                this.deleteThumbnailIfPossible(upload.ThumbnailFilePath);
            }

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldUploads));
        }

        private void uploadPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UploadStatus")
            {
                this.raiseNotifyPropertyChanged("TotalBytesToUpload");
                this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
                this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
                this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            }

            if (e.PropertyName == "ThumbnailFilePath")
            {
                PropertyChangedEventArgsEx args = (PropertyChangedEventArgsEx)e;

                string oldValue = (string)args.OldValue;

                this.deleteThumbnailIfPossible(oldValue);
            }

            if (e.PropertyName == "BytesSent")
            {
                this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
                this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            }
        }

        private void deleteThumbnailIfPossible(string thumbnailFilePath)
        {
            string thumbnailFileFolder = Path.GetDirectoryName(thumbnailFilePath);
            if (!string.IsNullOrWhiteSpace(thumbnailFileFolder))
            {
                if (String.Compare(Path.GetFullPath(this.thumbnailFallbackImageFolder).TrimEnd('\\'), thumbnailFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }

                bool found = false;
                foreach (Upload upload in this.uploads)
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

                if (!found)
                {
                    if (this.checkFileUsage != null)
                    {
                        found = this.checkFileUsage(thumbnailFilePath);
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

        public bool UploadContainsFallbackThumbnail(string filePath)
        {
            if (filePath != null)
            {
                foreach (Upload upload in this.uploads)
                {
                    if (upload.ThumbnailFilePath != null)
                    {
                        if (String.Compare(Path.GetFullPath(filePath), Path.GetFullPath(upload.ThumbnailFilePath), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            return true;
                        }
                    }
                }
            }

            return false;
        }

        public void RemoveTemplate(Template template)
        {
            foreach (Upload upload in this.uploads)
            {
                if (upload.Template == template)
                {
                    upload.Template = null;
                }
            }
        }

        public void RemovePlaylist(Playlist playlist)
        {
            foreach (Upload upload in this.uploads)
            {
                if (upload.Playlist == playlist)
                {
                    upload.Playlist = null;
                }
            }
        }

        public Upload GetUpload(int index)
        {
            return this.uploads[index];
        }

        public Upload GetUpload(Predicate<Upload> match)
        {
            return this.uploads.Find(match);
        }

        public int FindIndex(Predicate<Upload> predicate)
        {
            return this.uploads.FindIndex(predicate);
        }

        public IEnumerator<Upload> GetEnumerator()
        {
            return this.uploads.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        private void raiseNotifyCollectionChanged(NotifyCollectionChangedEventArgs args)
        {
            NotifyCollectionChangedEventHandler handler = this.CollectionChanged;
            if (handler != null)
            {
                handler(this, args);
            }
        }

        private void raiseNotifyPropertyChanged(string propertyName)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        public void ReOrder(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            if (uploadToMove == uploadAtTargetPosition)
            {
                return;
            }

            int targetIndex = this.uploads.IndexOf(uploadAtTargetPosition);
            int moveIndex = this.uploads.IndexOf(uploadToMove);

            if (targetIndex > moveIndex)
            {
                for (int index = moveIndex; index < targetIndex - 1; index++)
                {
                    this.uploads[index] = this.uploads[index + 1];
                }

                this.uploads[targetIndex - 1] = uploadToMove;
            }
            else
            {
                for (int index = moveIndex; index > targetIndex; index--)
                {
                    this.uploads[index] = this.uploads[index - 1];
                }

                this.uploads[targetIndex] = uploadToMove;
            }
        }

        public void SetStartDateOnTemplateSchedule(Template template, DateTime startDate)
        {
            if (template.PublishAtSchedule != null)
            {
                template.PublishAtSchedule.IgnoreUploadsBefore = DateTime.Now;
                template.PublishAtSchedule.StartDate = startDate;
            }
        }

        public void SetStartDateOnAllTemplateSchedules(DateTime startDate)
        {
            foreach (Template template in this.templateList)
            {
                this.SetStartDateOnTemplateSchedule(template, startDate);
            }
        }
    }
}
