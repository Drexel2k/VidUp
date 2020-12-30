using System;
using Newtonsoft.Json;

namespace Drexel.VidUp.Utils
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UserSettings
    {
        [JsonProperty]
        private bool trace;

        [JsonProperty]
        private bool autoSetPlaylists;

        [JsonProperty]
        private DateTime lastAutoAddToPlaylist;

        public bool Trace
        {
            get => this.trace;
        }

        public bool AutoSetPlaylists
        {
            get => this.autoSetPlaylists;
            set
            {
                this.autoSetPlaylists = value;
            }
        }

        public DateTime LastAutoAddToPlaylist
        {
            get => this.lastAutoAddToPlaylist;
            set
            {
                this.lastAutoAddToPlaylist = value;
            }
        }

        [JsonConstructor]
        public UserSettings()
        {

        }
    }
}