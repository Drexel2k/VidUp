#region

using System;
using System.Collections.Generic;
using Drexel.VidUp.UI.ViewModels;

#endregion

namespace Drexel.VidUp.UI.Definitions
{
    public class QuarterHoursSource
    {
        private static List<QuarterHourViewModel> quarterHours;
        private static List<QuarterHourViewModel> quarterHoursEmptyStartValue ;

        public static List<QuarterHourViewModel> QuarterHours
        {
            get
            {
                return QuarterHoursSource.quarterHours;
            }
        }

        public static List<QuarterHourViewModel> QuarterHoursEmptyStartValue
        {
            get
            {
                return QuarterHoursSource.quarterHoursEmptyStartValue;
            }
        }

        static QuarterHoursSource()
        {
            QuarterHoursSource.quarterHours = new List<QuarterHourViewModel>(96);
            QuarterHoursSource.quarterHoursEmptyStartValue = new List<QuarterHourViewModel>(97);
            QuarterHoursSource.quarterHoursEmptyStartValue.Add(new QuarterHourViewModel(null));

            TimeSpan currentTimeSpan = new TimeSpan(0, 0, 0);
            TimeSpan targetTimeSpan = new TimeSpan(23, 45, 0);
            TimeSpan quarterHour = new TimeSpan(0, 15, 0);
            while (currentTimeSpan <= targetTimeSpan)
            {
                QuarterHoursSource.quarterHours.Add(new QuarterHourViewModel(currentTimeSpan));
                QuarterHoursSource.quarterHoursEmptyStartValue.Add(new QuarterHourViewModel(currentTimeSpan));
                currentTimeSpan = currentTimeSpan.Add(quarterHour);
            }
        }
    }
}
