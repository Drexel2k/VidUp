using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Playlist : INotifyPropertyChanged
    {
        [JsonProperty]
        private string playlistId;
        [JsonProperty]
        private string name;
        [JsonProperty]
        private DateTime created;
        [JsonProperty]
        private DateTime lastModified;

        private DateTime lastModifiedInternal
        {
            set
            {
                this.lastModified = value;
                this.raisePropertyChanged("LastModified");
            }
        }

        public string PlaylistId
        {
            get => this.playlistId;
            set
            {
                this.playlistId = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("PlaylistId");
            }
        }

        public string Name
        {
            get => this.name;
            set
            {
                this.name = value;
                this.lastModifiedInternal = DateTime.Now;
                this.raisePropertyChanged("Name");
            }
        }

        public DateTime Created { get => this.created; }
        public DateTime LastModified { get => this.lastModified; }

        public event PropertyChangedEventHandler PropertyChanged;

        public Playlist(string name, string playlistId)
        {
            this.playlistId = playlistId;
            this.name = name;
            this.created = DateTime.Now;
            this.lastModified = this.created;
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
