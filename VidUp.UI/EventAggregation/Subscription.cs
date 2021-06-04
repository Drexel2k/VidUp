using System;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class Subscription : IDisposable
    {
        private Action remove;

        public Subscription(Action remove)
        {
            this.remove = remove;
        }
        public void Dispose()
        {
            if (this.remove != null)
            {
                this.remove();
            }
        }
    }
}
