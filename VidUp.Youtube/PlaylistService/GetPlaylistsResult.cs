using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.PlaylistService
{
    public class GetPlaylistsResult
    {
        public List<PlaylistApi> Playlists = new List<PlaylistApi>();
        public StatusInformation StatusInformation;
    }
}
