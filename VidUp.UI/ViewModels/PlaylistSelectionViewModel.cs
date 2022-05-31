using System;
using System.ComponentModel;

namespace Drexel.VidUp.UI.ViewModels
{
    public class PlaylistSelectionViewModel : INotifyPropertyChanged
    {
        private string id;
        private string title;
        private bool isChecked;

        public event PropertyChangedEventHandler PropertyChanged;
        
        #region properties

        public string Id { get => this.id; }

        public string Title { get => this.title; }

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

        public PlaylistSelectionViewModel(string id, string title, bool isChecked)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentOutOfRangeException("Id must not be null or empty.");
            }

            this.id = id;
            this.title = title;
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
