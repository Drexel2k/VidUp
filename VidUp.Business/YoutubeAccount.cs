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
        [JsonProperty]
        private string refreshTokenCustomApiCredentials;

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

        public string RefreshTokenCustomApiCredentials
        {
            get => this.refreshTokenCustomApiCredentials;
            set => this.refreshTokenCustomApiCredentials = value;
        }

        public string ActiveRefreshToken
        {
            get
            {
                if (Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials)
                {
                    return this.refreshTokenCustomApiCredentials;
                }

                return this.refreshToken;
            }
        }

        public bool IsAuthenticated
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.ActiveRefreshToken))
                {
                    return false;
                }

                return true;
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

        public YoutubeAccount(string name)
        {
            this.guid = Guid.NewGuid();
            this.name = name;
        }

        //for dummy templates with changing account info
        public YoutubeAccount(string name, Func<string> getaccountName)
        {
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
