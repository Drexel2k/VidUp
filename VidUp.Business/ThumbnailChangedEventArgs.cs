using System;

namespace Drexel.VidUp.Business
{
    public class ThumbnailChangedEventArgs : EventArgs
    {
        public string OldThumbnailFilePath { get; }

        public ThumbnailChangedEventArgs(string oldThumbnailFilePath)
        {
            this.OldThumbnailFilePath = oldThumbnailFilePath;
        }
    }
}
