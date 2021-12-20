using System;

namespace Drexel.VidUp.Business
{
    [Flags]
    public enum StatusInformationType
    {
        QuotaError = 1,
        AuthenticationError = 2,
        AuthenticationApiResponseError = 4,
        Other = 8
    }
}