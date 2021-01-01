
using System.Collections.Generic;
using System.Globalization;

namespace Drexel.VidUp.Business
{
    public static class Cultures
    {
        public static CultureInfo[] CultureInfos { get; }
        static Cultures()
        {
            Cultures.CultureInfos = CultureInfo.GetCultures(CultureTypes.SpecificCultures);
        }
    }
}