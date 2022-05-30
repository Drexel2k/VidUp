using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class TemplateDeleteMessage
    {
        public Template Template { get; }

        public TemplateDeleteMessage(Template template)
        {
            this.Template = template;
        }
    }
}
