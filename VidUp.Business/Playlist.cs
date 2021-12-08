using System;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class Playlist
    {
        [JsonProperty]
        private string playlistId;
        [JsonProperty]
        private string title;
        [JsonProperty]
        private DateTime created;
        [JsonProperty]
        private DateTime lastModified;
        [JsonProperty]
        private bool notExistsOnYoutube;
        [JsonProperty]
        private YoutubeAccount youtubeAccount;

        private DateTime lastModifiedInternal
        {
            set
            {
                this.lastModified = value;
            }
        }

        public string PlaylistId
        {
            get => this.playlistId;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                {
                    throw new ArgumentException("playlistId must not be null or white space.");
                }
                this.playlistId = value;
                this.lastModifiedInternal = DateTime.Now;
            }
        }

        public string Title
        {
            get => this.title;
            set
            {
                this.title = value;
                this.lastModifiedInternal = DateTime.Now;
            }
        }

        public DateTime Created { get => this.created; }
        public DateTime LastModified { get => this.lastModified; }

        public bool NotExistsOnYoutube
        {
            get => this.notExistsOnYoutube;
            set
            {
                this.notExistsOnYoutube = value;
                this.lastModifiedInternal = DateTime.Now;
            }
        }
        public YoutubeAccount YoutubeAccount
        {
            get => this.youtubeAccount;
        }

        [JsonConstructor]
        private Playlist()
        {

        }

        public Playlist(string playlistId, string title, YoutubeAccount youtubeAccount)
        {
            if (string.IsNullOrWhiteSpace(playlistId) || youtubeAccount == null)
            {
                throw new ArgumentException("playlistId or youtubeAccount must not be null or white space.");
            }

            this.playlistId = playlistId;
            this.title = title;
            this.youtubeAccount = youtubeAccount;
            this.created = DateTime.Now;
            this.lastModified = this.created;
        }
    }
}
