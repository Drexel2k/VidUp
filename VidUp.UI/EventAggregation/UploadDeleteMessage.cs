using System;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class UploadDeleteMessage
    {
        public Guid UploadGuid { get; }

        public UploadDeleteMessage(Guid uploadGuid)
        {
            this.UploadGuid = uploadGuid;
        }
    }
}