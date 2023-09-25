using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;

namespace Drexel.VidUp.UI.Converters
{
    public class ExpanderIndexToBooleanConverter : MarkupExtension,  IValueConverter
    {
        private static ExpanderIndexToBooleanConverter converter;
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            int intParameter;
            if(int.TryParse((string)parameter, out intParameter))
            {
                if((int)value == intParameter)
                {
                    return true;
                }
            }

            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (System.Convert.ToBoolean(value)) return parameter;
            return null;
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new ExpanderIndexToBooleanConverter());
        }
    }
}
