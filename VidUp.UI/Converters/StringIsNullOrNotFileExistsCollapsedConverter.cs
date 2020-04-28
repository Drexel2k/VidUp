using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows.Markup;

namespace Drexel.VidUp.UI.Converters
{
    public class StringIsNullOrNotFileExistsCollapsedConverter : MarkupExtension, IValueConverter
    {
        private static StringIsNullOrNotFileExistsCollapsedConverter converter;

        public StringIsNullOrNotFileExistsCollapsedConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
            {
                return "Collapsed";
            }

            string targetFolder = null;

            //parameter is source folder to ignore
            if (parameter != null)
            {
                if (parameter is string)
                {
                    targetFolder = (string)parameter;
                }
                else
                {
                    throw new ArgumentException("Parameter is no string.");
                }
            }

            if (value is string)
            {
                string input = (string)value;
                if (string.IsNullOrWhiteSpace(input))
                {
                    return "Collapsed";
                }
                else
                { 
                    if (targetFolder != null)
                    {
                        string fileFolder = Path.GetDirectoryName(input);
                        if (String.Compare(targetFolder.TrimEnd('\\'), fileFolder.TrimEnd('\\'), StringComparison.InvariantCultureIgnoreCase) == 0)
                        {
                            return "Collapsed";
                        }
                        else
                        {
                            string fileName = new FileInfo(input).Name;
                            string targetFilePath = Path.Combine(targetFolder, fileName);
                            if (File.Exists(targetFilePath))
                            {
                                return "Visible";
                            }
                            else
                            {
                                return "Collapsed";
                            }

                        }
                    }
                    else
                    {
                        return "Collapsed";
                    }
                }
            }
            else
            {
                throw new ArgumentException("Value to convert is no string.");
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new StringIsNullOrNotFileExistsCollapsedConverter());
        }
    }
}
