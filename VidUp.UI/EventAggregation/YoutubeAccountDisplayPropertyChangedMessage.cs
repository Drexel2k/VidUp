namespace Drexel.VidUp.UI.EventAggregation
{
    public class YoutubeAccountDisplayPropertyChangedMessage
    {
        public string Property { get; }

        public YoutubeAccountDisplayPropertyChangedMessage(string property)
        {
            this.Property = property;
        }
    }
}
