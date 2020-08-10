using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json;
using Drexel.VidUp.UI.Controls;
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
        private GenericCommand removeTemplateCommand;
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
                return this.observablePlaylistViewModels.GetViewModel(this.template.Playlist);
            }
            set
            {
                if (value != null)
                {
                    if (this.template.Playlist != value.Playlist)
                    {
                        this.template.Playlist = value.Playlist;
                        this.raisePropertyChangedAndSerializeTemplateList("SelectedPlaylist");
                    }
                }
            }
        }

        public GenericCommand NewTemplateCommand
        {
            get
            {
                return this.newTemplateCommand;
            }
        }

        public GenericCommand RemoveTemplateCommand
        {
            get
            {
                return this.removeTemplateCommand;
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
                this.raisePropertyChangedAndSerializeTemplateList("Name");
            }
        }

        public string Title 
        { 
            get => this.template != null ? this.template.Title : null;
            set
            {
                this.template.Title = value;
                this.raisePropertyChangedAndSerializeTemplateList("Title");

            }
        }
        public string Description 
        { 
            get => this.template != null ? this.template.Description : null;
            set
            {
                this.template.Description = value;
                this.raisePropertyChangedAndSerializeTemplateList("Description");

            }
        }

        public string TagsAsString 
        { 
            get => this.template != null ? string.Join(",", this.template.Tags) : null;
            set
            {
                this.template.Tags = new List<string>(value.Split(','));
                this.raisePropertyChangedAndSerializeTemplateList("TagsAsString");
            }
        }
        public Visibility Visibility 
        { 
            get => this.template != null ? this.template.YtVisibility : Visibility.Private;
            set
            {
                this.template.YtVisibility = value;
                this.raisePropertyChangedAndSerializeTemplateList("Visibility");
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

                this.raisePropertyChanged("ImageBitmap");
                this.raisePropertyChangedAndSerializeTemplateList("ImageFilePathForEditing");
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
                this.raisePropertyChangedAndSerializeTemplateList("RootFolderPath");
            }
        }

        public string ThumbnailFolderPath
        {
            get => this.template != null ? this.template.ThumbnailFolderPath : null;
            set
            {
                this.template.ThumbnailFolderPath = value;
                this.raisePropertyChangedAndSerializeTemplateList("ThumbnailFolderPath");
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
                this.raisePropertyChangedAndSerializeTemplateList("IsDefault");
            }
        }

        public bool UsePublishAtSchedule
        {
            get => this.template != null && this.template.UsePublishAtSchedule;
            set
            {
                this.template.UsePublishAtSchedule = value;
                this.raisePropertyChangedAndSerializeTemplateList("UsePublishAtSchedule");
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
            this.SelectedPlaylist = this.observablePlaylistViewModels.GetViewModel(this.template.Playlist);

            this.newTemplateCommand = new GenericCommand(this.OpenNewTemplateDialog);
            this.removeTemplateCommand = new GenericCommand(this.RemoveTemplate);
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
                DataContext = new NewTemplateViewModel(Settings.TemplateImageFolder)
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                NewTemplateViewModel data = (NewTemplateViewModel) view.DataContext;
                Template template = new Template(data.Name, data.ImageFilePath, data.RootFolderPath, this.templateList); 
                this.AddTemplate(template);

            }
        }

        public void RemoveTemplate(Object guid)
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

            JsonSerialization.JsonSerializer.SerializeTemplateList();
            JsonSerialization.JsonSerializer.SerializeAllUploads();
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
                JsonSerialization.JsonSerializer.SerializeTemplateList();
            }
        }

        private void raisePropertyChangedAndSerializeTemplateList(string propertyName)
        {
            this.raisePropertyChanged(propertyName);
            JsonSerialization.JsonSerializer.SerializeTemplateList();
        }

        //exposed for testing
        public void AddTemplate(Template template)
        {
            List<Template> list = new List<Template>();
            list.Add(template);
            this.templateList.AddTemplates(list);
            JsonSerialization.JsonSerializer.SerializeTemplateList();

            this.SelectedTemplate = new TemplateComboboxViewModel(template);
        }
    }
}
