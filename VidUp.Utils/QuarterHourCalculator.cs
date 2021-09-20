using System;

namespace Drexel.VidUp.Utils
{
    public class QuarterHourCalculator
    {
        public static DateTime GetRoundedToNextQuarterHour(DateTime dateTime)
        {
            //if minutes are no quarter hour, round up
            if (dateTime.Minute % 15 != 0)
            {
                int minutes = dateTime.Minute / 15 * 15 + 15;
                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, 0, 0);
                //if minutes are 60, DateTime initialization will fail.
                dateTime = dateTime.AddMinutes(minutes);
            }

            return dateTime;
        }
    }
}
