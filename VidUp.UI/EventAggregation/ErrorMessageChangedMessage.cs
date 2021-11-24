using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class ErrorMessageChangedMessage
    {
        public Upload Upload { get; }

        public ErrorMessageChangedMessage(Upload upload)
        {
            this.Upload = upload;
        }
    }
}
