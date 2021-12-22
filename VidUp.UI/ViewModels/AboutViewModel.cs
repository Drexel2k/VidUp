using System;
using System.ComponentModel;
using System.IO;
using System.Reflection;

namespace Drexel.VidUp.UI.ViewModels
{
    class AboutViewModel : INotifyPropertyChanged
    {
        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString(3);
            }
        }

        public Uri LicenseUri
        {
            get
            {
                string s = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), "license.rtf");
                Uri uri = new Uri(s);
                return uri;
            }
        }


        public event PropertyChangedEventHandler PropertyChanged;

        public AboutViewModel()
        {
        }

        private void RaisePropertyChanged(string propertyName)
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