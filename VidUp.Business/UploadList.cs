using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;
using System.Linq;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    public delegate bool CheckFileUsage(string filePath);

    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UploadList : INotifyCollectionChanged, IEnumerable<Upload>
    {
        [JsonProperty]
        private List<Upload> uploads;

        public event NotifyCollectionChangedEventHandler CollectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;
        private CheckFileUsage checkFileUsage;
        private string thumbnailFallbackImageFolder;
        private TemplateList templateList;
        private PlaylistList playlistList;

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

        public UploadList(List<Upload> uploads, TemplateList templateList, PlaylistList playlistList, string thumbnailFallbackImageFolder)
        {
            this.templateList = templateList;
            this.templateList.CollectionChanged += this.templateListCollectionChanged;

            this.playlistList = playlistList;
            this.playlistList.CollectionChanged += this.playlistListCollectionChanged;

            this.uploads = new List<Upload>();
            if (uploads != null)
            {
                this.uploads = uploads;

                foreach (Upload upload in uploads)
                {
                    upload.ThumbnailChanged += this.onThumbnailChanged;
                }
            }

            this.thumbnailFallbackImageFolder = thumbnailFallbackImageFolder;
        }

        private void templateListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (object template in e.OldItems)
                {
                    this.removeTemplate((Template)template);
                }
            }
        }

        private void playlistListCollectionChanged(object sender, NotifyCollectionChangedEventArgs e)
        {
            if (e.Action == NotifyCollectionChangedAction.Remove)
            {
                foreach (object playlist in e.OldItems)
                {
                    this.removePlaylist((Playlist)playlist);
                }
            }
        }

        private void onThumbnailChanged(object sender, OldValueArgs args)
        {
            this.DeleteThumbnailFallbackIfPossible(args.OldValue);
        }

        public void AddUploads(List<Upload> uploads)
        {
            Tracer.Write($"UploadList.AddUploads: Start, add {uploads.Count} uploads.");
            foreach (Upload upload in uploads)
            {
                Tracer.Write($"UploadList.AddUploads: Add '{upload.FilePath}'.");
                Template template = this.templateList.GetTemplateForUpload(upload);
                if (template != null)
                {
                    Tracer.Write($"UploadList.AddUploads: Template '{template.Name}' found for upload.");
                    upload.Template = template;
                }
                else
                {
                    Tracer.Write($"UploadList.AddUploads: No template found for upload, try to get default template.");
                    template = this.templateList.GetDefaultTemplate();
                    if (template != null)
                    {
                        Tracer.Write($"UploadList.AddUploads: Default template '{template.Name}' found.");
                        upload.Template = template;
                    }
                    else
                    {
                        Tracer.Write($"UploadList.AddUploads: No default template found.");
                    }
                }

                upload.ThumbnailChanged += (sender, args) => this.onThumbnailChanged(sender, args);
            }

            this.uploads.AddRange(uploads);

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, uploads));
            Tracer.Write($"UploadList.AddUploads: End.");
        }

        public void DeleteUploads(Predicate<Upload> predicate)
        {
            Tracer.Write($"UploadList.DeleteUploads: Start.");
            List<Upload> oldUploads = this.uploads.FindAll(predicate);

            Tracer.Write($"UploadList.DeleteUploads: Delete {oldUploads.Count} uploads from upload list.");
            this.uploads.RemoveAll(predicate);

            foreach (Upload upload in oldUploads)
            {
                Tracer.Write($"UploadList.DeleteUploads: Delete upload '{upload.FilePath}'.");
                if (upload.UploadStatus != UplStatus.Finished && upload.Template != null)
                {
                    upload.Template = null;
                }

                this.DeleteThumbnailFallbackIfPossible(upload.ThumbnailFilePath);
            }

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldUploads));
            Tracer.Write($"UploadList.DeleteUploads: Ende.");
        }

        //deletes fallback thumbnail if not in use anymore.
        public void DeleteThumbnailFallbackIfPossible(string thumbnailFilePath)
        {
            string thumbnailFileFolder = Path.GetDirectoryName(thumbnailFilePath);

            if (!string.IsNullOrWhiteSpace(thumbnailFileFolder))
            {
                //check if folder of thumbnail is fallbackfolder
                if (String.Compare(Path.GetFullPath(this.thumbnailFallbackImageFolder).TrimEnd('\\'), thumbnailFileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) != 0)
                {
                    return;
                }


                bool found = false;
                //check if thumbnail is in use by another upload
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

                //check if template with thumbnail exist
                if (!found)
                {
                    if (this.checkFileUsage != null)
                    {
                        found = this.checkFileUsage(thumbnailFilePath);
                    }
                }

                //delete if no usage found
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

        private void removeTemplate(Template template)
        {
            foreach (Upload upload in this.uploads)
            {
                if (upload.Template == template)
                {
                    upload.Template = null;
                }
            }
        }

        private void removePlaylist(Playlist playlist)
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

        public List<Upload> GetUploads(Predicate<Upload> match)
        {
            return this.uploads.Where(upload => match(upload)).ToList();
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

        public void SetStartDateOnAllTemplateSchedules(DateTime startDate)
        {
            foreach (Template template in this.templateList)
            {
                template.SetStartDateOnTemplateSchedule(startDate);
            }
        }

        public List<Upload> FindAll(Predicate<Upload> match)
        {
            return this.uploads.FindAll(match);
        }
    }
}
