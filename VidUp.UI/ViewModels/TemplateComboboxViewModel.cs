using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.EventAggregation;

namespace Drexel.VidUp.UI.ViewModels
{

    public class TemplateComboboxViewModel : INotifyPropertyChanged, IDisposable
    {
        private Template template;
        private Subscription templateDisplayPropertyChangedSubscription;
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

        public string NameWithDefaultIndicator
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

        public string Name
        {
            get
            {
                if (this.template != null)
                {
                    return this.template.Name;
                }

                return null;
            }
        }

        public TemplateComboboxViewModel(Template template)
        {
            this.template = template;
            this.templateDisplayPropertyChangedSubscription =
                EventAggregator.Instance.Subscribe<TemplateDisplayPropertyChangedMessage>(this.onTemplateDisplayPropertyChanged);
        }

        private void onTemplateDisplayPropertyChanged(TemplateDisplayPropertyChangedMessage templateDisplayPropertyChangedMessage)
        {
            this.raisePropertyChanged("Name");
            this.raisePropertyChanged("NameWithDefaultIndicator");
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

        public void Dispose()
        {
            if (this.templateDisplayPropertyChangedSubscription != null)
            {
                this.templateDisplayPropertyChangedSubscription.Dispose();
            }
        }
    }
}
