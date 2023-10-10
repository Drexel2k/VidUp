using System;
using Drexel.VidUp.Youtube;

namespace Drexel.VidUp.UI.Events
{
    public class UploadListStartedEventArgs : EventArgs
    {
        private UploadStats uploadStats;

        public UploadListStartedEventArgs(UploadStats uploadStats)
        {
            this.uploadStats = uploadStats;
        }

        public UploadStats UploadStats
        {
            get => this.uploadStats;
        }
    }
}
