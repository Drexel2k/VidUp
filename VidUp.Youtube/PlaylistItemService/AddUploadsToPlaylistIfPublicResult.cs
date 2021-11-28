using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.PlaylistItemService
{
    public class AddUploadsToPlaylistIfPublicResult
    {
        public Dictionary<Playlist, List<Upload>> UploadsNotAddedByPlaylist = new Dictionary<Playlist, List<Upload>>();
        public Dictionary<Playlist, List<Upload>> UploadsAddedByPlaylist = new Dictionary<Playlist, List<Upload>>();
        public StatusInformation StatusInformation;
    }
}
