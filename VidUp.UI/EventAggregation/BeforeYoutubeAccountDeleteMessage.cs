using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class BeforeYoutubeAccountDeleteMessage
    {
        public YoutubeAccount AccountToBeDeleted { get; }

        public BeforeYoutubeAccountDeleteMessage(YoutubeAccount accountToBeDeleted)
        {
            this.AccountToBeDeleted = accountToBeDeleted;
        }
    }
}
