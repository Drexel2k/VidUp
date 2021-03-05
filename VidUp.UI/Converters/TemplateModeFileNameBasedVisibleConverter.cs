using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.Converters
{
    public class TemplateModeFileNameBasedVisibleConverter : MarkupExtension, IValueConverter
    {
        private static TemplateModeFileNameBasedVisibleConverter converter;

        public TemplateModeFileNameBasedVisibleConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is TemplateMode)
            {
                TemplateMode templateMode = (TemplateMode)value;
                if (templateMode == TemplateMode.FileNameBased)
                {
                    return "Visible";
                }

                return "Collapsed";
            }

            throw new ArgumentException("Value to convert is not a TemplateMode.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new TemplateModeFileNameBasedVisibleConverter());
        }
    }
}