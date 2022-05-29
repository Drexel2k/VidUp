using Drexel.VidUp.Business;
using System;
using System.Collections.Generic;
using System.Windows.Media;

namespace Drexel.VidUp.UI.ViewModels
{
    public class DisplayScheduleControlDayViewModel
    {
        public int Day { get; set; }

        public string DayInfo
        {
            get => this.Day > 0 ? $"{this.Day}" : "-";
        }
        public List<DisplayScheduleColorQuarterHourViewModel> DisplayScheduleColorQuarterHourViewModels { get; set; }
    }

}