using System;
using System.ComponentModel;
using System.IO;
using Drexel.VidUp.Utils;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class YoutubeAccount : INotifyPropertyChanged
    {
        [JsonProperty]
        private Guid guid;
        [JsonProperty]
        private string name;
        [JsonProperty]
        private string refreshToken;

        private string filePath;
        private bool isDummy = false;
        private Func<string> getAccountName;
        
        public Guid Guid
        {
            get => this.guid;
        }

        public string RefreshToken
        {
            get => this.refreshToken;
            set => this.refreshToken = value;
        }

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
                this.name = value;
                this.raisePropertyChanged("Name");
            }
        }

        public string DummyNameFlex
        {
            get => this.getAccountName();
        }

        public bool IsDummy
        {
            get => this.isDummy;
        }

        public event PropertyChangedEventHandler PropertyChanged;

        [JsonConstructor]
        public YoutubeAccount()
        {
        }

        public YoutubeAccount(string name, bool isDummy)
        {
            this.name = name;
            this.isDummy = isDummy;
        }

        public YoutubeAccount(string filePath, string name)
        {
            this.guid = Guid.NewGuid();
            this.filePath = filePath;
            this.name = name;
        }

        //for dummy templates with changing account info
        public YoutubeAccount(string name, Func<string> getaccountName)
        {
            this.filePath = null;
            this.name = name;
            this.getAccountName = getaccountName;
            this.isDummy = true;
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
