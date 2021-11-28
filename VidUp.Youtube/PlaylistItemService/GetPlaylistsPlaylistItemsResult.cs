using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.PlaylistItemService
{
    public class GetPlaylistsPlaylistItemsResult
    {
        public Dictionary<Playlist, List<string>> PlaylistItemsByPlaylist = new Dictionary<Playlist, List<string>>();
        public StatusInformation StatusInformation;
    }
}
