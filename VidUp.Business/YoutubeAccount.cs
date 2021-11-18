using System.ComponentModel;
using System.IO;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.Business
{
    public class YoutubeAccount : INotifyPropertyChanged
    {

        private string filePath;
        private string name;



        public string FilePath
        {
            get => this.filePath;
            set
            {
                this.filePath = value;
                this.raisePropertyChanged("FilePath");
            }
        }

        public string Name
        {
            get => this.name;
            set
            {
                value = string.Concat(value.Split(Path.GetInvalidFileNameChars()));
                string newFilePath = Path.Combine(Settings.Instance.StorageFolder, $"uploadrefreshtoken_{value}");

                if (!string.IsNullOrWhiteSpace(value) && !File.Exists(newFilePath))
                {
                    File.Move(this.filePath, newFilePath);
                    this.name = value;
                    this.filePath = newFilePath;
                    this.raisePropertyChanged("Name");
                    this.raisePropertyChanged("FilePath");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public YoutubeAccount(string filePath, string name)
        {
            this.filePath = filePath;
            this.name = name;
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
