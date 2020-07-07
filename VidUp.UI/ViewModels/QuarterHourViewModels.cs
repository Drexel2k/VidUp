#region

using System;
using System.Collections;
using System.Collections.Generic;
using Drexel.VidUp.UI.Definitions;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    public class QuarterHourViewModels : IEnumerable<QuarterHourViewModel>
    {
        private List<QuarterHourViewModel> quaterHourViewModels;

        public QuarterHourViewModels(bool emptyValueAtBeginning)
        {
            if (emptyValueAtBeginning)
            {
                this.quaterHourViewModels = QuarterHoursSource.QuarterHoursEmptyStartValue;
            }
            else
            {
                this.quaterHourViewModels = QuarterHoursSource.QuarterHours;
            }
        }

        public IEnumerator<QuarterHourViewModel> GetEnumerator()
        {
            return this.quaterHourViewModels.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return this.GetEnumerator();
        }

        public QuarterHourViewModel GetQuarterHourViewModel(TimeSpan? timeSpan)
        {
            if (timeSpan == null)
            {
                if (this.quaterHourViewModels.Count < 97)
                {
                    throw new ArgumentException("No empty quarter hour videw model in collection.");
                }

                return this.quaterHourViewModels.Find(quarterHourViewModel => quarterHourViewModel.QuarterHour == null);
            }
            else
            {
                TimeSpan searchedQuarterHour = new TimeSpan(timeSpan.Value.Hours, timeSpan.Value.Minutes, timeSpan.Value.Seconds);
                return this.quaterHourViewModels.Find(quarterHourViewModel => quarterHourViewModel.QuarterHour == searchedQuarterHour);
            }
        }
    }
}
