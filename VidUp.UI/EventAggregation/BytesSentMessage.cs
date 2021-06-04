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
