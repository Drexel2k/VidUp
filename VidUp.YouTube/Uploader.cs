using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using Drexel.VidUp.Youtube;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.UI;

namespace Drexel.VidUp.Youtube
{
    public class Uploader
    {
        private UploadList uploadList;
        //will be updated during upload when files are added or removed
        private long sessionTotalBytesToUplopad = 0;
        private long uploadedLength = 0;

        private UploadStats uploadStats;

        private Action notifyUploadProgress;

        public long MaxUploadInBytesPerSecond
        {
            set
            {
                YoutubeUpload.MaxUploadInBytesPerSecond = value;
            }
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
                this.uploadStats.Initialize(this.uploadList.TotalBytesToUpload);
            }
        }

        public async Task<bool> Upload(Action<Upload> notifyUploadStart, Action<Upload> notifyUploadEnd, Action notifyUploadProgress, UploadStats uploadStats, long maxUploadInBytesPerSecond)
        {
            this.uploadStats = uploadStats;
            this.notifyUploadProgress = notifyUploadProgress;
            bool oneUploadFinished = false;

            Upload upload = this.uploadList.GetUpload(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload && File.Exists(upload2.FilePath));

            this.sessionTotalBytesToUplopad = this.uploadList.TotalBytesToUpload;
            this.uploadedLength = 0;

            uploadStats.Initialize(this.uploadList.TotalBytesToUpload); ;
            while (upload != null)
            {
                upload.UploadErrorMessage = null;
                this.uploadStats.InitializeNewUpload(DateTime.Now, upload.FileLength);

                upload.UploadStatus = UplStatus.Uploading;

                if(notifyUploadStart != null)
                {
                    notifyUploadStart(upload);
                }

                UploadResult result = await YoutubeUpload.Upload(upload, maxUploadInBytesPerSecond, updateUploadProgress);

                if (!string.IsNullOrWhiteSpace(result.VideoId))
                {
                    oneUploadFinished = true;
                    upload.UploadStatus = UplStatus.Finished;
                }
                else
                {
                    upload.UploadStatus = UplStatus.Failed;
                }

                this.uploadedLength += upload.FileLength;
                this.uploadStats.FinishUpload(upload.FileLength);

                if (notifyUploadEnd != null)
                {
                    notifyUploadEnd(upload);
                }

                JsonSerialization.SerializeAllUploads();

                upload = this.uploadList.GetUpload(upload2 => upload2.UploadStatus == UplStatus.ReadyForUpload);
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
            long delta = this.sessionTotalBytesToUplopad - (this.uploadList.TotalBytesToUpload + this.uploadedLength);
            if (delta != 0)
            {
                //delta is negative when files have been added
                this.sessionTotalBytesToUplopad -= delta;
            }

            this.uploadStats.UpdateStats(stats.BytesSent, this.sessionTotalBytesToUplopad, stats.CurrentSpeedInBytesPerSecond);
            if(this.notifyUploadProgress != null)
            {
                this.notifyUploadProgress();
            }
        }
    }
}
