#region

using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Markup;

#endregion

namespace Drexel.VidUp.UI.Converters
{
    public class StringIsNullOrFileExistsCollapsedConverter : MarkupExtension, IValueConverter
    {
        private static StringIsNullOrFileExistsCollapsedConverter converter;

        public StringIsNullOrFileExistsCollapsedConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return "Collapsed";
            }

            if (value is string)
            {
                string input = (string)value;
                if (string.IsNullOrWhiteSpace(input) || File.Exists(input))
                {
                    return "Collapsed";
                }

                return "Visible";
            }

            throw new ArgumentException("Value to convert is no string.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new StringIsNullOrFileExistsCollapsedConverter());
        }
    }
}
