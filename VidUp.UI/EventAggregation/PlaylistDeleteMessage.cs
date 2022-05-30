using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class PlaylistDeleteMessage
    {
        public Playlist Playlist { get; }

        public PlaylistDeleteMessage(Playlist playlist)
        {
            this.Playlist = playlist;
        }
    }
}
