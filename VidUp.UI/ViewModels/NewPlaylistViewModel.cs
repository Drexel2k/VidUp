#region

using System.ComponentModel;
using System.Windows.Forms;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{
    class NewPlaylistViewModel : INotifyPropertyChanged
    {
        private string name;
        private string playlistId;
        private bool formVaild = false;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    if(!string.IsNullOrWhiteSpace(this.name) && !string.IsNullOrWhiteSpace(this.playlistId))
                    {
                        this.FormValid = true;
                    }
                    else
                    {
                        this.FormValid = false;
                    }

                    this.raisePropertyChanged("Name");
                }
            }
        }
        public string PlaylistId
        {
            get
            {
                return this.playlistId;
            }
            set
            {
                if (this.playlistId != value)
                {
                    this.playlistId = value;
                    if (!string.IsNullOrWhiteSpace(this.playlistId) && !string.IsNullOrWhiteSpace(this.name))
                    {
                        this.FormValid = true;
                    }
                    else
                    {
                        this.FormValid = false;
                    }

                    this.raisePropertyChanged("PlaylistId");
                }
            }
        }

        public bool FormValid
        {
            get => this.formVaild;
            private set
            {
                this.formVaild = value;
                this.raisePropertyChanged("FormValid");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewPlaylistViewModel()
        {
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}