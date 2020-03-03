using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.UI
{
    class Settings
    {
        public static string SerializationFolder { get; }
        public static string UserSuffix { get; }

        static Settings()
        {
            Settings.SerializationFolder = string.Format("{0}\\VidUp", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments));
            //Settings.UserSuffix = "Dev";
            Settings.UserSuffix = string.Empty;
        }
    }
}
