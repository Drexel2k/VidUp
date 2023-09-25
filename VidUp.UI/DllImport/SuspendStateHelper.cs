using System.Runtime.InteropServices;

namespace Drexel.VidUp.UI.DllImport
{
    public class SuspendStateHelper
    {
        [DllImport("Powrprof.dll", CharSet = CharSet.Auto, ExactSpelling = true)]
        public static extern bool SetSuspendState(bool hibernate, bool forceCritical, bool disableWakeEvent);
    }
}
