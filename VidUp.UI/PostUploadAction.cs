using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Drexel.VidUp.UI
{
    public enum PostUploadAction
    {
        [Description("None")]
        None,
        [Description("Sleep Mode")]
        SleepMode,
        [Description("Hibernate")]
        Hibernate,
        [Description("Shutdown System")]
        Shutdown
    }
}
