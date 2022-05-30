using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.EventAggregation
{
    public class SelectedPlaylistChangedMessage
    {
        public Playlist OldPlaylist { get; }
        public Playlist NewPlaylist { get; }

        public SelectedPlaylistChangedMessage(Playlist oldPlaylist, Playlist newPlaylist)
        {
            this.OldPlaylist = oldPlaylist;
            this.NewPlaylist = newPlaylist;
        }
    }
}
