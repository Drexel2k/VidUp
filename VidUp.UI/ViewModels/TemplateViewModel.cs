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
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class TemplateViewModel : INotifyPropertyChanged
    {
        private Template template;

        private ObservablePlaylistViewModels observablePlaylistViewModels;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;

        private YoutubeAccount selectedYoutubeAccount;

        //command execution doesn't need any parameter, parameter is only action to do.
        private GenericCommand parameterlessCommand;

        private string lastThumbnailFallbackFilePathAdded = null;
        private string lastImageFilePathAdded = null;

        //needs to be a separate field if the user cancels the account change it needs to 
        //be set once to the new selected value and then reverted back to reflect
        //the change in the GUI.
        private YoutubeAccountComboboxViewModel selectedYoutubeAccountComboboxViewModel;

        public event PropertyChangedEventHandler PropertyChanged;
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
                    if (value != null)
                    {
                        this.selectedYoutubeAccountComboboxViewModel = this.observableYoutubeAccountViewModels.GetViewModel(value.YoutubeAccount);
                    }
                    else
                    {
                        this.selectedYoutubeAccountComboboxViewModel = null;
                    }

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

        public ObservablePlaylistViewModels ObservablePlaylistViewModels
        {
            get
            {
                if (this.template != null)
                {
                    return this.observablePlaylistViewModels[this.template.YoutubeAccount];
                }

                return null;
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get
            {
                return this.observableYoutubeAccountViewModels;
            }
        }

        public PlaylistComboboxViewModel SelectedPlaylist
        {
            get
            {
                if (this.template != null && this.template.Playlist != null)
                {
                    return this.observablePlaylistViewModels[template.YoutubeAccount].GetViewModel(this.template.Playlist);
                }

                return null;
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

        public YoutubeAccountComboboxViewModel SelectedYoutubeAccount
        {
            get
            {
                return this.selectedYoutubeAccountComboboxViewModel;
            }
            set
            {
                this.selectedYoutubeAccountComboboxViewModel = value;

                if (value == null)
                {
                    throw new InvalidOperationException("A template must have a Youtube account.");
                }
                else
                {
                    this.confirmAccountChange(value.YoutubeAccount);
                }
            }
        }

        private async void confirmAccountChange(YoutubeAccount youtubeAccount)
        {
            ConfirmControl control = new ConfirmControl(
                $"WARNING! If you change the account, template will be removed from all uploads belonging to this template until now!",
                true);

            //a collection is change later, so we must return to the Gui thread.
            bool result = (bool) await DialogHost.Show(control, "RootDialog").ConfigureAwait(true);

            if (result)
            {
                YoutubeAccount oldAccount = this.template.YoutubeAccount;
                this.template.YoutubeAccount = youtubeAccount;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                EventAggregator.Instance.Publish(new TemplateDisplayPropertyChangedMessage("youtubeaccount"));
                EventAggregator.Instance.Publish(new TemplateYoutubeAccountChangedMessage(this.template, oldAccount, youtubeAccount));
            }
            else
            {
                this.selectedYoutubeAccountComboboxViewModel = this.observableYoutubeAccountViewModels.GetViewModel(this.template.YoutubeAccount);
            }

            this.raisePropertyChanged("SelectedYoutubeAccount");
            this.raisePropertyChanged("ObservablePlaylistViewModels");
            this.raisePropertyChanged("SelectedPlaylist");
        }

        public GenericCommand ParameterlessCommand
        {
            get
            {
                return this.parameterlessCommand;
            }
        }

        public string Name 
        { 
            get => this.template != null ? this.template.Name : null;
            set
            {
                this.template.Name = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("Name");
                EventAggregator.Instance.Publish<TemplateDisplayPropertyChangedMessage>(new TemplateDisplayPropertyChangedMessage("name"));
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
                EventAggregator.Instance.Publish<TemplateDisplayPropertyChangedMessage>(new TemplateDisplayPropertyChangedMessage("default"));
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

        public string PlaceholderFolderPath
        {
            get => this.template != null ? this.template.PlaceholderFolderPath : null;
            set
            {
                this.template.PlaceholderFolderPath = value;
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                this.raisePropertyChanged("PlaceholderFolderPath");
            }
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

        public TemplateViewModel(Template template, ObservablePlaylistViewModels observablePlaylistViewModels, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels, YoutubeAccount selectedYoutubeAccount)
        {
            if (observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModels must not be null.");
            }

            if (observableYoutubeAccountViewModels == null)
            {
                throw new ArgumentException("ObservableYoutubeAccountViewModels must not be null.");
            }

            this.template = template;

            this.observablePlaylistViewModels = observablePlaylistViewModels;
            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;

            this.selectedYoutubeAccount = selectedYoutubeAccount;

            this.parameterlessCommand = new GenericCommand(this.parameterlessCommandAction);

            EventAggregator.Instance.Subscribe<SelectedTemplateChangedMessage>(this.selectedTemplateChanged);
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


        private void parameterlessCommandAction(object target)
        {
            switch (target)
            {
                case "delete":
                    this.deleteTemplate();
                    break;
                case "copy":
                    this.copyTemplate();
                    break;
                case "openfiledialogplaceholder":
                    this.openFileDialog("placeholder");
                    break;
                case "resetplaceholder":
                    this.resetValue("placeholder");
                    break;
                case "openfiledialogthumbfallback":
                    this.openFileDialog("thumbfallback");
                    break;
                case "resetthumbfallback":
                    this.resetValue("thumbfallback");
                    break;
                case "openfiledialogthumb":
                    this.openFileDialog("thumb");
                    break;
                case "resetthumb":
                    this.resetValue("thumb");
                    break;
                case "openfiledialogpic":
                    this.openFileDialog("pic");
                    break;
                case "resetpic":
                    this.resetValue("pic");
                    break;
                case "openfiledialogroot":
                    this.openFileDialog("root");
                    break;
                case "resetroot":
                    this.resetValue("root");
                    break;
                case "openpublishat":
                    this.openPublishAtAsync();
                    break;
                case "removecomboplaylist":
                    this.removeComboBoxValue("playlist");
                    break;
                case "removecombovideolanguage":
                    this.removeComboBoxValue("videolanguage");
                    break;
                case "removecombodescriptionlanguage":
                    this.removeComboBoxValue("descriptionlanguage");
                    break;
                case "removecombocategory":
                    this.removeComboBoxValue("category");
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for parameterlessCommandAction.");
                    break;
            }
        }

        private void copyTemplate()
        {
            EventAggregator.Instance.Publish(new TemplateCopyMessage(this.template));
        }

        private void deleteTemplate()
        {
            EventAggregator.Instance.Publish(new TemplateDeleteMessage(this.template));
        }

        private void removeComboBoxValue(string target)
        {
            switch (target)
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
                    throw new InvalidOperationException("Invalid parameter for removeComboBoxValue.");
                    break;
            }
        }

        private void openFileDialog(string target)
        {
            if (target == "pic")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ImageFilePathForEditing = fileDialog.FileName;
                }

                return;
            }

            if (target == "root")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.RootFolderPath = folderDialog.SelectedPath;
                }

                return;
            }

            if (target == "thumb")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFolderPath = folderDialog.SelectedPath;
                }

                return;
            }

            if (target == "thumbfallback")
            {
                OpenFileDialog fileDialog = new OpenFileDialog();
                DialogResult result = fileDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.ThumbnailFallbackFilePath = fileDialog.FileName;
                }

                return;
            }

            if (target == "placeholder")
            {
                FolderBrowserDialog folderDialog = new FolderBrowserDialog();
                DialogResult result = folderDialog.ShowDialog();

                if (result == DialogResult.OK)
                {
                    this.PlaceholderFolderPath = folderDialog.SelectedPath;
                }

                return;
            }

            throw new InvalidOperationException("Invalid parameter for openFileDialog.");
        }

        private void resetValue(string target)
        {
            switch (target)
            {
                case "pic":
                    this.ImageFilePathForEditing = null;
                    break;
                case "root":
                    this.RootFolderPath = null;
                    break;
                case "thumb":
                    this.ThumbnailFolderPath = null;
                    break;
                case "thumbfallback":
                    this.ThumbnailFallbackFilePath = null;
                    break;
                case "placeholder":
                    this.PlaceholderFolderPath = null;
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for resetValue.");
                    break;
            }
        }

        private async void openPublishAtAsync()
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

        private void selectedTemplateChanged(SelectedTemplateChangedMessage selectedTemplateChangedMessage)
        {
            //raises all properties changed
            this.Template = selectedTemplateChangedMessage.NewTemplate;
        }
    }
}
