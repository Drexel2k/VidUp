using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Service;


namespace Drexel.VidUp.Youtube
{
    public class Uploader
    {
        private UploadList uploadList;
        //will be updated during upload when files are added or removed
        private long sessionTotalBytesOfFilesToUpload = 0;
        private long uploadedLength = 0;

        private UploadStats uploadStats;

        private bool stopUpload = false;
        private bool resumeUploads;

        public event EventHandler UploadStatsUpdated;

        public long MaxUploadInBytesPerSecond
        {
            set
            {
                YoutubeUploadService.MaxUploadInBytesPerSecond = value;
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
        }

        private void onUploadStatsUpdated()
        {
            EventHandler handler = this.UploadStatsUpdated;

            if (handler != null)
            {
                handler(this, null);
            }
        }

        public async Task<UploaderResult> Upload(UploadStats uploadStats, bool resumeUploads, long maxUploadInBytesPerSecond)
        {
            this.uploadStats = uploadStats;
            this.resumeUploads = resumeUploads;

            List<Predicate<Upload>> predicates = new List<Predicate<Upload>>(2);
            predicates.Add(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload && File.Exists(upload2.FilePath));
            if (this.resumeUploads)
            {
                predicates.Add(upload2 => (upload2.UploadStatus == UplStatus.Failed || upload2.UploadStatus == UplStatus.Stopped) && File.Exists(upload2.FilePath));
            }

            Upload upload = this.uploadList.GetUpload(PredicateCombiner.Or(predicates.ToArray()));
            if (upload == null)
            {
                return UploaderResult.NothingDone;
            }

            bool oneUploadFinished = false;

            this.sessionTotalBytesOfFilesToUpload = this.resumeUploads ? this.uploadList.TotalBytesOfFilesToUploadIncludingResumable : this.uploadList.TotalBytesOfFilesToUpload;
            this.uploadStats.UploadsChanged(this.sessionTotalBytesOfFilesToUpload, this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);
            this.uploadedLength = 0;

            List<Upload> uploadsOfSession = new List<Upload>();

            while (upload != null && !this.stopUpload)
            {
                uploadsOfSession.Add(upload);
                upload.UploadErrorMessage = null;
                this.uploadStats.InitializeNewUpload(upload, this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);

                upload.UploadStatus = UplStatus.Uploading;

                UploadResult result = await YoutubeUploadService.Upload(upload, maxUploadInBytesPerSecond,
                    updateUploadProgress, this.isStopped);

                if (result.UploadSuccessFull)
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
                this.uploadStats.FinishUpload();

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                upload = this.uploadList.GetUpload(
                    PredicateCombiner.And(
                        new Predicate<Upload>[]
                        {
                            PredicateCombiner.Or(predicates.ToArray()),
                            upload2 => !uploadsOfSession.Contains(upload2)
                        }));
            }

            if (oneUploadFinished)
            {
                this.updateSchedules(uploadsOfSession);
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            this.uploadStats.UploadFinished = true;

            this.uploadStats = null;

            return oneUploadFinished ? UploaderResult.OneUploadFinished : UploaderResult.NoUploadFinished;
        }

        private void updateSchedules(List<Upload> finishedUploads)
        {
            IEnumerable<Template> templates = finishedUploads.Select(upload => upload.Template).Distinct().Where(template => template != null);

            if (templates.Any())
            {
                foreach (Template template in templates)
                {
                    template.SetScheduleProgress();
                }
            }
        }

        void updateUploadProgress(YoutubeUploadStats stats)
        {
            //check if upload has been added or removed
            long totalBytesOfFilesToUpload = this.resumeUploads ? this.uploadList.TotalBytesOfFilesToUploadIncludingResumable : this.uploadList.TotalBytesOfFilesToUpload;
            long delta = this.sessionTotalBytesOfFilesToUpload - (totalBytesOfFilesToUpload + this.uploadedLength);
            if (delta != 0)
            {
                //delta is negative when files have been added
                this.sessionTotalBytesOfFilesToUpload -= delta;
                this.uploadStats.UploadsChanged(this.sessionTotalBytesOfFilesToUpload, this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);
            }

            this.uploadStats.CurrentTotalBytesLeftRemaining = this.resumeUploads
                ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable
                : this.uploadList.RemainingBytesOfFilesToUpload;
            this.uploadStats.CurrentSpeedInBytesPerSecond = stats.CurrentSpeedInBytesPerSecond;
            this.onUploadStatsUpdated();
        }

        private bool isStopped()
        {
            return this.stopUpload;
        }
    }
}
