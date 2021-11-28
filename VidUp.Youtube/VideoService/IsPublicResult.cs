using System.Collections.Generic;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.Youtube.VideoService
{ public class IsPublicResult
    {
        public Dictionary<string, bool> IsPublicByVideoId = new Dictionary<string, bool>();
        public StatusInformation StatusInformation;
    }
}
