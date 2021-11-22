using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class TemplateYoutubeAccountChangedMessage
    {
        public Template Template { get; }
        public YoutubeAccount OldAccount { get; }
        public YoutubeAccount NewAccount { get; }

        public TemplateYoutubeAccountChangedMessage(Template template, YoutubeAccount oldAccount, YoutubeAccount newAccount)
        {
            this.Template = template;
            this.OldAccount = oldAccount;
            this.NewAccount = newAccount;
        }
    }
}
