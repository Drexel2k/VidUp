using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MonthRelativeCombinationViewModel
    {
        private MonthRelativeCombination monthRelativeCombination;

        public DayPosition DayPosition
        {
            get => this.monthRelativeCombination.DayPosition;
        }

        public DayOfWeek DayOfWeek
        {
            get => this.monthRelativeCombination.DayOfWeek;
        }

        public string MonthRelativeCombinationString
        {
            get => $"{this.monthRelativeCombination.DayPosition} {this.monthRelativeCombination.DayOfWeek}";
        }

        public MonthRelativeCombinationViewModel(MonthRelativeCombination monthRelativeCombination)
        {
            this.monthRelativeCombination = monthRelativeCombination;
        }
    }
}
