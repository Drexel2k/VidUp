using System;
using System.ComponentModel;
using System.Windows.Forms;
using Drexel.VidUp.Business;

namespace Drexel.VidUp.UI.ViewModels
{
    class NewTemplateViewModel : INotifyPropertyChanged
    {
        private GenericCommand openFileDialogCommand;
        private string name;
        private string imageFilePath;
        private TemplateMode templateMode;
        private string rootFolderPath;
        private string partOfFileName;
        private bool formVaild = false;

        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccountComboboxViewModel selectedYoutubeAccount;

        public string Name
        {
            get
            {
                return this.name;
            }
            set
            {
                if (this.name != value)
                {
                    this.name = value;
                    if(!string.IsNullOrWhiteSpace(this.name))
                    {
                        this.FormValid = true;
                    }
                    else
                    {
                        this.FormValid = false;
                    }

                    this.raisePropertyChanged("Name");
                }
            }
        }
        public string ImageFilePath
        {
            get
            {
                return this.imageFilePath;
            }
            set
            {
                if (this.imageFilePath != value)
                {
                    this.imageFilePath = value;
                    this.raisePropertyChanged("ImageFilePath");
                }
            }
        }

        public TemplateMode TemplateMode
        {
            get
            {
                return this.templateMode;
            }
            set
            {
                if (this.templateMode != value)
                {
                    this.templateMode = value;
                    this.raisePropertyChanged("TemplateMode");
                }
            }
        }

        public Array TemplateModes
        {
            get
            {
                return Enum.GetValues(typeof(TemplateMode));
            }
        }

        public string RootFolderPath
        {
            get
            {
                return this.rootFolderPath;
            }
            set
            {
                if (this.rootFolderPath != value)
                {
                    this.rootFolderPath = value;
                    this.raisePropertyChanged("RootFolderPath");
                }
            }
        }

        public string PartOfFileName
        {
            get
            {
                return this.partOfFileName;
            }
            set
            {
                if (this.partOfFileName != value)
                {
                    this.partOfFileName = value;
                    this.raisePropertyChanged("PartOfFileName");
                }
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get => this.observableYoutubeAccountViewModels;
        }

        public YoutubeAccountComboboxViewModel SelectedYouTubeAccount
        {
            get => this.selectedYoutubeAccount;
            set => this.selectedYoutubeAccount = value;
        }

        public GenericCommand OpenFileDialogCommand
        {
            get
            {
                return this.openFileDialogCommand;
            }
        }

        public bool FormValid
        {
            get => this.formVaild;
            private set
            {
                this.formVaild = value;
                this.raisePropertyChanged("FormValid");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewTemplateViewModel(string templateImageFolder, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount selectedYoutubeAccount)
        {
            if (observableYoutubeAccountViewModels == null || observableYoutubeAccountViewModels.YoutubeAccountCount <= 0)
            {
                throw new ArgumentException("observableYoutubeAccountViewModels must not be null and must contain accounts.");
            }

            if (selectedYoutubeAccount == null)
            {
                throw new ArgumentException("selectedYoutubeAccount must not be null.");
            }

            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.selectedYoutubeAccount = this.observableYoutubeAccountViewModels.GetViewModel(selectedYoutubeAccount);

            this.openFileDialogCommand = new GenericCommand(openFileDialog);
        }

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private void openFileDialog(object parameter)
        {
            if ((string)parameter == "pic")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ImageFilePath = fileDialog.FileName;
                }
            }

            if ((string)parameter == "root")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.RootFolderPath = folderDialog.SelectedPath;
                }
            }
        }
    }
}