using System;
using System.Globalization;
using System.Windows.Controls;

namespace Drexel.VidUp.UI.Validators
{
    public class OnlyIntGreaterZeroRule : ValidationRule
    {
        public OnlyIntGreaterZeroRule()
        {
        }

        public override ValidationResult Validate(object value, CultureInfo cultureInfo)
        {
            int intValue;
            string inputString = (string)value;
            if (Int32.TryParse(inputString, out intValue))
            {
                if (intValue > 0)
                {
                    return ValidationResult.ValidResult;
                }
            }

            return new ValidationResult(false, "Only numbers > 0 allowed.");
        }
    }
}