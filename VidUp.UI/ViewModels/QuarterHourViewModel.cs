using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.UI.ViewModels
{
    public class QuarterHourViewModel
    {
        private DateTime quarterHour;

        public QuarterHourViewModel(DateTime quarterHour)
        {
            this.quarterHour = quarterHour;
        }

        public string QuarterHourAs24hString
        {
            get
            {
                return this.quarterHour.ToString("HH:mm");
            }
        }

        public DateTime QuarterHour
        {
            get
            {
                return this.quarterHour;
            }
        }
    }
}
