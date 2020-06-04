#region

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Service;

#endregion

namespace Drexel.VidUp.Youtube
{
    public class Uploader : IDisposable
    {
        private UploadList uploadList;
        //will be updated during upload when files are added or removed
        private long sessionTotalBytesToUploadFullFilesize = 0;
        private long uploadedLength = 0;

        private UploadStats uploadStats;

        private Action notifyUploadProgress;
        private bool stopUpload = false;
        private bool resumeUploads;

        public long MaxUploadInBytesPerSecond
        {
            set
            {
                YoutubeUpload.MaxUploadInBytesPerSecond = value;
            }
        }

        public bool StopUpload
        {
            get => this.stopUpload;
            set => this.stopUpload = value;
        }

        public Uploader(UploadList uploadList)
        {
            if (uploadList == null)
            {
                throw new ArgumentException("uploadList must not be null.");
            }

            this.uploadList = uploadList;
            this.uploadList.PropertyChanged += uploadListPropertyChanged;
        }

        private void uploadListPropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "TotalBytesToUpload")
            {
                this.uploadStats.CurrentTotalBytesToUpload = this.resumeUploads ? this.uploadList.TotalBytesToUploadIncludingResumableRemaining : this.uploadList.TotalBytesToUploadRemaining;
            }
        }

        public async Task<bool> Upload(Action<Upload> notifyUploadStart, Action<Upload> notifyUploadEnd, Action notifyUploadProgress, UploadStats uploadStats, bool resumeUploads, long maxUploadInBytesPerSecond)
        {
            this.uploadStats = uploadStats;
            this.notifyUploadProgress = notifyUploadProgress;
            this.resumeUploads = resumeUploads;
            bool oneUploadFinished = false;

            this.sessionTotalBytesToUploadFullFilesize = this.resumeUploads ? this.uploadList.TotalBytesToUploadIncludingResumable : this.uploadList.TotalBytesToUpload;
            uploadStats.CurrentTotalBytesToUpload = this.resumeUploads ? this.uploadList.TotalBytesToUploadIncludingResumableRemaining : this.uploadList.TotalBytesToUploadRemaining;
            this.uploadedLength = 0;

            List<Predicate<Upload>> predicates = new List<Predicate<Upload>>(2);
            predicates.Add(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload && File.Exists(upload2.FilePath));
            if (this.resumeUploads)
            {
                predicates.Add(upload2 => (upload2.UploadStatus == UplStatus.Failed || upload2.UploadStatus == UplStatus.Stopped) && File.Exists(upload2.FilePath));
            }

            List<Upload> uploadsOfSession = new List<Upload>();
            Upload upload = this.uploadList.GetUpload(PredicateCombiner.Or(predicates.ToArray()));

            while (upload != null && !this.stopUpload)
            {
                uploadsOfSession.Add(upload);
                upload.UploadErrorMessage = null;
                this.uploadStats.InitializeNewUpload(upload);

                upload.UploadStatus = UplStatus.Uploading;

                if (notifyUploadStart != null)
                {
                    notifyUploadStart(upload);
                }

                UploadResult result = await YoutubeUpload.Upload(upload, maxUploadInBytesPerSecond,
                    updateUploadProgress, this.isStopped);

                if (!string.IsNullOrWhiteSpace(result.VideoId))
                {
                    oneUploadFinished = true;
                    upload.UploadStatus = UplStatus.Finished;
                }
                else
                {
                    if (upload.UploadStatus != UplStatus.Stopped)
                    {
                        upload.UploadStatus = UplStatus.Failed;
                    }
                }

                this.uploadedLength += upload.FileLength;
                this.uploadStats.FinishUpload(upload.FileLength);

                if (notifyUploadEnd != null)
                {
                    notifyUploadEnd(upload);
                }

                JsonSerialization.SerializeAllUploads();

                upload = this.uploadList.GetUpload(
                    PredicateCombiner.And(
                        new Predicate<Upload>[]
                        {
                            PredicateCombiner.Or(predicates.ToArray()),
                            upload2 => !uploadsOfSession.Contains(upload2)
                        }));
            }

            if(oneUploadFinished)
            {
                this.uploadStats.AllFinished();
                if(this.notifyUploadProgress != null)
                {
                    this.notifyUploadProgress();
                }
            }

            this.uploadStats = null;
            return oneUploadFinished;
        }

        void updateUploadProgress(YoutubeUploadStats stats)
        {
            //upload has been added or removed
            long totalBytesToUpload = this.resumeUploads ? this.uploadList.TotalBytesToUploadIncludingResumable : this.uploadList.TotalBytesToUpload;
            long delta = this.sessionTotalBytesToUploadFullFilesize - (totalBytesToUpload + this.uploadedLength);
            if (delta != 0)
            {
                //delta is negative when files have been added
                this.sessionTotalBytesToUploadFullFilesize -= delta;
            }

            this.uploadStats.UpdateStats(this.sessionTotalBytesToUploadFullFilesize, stats.CurrentSpeedInBytesPerSecond);
            if(this.notifyUploadProgress != null)
            {
                this.notifyUploadProgress();
            }
        }

        private bool isStopped()
        {
            return this.stopUpload;
        }

        public void Dispose()
        {
            this.uploadList.PropertyChanged -= uploadListPropertyChanged;
        }
    }
}
