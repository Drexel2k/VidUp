using System;
using Drexel.VidUp.Youtube;

namespace Drexel.VidUp.UI.Events
{
    public class UploadFinishedEventArgs : EventArgs
    {
        private bool oneUploadFinished;
        private bool uploadStopped;

        public UploadFinishedEventArgs(bool oneUploadFinished, bool uploadStopped)
        {
            this.oneUploadFinished = oneUploadFinished;
            this.uploadStopped = uploadStopped;
        }

        public bool OneUploadFinished
        {
            get => this.oneUploadFinished;
        }

        public bool UploadStopped
        {
            get => this.uploadStopped;
        }
    }
}