using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Text;

namespace Drexel.VidUp.UI.ViewModels
{

    public class TemplateComboboxViewModel : INotifyPropertyChanged
    {
        private Template template;
        public event PropertyChangedEventHandler PropertyChanged;

        public Template Template
        {
            get
            {
                return this.template;
            }
        }

        public string Guid
        {
            get => this.template != null ? this.template.Guid.ToString() : string.Empty;
        }
        public string Name
        {
            get
            {
                if (this.template != null)
                {
                    return this.template.IsDefault ? string.Format("{0} *", this.template.Name) : this.template.Name;
                }

                return null;
            }
        }

        public TemplateComboboxViewModel(Template template)
        {
            this.template = template;
        }

        internal void RaiseNameChange()
        {
            this.raisePropertyChanged("Name");
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
