using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Drexel.VidUp.UI.ViewModels
{
    public class MonthDayViewModel : INotifyPropertyChanged
    {
        public event PropertyChangedEventHandler PropertyChanged;

        private int day;
        private bool active;

        public int Day
        {
            get => this.day;
        }

        public bool Active
        {
            get => active;
            set
            {
                active = value;
                this.raisePropertyChanged("Active");
            }
        }

        public MonthDayViewModel(int day)
        {
            this.day = day;
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
