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

        public long GetRemainingBytesOfFilesToUpload(List<Upload> uploadsToIgnore)
        {
            long length = 0;
            List<Upload> uploadsInternal = this.getUploadsInternal(uploadsToIgnore);

            foreach (Upload upload in uploadsInternal.FindAll(
                upload => (upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading) 
                          //upload currently uploading shall always be in result
                          && (upload.YoutubeAccount.IsAuthenticated || upload.UploadStatus == UplStatus.Uploading)))
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

        public long GetRemainingBytesOfFilesToUploadIncludingResumable(List<Upload> uploadsToIgnore)
        {
            long length = 0;
            List<Upload> uploadsInternal = this.getUploadsInternal(uploadsToIgnore);

            foreach (Upload upload in uploadsInternal.FindAll(
                upload => (upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading ||
                          upload.UploadStatus == UplStatus.Stopped || upload.UploadStatus == UplStatus.Failed)
                          //upload currently uploading shall always be in result
                          && (upload.YoutubeAccount.IsAuthenticated || upload.UploadStatus == UplStatus.Uploading)))
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

        public long GetTotalBytesOfFilesToUpload(List<Upload> uploadsToIgnore)
        {
            long length = 0;
            List<Upload> uploadsInternal = this.getUploadsInternal(uploadsToIgnore);

            foreach (Upload upload in uploadsInternal.FindAll(
                upload => (upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading)
                          //upload currently uploading shall always be in result
                          && (upload.YoutubeAccount.IsAuthenticated || upload.UploadStatus == UplStatus.Uploading)))
            {
                length += upload.FileLength;
            }

            return length;
        }

        public long GetTotalBytesOfFilesToUploadIncludingResumable(List<Upload> uploadsToIgnore)
        {
            long length = 0;
            List<Upload> uploadsInternal = this.getUploadsInternal(uploadsToIgnore);

            foreach (Upload upload in uploadsInternal.FindAll(
                upload => (upload.UploadStatus == UplStatus.ReadyForUpload || upload.UploadStatus == UplStatus.Uploading ||
                          upload.UploadStatus == UplStatus.Stopped || upload.UploadStatus == UplStatus.Failed)
                          //upload currently uploading shall always be in result
                          && (upload.YoutubeAccount.IsAuthenticated || upload.UploadStatus == UplStatus.Uploading)))
            {
                length += upload.FileLength;
            }

            return length;
        }

        private List<Upload> getUploadsInternal(List<Upload> uploadsToIgnore)
        {
            List<Upload> uploadsInternal;
            if (uploadsToIgnore != null && uploadsToIgnore.Count > 0)
            {
                uploadsInternal = this.uploads.FindAll(upload => !uploadsToIgnore.Contains(upload)).ToList();
            }
            else
            {
                uploadsInternal = new List<Upload>(this.uploads);
            }

            return uploadsInternal;
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

        public bool AddFiles(string[] files, YoutubeAccount fallbackYoutubeAccount)
        {
            Tracer.Write($"UploadList.AddFiles: Start, add {files.Length} files to uploads.");
            bool templateAutoAdded = false;

            List<Upload> newUploads = new List<Upload>();
            foreach (string file in files)
            {
                Tracer.Write($"UploadList.AddFiles: Add '{file}'.");
                Template template = this.templateList.GetTemplateForFilePath(file);
                Upload upload = null;
                if (template != null)
                {
                    Tracer.Write($"UploadList.AddFiles: Template '{template.Name}' found for file.");
                    upload = new Upload(file, template);
                    newUploads.Add(upload);
                    templateAutoAdded = true;
                }
                else
                {
                    Tracer.Write($"UploadList.AddFiles: No template found for upload, try to get default template.");
                    template = this.templateList.GetDefaultTemplate();
                    if (template != null)
                    {
                        Tracer.Write($"UploadList.AddFiles: Default template '{template.Name}' found.");
                        upload = new Upload(file, template);
                        newUploads.Add(upload);
                        templateAutoAdded = true;
                    }
                    else
                    {
                        Tracer.Write($"UploadList.AddAddFilesUploads: No default template found.");
                        upload = new Upload(file, fallbackYoutubeAccount);
                        newUploads.Add(upload);
                    }
                }

                upload.ThumbnailChanged += this.onThumbnailChanged;
            }

            this.uploads.AddRange(newUploads);

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadRemaining");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumable");
            this.raiseNotifyPropertyChanged("TotalBytesToUploadIncludingResumableRemaining");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newUploads));
            Tracer.Write($"UploadList.AddFiles: End.");

            return templateAutoAdded;
        }

        public void DeleteUploads(Predicate<Upload> predicate, bool keepLastPerTemplate)
        {
            Tracer.Write($"UploadList.DeleteUploads: Start, keepLastPerTemplate {keepLastPerTemplate}.");
            if (keepLastPerTemplate)
            {
                Dictionary<Template, Upload> templateLastUploadMap = new Dictionary<Template, Upload>();
                foreach (Upload upload in this.uploads)
                {
                    templateLastUploadMap[upload.Template] = upload;
                }

                predicate = TinyHelpers.PredicateAnd(new Predicate<Upload>[] { predicate, upl => !templateLastUploadMap.ContainsValue(upl) });
            }

            List<Upload> oldUploads = this.uploads.FindAll(predicate);
            Tracer.Write($"UploadList.DeleteUploads: Delete {oldUploads.Count} uploads from upload list.");

            this.uploads.RemoveAll(predicate);

            foreach (Upload upload in oldUploads)
            {
                Tracer.Write($"UploadList.DeleteUploads: Delete upload '{upload.FilePath}'.");
                if (upload.UploadStatus != UplStatus.Finished && upload.Template != null)
                {
                    //removes upload from template as it was not uploaded
                    upload.Template = null;
                }

                if (!keepLastPerTemplate)
                {
                    this.DeleteThumbnailFallbackIfPossible(upload.ThumbnailFilePath);
                }
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

        public void Reorder(Upload uploadToMove, Upload uploadAtTargetPosition)
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

        public void SetStartDateOnAllTemplateSchedules(DateTime startDate, YoutubeAccount youtubeAccount)
        {
            foreach (Template template in this.templateList)
            {
                if (youtubeAccount.IsDummy)
                {
                    if (youtubeAccount.Name == "All")
                    {
                        template.SetStartDateOnTemplateSchedule(startDate);
                    }
                }
                else
                {
                    if (template.YoutubeAccount == youtubeAccount)
                    {
                        template.SetStartDateOnTemplateSchedule(startDate);
                    }
                }
                
            }
        }

        public List<Upload> FindAll(Predicate<Upload> match)
        {
            return this.uploads.FindAll(match);
        }
    }
}
