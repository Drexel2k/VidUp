
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class PublishAtChangedMessage
    {
        public Upload Upload { get; }

        public PublishAtChangedMessage(Upload upload)
        {
            this.Upload = upload;
        }
    }
}
