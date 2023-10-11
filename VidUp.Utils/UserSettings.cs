using System;
using System.Collections.Generic;
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

        [JsonProperty]
        private bool useCustomYouTubeApiCredentials;

        [JsonProperty]
        private string clientId;

        [JsonProperty]
        private string clientSecret;

        [JsonProperty]
        private int networkPackageSizeInBytes;

        [JsonProperty] 
        private bool keepLastUploadPerTemplate;

        [JsonProperty]
        private bool keepLastUploadPerTemplateSide;

        [JsonProperty]
        private int automationDelayInSeconds;

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

        public bool UseCustomYouTubeApiCredentials
        {
            get => this.useCustomYouTubeApiCredentials;
            set => this.useCustomYouTubeApiCredentials = value;
        }

        public string ClientId
        {
            get => this.clientId;
            set => this.clientId = value;
        }

        public string ClientSecret
        {
            get => this.clientSecret;
            set => this.clientSecret = value;
        }

        public int NetworkPackageSizeInBytes
        {
            get => this.networkPackageSizeInBytes;
            set => this.networkPackageSizeInBytes = value;
        }

        public bool KeepLastUploadPerTemplate
        {
            get => this.keepLastUploadPerTemplate;
            set => this.keepLastUploadPerTemplate = value;
        }

        public bool KeepLastUploadPerTemplateSide
        {
            get => this.keepLastUploadPerTemplateSide;
            set => this.keepLastUploadPerTemplateSide = value;
        }
        public int AutomationExecutionDelayInSeconds
        {
            get => this.automationDelayInSeconds;
            set => this.automationDelayInSeconds = value;
        }

        [JsonConstructor]
        public UserSettings()
        {

        }
    }
}