using System.Globalization;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Business
{
    public static class Cultures
    {
        public static CultureInfo[] CultureInfos { get; }
        static Cultures()
        {
            if (Settings.SettingsInstance.UserSettings.VideoLanguagesFilter == null || Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Length <= 0)
            {
                Cultures.CultureInfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
            }
            else
            {
                Cultures.CultureInfos = new CultureInfo[Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Length];

                for (int i = 0; i < Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Length; i++)
                {
                    Cultures.CultureInfos[i] =
                        CultureInfo.GetCultureInfo(Settings.SettingsInstance.UserSettings.VideoLanguagesFilter[i]);
                }
            }
        }
    }
}