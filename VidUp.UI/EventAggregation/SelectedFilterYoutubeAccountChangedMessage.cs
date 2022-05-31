using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class SelectedFilterYoutubeAccountChangedMessage
    {
        public YoutubeAccount OldYoutubeAccount { get; }
        public YoutubeAccount NewYoutubeAccount { get; }

        public YoutubeAccount FirstNotAllYoutubeAccount { get; }

        public SelectedFilterYoutubeAccountChangedMessage(YoutubeAccount oldYoutubeAccount, YoutubeAccount newYoutubeAccount, YoutubeAccount firstNotAllYoutubeAccount)
        {
            this.OldYoutubeAccount = oldYoutubeAccount;
            this.NewYoutubeAccount = newYoutubeAccount;
            this.FirstNotAllYoutubeAccount = firstNotAllYoutubeAccount;
        }
    }
}
