using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class UploadListReorderMessage
    {
        public Upload UploadToMove { get; }
        public Upload UploadAtTargetPosition { get; }

        public UploadListReorderMessage(Upload uploadToMove, Upload uploadAtTargetPosition)
        {
            this.UploadToMove = uploadToMove;
            this.UploadAtTargetPosition = uploadAtTargetPosition;
        }
    }
}