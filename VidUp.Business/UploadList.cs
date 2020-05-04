using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.IO;

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

        public long TotalBytesToUpload
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

        public CheckFileUsage CheckFileUsage
        {
            set
            {
                this.checkFileUsage = value;
            }
        }

        public string ThumbnailFallbackImageFolder
        {
            set
            {
                this.thumbnailFallbackImageFolder = value;
            }
        }

        public UploadList()
        {
            this.uploads = new List<Upload>();
        }

        public void Remove(Upload upload)
        {
            this.uploads.Remove(upload);
            upload.PropertyChanged -= this.uploadPropertyChanged;

            this.deleteThumbnailIfPossible(upload.ThumbnailFilePath);

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, upload));
        }

        public void AddUploads(List<Upload> uploads, TemplateList templateList)
        {
            foreach(Upload upload in uploads)
            {
                Template template = templateList.GetTemplateForUpload(upload);
                if (template != null)
                {
                    upload.Template = template;
                }
                else
                {
                    template = templateList.GetDefaultTemplate();
                    if (template != null)
                    {
                        upload.Template = template;
                    }
                }

                upload.PropertyChanged += uploadPropertyChanged;
            }

            this.uploads.AddRange(uploads);

            this.raiseNotifyPropertyChanged("TotalBytesToUpload");
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
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldUploads));
        }

        private void uploadPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "UploadStatus")
            {
                this.raiseNotifyPropertyChanged("TotalBytesToUpload");
            }

            if (e.PropertyName == "ThumbnailFilePath")
            {
                PropertyChangedEventArgsEx args = (PropertyChangedEventArgsEx)e;

                string oldValue = (string)args.OldValue;

                this.deleteThumbnailIfPossible(oldValue);
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

        public ReadOnlyCollection<Upload> GetReadyOnlyUploadList()
        {
            return this.uploads.AsReadOnly();
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
    }
}
