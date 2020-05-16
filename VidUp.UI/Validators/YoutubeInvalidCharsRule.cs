#region

using System.Globalization;
using System.Linq;
using System.Windows.Controls;

#endregion

namespace Drexel.VidUp.UI.Validators
{
    public class YoutubeInvalidCharsRule : ValidationRule
    {
        public YoutubeInvalidCharsRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            string inputString = (string)value;
            if (inputString.Contains('<') || inputString.Contains('>'))
            {
                return new ValidationResult(false, "Characters < and > not allowed.");
            }

            return ValidationResult.ValidResult;
        }
    }
}
