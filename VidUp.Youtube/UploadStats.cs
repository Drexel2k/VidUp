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
        private long totalFileLengthToUpload;
        //total remaining bytes of all files to uploads, changes only on upload list changes
        private long totalRemainingBytes;

        //total file size of all files which were tried to upload, e.g. finished or failed, changes on upload change
        private long totalFileLengthProcessed;
        //bytes sent of current upload, changes on upload change
        private long currentUploadBytesSentInitial;

        //reset on every update to avoid reevaluations for some of the stats
        private long currentRemainingBytesLeftToUpload;
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
                    return (this.totalFileLengthProcessed + (this.currentUpload.BytesSent - this.currentUploadBytesSentInitial)) / (float)this.totalRemainingBytes;
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
                return (int)((float)this.currentRemainingBytesLeftToUpload / Constants.ByteMegaByteFactor);
            }

        }

        public TimeSpan? TotalTimeLeft
        {
            get
            {
                if (this.currentUploadSpeedInBytesPerSecond > 0)
                {
                    float seconds = this.currentRemainingBytesLeftToUpload / (float) this.currentUploadSpeedInBytesPerSecond;
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

        public UploadStats(AutoResetEvent resetEvent)
        {
            this.resetEvent = resetEvent;
        }

        public void Update()
        {
            this.resetEvent.WaitOne();

            this.currentRemainingBytesLeftToUpload = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetRemainingBytesOfFilesToUpload(this.uploaded);

            //check if upload has been added, removed, paused, reset...
            long currentTotalBytesLeftToUpload = (this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetTotalBytesOfFilesToUpload(this.uploaded)) + this.totalFileLengthProcessed;
            long delta = currentTotalBytesLeftToUpload - this.totalFileLengthToUpload;
            if (delta != 0)
            {
                this.totalFileLengthToUpload = this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetTotalBytesOfFilesToUpload(this.uploaded);
                this.totalRemainingBytes = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(this.uploaded) : this.uploadList.GetRemainingBytesOfFilesToUpload(this.uploaded);
                this.totalFileLengthProcessed = 0;
            }

            this.resetEvent.Set();
        }

        public void Initialize(UploadList uploadList, in bool resumeUploads)
        {
            this.uploadList = uploadList;
            this.resumeUploads = resumeUploads;
            this.totalFileLengthToUpload = this.resumeUploads ? this.uploadList.GetTotalBytesOfFilesToUploadIncludingResumable(null) : this.uploadList.GetTotalBytesOfFilesToUpload(null);
            this.totalRemainingBytes = this.resumeUploads ? this.uploadList.GetRemainingBytesOfFilesToUploadIncludingResumable(null) : this.uploadList.GetRemainingBytesOfFilesToUpload(null);
        }

        public void NewUpload(Upload upload)
        {
            if (this.currentUpload != null)
            {
                this.totalFileLengthProcessed += this.currentUpload.FileLength;
                this.uploaded.Add(this.currentUpload);
            }

            this.currentUpload = upload;
            this.currentUploadBytesSentInitial = upload.BytesSent;
        }
    }
}
