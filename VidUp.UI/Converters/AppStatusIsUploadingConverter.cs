using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Windows.Data;
using System.Windows.Markup;

namespace Drexel.VidUp.UI.Converters
{
    public class AppStatusIsUploadingConverter : MarkupExtension, IValueConverter
    {
        private static AppStatusIsUploadingConverter converter;

        public AppStatusIsUploadingConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is AppStatus)
            {
                AppStatus appStatus = (AppStatus) value;
                if (appStatus == AppStatus.Uploading)
                {
                    return false;
                }

                return true;
            }
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new AppStatusIsUploadingConverter());
        }
    }
}