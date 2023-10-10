using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    internal class UploadStartingMessage
    {
        private Upload upload;

        public Upload Upload { get => this.upload; }

        public UploadStartingMessage(Upload upload)
        {
            this.upload = upload;
        }
    }
}