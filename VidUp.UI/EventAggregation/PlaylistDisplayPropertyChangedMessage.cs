namespace Drexel.VidUp.UI.EventAggregation
{
    public class PlaylistDisplayPropertyChangedMessage
    {
        public string Property { get; }

        public PlaylistDisplayPropertyChangedMessage(string property)
        {
            this.Property = property;
        }
    }
}
