using System;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Windows.Data;
using System.Windows.Markup;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.Converters
{
    public class UplStatusStringValuesConverter : MarkupExtension, IValueConverter
    {
        private static UplStatusStringValuesConverter converter;

        public UplStatusStringValuesConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string)
            {
                string stringValue = (string)value;
                if (stringValue == "All")
                {
                    return stringValue;
                }

                UplStatus status = (UplStatus)Enum.Parse(typeof(UplStatus), (string)value);

                FieldInfo fieldInfo = status.GetType().GetField(status.ToString());
                if (fieldInfo != null)
                {
                    object[] attributes = fieldInfo.GetCustomAttributes(typeof(DescriptionAttribute), true);
                    if (attributes.Length > 0)
                    {
                        return ((DescriptionAttribute)attributes[0]).Description;
                    }
                }
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new UplStatusStringValuesConverter());
        }
    }
}
