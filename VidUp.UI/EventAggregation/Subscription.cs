using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
