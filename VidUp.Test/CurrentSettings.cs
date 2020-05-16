#region

using System;
using System.IO;

#endregion

namespace Drexel.VidUp.Test
{
    class CurrentSettings
    {
        private static string templateImageFolder;
        private static string thumbnailFallbackImageFolder;
        private static string storageFolder;
        public static string StorageFolder
        {
            get
            {
                return CurrentSettings.storageFolder;
            }
            set
            {
                CurrentSettings.storageFolder = value;
                CurrentSettings.templateImageFolder = Path.Combine(CurrentSettings.storageFolder, "TemplateImages");
                CurrentSettings.thumbnailFallbackImageFolder = Path.Combine(CurrentSettings.storageFolder, "FallbackThumbnailImages");
            }
        }
        public static string TemplateImageFolder { get => CurrentSettings.templateImageFolder; }
        public static string ThumbnailFallbackImageFolder { get => CurrentSettings.thumbnailFallbackImageFolder; }
        static CurrentSettings()
        {
            CurrentSettings.StorageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", BaseSettings.UserSuffix));
            CurrentSettings.templateImageFolder = Path.Combine(CurrentSettings.storageFolder, "TemplateImages");
            CurrentSettings.thumbnailFallbackImageFolder = Path.Combine(CurrentSettings.storageFolder, "FallbackThumbnailImages");
        }
    }
}