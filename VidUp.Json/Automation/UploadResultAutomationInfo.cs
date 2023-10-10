using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace Drexel.VidUp.Utils
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class UploadResultAutomationInfo
    {
        [JsonProperty]
        private string templateName;

        [JsonProperty]
        private Dictionary<string,string> uploadedFiles = new Dictionary<string, string>();

        public Dictionary<string, string> UploadedFiles
        {
            get => this.uploadedFiles;
        }

        public string TemplateName
        {
            set => this.templateName = value;
        }

        [JsonConstructor]
        public UploadResultAutomationInfo()
        {

        }
    }
}