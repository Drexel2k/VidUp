using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.UI.DllImport
{
    public enum ExitWindows : uint
    {
        // ONE of the following six:
        LogOff = 0x00000000,
        ShutDown = 0x00000001,
        Reboot = 0x00000002,
        PowerOff = 0x00000008,
        RestartApps = 0x00000040,
        HybridShutdown = 0x00400000,
        // plus AT MOST ONE of the following two:
        Force = 0x00000004,
        ForceIfHung = 0x00000010,
    }
}
