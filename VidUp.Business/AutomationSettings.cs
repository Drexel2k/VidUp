using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AutomationSettings
    {
        [JsonProperty]
        private bool addNewFilesAutomatically;
        [JsonProperty]
        private string fileExtensions;
        [JsonProperty]
        private string deviatingFolderPath;
        [JsonProperty]
        private UplStatus addWithStatus;
        [JsonProperty]
        private bool startUploadingAfterAdd;
        [JsonProperty]
        private string executeAfterEachPath;
        [JsonProperty]
        private bool addUploadInfoParameterAfterEach;
        [JsonProperty]
        private string executeAfterTemplatePath;
        [JsonProperty]
        private bool addUploadInfoParameterAfterTemplate;
        [JsonProperty]
        private string executeAfterAllPath;
        [JsonProperty]
        private bool addUploadInfoParameterAfterAll;

        public AutomationSettings()
        {
        }

        public bool AddNewFilesAutomatically 
        { 
            get => addNewFilesAutomatically;
            set => addNewFilesAutomatically = value; 
        
        }

        public string FileExtensions 
        { 
            get => fileExtensions;
            set => fileExtensions = value; 
        }

        public string DeviatingFolderPath 
        { 
            get => deviatingFolderPath; 
            set => deviatingFolderPath = value; 
        }

        public UplStatus AddWithStatus 
        { 
            get => addWithStatus;
            set => addWithStatus = value; 
        }

        public bool StartUploadingAfterAdd 
        { 
            get => startUploadingAfterAdd;
            set => startUploadingAfterAdd = value; 
        }

        public string ExecuteAfterEachPath 
        { 
            get => executeAfterEachPath; 
            set => executeAfterEachPath = value;
        }

        public bool AddUploadInfoParameterAfterEach 
        { 
            get => addUploadInfoParameterAfterEach;
            set => addUploadInfoParameterAfterEach = value; 
        }

        public string ExecuteAfterTemplatePath 
        { 
            get => executeAfterTemplatePath; 
            set => executeAfterTemplatePath = value; 
        }

        public bool AddUploadInfoParameterAfterTemplate 
        { 
            get => addUploadInfoParameterAfterTemplate; 
            set => addUploadInfoParameterAfterTemplate = value; 
        }

        public string ExecuteAfterAllPath 
        { 
            get => executeAfterAllPath;
            set => executeAfterAllPath = value; 
        }

        public bool AddUploadInfoParameterAfterAll 
        { 
            get => addUploadInfoParameterAfterAll;
            set => addUploadInfoParameterAfterAll = value;
        }
    }
}