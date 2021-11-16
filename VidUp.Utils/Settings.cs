using System;
using System.IO;

namespace Drexel.VidUp.Utils
{
    public class Settings
    {
        private string folderSuffix;
        public string subFolder;
        private string storageFolder;
        private string templateImageFolder;
        private string thumbnailFallbackImageFolder;

        private UserSettings userSettings;

        public static Settings Instance { get; set; }
        public string FolderSuffix { get => this.folderSuffix; }
        public string StorageFolder { get => this.storageFolder; }
        public string TemplateImageFolder { get => this.templateImageFolder;  }
        public string ThumbnailFallbackImageFolder { get => this.thumbnailFallbackImageFolder;  }

        public UserSettings UserSettings
        {
            get => this.userSettings;
            set
            {
                this.userSettings = value;
            }
        }

        //for testing purposes
        public Settings(string folderSuffix, string subFolder)
        {
            this.folderSuffix = string.IsNullOrWhiteSpace(folderSuffix) ? string.Empty : folderSuffix;
            this.subFolder = string.IsNullOrWhiteSpace(subFolder) ? string.Empty : subFolder;

            this.storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", this.folderSuffix), this.subFolder);
            this.templateImageFolder = Path.Combine(this.StorageFolder, "TemplateImages");
            this.thumbnailFallbackImageFolder = Path.Combine(this.StorageFolder, "FallbackThumbnailImages");
            this.userSettings = new UserSettings();
        }
    }
}
