using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class SelectedYoutubeAccountChangedMessage
    {
        public YoutubeAccount OldAccount { get; }
        public YoutubeAccount NewAccount { get; }

        public YoutubeAccount FirstNotAllAccount { get; }

        public SelectedYoutubeAccountChangedMessage(YoutubeAccount oldAccount, YoutubeAccount newAccount, YoutubeAccount firstNotAllAccount)
        {
            this.OldAccount = oldAccount;
            this.NewAccount = newAccount;
            this.FirstNotAllAccount = firstNotAllAccount;
        }
    }
}
