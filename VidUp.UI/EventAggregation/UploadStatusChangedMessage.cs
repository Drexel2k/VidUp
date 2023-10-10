﻿using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    internal class UploadStatusChangedMessage
    {
        private Upload upload;

        public Upload Upload { get => this.upload; }

        public UploadStatusChangedMessage(Upload upload)
        {
            this.upload = upload;
        }
    }
}