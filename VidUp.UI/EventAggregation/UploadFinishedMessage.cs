using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    internal class UploadFinishedMessage
    {
        private Upload upload;

        public Upload Upload { get => this.upload; }

        public UploadFinishedMessage(Upload upload)
        {
            this.upload = upload;
        }
    }
}