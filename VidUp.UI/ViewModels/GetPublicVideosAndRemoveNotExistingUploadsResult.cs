using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    public class GetPublicVideosAndRemoveNotExistingUploadsResult
    {
        public Dictionary<string, bool> PublicByVideo = new Dictionary<string, bool>();
        public Dictionary<Playlist, List<Upload>> UploadsNotExistOnYouTubeByPlaylist = new Dictionary<Playlist, List<Upload>>();
        public StatusInformation StatusInformation;
    }
}
