using System;
using System.IO;
using Newtonsoft.Json;

namespace Drexel.VidUp.UI
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public static class Settings
    {
        public static string UserSuffix { get; set; }
        public static string StorageFolder { get; set; }
        public static string TemplateImageFolder { get; set; }
        public static string ThumbnailFallbackImageFolder { get; set; }

        [JsonConstructor]
        static Settings()
        {
            Settings.UserSuffix = "";
            Settings.StorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", Settings.UserSuffix));
            Settings.TemplateImageFolder = Path.Combine(Settings.StorageFolder, "TemplateImages");
            Settings.ThumbnailFallbackImageFolder = Path.Combine(Settings.StorageFolder, "FallbackThumbnailImages");
        }

        public static void ReInitializeFolders()
        {
            Settings.StorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", Settings.UserSuffix));
            Settings.TemplateImageFolder = Path.Combine(Settings.StorageFolder, "TemplateImages");
            Settings.ThumbnailFallbackImageFolder = Path.Combine(Settings.StorageFolder, "FallbackThumbnailImages");
        }
    }
}
