using System;
using System.Collections.Generic;
using System.Threading;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Youtube
{
    public class UploadStats
    {
        private bool uploadFinished;
        private UploadList uploadList;
        private Upload currentUpload;
        private List<Upload> uploaded = new List<Upload>();
        private bool resumeUploads;

        private long currentUploadSpeedInBytesPerSecond;

        //total file size of all files to upload to check changes, changes only on upload list changes
        private long totalFileLengthToSend;
        //total remaining bytes of all files to uploads, changes only on upload list changes
        private long totalRemainingBytes;

        //total file size of all files which were tried to upload, e.g. finished or failed, changes on upload change
        private long totalFileLengthProcessed;
        //remaining bytes uploaded of all files which were tried to upload, e.g. finished or failed, changes on upload change
        private long totalBytesSent;
        //bytes sent of current upload, changes on upload change
        private long currentUploadBytesSentInitial;

        //reset on every update to avoid reevaluations for some of the stats
        private long currentRemainingBytesLeftToSend;
        private AutoResetEvent resetEvent;

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
                    return (this.totalBytesSent + (this.currentUpload.BytesSent - this.currentUploadBytesSentInitial)) / (float)this.totalRemainingBytes;
                }
            } 
        }

        public TimeSpan? CurrentFileTimeLeft 
        { 
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = (this.currentUpload.FileLength - this.currentUpload.BytesSent) / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return null;
            }
        }
        public int CurrentFileMbLeft
        {
            get
            {
                return (int)((float)(this.currentUpload.FileLength - this.currentUpload.BytesSent) / Constants.ByteMegaByteFactor);
            }
        }
        public int CurrentFilePercent
        { 
            get
            {
                return (int)((float)this.currentUpload.BytesSent / this.currentUpload.FileLength * 100);
            }
        }

        public int TotalMbLeft
        {
            get
            {
                return (int)((float)this.currentRemainingBytesLeftToSend / Constants.ByteMegaByteFactor);
            }

        }

        public TimeSpan? TotalTimeLeft
        {
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = this.currentRemainingBytesLeftToSend / (float) this.currentUploadSpeedInBytesPerSecond;
                    return TimeSpan.FromSeconds(seconds);
                }

                return null;
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

        public bool ResumeUploads
        {
            set => this.resumeUploads = value;
        }

        public UploadStats(AutoResetEvent resetEvent)
        {
            this.resetEvent = resetEvent;
        }

        public void Update()
        {
            this.resetEvent.WaitOne();

            this.currentRemainingBytesLeftToSend = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetRemainingBytesOfFilesToUpload(this.uploaded);

            //check if upload has been added, removed, paused, reset...
            long currentTotalBytesInSession = (this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetTotalBytesOfFilesToUpload(this.uploaded)) + this.totalFileLengthProcessed;
            long delta = currentTotalBytesInSession - this.totalFileLengthToSend;
            if (delta != 0)
            {
                this.totalFileLengthToSend = this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetTotalBytesOfFilesToUpload(this.uploaded);
                this.totalRemainingBytes = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetRemainingBytesOfFilesToUpload(this.uploaded);
                this.totalFileLengthProcessed = 0;
                this.totalBytesSent = 0;
            }

            this.resetEvent.Set();
        }

        public void Initialize(UploadList uploadList, in bool resumeUploads)
        {
            this.uploadList = uploadList;
            this.resumeUploads = resumeUploads;
            this.totalFileLengthToSend = this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(null) : this.uploadList.GetTotalBytesOfFilesToUpload(null);
            this.totalRemainingBytes = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(null) : this.uploadList.GetRemainingBytesOfFilesToUpload(null);
        }

        public void NewUpload(Upload upload)
        {
            if (this.currentUpload != null)
            {
                this.totalBytesSent += this.currentUpload.FileLength - this.currentUploadBytesSentInitial;
                this.totalFileLengthProcessed += this.currentUpload.FileLength;
                this.uploaded.Add(this.currentUpload);
            }

            this.currentUpload = upload;
            this.currentUploadBytesSentInitial = upload.BytesSent;
        }
    }
}
