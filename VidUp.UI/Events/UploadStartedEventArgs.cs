using System;
using Drexel.VidUp.Youtube;

namespace Drexel.VidUp.UI.Events
{
    public class UploadStartedEventArgs : EventArgs
    {
        private UploadStats uploadStats;

        public UploadStartedEventArgs(UploadStats uploadStats)
        {
            this.uploadStats = uploadStats;
        }

        public UploadStats UploadStats
        {
            get => this.uploadStats;
        }
    }
}
