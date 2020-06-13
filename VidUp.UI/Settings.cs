using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Drexel.VidUp.UI
{
    class Settings
    {
        public static string UserSuffix { get; set; }
        public static string StorageFolder { get; set; }
        public static string TemplateImageFolder { get; set; }
        public static string ThumbnailFallbackImageFolder { get; set; }
        static Settings()
        {
            Settings.UserSuffix = "Dev";
            //Settings.UserSuffix = string.Empty;

            Settings.StorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", Settings.UserSuffix));
            Settings.TemplateImageFolder = Path.Combine(Settings.StorageFolder, "TemplateImages");
            Settings.ThumbnailFallbackImageFolder = Path.Combine(Settings.StorageFolder, "FallbackThumbnailImages");
        }
    }
}
