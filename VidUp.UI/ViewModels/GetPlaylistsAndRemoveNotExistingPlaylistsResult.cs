using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class GetPlaylistsAndRemoveNotExistingPlaylistsResult
    {
        public Dictionary<Playlist, List<string>> PlaylistItemsByPlaylist = new Dictionary<Playlist, List<string>>();
        public List<Playlist> RemovedPlaylists = new List<Playlist>();
        public StatusInformation StatusInformation;
    }
}
