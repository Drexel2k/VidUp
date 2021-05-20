using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class UploadStatusChangedMessage
    {
        public Upload Upload { get; }

        public UploadStatusChangedMessage(Upload upload)
        {
            this.Upload = upload;
        }
    }
}
