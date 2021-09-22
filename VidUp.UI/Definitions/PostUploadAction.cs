using System.ComponentModel;

namespace Drexel.VidUp.UI.Definitions
{
    public enum PostUploadAction
    {
        [Description("None")]
        None,
        [Description("Taskbar Notification")]
        FlashTaskbar,
        [Description("Close VidUp")]
        Close,
        [Description("Sleep Mode")]
        SleepMode,
        [Description("Hibernate")]
        Hibernate,
        [Description("Shutdown System")]
        Shutdown
    }
}
