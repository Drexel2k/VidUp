#region

using System;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

#endregion

namespace Drexel.VidUp.Youtube
{
    public class UploadStats
    {
        private bool finished;

        private DateTime currentUploadStart = DateTime.MinValue;
        private long uploadedLength;
        private long currentUploadBytesResumed;
        private long currentTotalBytesToUpload;
        private long sessionTotalBytesToUplopad;
        private long currentUploadSpeedInBytesPerSecond;

        private Upload upload;

        public float ProgressPercentage
        { 
            get
            {
                if(finished)
                {
                    return 1;
                }
                {
                    return (this.uploadedLength + (this.upload.BytesSent - this.currentUploadBytesResumed)) / (float)sessionTotalBytesToUplopad;
                }
            } 
        }

        public TimeSpan CurrentFileTimeLeft 
        { 
            get
            {
                TimeSpan duration = DateTime.Now - this.currentUploadStart;
                if ((this.upload.BytesSent - this.currentUploadBytesResumed) > 0)
                {
                    float factor = (float)(this.upload.BytesSent - this.currentUploadBytesResumed) / (this.upload.FileLength - this.currentUploadBytesResumed);
                    TimeSpan totalDuration = TimeSpan.FromMilliseconds(duration.TotalMilliseconds / factor);
                    return totalDuration - duration;
                }
                else
                {
                    return TimeSpan.MinValue;
                }
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
                return (int)((float)(this.currentTotalBytesToUpload - (this.upload.BytesSent - this.currentUploadBytesResumed)) / Constants.ByteMegaByteFactor);
            }
        }
        public TimeSpan TotalTimeLeft
        {
            get
            {
                TimeSpan duration2 = DateTime.Now - this.currentUploadStart;
                if ((this.upload.BytesSent - this.currentUploadBytesResumed) > 0)
                {
                    float factor = (float)(this.upload.BytesSent - this.currentUploadBytesResumed) / currentTotalBytesToUpload;
                    TimeSpan totalDuration = TimeSpan.FromMilliseconds(duration2.TotalMilliseconds / factor);
                    return totalDuration - duration2;
                }
                else
                {
                    return TimeSpan.MinValue;
                }
            }
                
        }
        public long CurrentSpeedInKiloBytesPerSecond
        { 
            get
            {
                return (long)(currentUploadSpeedInBytesPerSecond / 1024f);
            }
        }

        public long CurrentTotalBytesToUpload
        {
            get => this.currentTotalBytesToUpload;
            set => this.currentTotalBytesToUpload = value;
        }

        public UploadStats()
        {
        }

        public void UpdateStats(long sessionTotalBytesToUplopad, long currentUploadSpeedInBytesPerSecond)
        {
            this.sessionTotalBytesToUplopad = sessionTotalBytesToUplopad;
            this.currentUploadSpeedInBytesPerSecond = currentUploadSpeedInBytesPerSecond;
        }

        public void InitializeNewUpload(Upload upload)
        {
            this.currentUploadStart = DateTime.Now;
            this.upload = upload;
            this.currentUploadBytesResumed = this.upload.BytesSent;
        }

        public void FinishUpload(long uploadedLength)
        {
            this.uploadedLength += uploadedLength;
        }

        public void AllFinished()
        {
            this.finished = true;
        }
    }
}
