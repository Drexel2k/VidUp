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
            get => this.template != null ? this.template.Name : null;
        }

        public TemplateComboboxViewModel(Template template)
        {
            this.template = template;
        }
    }
}
