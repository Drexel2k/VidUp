using System;
using System.Collections.Generic;
using System.Globalization;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.UI.Definitions
{
    public static class CulturesSource
    {
        public static List<CultureInfo> AllCultureInfos { get; }
        public static List<CultureInfo> RelevantCultureInfos { get; private set; }
        static CulturesSource()
        {
            CulturesSource.AllCultureInfos = new List<CultureInfo>(CultureInfo.GetCultures(CultureTypes.SpecificCultures));
            CulturesSource.AllCultureInfos.Sort((cu1, cu2) => cu1.Name.CompareTo(cu2.Name));

            CulturesSource.RelevantCultureInfos = new List<CultureInfo>();
            CulturesSource.SetRelevantCultures();
        }

        public static void SetRelevantCultures()
        {
            CulturesSource.RelevantCultureInfos.Clear();
            if (Settings.Instance.UserSettings.VideoLanguagesFilter == null || Settings.Instance.UserSettings.VideoLanguagesFilter.Count <= 0)
            {
                CulturesSource.RelevantCultureInfos.AddRange(CultureInfo.GetCultures(CultureTypes.SpecificCultures));

            }
            else
            {
                for (int i = 0; i < Settings.Instance.UserSettings.VideoLanguagesFilter.Count; i++)
                {
                    CulturesSource.RelevantCultureInfos.Add(CultureInfo.GetCultureInfo(Settings.Instance.UserSettings.VideoLanguagesFilter[i]));
                }
            }

            CulturesSource.RelevantCultureInfos.Sort((cu1, cu2) => cu1.Name.CompareTo(cu2.Name));
        }
    }
}