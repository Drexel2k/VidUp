using System;
using System.Collections.Generic;
using System.Globalization;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Business
{
    public static class Cultures
    {

        public static List<CultureInfo> AllCultureInfos { get; }
        public static List<CultureInfo> RelevantCultureInfos { get; private set; }
        static Cultures()
        {
            Cultures.AllCultureInfos = new List<CultureInfo>(CultureInfo.GetCultures(CultureTypes.SpecificCultures));
            Cultures.AllCultureInfos.Sort((cu1, cu2) => cu1.Name.CompareTo(cu2.Name));

            Cultures.RelevantCultureInfos = new List<CultureInfo>();
            Cultures.SetRelevantCultures();
        }

        public static void SetRelevantCultures()
        {
            Cultures.RelevantCultureInfos.Clear();
            if (Settings.Instance.UserSettings.VideoLanguagesFilter == null || Settings.Instance.UserSettings.VideoLanguagesFilter.Count <= 0)
            {
                Cultures.RelevantCultureInfos.AddRange(CultureInfo.GetCultures(CultureTypes.SpecificCultures));

            }
            else
            {
                for (int i = 0; i < Settings.Instance.UserSettings.VideoLanguagesFilter.Count; i++)
                {
                    Cultures.RelevantCultureInfos.Add(CultureInfo.GetCultureInfo(Settings.Instance.UserSettings.VideoLanguagesFilter[i]));
                }
            }

            Cultures.RelevantCultureInfos.Sort((cu1, cu2) => cu1.Name.CompareTo(cu2.Name));
        }
    }
}