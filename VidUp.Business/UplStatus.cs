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
        [Description("Canceled")]
        Canceled,
        [Description("Finished")]
        Finished,
        [Description("Failed")]
        Failed
    }
}
