using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;

namespace Drexel.VidUp.UI.ViewModels
{
    public class CultureViewModel : INotifyPropertyChanged
    {
        private CultureInfo cultureInfo;
        private bool isChecked;

        public event PropertyChangedEventHandler PropertyChanged;

        #region properties
        public string DisplayName
        {
            get => $"{this.cultureInfo.Name} - {this.cultureInfo.EnglishName}";
        }

        public string Name
        {
            get => this.cultureInfo.Name;
        }

        public bool IsChecked
        {
            get => this.isChecked;
            set
            {
                this.isChecked = value;
                this.raisePropertyChanged("IsChecked");
            }
        }
        #endregion properties

        public CultureViewModel(CultureInfo cultureInfo, bool isChecked)
        {
            if (cultureInfo == null)
            {
                throw new ArgumentException("CultureInfo is null.");
            }

            this.cultureInfo = cultureInfo;
            this.isChecked = isChecked;
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