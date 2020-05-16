#region

using System.Collections.Generic;
using Drexel.VidUp.Business;

#endregion

namespace Drexel.VidUp.JSON
{
    public class DeSerializationRepository
    {
        public static List<Upload> AllUploads { get; set; }
        public static List<Template> Templates { get; set; }
        public static UploadList UploadList { get; set; }
        public static void ClearRepositories()
        {
            AllUploads = null;
            Templates = null;
            UploadList = null;
        }
    }
}
