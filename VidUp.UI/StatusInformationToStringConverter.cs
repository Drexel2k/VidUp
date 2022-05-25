using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Drexel.VidUp.Business;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.UI
{
    public static class StatusInformationToStringConverter
    {
        //showAllCodes = false shows only error codes, not information codes.
        public static string GetStatusInformationString(IEnumerable<StatusInformation> statusInformation, bool withDateTimeInfo, bool showAllCodes)
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
                string codeString = $" ({statusInfo.Code})";
                if (statusInfo.Code.StartsWith("I", StringComparison.InvariantCultureIgnoreCase))
                {
                    if(!showAllCodes)
                    {
                        codeString =String.Empty;
                    }
                }

                if (withDateTimeInfo)
                {
                    stringBuilder.AppendLine($"{statusInfo.DateTime} {statusInfo.Message}{codeString}");
                }
                else
                {
                    stringBuilder.AppendLine($"{statusInfo.Message}{codeString}");
                }
            }

            return TinyHelpers.TrimLineBreakAtEnd(stringBuilder.ToString());
        }
    }
}
