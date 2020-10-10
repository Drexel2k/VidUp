using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Drexel.VidUp.UI.Converters
{
    public class InvertBooleanConverter : MarkupExtension, IValueConverter
    {
        private static InvertBooleanConverter converter;

        public InvertBooleanConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool)
            {
                bool valueInternal = (bool)value;
                return !valueInternal;
            }

            throw new ArgumentException("Value is no boolean.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new InvertBooleanConverter());
        }
    }
}
