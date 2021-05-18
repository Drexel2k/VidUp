using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.Utils;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class TemplateViewModel : INotifyPropertyChanged
    {
        private TemplateList templateList;
        private Template template;

        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;

        private TemplateComboboxViewModel selectedTemplate;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newTemplateCommand;
        private GenericCommand deleteCommand;
        private GenericCommand removeComboBoxValueCommand;
        private GenericCommand openFileDialogCommand;
        private GenericCommand resetCommand;
        private GenericCommand openPublishAtCommand;

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

        public bool TemplateSet
        {
            get => this.template != null;
        }

        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels;
            }
        }

        public ObservablePlaylistViewModels ObservablePlaylistViewModels
        {
            get
            {
                return this.observablePlaylistViewModels;
            }
        }

        public TemplateComboboxViewModel SelectedTemplate
        {
            get
            {
                return this.selectedTemplate;
            }
            set
            {
                if (this.selectedTemplate != value)
                {
                    this.selectedTemplate = value;
                    if (value != null)
                    {
                        this.Template = value.Template;
                    }
                    else
                    {
                        this.Template = null;
                    }

                    this.raisePropertyChanged("SelectedTemplate");
                }
            }
        }

        public PlaylistComboboxViewModel SelectedPlaylist
        {
            get
            {
                return this.template != null ? this.observablePlaylistViewModels.GetViewModel(this.template.Playlist) : null;
            }
            set
            {
                if (value == null)
                {
                    this.template.Playlist = null;
                }
                else
                {
                    this.template.Playlist = value.Playlist;
                }

                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedPlaylist");
            }
        }

        public GenericCommand NewTemplateCommand
        {
            get
            {
                return this.newTemplateCommand;
            }
        }

        public GenericCommand DeleteCommand
        {
            get
            {
                return this.deleteCommand;
            }
        }

        public GenericCommand RemoveComboBoxValueCommand
        {
            get
            {
                return this.removeComboBoxValueCommand;
            }
        }

        public GenericCommand OpenFileDialogCommand
        {
            get
            {
                return this.openFileDialogCommand;
            }
        }

        public GenericCommand OpenPublishAtCommand
        {
            get
            {
                return this.openPublishAtCommand;
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
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("Name");
            }
        }

        public string Title 
        { 
            get => this.template != null ? this.template.Title : null;
            set
            {
                this.template.Title = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("Title");

            }
        }
        public string Description 
        { 
            get => this.template != null ? this.template.Description : null;
            set
            {
                this.template.Description = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("Description");

            }
        }

        public string TagsAsString 
        { 
            get => this.template != null ? string.Join(",", this.template.Tags) : null;
            set
            {
                this.template.Tags = new List<string>(value.Split(','));
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("TagsAsString");
            }
        }
        public Visibility SelectedVisibility 
        { 
            get => this.template != null ? this.template.YtVisibility : Visibility.Private;
            set
            {
                if (value != Visibility.Private && this.template.YtVisibility == Visibility.Private)
                {
                    if (this.template.UsePublishAtSchedule)
                    {
                        this.template.UsePublishAtSchedule = false;
                    }
                }

                this.template.YtVisibility = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("UsePublishAtSchedule");
                this.raisePropertyChanged("SelectedVisibility");
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
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
                this.raisePropertyChanged("LastImageFilePathAdded");

                this.template.ImageFilePathForEditing = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();

                this.raisePropertyChanged("ImageBitmap");
                this.raisePropertyChanged("ImageFilePathForEditing");
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
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("RootFolderPath");
            }
        }

        public string PartOfFileName
        {
            get => this.template != null ? this.template.PartOfFileName : null;
            set
            {
                this.template.PartOfFileName = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("PartOfFileName");
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.template != null ? this.template.ThumbnailFolderPath : null;
            set
            {
                this.template.ThumbnailFolderPath = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("ThumbnailFolderPath");
            }
        }

        public string ThumbnailFallbackFilePath
        {
            get => this.template != null ? this.template.ThumbnailFallbackFilePath : null;
            set
            {
                //care this RaisePropertyChanged must take place immediately to show rename hint correctly.
                this.lastThumbnailFallbackFilePathAdded = value;
                this.raisePropertyChanged("LastThumbnailFallbackFilePathAdded");
                this.template.ThumbnailFallbackFilePath = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("ThumbnailFallbackFilePath");
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
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("IsDefault");
            }
        }

        public bool UsePublishAtSchedule
        {
            get => this.template != null && this.template.UsePublishAtSchedule;
            set
            {
                if (value && !this.template.UsePublishAtSchedule)
                {
                    if (this.template.YtVisibility != Visibility.Private)
                    {
                        this.template.YtVisibility = Visibility.Private;
                    }
                }

                this.template.UsePublishAtSchedule = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedVisibility");
                this.raisePropertyChanged("UsePublishAtSchedule");
            }
        }

        public bool SetPlaylistAfterPublication
        {
            get => this.template != null && this.template.SetPlaylistAfterPublication;
            set
            {
                this.template.SetPlaylistAfterPublication = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SetPlaylistAfterPublication");
            }
        }

        public Array TemplateModes
        {
            get
            {
                return Enum.GetValues(typeof(TemplateMode));
            }
        }

        public TemplateMode SelectedTemplateMode
        {
            get => this.template != null ? this.template.TemplateMode : TemplateMode.FolderBased;
            set
            {
                this.template.TemplateMode = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedTemplateMode");
            }
        }

        public List<CultureInfo> Languages
        {
            get => Cultures.RelevantCultureInfos;
        }

        public CultureInfo SelectedVideoLanguage
        {
            get => this.template != null ? this.template.VideoLanguage : null;
            set
            {
                this.template.VideoLanguage = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedVideoLanguage");
            }
        }

        public CultureInfo SelectedDescriptionLanguage
        {
            get => this.template != null ? this.template.DescriptionLanguage : null;
            set
            {
                this.template.DescriptionLanguage = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedDescriptionLanguage");
            }
        }

        public Category[] Categories
        {
            get => Category.Categories;
        }

        public Category SelectedCategory
        {
            get => this.template != null ? this.template.Category : null;
            set
            {
                this.template.Category = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("SelectedCategory");
            }
        }

        #endregion properties

        public TemplateViewModel(TemplateList templateList, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels)
        {
            if(templateList == null)
            {
                throw new ArgumentException("TemplateList must not be null.");
            }

            if(observableTemplateViewModels == null)
            {
                throw new ArgumentException("ObservableTemplateViewModels must not be null.");
            }

            if (observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels must not be null.");
            }

            this.templateList = templateList;
            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.SelectedTemplate = this.observableTemplateViewModels.TemplateCount > 0 ? this.observableTemplateViewModels[0] : null;

            this.newTemplateCommand = new GenericCommand(this.OpenNewTemplateDialog);
            this.deleteCommand = new GenericCommand(this.DeleteTemplate);
            this.removeComboBoxValueCommand = new GenericCommand(this.removeComboBoxValue);
            this.openFileDialogCommand = new GenericCommand(this.openFileDialog);
            this.resetCommand = new GenericCommand(this.resetValue);
            this.openPublishAtCommand = new GenericCommand(this.openPublishAt);
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

        public async void OpenNewTemplateDialog(object obj)
        {
            var view = new NewTemplateControl
            {
                DataContext = new NewTemplateViewModel(Settings.SettingsInstance.TemplateImageFolder)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewTemplateViewModel data = (NewTemplateViewModel) view.DataContext;
                Template template = new Template(data.Name, data.ImageFilePath, data.TemplateMode, data.RootFolderPath, data.PartOfFileName, this.templateList);
                this.AddTemplate(template);

            }
        }

        public void DeleteTemplate(Object guid)
        {       
            Template template = this.templateList.GetTemplate(System.Guid.Parse((string)guid));

            //Needs to set before deleting the ViewModel in ObservableTemplateViewModels, otherwise the RaiseNotifyCollectionChanged
            //will set the SelectedTemplate to null which causes problems if there are templates left
            if (this.observableTemplateViewModels.TemplateCount > 1)
            {
                if (this.observableTemplateViewModels[0].Template == template)
                {
                    this.SelectedTemplate = this.observableTemplateViewModels[1];
                }
                else
                {
                    this.SelectedTemplate = this.observableTemplateViewModels[0];
                }
            }
            else
            {
                this.SelectedTemplate = null;
            }

            this.templateList.Remove(template);

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            JsonSerializationContent.JsonSerializer.SerializeTemplateList();
        }


        private void removeComboBoxValue(object parameter)
        {
            switch (parameter)
            {
                case "playlist":
                    this.SelectedPlaylist = null;
                    break;
                case "videolanguage":
                    this.SelectedVideoLanguage = null;
                    break;
                case "descriptionlanguage":
                    this.SelectedDescriptionLanguage = null;
                    break;
                case "category":
                    this.SelectedCategory = null;
                    break;
                default:
                    throw new InvalidOperationException("No parameter for removeComboBoxValue specified.");
                    break;
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

        private async void openPublishAt(object obj)
        {
            var view = new PublishAtScheduleControl
            {
                DataContext = new PublishAtScheduleViewModel(this.template.PublishAtSchedule)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                PublishAtScheduleViewModel data = (PublishAtScheduleViewModel)view.DataContext;
                this.template.PublishAtSchedule = data.Schedule;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
            }
        }

        //private void raisePropertyChangedAndSerializeTemplateList(string propertyName)
        //{
        //    this.raisePropertyChanged(propertyName);
        //    JsonSerializationContent.JsonSerializer.SerializeTemplateList();
        //}

        //exposed for testing
        public void AddTemplate(Template template)
        {
            List<Template> list = new List<Template>();
            list.Add(template);
            this.templateList.AddTemplates(list);

            JsonSerializationContent.JsonSerializer.SerializeTemplateList();

            this.SelectedTemplate = new TemplateComboboxViewModel(template);
        }
    }
}
