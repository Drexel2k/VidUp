using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Drexel.VidUp.Business
{
    public enum UplStatus
    {
        [Description("Ready for Upload")]
        ReadyForUpload,
        [Description("Paused")]
        Paused,
        [Description("Uploading")]
        Uploading,
        [Description("Upload Stopped")]
        Stopped,
        [Description("Upload Finished")]
        Finished,
        [Description("Upload Failed")]
        Failed
    }
}
