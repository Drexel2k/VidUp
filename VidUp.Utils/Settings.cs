using System;
using System.IO;

namespace Drexel.VidUp.Utils
{
    public class Settings
    {
        private string user;
        private string storageFolder;
        private string templateImageFolder;
        private string thumbnailFallbackImageFolder;

        private UserSettings userSettings;

        public static Settings SettingsInstance { get; set; }
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

        public Settings()
        {
            this.user = string.Empty;
            this.storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", this.user));
            this.templateImageFolder = Path.Combine(this.StorageFolder, "TemplateImages");
            this.thumbnailFallbackImageFolder = Path.Combine(this.StorageFolder, "FallbackThumbnailImages");
            this.userSettings = new UserSettings();
        }

        public void SetNewUser(string user)
        {
            this.user = user;
            this.storageFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), string.Format("VidUp{0}", this.user));
            this.templateImageFolder = Path.Combine(this.StorageFolder, "TemplateImages");
            this.thumbnailFallbackImageFolder = Path.Combine(this.StorageFolder, "FallbackThumbnailImages");
        }
    }
}
