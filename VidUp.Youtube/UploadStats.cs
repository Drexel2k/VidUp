using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Youtube
{
    public class UploadStats
    {
        private bool uploadFinished;

        private DateTime currentUploadStart = DateTime.MinValue;

        //sum of potentially bytes to upload of all uploads already handled in this session, independent form result.
        private long sessionBytesUploadedFinishedUploads;

        //bytes already sent of current upload when upload started.
        private long currentUploadBytesResumed;

        //total remaining bytes to upload of upload list, either with or without uploads to resume, depending on setting. Changes when upload list (add or remove uploads or change status of upload) changes.
        private long sessionTotalBytesToUploadRemaining;

        //total file size in bytes of uploads to be uploaded of upload list, either with or without uploads to resume, depending on setting. Changes when upload list (add or remove uploads or change status of upload) changes.
        private long sessionTotalBytesOfFilesToUpload;

        //total bytes to upload left, either with or without uploads to resume, depending on setting, change on bytes sent.
        private long currentTotalBytesLeftRemaining;

        private long currentUploadSpeedInBytesPerSecond;

        private Upload upload;


        public float TotalProgressPercentage
        { 
            get
            {
                if(uploadFinished)
                {
                    return 1;
                }
                else
                {
                    return (this.sessionBytesUploadedFinishedUploads + (this.upload.BytesSent - this.currentUploadBytesResumed)) / (float)this.sessionTotalBytesToUploadRemaining;
                }
            } 
        }

        public TimeSpan? CurrentFileTimeLeft 
        { 
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = (this.upload.FileLength - this.upload.BytesSent) / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return null;
            }
        }
        public int CurrentFileMbLeft
        {
            get
            {
                return (int)((float)(this.upload.FileLength - this.upload.BytesSent) / Constants.ByteMegaByteFactor);
            }
        }
        public int CurrentFilePercent
        { 
            get
            {
                return (int)((float)this.upload.BytesSent / this.upload.FileLength * 100);
            }
        }

        public int TotalMbLeft
        {
            get
            {
                return (int)((float)this.currentTotalBytesLeftRemaining / Constants.ByteMegaByteFactor);
            }

        }

        public TimeSpan? TotalTimeLeft
        {
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = this.currentTotalBytesLeftRemaining / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return null;
            }
                
        }

        public long CurrentTotalBytesLeftRemaining
        {
            set
            {
                this.currentTotalBytesLeftRemaining = value;
            }
        }
        public long CurrentSpeedInBytesPerSecond
        {
            set
            {
                this.currentUploadSpeedInBytesPerSecond = value;
            }
        }

        public long CurrentSpeedInKiloBytesPerSecond
        { 
            get
            {
                return (long)(this.currentUploadSpeedInBytesPerSecond / 1024f);
            }
        }

        public bool UploadFinished
        {
            set
            {
                this.uploadFinished = value;
            }
        }

        public UploadStats()
        {
        }

        public void UploadsChanged(long sessionTotalBytesOfFilesToUpload, long sessionTotalBytesToUploadRemaining)
        {
            this.sessionBytesUploadedFinishedUploads = 0;
            this.sessionTotalBytesToUploadRemaining = sessionTotalBytesToUploadRemaining;
            this.sessionTotalBytesOfFilesToUpload = sessionTotalBytesOfFilesToUpload;
            this.currentTotalBytesLeftRemaining = sessionTotalBytesToUploadRemaining;
        }

        public void InitializeNewUpload(Upload upload, long currentTotalBytesLeftRemaining)
        {
            this.currentUploadStart = DateTime.Now;
            this.upload = upload;
            this.currentUploadBytesResumed = this.upload.BytesSent;
            this.currentTotalBytesLeftRemaining = currentTotalBytesLeftRemaining;
        }

        public void FinishUpload()
        {
            this.sessionBytesUploadedFinishedUploads += this.upload.FileLength - this.currentUploadBytesResumed;
        }
    }
}
