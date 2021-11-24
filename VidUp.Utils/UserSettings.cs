using System;
using System.Collections.Generic;
using System.ComponentModel;
using Newtonsoft.Json;

namespace Drexel.VidUp.Utils
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UserSettings
    {
        [JsonProperty]
        private bool trace;

        [JsonProperty]
        private TraceLevel traceLevel;

        [JsonProperty]
        private bool autoSetPlaylists;

        [JsonProperty]
        private DateTime lastAutoAddToPlaylist;

        [JsonProperty]
        private int? windowWidth;

        [JsonProperty]
        private int? windowHeight;

        [JsonProperty]
        private int? windowLeft;

        [JsonProperty]
        private int? windowTop;

        [JsonProperty]
        private List<string> videoLanguagesFilter;

        [JsonProperty]
        private string postponePostUploadActionProcessName;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        private string clientId;

        [JsonProperty(NullValueHandling = NullValueHandling.Ignore)]
        private string clientSecret;


        public string ClientId
        {
            get => this.clientId;
        }

        public string ClientSecret
        {
            get => this.clientSecret;
        }

        public bool Trace
        {
            get => this.trace;
            set => this.trace = value;
        }

        public TraceLevel TraceLevel
        {
            get => this.traceLevel;
            set => this.traceLevel = value;
        }

        public bool AutoSetPlaylists
        {
            get => this.autoSetPlaylists;
            set => this.autoSetPlaylists = value;
        }

        public DateTime LastAutoAddToPlaylist
        {
            get => this.lastAutoAddToPlaylist;
            set => this.lastAutoAddToPlaylist = value;
        }

        public int? WindowWidth
        {
            get => this.windowWidth;
            set => this.windowWidth = value;
        }

        public int? WindowHeight
        {
            get => this.windowHeight;
            set => this.windowHeight = value;
        }

        public int? WindowLeft
        {
            get => this.windowLeft;
            set => this.windowLeft = value;
        }

        public int? WindowTop
        {
            get => this.windowTop;
            set => this.windowTop = value;
        }

        public List<string> VideoLanguagesFilter
        {
            get => this.videoLanguagesFilter;
            set => this.videoLanguagesFilter = value;
        }

        public string PostponePostUploadActionProcessName
        {
            get => this.postponePostUploadActionProcessName;
            set => this.postponePostUploadActionProcessName = value;
        }

        [JsonConstructor]
        public UserSettings()
        {

        }
    }
}