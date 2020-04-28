using System;
using System.Collections.Generic;
using System.Text;

namespace Drexel.VidUp.UI
{
    class Settings
    {
        public static string UserSuffix { get; }
        public static string StorageFolder { get; }
        public static string TemplateImageFolder { get; }
        public static string ThumbnailFallbackImageFolder { get; }
        static Settings()
        {
            Settings.UserSuffix = "Dev";
            //Settings.UserSuffix = string.Empty;

            Settings.StorageFolder = string.Format("{0}\\VidUp{1}", Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), Settings.UserSuffix);
            Settings.TemplateImageFolder = string.Format("{0}\\TemplateImages", Settings.StorageFolder);
            Settings.ThumbnailFallbackImageFolder = string.Format("{0}\\FallbackThumbnailImages", Settings.StorageFolder);
        }
    }
}
