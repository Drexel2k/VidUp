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
        private bool visible = true;

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
                    return this.template.IsDefault ? $"{this.template.Name} *" : this.template.Name;
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

        public string NameWithYoutubeAccountName
        {
            get
            {
                if (this.template != null)
                {
                    if (this.template.IsDummy)
                    {
                        return $"{this.template.Name} [{this.template.YoutubeAccount.DummyNameFlex}]";
                    }
                    else
                    {
                        return $"{this.template.Name} [{this.template.YoutubeAccount.Name}]";
                    }
                }

                return string.Empty;
            }
        }

        public string NameWithDefaultIndicatorAndYoutubeAccountName
        {
            get
            {
                if (this.template != null)
                {
                    if (this.template.IsDummy)
                    {
                        return $"{this.template.Name} [{this.template.YoutubeAccount.DummyNameFlex}]";
                    }
                    else
                    {
                        string defaultSign = this.template.IsDefault ? "* " : string.Empty;
                        return this.template != null ? $"{this.template.Name} {defaultSign}[{this.template.YoutubeAccount.Name}]" : string.Empty;
                    }
                }

                return string.Empty;
            }
        }

        public string YoutubeAccountName
        {
            get => this.template != null ? this.template.YoutubeAccount.Name : string.Empty;
        }

        public bool Visible
        {
            get => this.visible;
            set
            {
                this.visible = value;
                this.raisePropertyChanged("Visible");
            }
        }

        public TemplateComboboxViewModel(Template template)
        {
            this.template = template;
            this.templateDisplayPropertyChangedSubscription = EventAggregator.Instance.Subscribe<TemplateDisplayPropertyChangedMessage>(this.onTemplateDisplayPropertyChanged);
        }

        private void onTemplateDisplayPropertyChanged(TemplateDisplayPropertyChangedMessage templateDisplayPropertyChangedMessage)
        {
            this.raisePropertyChanged("Name");
            this.raisePropertyChanged("NameWithDefaultIndicator");
            this.raisePropertyChanged("NameWithYoutubeAccountName");
            this.raisePropertyChanged("NameWithDefaultIndicatorAndYoutubeAccountName");
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
