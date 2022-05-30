using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class TemplateCopyMessage
    {
        public Template Template { get; }

        public TemplateCopyMessage(Template template)
        {
            this.Template = template;
        }
    }
}
