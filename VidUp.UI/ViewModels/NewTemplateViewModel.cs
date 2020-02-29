using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Windows.Forms;

namespace Drexel.VidUp.UI.ViewModels
{
    class NewTemplateViewModel : INotifyPropertyChanged
    {
        private GenericCommand openFileDialogCommand;
        private string name;
        private string pictureFilePath;
        private string rootFolderPath;
        private bool formVaild = false;

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

                    RaisePropertyChanged("Name");
                }
            }
        }
        public string PictureFilePath
        {
            get
            {
                return this.pictureFilePath;
            }
            set
            {
                if (this.pictureFilePath != value)
                {
                    this.pictureFilePath = value;
                    RaisePropertyChanged("PictureFilePath");
                }
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
                    RaisePropertyChanged("RootFolderPath");
                }
            }
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
                this.RaisePropertyChanged("FormValid");
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public NewTemplateViewModel()
        {
            this.openFileDialogCommand = new GenericCommand(openFileDialog);
        }

        private void RaisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
            if (PropertyChanged != null)
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
                    this.PictureFilePath = fileDialog.FileName;
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