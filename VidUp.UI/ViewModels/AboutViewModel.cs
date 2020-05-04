using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows.Forms;

namespace Drexel.VidUp.UI.ViewModels
{
    class AboutViewModel : INotifyPropertyChanged
    {

        public string Version
        {
            get
            {
                return Assembly.GetExecutingAssembly().GetName().Version.ToString(); ;
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