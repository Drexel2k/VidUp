using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.UI
{
    public static class StatusInformationToStringConverter
    {
        public static string GetStatusInformationString(IEnumerable<StatusInformation> statusInformation, bool withDateTimeInfo)
        {
            if(statusInformation == null)
            {
                return null;
            }

            StringBuilder stringBuilder = new StringBuilder();

            if (statusInformation.Any(error => error.IsQuotaError == true))
            {
                stringBuilder.AppendLine(TinyHelpers.QuotaExceededString);
            }

            if (statusInformation.Any(error => error.IsAuthenticationApiResponseError == true))
            {
                stringBuilder.AppendLine(TinyHelpers.AuthenticationErrorString);
            }

            foreach (StatusInformation statusInfo in statusInformation)
            {
                if (withDateTimeInfo)
                {
                    stringBuilder.AppendLine($"{statusInfo.DateTime} {statusInfo.Message}");
                }
                else
                {
                    stringBuilder.AppendLine(statusInfo.Message);
                }
            }

            return TinyHelpers.TrimLineBreakAtEnd(stringBuilder.ToString());
        }
    }
}
