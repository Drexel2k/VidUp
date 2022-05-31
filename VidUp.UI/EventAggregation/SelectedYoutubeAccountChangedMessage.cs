using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class SelectedYoutubeAccountChangedMessage
    {
        public YoutubeAccount OldYoutubeAccount { get; }
        public YoutubeAccount NewYoutubeAccount { get; }

        public SelectedYoutubeAccountChangedMessage(YoutubeAccount oldYoutubeAccount, YoutubeAccount newYoutubeAccount)
        {
            this.OldYoutubeAccount = oldYoutubeAccount;
            this.NewYoutubeAccount = newYoutubeAccount;
        }
    }
}
