using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class ResumableSessionUriChangedMessage
    {
        public Upload Upload { get; }

        public ResumableSessionUriChangedMessage(Upload upload)
        {
            this.Upload = upload;
        }
    }
}
