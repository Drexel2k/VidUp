using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class SelectedTemplateChangedMessage
    {
        public Template OldTemplate { get; }
        public Template NewTemplate { get; }

        public SelectedTemplateChangedMessage(Template oldTemplate, Template newTemplate)
        {
            this.OldTemplate = oldTemplate;
            this.NewTemplate = newTemplate;
        }
    }
}
