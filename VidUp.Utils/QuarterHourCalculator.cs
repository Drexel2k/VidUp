using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

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
                dateTime = new DateTime(dateTime.Year, dateTime.Month, dateTime.Day, dateTime.Hour, minutes, 0);
            }

            return dateTime;
        }
    }
}
