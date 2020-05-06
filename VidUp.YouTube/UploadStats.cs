using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Drexel.VidUp.Youtube
{
    public class UploadStats
    {
        private const int byteMegaByteFactor = 1048567;
        private bool finished;

        private DateTime currentUploadStart = DateTime.MinValue;
        private long uploadedLength;
        private long currentUploadBytes;
        private long currentTotalBytesToUpload;
        private long currentUploadBytesSent;
        private long sessionTotalBytesToUplopad;
        private long currentUploadSpeedInBytesPerSecond;

        public float ProgressPercentage
        { 
            get
            {
                if(finished)
                {
                    return 1;
                }
                {
                    return (this.uploadedLength + currentUploadBytesSent) / (float)sessionTotalBytesToUplopad;
                }
            } 
        }

        public TimeSpan CurrentFileTimeLeft 
        { 
            get
            {
                TimeSpan duration = DateTime.Now - this.currentUploadStart;
                if (currentUploadBytesSent > 0)
                {
                    float factor = (float)currentUploadBytesSent / currentUploadBytes;
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
                return (int)((float)(currentUploadBytes - currentUploadBytesSent) / byteMegaByteFactor);
            }
        }
        public int CurrentFilePercent
        { 
            get
            {
                return (int)((float)currentUploadBytesSent / currentUploadBytes * 100);
            }
        }
        public int TotalMbLeft
        { 
            get
            {
                return (int)((float)(currentTotalBytesToUpload - currentUploadBytesSent) / byteMegaByteFactor);
            }
        }
        public TimeSpan TotalTimeLeft
        {
            get
            {
                TimeSpan duration2 = DateTime.Now - this.currentUploadStart;
                if (currentUploadBytesSent > 0)
                {
                    float factor = (float)currentUploadBytesSent / currentTotalBytesToUpload;
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

        public UploadStats()
        {
        }

        public void UpdateStats(long currentUploadBytesSent, long sessionTotalBytesToUplopad, long currentUploadSpeedInBytesPerSecond)
        {
            this.currentUploadBytesSent = currentUploadBytesSent;
            this.sessionTotalBytesToUplopad = sessionTotalBytesToUplopad;
            this.currentUploadSpeedInBytesPerSecond = currentUploadSpeedInBytesPerSecond;
        }

        public void Initialize(long currentTotalBytesToUpload)
        {
            this.currentTotalBytesToUpload = currentTotalBytesToUpload;
        }

        public void InitializeNewUpload(DateTime currentUploadStart, long currentUploadBytes)
        {
            this.currentUploadStart = currentUploadStart;
            this.currentUploadBytes = currentUploadBytes;
        }

        public void FinishUpload(long uploadedLength)
        {
            this.uploadedLength += uploadedLength;
            this.currentUploadBytesSent = 0;
            this.currentUploadBytes = 0;
        }

        public void AllFinished()
        {
            this.finished = true;
        }
    }
}
