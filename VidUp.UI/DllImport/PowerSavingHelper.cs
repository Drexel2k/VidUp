#region

using System.Runtime.InteropServices;

#endregion

namespace Drexel.VidUp.UI.DllImport
{
    public class PowerSavingHelper
    {
        //to prevent the computer going into sleep mode
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern ExecutionState SetThreadExecutionState(ExecutionState esFlags);

        public static void DisablePowerSaving()
        {
            SetThreadExecutionState(ExecutionState.EsContinous | ExecutionState.EsSystemRequired | ExecutionState.EsAwayModeRequired);
        }

        internal static void EnablePowerSaving()
        {
            SetThreadExecutionState(ExecutionState.EsContinous);
        }
    }
}
