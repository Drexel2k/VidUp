using System;
using System.Windows.Media;

namespace Drexel.VidUp.UI.ViewModels
{
    public struct DisplayScheduleColorQuarterHourViewModel
    {
        public Color Color { get; set; }
        public TimeSpan QuarterHour { get; set; }

        public bool Empty { get; set; }

        public string QuarterHourAs24hString
        {
            get
            {
                if (this.Empty)
                {
                    return "-";
                }
                else
                {
                    return this.QuarterHour.ToString(@"hh\:mm");
                }
            }
        }

        public SolidColorBrush BackgroundSolidColorBrush
        {
            get => new SolidColorBrush(this.Color);
        }
    }
}
