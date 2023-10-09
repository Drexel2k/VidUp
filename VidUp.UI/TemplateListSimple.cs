using System.Collections.Generic;
using System.Collections.Specialized;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI
{
    //Only for visualization purposes in combobox controls e.g.
    //Must only be used for cases whith own instances of template lists (currently only for templates by account)
    //where now additional checks like deleting of images is necessary or wanted on adding or deleting templates,
    //adding or deleting templates in general must be done in the main TemplateList

    public class TemplateListSimple : TemplateListBase
    {
        public TemplateListSimple(List<Template> templates)
        {
            this.templates = templates;
        }

        public override void AddTemplates(Template[] templates)
        {
            this.templates.AddRange(templates);

            this.raiseNotifyPropertyChanged("TemplateCount");
            this.raiseNotifyCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, templates));
        }

        //this removes only the templates from the template list copy, e.g. for the templates by account
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
