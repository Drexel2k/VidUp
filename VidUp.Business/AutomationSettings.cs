using Newtonsoft.Json;

namespace Drexel.VidUp.Business
{
    [JsonObject(MemberSerialization = MemberSerialization.OptIn)]
    public class AutomationSettings
    {
        [JsonProperty]
        private bool addNewFilesAutomatically;
        [JsonProperty]
        private string fileFilter;
        [JsonProperty]
        private string deviatingFolderPath;
        [JsonProperty]
        private UplStatus addWithStatus;
        [JsonProperty]
        private bool startUploadingAfterAdd;
        [JsonProperty]
        private string executeAfterEachPath;
        [JsonProperty]
        private string executeAfterTemplatePath;
        [JsonProperty]
        private string executeAfterAllPath;

        public AutomationSettings()
        {
        }

        public bool AddNewFilesAutomatically 
        { 
            get => addNewFilesAutomatically;
            set => addNewFilesAutomatically = value; 
        
        }

        public string FileFilter
        { 
            get => fileFilter;
            set => fileFilter = value; 
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

        public string ExecuteAfterTemplatePath 
        { 
            get => executeAfterTemplatePath; 
            set => executeAfterTemplatePath = value; 
        }

        public string ExecuteAfterAllPath 
        { 
            get => executeAfterAllPath;
            set => executeAfterAllPath = value; 
        }
    }
}