using System;
using Drexel.VidUp.Youtube;

namespace Drexel.VidUp.UI.Events
{
    public class UploadListFinishedEventArgs : EventArgs
    {
        private bool dataSent;
        private bool uploadStopped;

        public UploadListFinishedEventArgs(bool dataSent, bool uploadStopped)
        {
            this.dataSent = dataSent;
            this.uploadStopped = uploadStopped;
        }

        public bool DataSent
        {
            get => this.dataSent;
        }

        public bool UploadStopped
        {
            get => this.uploadStopped;
        }
    }
}