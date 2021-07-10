using System;
using System.IO;

namespace Drexel.VidUp.Utils
{
    public class Settings
    {
        private string user;
        public string subFolder;
        private string storageFolder;
        private string templateImageFolder;
        private string thumbnailFallbackImageFolder;

        private UserSettings userSettings;

        public static Settings Instance { get; set; }
        public string User { get => this.user; }
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
        public Settings(string user, string subFolder)
        {
            this.user = string.IsNullOrWhiteSpace(user) ? string.Empty : user;
            this.subFolder = string.IsNullOrWhiteSpace(subFolder) ? string.Empty : subFolder;

            this.storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", this.user), this.subFolder);
            this.templateImageFolder = Path.Combine(this.StorageFolder, "TemplateImages");
            this.thumbnailFallbackImageFolder = Path.Combine(this.StorageFolder, "FallbackThumbnailImages");
            this.userSettings = new UserSettings();
        }
    }
}
