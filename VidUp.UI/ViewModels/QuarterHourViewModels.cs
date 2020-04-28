using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using Drexel.VidUp.UI.Definitions;

namespace Drexel.VidUp.UI.ViewModels
{
    public class QuarterHourViewModels : IEnumerable<QuarterHourViewModel>
    {
        private List<QuarterHourViewModel> quaterHourViewModels;

        public QuarterHourViewModels()
        {
            this.quaterHourViewModels = new List<QuarterHourViewModel>(96);
            foreach (DateTime quarterHour in QuarterHoursSource.QuarterHours)
            {
                this.quaterHourViewModels.Add(new QuarterHourViewModel(quarterHour));
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

        public QuarterHourViewModel GetQuarterHourViewModel(DateTime publishAt)
        {
            DateTime searchedQuarterHour = new DateTime(1, 1, 1, publishAt.Hour, publishAt.Minute, publishAt.Second);
            return this.quaterHourViewModels.Find(quarterHourViewModel => quarterHourViewModel.QuarterHour == searchedQuarterHour);
        }

        internal QuarterHourViewModel GetQuarterHourViewModel(object defaultPublishAt)
        {
            throw new NotImplementedException();
        }
    }
}
