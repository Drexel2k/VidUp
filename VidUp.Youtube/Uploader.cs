﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.PlaylistItem;
using Drexel.VidUp.Youtube.Thumbnail;
using Drexel.VidUp.Youtube.VideoUpload;


namespace Drexel.VidUp.Youtube
{
    public delegate void ResumableSessionUriSetHandler(object sender, Upload upload);

    public delegate void UploadChangedHandler(object sender, Upload upload);

    public delegate void UploadStatsUpdatedHandler(object sender);

    public delegate void UploadStatusChangedHandler(object sender, Upload upload);

    public delegate void UploadBytesSentHandler(object sender, Upload upload);
    public class Uploader
    {
        private UploadList uploadList;
        //will be updated during upload when files are added or removed
        //todo: change to file count
        private long sessionTotalBytesOfFilesToUpload = 0;
        private long uploadedLengthTotalFileSize = 0;

        private UploadStats uploadStats;

        private bool resumeUploads;
        private CancellationTokenSource tokenSource;
        private bool uploadStopped;

        private static double serializationInterval = 30d;
        private DateTime lastSerialization;
        private Upload currentUpload;

        public event UploadStatsUpdatedHandler UploadStatsUpdated;
        public event UploadStatusChangedHandler UploadStatusChanged;
        public event UploadChangedHandler UploadChanged;
        public event ResumableSessionUriSetHandler ResumableSessionUriSet;
        public event UploadBytesSentHandler UploadBytesSent;

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
            UploadStatsUpdatedHandler handler = this.UploadStatsUpdated;

            if (handler != null)
            {
                handler(this);
            }
        }

        private void onUploadStatusChanged(Upload upload)
        {
            UploadStatusChangedHandler handler = this.UploadStatusChanged;

            if (handler != null)
            {
                handler(this, upload);
            }
        }

        private void onUploadChanged(Upload upload)
        {
            UploadChangedHandler handler = this.UploadChanged;

            if (handler != null)
            {
                handler(this, upload);
            }
        }

        private void onResumableSessionUriSet(Upload upload)
        {
            ResumableSessionUriSetHandler handler = this.ResumableSessionUriSet;

            if (handler != null)
            {
                handler(this, upload);
            }
        }

        private void onUploadBytesSent(Upload upload)
        {
            UploadBytesSentHandler handler = this.UploadBytesSent;

            if (YoutubeVideoUploadService.CurrentPosition != null)
            {
                upload.BytesSent = YoutubeVideoUploadService.CurrentPosition.Value;
            }

            if (handler != null)
            {
                handler(this, upload);
            }
        }

        public async Task<UploaderResult> UploadAsync(UploadStats uploadStats, bool resumeUploads, long maxUploadInBytesPerSecond)
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

            bool dataSent = false;

            this.sessionTotalBytesOfFilesToUpload = this.resumeUploads ? this.uploadList.TotalBytesOfFilesToUploadIncludingResumable : this.uploadList.TotalBytesOfFilesToUpload;
            this.uploadStats.UploadsChanged(this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);
            this.uploadedLengthTotalFileSize = 0;

            List<Upload> uploadsOfSession = new List<Upload>();

            using (System.Timers.Timer timer = new System.Timers.Timer(2000))
            {
                timer.Elapsed += (sender, args) => this.onTimerElapsed();
                
                while (upload != null)
                {
                    this.currentUpload = upload;
                    this.onUploadChanged(upload);
                    uploadsOfSession.Add(upload);
                    this.uploadStats.InitializeNewUpload(upload.FileLength, upload.BytesSent, this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);

                    upload.UploadStatus = UplStatus.Uploading;
                    this.onUploadStatusChanged(upload);

                    this.lastSerialization = DateTime.Now;
                    YoutubeVideoUploadService.MaxUploadInBytesPerSecond = maxUploadInBytesPerSecond;
                    timer.Start();
                    UploadResult videoResult = await YoutubeVideoUploadService.UploadAsync(upload, this.resumableSessionUriSet, this.tokenSource.Token).ConfigureAwait(false);
                    timer.Stop();
                    this.onTimerElapsed();
                    this.onUploadStatusChanged(upload);

                    if (videoResult == UploadResult.Finished)
                    {
                        dataSent = true;
                        await YoutubeThumbnailService.AddThumbnailAsync(upload).ConfigureAwait(false);
                        await YoutubePlaylistItemService.AddToPlaylistAsync(upload).ConfigureAwait(false);
                    }

                    if (videoResult == UploadResult.FailedWithDataSent || videoResult == UploadResult.StoppedWithDataSent)
                    {
                        dataSent = true;
                    }

                    this.uploadedLengthTotalFileSize += upload.FileLength;
                    this.uploadStats.FinishUpload();

                    JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                    if (videoResult == UploadResult.StoppedWithDataSent  || videoResult == UploadResult.StoppedWithoutDataSent)
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
            }

            if (dataSent)
            {
                this.updateSchedules(uploadsOfSession);
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }

            this.uploadStats.UploadFinished = true;

            this.uploadStats = null;

            Tracer.Write($"Uploader.Upload: End.");
            return dataSent ? UploaderResult.DataSent : UploaderResult.NoDataSent;
        }

        private void updateSchedules(List<Upload> finishedUploads)
        {
            Template[] templates = finishedUploads.Select(upload => upload.Template).Distinct().Where(template => template != null).ToArray();

            if (templates.Any())
            {
                foreach (Template template in templates)
                {
                    template.SetScheduleProgress();
                }
            }
        }

        private void onTimerElapsed()
        {
            //updates BytesSent, which are used for upload stats update
            //sequence is important for consistent GUI updates
            this.onUploadBytesSent(this.currentUpload);
            this.updateUploadProgress();
            this.onUploadStatsUpdated();
            this.serializeOnUpload();
        }

        private void updateUploadProgress()
        {
            //check if upload has been added or removed
            long totalBytesOfFilesToUpload = this.resumeUploads ? this.uploadList.TotalBytesOfFilesToUploadIncludingResumable : this.uploadList.TotalBytesOfFilesToUpload;
            long delta = this.sessionTotalBytesOfFilesToUpload - (totalBytesOfFilesToUpload + this.uploadedLengthTotalFileSize);
            if (delta != 0)
            {
                //delta is negative when files have been added
                this.sessionTotalBytesOfFilesToUpload -= delta;
                this.uploadStats.UploadsChanged(this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);
            }

            this.uploadStats.UpdateStats(this.currentUpload.BytesSent,
                this.resumeUploads ? this.uploadList.RemainingBytesOfFilesToUploadIncludingResumable : this.uploadList.RemainingBytesOfFilesToUpload);
            //this.uploadStats.CurrentTotalBytesLeftRemaining = ;
            this.uploadStats.CurrentSpeedInBytesPerSecond = YoutubeVideoUploadService.CurrentSpeedInBytesPerSecond;
        }

        private void serializeOnUpload()
        {
            if ((DateTime.Now - this.lastSerialization).TotalSeconds >= Uploader.serializationInterval)
            {
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.lastSerialization = DateTime.Now;
            }
        }

        private void resumableSessionUriSet(Upload upload)
        {
            this.onResumableSessionUriSet(upload);
        }


        public void StopUpload()
        {
            this.tokenSource.Cancel();
        }
    }
}
