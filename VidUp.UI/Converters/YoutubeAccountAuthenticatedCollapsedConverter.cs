using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Markup;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;

namespace Drexel.VidUp.UI.Converters
{
    public class YoutubeAccountAuthenticatedCollapsedConverter : MarkupExtension, IValueConverter
    {
        private static YoutubeAccountAuthenticatedCollapsedConverter converter;

        public YoutubeAccountAuthenticatedCollapsedConverter()
        {

        }

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if(value == null)
            {
                return "Collapsed";
            }

            if (value is YoutubeAccountComboboxViewModel)
            {
                YoutubeAccount youtubeAccount = ((YoutubeAccountComboboxViewModel) value).YoutubeAccount;

                if (youtubeAccount.IsAuthenticated)
                {
                    return "Collapsed";
                }

                return "Visible";
            }

            throw new ArgumentException("Value to convert is no YoutubeAccountComboboxViewModel.");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }

        public override object ProvideValue(IServiceProvider serviceProvider)
        {
            return converter ?? (converter = new YoutubeAccountAuthenticatedCollapsedConverter());
        }
    }
}
