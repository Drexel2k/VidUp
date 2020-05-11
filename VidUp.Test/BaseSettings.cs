using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Drexel.VidUp.Test
{
    public static class BaseSettings
    {
        public static string UserSuffix { get; }
        public static string StorageFolder { get; }

        static BaseSettings()
        {
            BaseSettings.UserSuffix = "Test";
            BaseSettings.StorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", BaseSettings.UserSuffix));
        }
    }
}