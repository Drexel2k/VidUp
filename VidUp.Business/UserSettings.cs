using System;
using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UserSettings
    {
        [JsonProperty]
        private DateTime lastAutoAddToPlaylist;

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