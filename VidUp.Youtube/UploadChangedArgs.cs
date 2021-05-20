using System;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube
{
    public class UploadChangedArgs : EventArgs
    {
        public Upload Upload { get; }

        public UploadChangedArgs(Upload upload)
        {
            this.Upload = upload;
        }
    }
}
