using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Video;
using Drexel.VidUp.Youtube.VideoUpload;


namespace Drexel.VidUp.Youtube
{
    public class Uploader
    {
        private UploadList uploadList;
        //will be updated during upload when files are added or removed
        private long sessionTotalBytesOfFilesToUpload = 0;
        private long uploadedLength = 0;

        private UploadStats uploadStats;

        private bool resumeUploads;
        private CancellationTokenSource tokenSource;
        private bool uploadStopped;

        private static double serializationInterval = 30d;
        private DateTime lastSerialization;

        public event EventHandler UploadStatsUpdated;

        public long MaxUploadInBytesPerSecond
        {
            set
            {
                YoutubeVideoUploadService.MaxUploadInBytesPerSecond = value;
            }
        }

        public bool UploadStopped { get => this.uploadStopped; }

        public Uploader(UploadList uploadList)
        {
            if (uploadList == null)
            {
                throw new ArgumentException("uploadList must not be null.");
            }

            this.uploadList = uploadList;

            this.tokenSource = new CancellationTokenSource();
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
            Tracer.Write($"Uploader.Upload: Start with resumeUploads: {resumeUploads}, maxUploadInBytesPerSecond: {maxUploadInBytesPerSecond}.");
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

            while (upload != null)
            {
                uploadsOfSession.Add(upload);
                this.uploadStats.InitializeNewUpload(upload, this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);

                upload.UploadStatus = UplStatus.Uploading;

                this.lastSerialization = DateTime.Now;
                YoutubeVideoUploadService.MaxUploadInBytesPerSecond = maxUploadInBytesPerSecond;
                UploadResult uploadResult = await YoutubeVideoUploadService.Upload(upload, updateUploadProgress, this.tokenSource.Token);

                if (uploadResult.VideoResult == VideoResult.Finished)
                {
                    oneUploadFinished = true;
                }

                this.uploadedLength += upload.FileLength;
                this.uploadStats.FinishUpload();

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                if (uploadResult.VideoResult == VideoResult.Stopped)
                {
                    this.uploadStopped = true;
                    break;
                }

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

            Tracer.Write($"Uploader.Upload: End.");
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

        private void updateUploadProgress(YoutubeUploadStats stats)
        {
            if ((DateTime.Now - this.lastSerialization).TotalSeconds >= Uploader.serializationInterval)
            {
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.lastSerialization = DateTime.Now;
            }

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

        public void StopUpload()
        {
            this.tokenSource.Cancel();
        }
    }
}
