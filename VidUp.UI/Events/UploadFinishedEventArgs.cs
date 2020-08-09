using System;
using Drexel.VidUp.Youtube;

namespace Drexel.VidUp.UI.Events
{
    public class UploadFinishedEventArgs : EventArgs
    {
        private bool oneUploadFinished;

        public UploadFinishedEventArgs(bool oneUploadFinished)
        {
            this.oneUploadFinished = oneUploadFinished;
        }

        public bool OneUploadFinished
        {
            get => this.oneUploadFinished;
        }
    }
}