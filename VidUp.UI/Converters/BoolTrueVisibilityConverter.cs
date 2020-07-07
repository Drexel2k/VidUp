using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Drexel.VidUp.UI.Converters
{
    public class BoolTrueVisibilityConverter : MarkupExtension, IValueConverter
    {
        private static BoolTrueVisibilityConverter converter;

        public BoolTrueVisibilityConverter()
        {
        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool boolValue = (bool)value;
                if (boolValue)
                {
                    return "Visible";
                }

                return "Collapsed";
            }

            throw new ArgumentException("Value to convert is not a boolean.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new BoolTrueVisibilityConverter());
        }
    }
}