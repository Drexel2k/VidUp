using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI
{
    //Only for visualization purposes
    
    public class TemplateListSimple : TemplateListBase
    {
        public TemplateListSimple(List<Template> templates)
        {
            this.templates = templates;
        }

        public override void AddTemplate(Template template)
        {
            this.templates.Add(template);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, template));
        }

        public override void AddTemplates(List<Template> templates)
        {
            this.templates.AddRange(templates);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, templates));
        }

        public override void Delete(Template template)
        {
            this.templates.Remove(template);
            this.raiseNotifyPropertyChanged("TemplateCount");
            //template is removed from uploads in event listener in MainWindowViewModel.templateListCollectionChanged
            //todo: Move to event aggregator

            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, template));
        }
    }
}
