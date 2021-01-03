using System;
using System.IO;

namespace Drexel.VidUp.Test
{
    public static class BaseSettings
    {
        public static string UserSuffix { get; }

        public static string SubFolder { get; set; }

        public static string StorageFolder
        {
            get => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments),
                $"VidUp{BaseSettings.UserSuffix}", BaseSettings.SubFolder);
        }

        public static string TemplateImageFolder
        {
            get => Path.Combine(BaseSettings.StorageFolder, "TemplateImages");
        }
        
        public static string ThumbnailFallbackImageFolder
        {
            get => Path.Combine(BaseSettings.StorageFolder, "FallbackThumbnailImages");
        }

        static BaseSettings()
        {
            BaseSettings.UserSuffix = "Test";
            BaseSettings.SubFolder = string.Empty;
        }
    }
}