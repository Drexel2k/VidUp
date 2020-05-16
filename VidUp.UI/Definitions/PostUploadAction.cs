#region

using System.ComponentModel;

#endregion

namespace Drexel.VidUp.UI.Definitions
{
    public enum PostUploadAction
    {
        [Description("None")]
        None,
        [Description("Taskbar Notification")]
        FlashTaskbar,
        [Description("Sleep Mode")]
        SleepMode,
        [Description("Hibernate")]
        Hibernate,
        [Description("Shutdown System")]
        Shutdown
    }
}
