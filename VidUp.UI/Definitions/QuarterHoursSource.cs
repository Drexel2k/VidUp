#region

using System;
using System.Collections.Generic;

#endregion

namespace Drexel.VidUp.UI.Definitions
{
    public class QuarterHoursSource
    {
        private static List<DateTime> quarterHours;

        public static List<DateTime> QuarterHours
        {
            get
            {
                return QuarterHoursSource.quarterHours;
            }
        }

        static QuarterHoursSource()
        {
            QuarterHoursSource.quarterHours = new List<DateTime>(96);
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 0, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 0, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 0, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 0, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 1, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 1, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 1, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 1, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 2, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 2, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 2, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 2, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 3, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 3, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 3, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 3, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 4, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 4, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 4, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 4, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 5, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 5, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 5, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 5, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 6, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 6, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 6, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 6, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 7, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 7, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 7, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 7, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 8, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 8, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 8, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 8, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 9, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 9, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 9, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 9, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 10, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 10, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 10, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 10, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 11, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 11, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 11, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 11, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 12, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 12, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 12, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 12, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 13, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 13, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 13, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 13, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 14, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 14, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 14, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 14, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 15, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 15, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 15, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 15, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 16, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 16, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 16, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 16, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 17, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 17, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 17, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 17, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 18, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 18, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 18, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 18, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 19, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 19, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 19, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 19, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 20, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 20, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 20, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 20, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 21, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 21, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 21, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 21, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 22, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 22, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 22, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 22, 45, 0));

            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 23, 0, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 23, 15, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 23, 30, 0));
            QuarterHoursSource.quarterHours.Add(new DateTime(1, 1, 1, 23, 45, 0));
        }
    }
}
