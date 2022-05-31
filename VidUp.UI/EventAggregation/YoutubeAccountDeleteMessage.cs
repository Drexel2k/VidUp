using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class YoutubeAccountDeleteMessage
    {
        public YoutubeAccount YoutubeAccount { get; }

        public YoutubeAccountDeleteMessage(YoutubeAccount youtubeAccount)
        {
            this.YoutubeAccount = youtubeAccount;
        }
    }
}
