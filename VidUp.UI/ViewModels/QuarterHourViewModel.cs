#region

using System;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    public class QuarterHourViewModel
    {
        private TimeSpan? quarterHour;

        public QuarterHourViewModel(TimeSpan? quarterHour)
        {
            this.quarterHour = quarterHour;
        }

        public string QuarterHourAs24hString
        {
            get
            {
                return this.quarterHour != null ? this.quarterHour.Value.ToString(@"hh\:mm") : string.Empty;
            }
        }

        public TimeSpan? QuarterHour
        {
            get
            {
                return this.quarterHour;
            }
        }
    }
}
