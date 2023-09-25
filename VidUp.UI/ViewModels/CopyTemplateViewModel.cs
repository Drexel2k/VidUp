using System;
using System.ComponentModel;

namespace Drexel.VidUp.UI.ViewModels
{
    class CopyTemplateViewModel : INotifyPropertyChanged
    {
        private string name;

        private bool formVaild = true;

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
                    if(!string.IsNullOrWhiteSpace(this.name))
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

        public CopyTemplateViewModel(string orirginalTemplateName, string templateImageFolder)
        {
            if(string.IsNullOrWhiteSpace(orirginalTemplateName))
            {
                throw new ArgumentException("orirginalTemplateName must not be null or empty.");
            }

            this.name = $"Copy of {orirginalTemplateName}";
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