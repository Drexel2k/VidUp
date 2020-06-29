#region

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.JSON;

#endregion

namespace Drexel.VidUp.UI.ViewModels
{

    public class TemplateViewModel : INotifyPropertyChanged
    {
        private Template template;
        public event PropertyChangedEventHandler PropertyChanged;

        private QuarterHourViewModels quarterHourViewModels;
        private GenericCommand openFileDialogCommand;
        private GenericCommand resetCommand;

        private string lastThumbnailFallbackFilePathAdded = null;
        private string lastImageFilePathAdded;

        #region properties

        public Template Template
        {
            get
            {
                return this.template;
            }
            set
            {
                if (this.template != value)
                {
                    this.template = value;
                    //all properties changed
                    this.raisePropertyChanged(null);
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

        public GenericCommand ResetCommand
        {
            get
            {
                return this.resetCommand;
            }
        }
        public string Guid
        { 
            get => this.template != null ? this.template.Guid.ToString() : string.Empty; 
        }

        public DateTime Created
        { 
            get => this.template != null ? this.template.Created : DateTime.MinValue; 
        }

        public DateTime LastModified 
        { 
            get => this.template != null ? this.template.LastModified : DateTime.MinValue; 
        }

        public string Name 
        { 
            get => this.template != null ? this.template.Name : null;
            set
            {
                this.template.Name = value;
                raisePropertyChangedAndSerializeTemplateList("Name");
            }
        }

        public string Title 
        { 
            get => this.template != null ? this.template.Title : null;
            set
            {
                this.template.Title = value;
                raisePropertyChangedAndSerializeTemplateList("YtTitle");

            }
        }
        public string Description 
        { 
            get => this.template != null ? this.template.Description : null;
            set
            {
                this.template.Description = value;
                raisePropertyChangedAndSerializeTemplateList("YtDescription");

            }
        }

        public string TagsAsString 
        { 
            get => this.template != null ? string.Join(",", this.template.Tags) : null;
            set
            {
                this.template.Tags = new List<string>(value.Split(','));
                raisePropertyChangedAndSerializeTemplateList("TagsAsString");
            }
        }
        public Visibility Visibility 
        { 
            get => this.template != null ? this.template.YtVisibility : Visibility.Private;
            set
            {
                this.template.YtVisibility = value;
                raisePropertyChangedAndSerializeTemplateList("YtVisibility");
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
        }

        public QuarterHourViewModels QuarterHourViewModels
        {
            get
            {
                return this.quarterHourViewModels;
            }
        }

        public QuarterHourViewModel DefaultPublishAtTime
        {
            get => this.template != null ? this.quarterHourViewModels.GetQuarterHourViewModel(this.template.DefaultPublishAtTime) : this.quarterHourViewModels.GetQuarterHourViewModel(new DateTime(1, 1, 1, 0, 0, 0));
        }



        public BitmapImage ImageBitmap
        {
            get
            {
                if(this.template != null && File.Exists(this.template.ImageFilePathForRendering))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(this.template.ImageFilePathForRendering, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    return bitmap;
                }

                return null;
            }
        }

        public string ImageFilePathForEditing
        {
            get => this.template != null ? this.template.ImageFilePathForEditing : null;
            set
            {
                //care this RaisePropertyChanged must take place immediately to show rename hint correctly.
                this.lastImageFilePathAdded = value;
                raisePropertyChanged("LastImageFilePathAdded");

                this.template.ImageFilePathForEditing = value;
                
                raisePropertyChanged("ImageBitmap");
                raisePropertyChangedAndSerializeTemplateList("ImageFilePathForEditing");
            }
        }

        public string LastImageFilePathAdded
        {
            get => this.lastImageFilePathAdded;
        }
        public string RootFolderPath 
        { 
            get => this.template != null ? this.template.RootFolderPath : null;
            set
            {
                this.template.RootFolderPath = value;
                raisePropertyChangedAndSerializeTemplateList("RootFolderPath");
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.template != null ? this.template.ThumbnailFolderPath : null;
            set
            {
                this.template.ThumbnailFolderPath = value;
                raisePropertyChangedAndSerializeTemplateList("ThumbnailFolderPath");
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.template != null ? this.template.ThumbnailFallbackFilePath : null;
            set
            {
                //care this RaisePropertyChanged must take place immediately to show rename hint correctly.
                this.lastThumbnailFallbackFilePathAdded = value;
                raisePropertyChanged("LastThumbnailFallbackFilePathAdded");
                this.template.ThumbnailFallbackFilePath = value;

                this.raisePropertyChangedAndSerializeTemplateList("ThumbnailFallbackFilePath");
            }
        }

        public string LastThumbnailFallbackFilePathAdded
        {
            get => this.lastThumbnailFallbackFilePathAdded;
        }

        public bool IsDefault
        {
            get => this.template != null ? this.template.IsDefault : false;
            set
            {
                this.template.IsDefault = value;
                raisePropertyChangedAndSerializeTemplateList("IsDefault");
            }
        }

        public string PlaylistId
        {
            get => this.template != null ? this.template.PlaylistId : null;
            set
            {
                this.template.PlaylistId = value;
                raisePropertyChangedAndSerializeTemplateList("PlaylistId");
            }
        }

        #endregion properties

        public TemplateViewModel()
        {
            this.openFileDialogCommand = new GenericCommand(this.openFileDialog);
            this.resetCommand = new GenericCommand(this.resetValue);
            this.quarterHourViewModels = new QuarterHourViewModels();
        }

        public void SetDefaultPublishAtTime(DateTime publishAtTime)
        {
            this.template.DefaultPublishAtTime = publishAtTime;
            this.SerializeTemplateList();
        }
        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = PropertyChanged;
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
                    this.ImageFilePathForEditing = fileDialog.FileName;
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

            if ((string)parameter == "thumb")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFolderPath = folderDialog.SelectedPath;
                }
            }

            if ((string)parameter == "thumbfallback")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFallbackFilePath = fileDialog.FileName;
                }
            }
        }

        private void resetValue(object parameter)
        {
            if ((string)parameter == "pic")
            {
                this.ImageFilePathForEditing = null;
            }

            if ((string)parameter == "root")
            {
                this.RootFolderPath = null;
            }

            if ((string)parameter == "thumb")
            {
                this.ThumbnailFolderPath = null;
            }

            if ((string)parameter == "thumbfallback")
            {
                this.ThumbnailFallbackFilePath = null;
            }
        }

        private void raisePropertyChangedAndSerializeTemplateList(string propertyName)
        {
            this.raisePropertyChanged(propertyName);
            this.SerializeTemplateList();
        }

        public void SerializeTemplateList()
        {
            JsonSerialization.SerializeTemplateList();
        }
    }
}
