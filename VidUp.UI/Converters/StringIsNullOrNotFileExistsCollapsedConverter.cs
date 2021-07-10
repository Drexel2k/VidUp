using System;
using System.Globalization;
using System.IO;
using System.Windows.Data;
using System.Windows.Markup;
using Drexel.VidUp.Utils;

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

            string targetFolder;
            string parameterInternal = parameter as string;
            //parameter is source folder to ignore
            if (parameterInternal == null)
            {
                throw new ArgumentException("Parameter is no string.");
            }

            if (parameterInternal == "image")
            {
                targetFolder = Settings.Instance.TemplateImageFolder;
            }
            else if (parameterInternal == "fallbackthumb")
            {
                targetFolder = Settings.Instance.ThumbnailFallbackImageFolder;
            }
            else
            {
                throw new ArgumentException("Unexpected parameter string.");
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
