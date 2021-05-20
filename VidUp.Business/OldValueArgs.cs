using System;

namespace Drexel.VidUp.Business
{
    public class OldValueArgs : EventArgs
    {
        public string OldValue { get; }

        public OldValueArgs(string oldValue)
        {
            this.OldValue = oldValue;
        }
    }
}
