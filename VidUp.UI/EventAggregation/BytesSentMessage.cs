using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class BytesSentMessage
    {
        public Upload Upload { get; }

        public BytesSentMessage(Upload upload)
        {
            this.Upload = upload;
        }

    }
}
