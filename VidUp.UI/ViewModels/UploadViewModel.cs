﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Windows.Forms;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;

namespace Drexel.VidUp.UI.ViewModels
{
    public class UploadViewModel : INotifyPropertyChanged, IDisposable
    {
        private Upload upload;
        private QuarterHourViewModels quarterHourViewModels;

        private bool visible = true;

        private GenericCommand parameterlessCommand;
        private GenericCommand removeComboBoxValueCommand;
        private GenericCommand resetToTemplateValueCommand;

        private ObservableTemplateViewModels observableTemplateViewModels;
        private ObservablePlaylistViewModels observablePlaylistViewModels;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;

        //need for determination of upload status color
        private bool resumeUploads;

        private Subscription attributeResetSubscription;
        private Subscription bytesSentSubscription;
        private Subscription uploadStatusChangedSubscription;
        private Subscription uploadStartingSubscription;
        private Subscription uploadFinishedSubscription;
        private Subscription resumableSessionUriChangedSubscription;
        private Subscription publishAtChangedSubscription;
        private Subscription errorMessageChangedSubscription;


        public event PropertyChangedEventHandler PropertyChanged;

        public ObservableTemplateViewModels ObservableTemplateViewModels
        {
            get
            {
                return this.observableTemplateViewModels[this.SelectedYoutubeAccount.YoutubeAccount];
            }
        }

        public ObservablePlaylistViewModels ObservablePlaylistViewModels
        {
            get
            {
                return this.observablePlaylistViewModels[this.SelectedYoutubeAccount.YoutubeAccount];
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get
            {
                return this.observableYoutubeAccountViewModels;
            }
        }

        public string Guid
        {
            get => this.upload.Guid.ToString();
        }

        public GenericCommand ParameterlessCommand
        {
            get
            {
                return this.parameterlessCommand;
            }
        }

        public GenericCommand RemoveComboBoxValueCommand
        {
            get
            {
                return this.removeComboBoxValueCommand;
            }
        }

        public GenericCommand ResetToTemplateValueCommand
        {
            get
            {
                return this.resetToTemplateValueCommand;
            }
        }


        public UplStatus UploadStatus
        {
            get => this.upload.UploadStatus;
        }

        public SolidColorBrush UploadStatusColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.UploadStatus == UplStatus.Finished || this.upload.UploadStatus == UplStatus.Uploading)
                {
                    color = Colors.Lime;
                }

                if (this.upload.UploadStatus == UplStatus.ReadyForUpload)
                {
                    color = Colors.Gold;
                }

                if (this.upload.UploadStatus == UplStatus.Failed || this.upload.UploadStatus == UplStatus.Stopped)
                {
                    if (this.resumeUploads)
                    {
                        color = Colors.Gold;
                    }
                    else
                    {
                        color = Colors.Transparent;
                    }
                }

                if (this.upload.UploadStatus == UplStatus.Paused)
                {
                    color = Colors.Transparent;
                }

                return new SolidColorBrush(color);
            }
        }

        //only visible if an error message is set at all.
        public SolidColorBrush WarningColor
        {
            get
            {
                Color color = Colors.Red;

                if (this.upload.UploadStatus == UplStatus.Finished)
                {
                    color = Colors.Gold;
                }

                return new SolidColorBrush(color);
            }
        }

        public bool UploadStatusColorAnimation
        {
            get
            {
                if ( this.upload.UploadStatus == UplStatus.Uploading)
                {
                    return true;
                }

                return false;
            }
        }

        public string FilePath
        {
            get => this.upload.FilePath;
        }

        public bool StateCommandsEnabled
        {
            get
            {
                if (this.UploadStatus != UplStatus.Uploading)
                {
                    return true;
                }

                return false;
            }
        }

        public bool ResetStateCommandEnabled
        {
            get
            {
                return this.StateCommandsEnabled && this.upload.VerifyForUpload();
            }
        }

        public TemplateComboboxViewModel SelectedTemplate
        {
            get
            {
                if (this.upload.Template != null)
                {
                    return this.observableTemplateViewModels[this.SelectedYoutubeAccount.YoutubeAccount].GetViewModel(this.upload.Template);
                }

                return null;
            }
            
            set
            {
                if (value == null)
                {
                    this.upload.Template = null;
                }
                else
                {
                    this.upload.Template = value.Template;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                //if template changes all values are set to template values
                this.raisePropertyChanged(null);
            }
        }

        public PlaylistComboboxViewModel SelectedPlaylist
        {
            get
            {
                if (this.upload.Playlist != null)
                {
                    return this.observablePlaylistViewModels[this.SelectedYoutubeAccount.YoutubeAccount].GetViewModel(this.upload.Playlist);
                }

                return null;
            }
            set
            {
                if (value == null)
                {
                    this.upload.Playlist = null;
                }
                else
                {
                    this.upload.Playlist = value.Playlist;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("ShowPlaylistHint");
                this.raisePropertyChanged("SelectedPlaylist");
            }
        }

        public DateTime Created
        {
            get => this.upload.Created;
        }

        public DateTime LastModified
        {
            get => this.upload.LastModified;
        }

        public string UploadStart
        {
            get
            {
                if (this.upload.UploadStart != null)
                {
                    return this.upload.UploadStart.Value.ToString("g");
                }

                return null;
            }
        }

        public string UploadEnd
        {
            get
            {
                if (this.upload.UploadEnd != null)
                {
                    return this.upload.UploadEnd.Value.ToString("g");
                }

                return null;
            }
        }

        public BitmapImage ImageBitmap
        {
            get
            {
                if (File.Exists(this.upload.ImageFilePath))
                {
                    BitmapImage bitmap = new BitmapImage();
                    bitmap.BeginInit();
                    bitmap.UriSource = new Uri(this.upload.ImageFilePath, UriKind.Absolute);
                    bitmap.CacheOption = BitmapCacheOption.OnLoad;
                    bitmap.EndInit();

                    return bitmap;
                }

                return null;
            }
        }

        public string Title
        {
            get => this.upload.Title;
            set
            {
                this.upload.Title = value;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                this.raiseTitleProperties();
            }
        }

        public string YtTitle
        {
            get
            {
                if (this.upload.Title.Length <= YoutubeLimits.TitleLimit)
                {
                    return this.upload.Title;
                }
                else
                {
                    return this.upload.Title.Substring(0, YoutubeLimits.TitleLimit);
                }
            }
        }

        public string TitleCharacterCount
        {
            get
            {
                return this.upload.Title.Length.ToString("N0");
            }
        }

        public SolidColorBrush TitleColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.Title.Length > YoutubeLimits.TitleLimit)
                {
                    color = Colors.Red;
                }
                
                return new SolidColorBrush(color);
            }
        }
        public string Description
        {
            get => this.upload.Description;
            set
            {
                this.upload.Description = value;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                this.raiseDescriptionProperties();
            }
        }

        public string DescriptionCharacterCount
        {
            get
            {
                return this.upload.Description != null ? 
                    this.upload.Description.Length.ToString("N0") : 0.ToString("N0");
            }
        }

        public SolidColorBrush DescriptionColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.Description != null && this.upload.Description.Length > YoutubeLimits.DescriptionLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public string MaxDescriptionCharacters
        {
            get
            {
                return $"/ {YoutubeLimits.DescriptionLimit.ToString("N0")}";
            }
        }


        public string TagsAsString
        {
            get => string.Join(",", this.upload.Tags);
            set
            { ;
                this.upload.SetTags(value.Split(','));

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();

                this.raiseTagsProperties();
            }
        }

        public string TagsCharacterCount
        {
            get
            {
                return this.upload.TagsCharacterCount.ToString("N0");
            }
        }

        public SolidColorBrush TagsColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.TagsCharacterCount > YoutubeLimits.TagsLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public Array Visibilities
        {
            get
            {
                return Enum.GetValues(typeof(Visibility));
            }
        }

        public Visibility SelectedVisibility
        {
            get => this.upload.Visibility;
            set
            {
                this.upload.Visibility = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePublishAtProperties();
            }
        }

        public YoutubeAccountComboboxViewModel SelectedYoutubeAccount
        {
            get => this.observableYoutubeAccountViewModels.GetViewModel(this.upload.YoutubeAccount);
            set
            {
                if (value == null)
                {
                    throw new ArgumentException("SelectedYoutubeAccount must not be null.");
                }
                else
                {
                    this.upload.YoutubeAccount = value.YoutubeAccount;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                JsonSerializationContent.JsonSerializer.SerializeTemplateList();

                this.raisePropertyChanged("ObservableTemplateViewModels");
                this.raisePropertyChanged("ObservablePlaylistViewModels");
                this.raisePropertyChanged("SelectedYoutubeAccount");
                this.raisePropertyChanged("SelectedPlaylist");
                this.raisePropertyChanged("SelectedTemplate");
            }
        }

        public SolidColorBrush FileSizeColor
        {
            get
            {
                Color color = Colors.Transparent;

                if (this.upload.FileLength > YoutubeLimits.FileSizeLimit)
                {
                    color = Colors.Red;
                }

                return new SolidColorBrush(color);
            }
        }

        public string UploadedTotalInfo
        {
            get => $"{((int)((float)this.upload.BytesSent / Constants.ByteMegaByteFactor)).ToString("N0")} / {((int)((float)this.upload.FileLength / Constants.ByteMegaByteFactor)).ToString("N0")} MB";
        }

        public string UploadErrorMessage
        {
            get
            {
                //shouldn't happen any more as upload is restarted automatically.
                //add more helpful information if upload index couldn't be received.
                //if (this.upload.UploadErrorMessage != null && this.upload.UploadErrorMessage.Contains("Getting resume position failed 3 times") && this.upload.UploadErrorMessage.Contains("Could not get range header"))
                //{
                //    string message = "If range header couldn't get 3 times in a row, the upload can't be continued, because the Youtube server doesn't deliver information where to continue the upload. Please restart the upload from beginning by resetting the upload until it is in state \"Ready for Upload\"\n\n";
                //    message += this.upload.UploadErrorMessage;
                //    return message;
                //}

                return StatusInformationToStringConverter.GetStatusInformationString(this.upload.UploadErrors, true, true);
            }
        }

        public bool ControlsEnabled
        {
            get
            {
                if (string.IsNullOrWhiteSpace(this.upload.ResumableSessionUri))
                {
                    return true;
                }

                return false;
            }
        }


        public void SetPublishAtTime(TimeSpan quarterHour)
        {
            this.upload.SetPublishAtTime(quarterHour);
            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            this.raisePropertyChanged("PublishAtTime");
        }

        public QuarterHourViewModels QuarterHourViewModels
        {
            get
            {
                return this.quarterHourViewModels;
            }
        }
        public bool PublishAt
        {
            get
            {
                if (this.upload.PublishAt == null)
                {
                    return false;
                }
                else
                {
                    return true;
                }
            }

            set
            {
                if (value)
                {
                    this.upload.ResetPublishAt();
                }
                else
                {
                    this.upload.PublishAt = null;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePublishAtProperties();
            }
        }

        public QuarterHourViewModel PublishAtTime
        {
            get
            {
                return this.quarterHourViewModels.GetQuarterHourViewModel(this.upload.PublishAt != null ? this.upload.PublishAt.Value.TimeOfDay : TimeSpan.MinValue);
            }
            set
            {
                if (this.Upload.PublishAt.Value.TimeOfDay != value.QuarterHour)
                {
                    this.SetPublishAtTime(value.QuarterHour.Value);
                }
            }
        }

        public DateTime? PublishAtDate
        {
            get
            {
                return this.upload.PublishAt;
            }
            set
            {
                this.upload.SetPublishAtDate(value.Value);
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("PublishAtDate");
            }
        }

        public bool PublishAtDateTimeControlsEnabled
        {
            get
            {
                if (!this.ControlsEnabled)
                {
                    return false;
                }
                else
                {
                    return this.upload.PublishAt != null;
                }
            }
        }

        public Upload Upload { get => this.upload;  }

        public string ThumbnailFilePath
        {
            get
            {
                return this.upload.ThumbnailFilePath;
            }
            set
            {
                this.upload.ThumbnailFilePath = value;

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("ThumbnailFilePath");
            }
        }

        public bool ResumeUploads
        {
            set
            {
                this.resumeUploads = value; 
                this.raisePropertyChanged("UploadStatusColor");
            }
        }

        public List<CultureInfo> Languages
        {
            get => Cultures.RelevantCultureInfos;
        }

        public CultureInfo SelectedVideoLanguage
        {
            get => this.upload.VideoLanguage;
            set
            {
                this.upload.VideoLanguage = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("SelectedVideoLanguage");
            }
        }

        public CultureInfo SelectedDescriptionLanguage
        {
            get => this.upload.DescriptionLanguage;
            set
            {
                this.upload.DescriptionLanguage = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("SelectedDescriptionLanguage");
            }
        }

        public Category[] Categories
        {
            get => Category.Categories;
        }

        public Category SelectedCategory
        {
            get => this.upload.Category;
            set
            {
                this.upload.Category = value;
                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                this.raisePropertyChanged("SelectedCategory");
            }
        }

        public string ShowPlaylistHint
        {
            get
            {
                if (this.upload.Playlist == null && this.upload.Template != null && this.upload.Template.SetPlaylistAfterPublication && this.upload.Template.Playlist != null)
                {
                    return "Visible";
                }

                return "Collapsed";
            }
        }

        public bool Visible
        {
            get => this.visible;
            set
            {
                this.visible = value;
                this.raisePropertyChanged("Visible");
            }
        }

        //get the template list and playlist list to create own filtered ObservableViewModels of that list
        //dependant on account selection
        public UploadViewModel (Upload upload, ObservableTemplateViewModels observableTemplateViewModels, ObservablePlaylistViewModels observablePlaylistViewModels, bool resumeUploads, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels)
        {
            if (observableTemplateViewModels == null)
            {
                throw new ArgumentException("ObservableTemplateViewModels must not be null.");
            }

            if (observablePlaylistViewModels == null)
            {
                throw new ArgumentException("ObservablePlaylistViewModelsByAccount must not be null.");
            }

            if (observableYoutubeAccountViewModels == null)
            {
                throw new ArgumentException("ObservableYoutubeAccountViewModels must not be null.");
            }

            this.upload = upload;


            this.observableTemplateViewModels = observableTemplateViewModels;
            this.observablePlaylistViewModels = observablePlaylistViewModels;

            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.resumeUploads = resumeUploads;

            this.quarterHourViewModels = new QuarterHourViewModels(false);

            this.parameterlessCommand = new GenericCommand(this.parameterlessCommandAction);
            this.removeComboBoxValueCommand = new GenericCommand(this.removeComboBoxValue);
            this.resetToTemplateValueCommand = new GenericCommand(this.resetToTemplateValue);

            this.attributeResetSubscription = EventAggregator.Instance.Subscribe<AttributeResetMessage>(this.attributeReset);
            this.bytesSentSubscription = EventAggregator.Instance.Subscribe<BytesSentMessage>(this.bytesSent);
            this.uploadStatusChangedSubscription = EventAggregator.Instance.Subscribe<UploadStatusChangedMessage>(this.uploadStatusChanged);
            this.uploadStartingSubscription = EventAggregator.Instance.Subscribe<UploadStartingMessage>(this.uploadStatusChanged);
            this.uploadFinishedSubscription = EventAggregator.Instance.Subscribe<UploadFinishedMessage>(this.uploadStatusChanged);
            this.resumableSessionUriChangedSubscription = EventAggregator.Instance.Subscribe<ResumableSessionUriChangedMessage>(this.resumableSessionUriChanged);
            this.publishAtChangedSubscription = EventAggregator.Instance.Subscribe<PublishAtChangedMessage>(this.publishAtChanged);
        }

        private void parameterlessCommandAction(object target)
        {
            switch (target)
            {
                case "resetstatus":
                    this.resetUploadStatus();
                    break;
                case "pause":
                    this.setPausedUploadStatus();
                    break;
                case "addthumbnail":
                    this.openThumbnailDialog();
                    break;
                case "resetthumbnail":
                    this.resetThumbnail();
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for parameterlessCommandAction.");
                    break;
            }
        }

        private void attributeReset(AttributeResetMessage attributeResetMessage)
        {
            if (attributeResetMessage.Upload == this.upload)
            {
                if (attributeResetMessage.Attribute == "all")
                {
                    this.raisePropertyChanged(null);
                }

                if (attributeResetMessage.Attribute == "title")
                {
                    this.raiseTitleProperties();
                }

                if (attributeResetMessage.Attribute == "description")
                {
                    this.raiseDescriptionProperties();
                }

                if (attributeResetMessage.Attribute == "tags")
                {
                    this.raiseTagsProperties();
                }

                if (attributeResetMessage.Attribute == "visibility")
                {
                    this.raisePropertyChanged("SelectedVisibility");
                }

                if (attributeResetMessage.Attribute == "videoLanguage")
                {
                    this.raisePropertyChanged("SelectedVideoLanguage");
                }

                if (attributeResetMessage.Attribute == "descriptionLanguage")
                {
                    this.raisePropertyChanged("SelectedDescriptionLanguage");
                }

                if (attributeResetMessage.Attribute == "publishAt")
                {
                    this.raisePublishAtProperties();
                }

                if (attributeResetMessage.Attribute == "playlist")
                {
                    this.raisePropertyChanged("SelectedPlaylist");
                    this.raisePropertyChanged("ShowPlaylistHint");
                }

                if (attributeResetMessage.Attribute == "category")
                {
                    this.raisePropertyChanged("SelectedCategory");
                }
            }
        }

        private void bytesSent(BytesSentMessage bytesSentMessage)
        {
            if (bytesSentMessage.Upload == this.upload)
            {
                this.raisePropertyChanged("UploadedTotalInfo");
            }
        }

        private void bytesSent(ResumableSessionUriChangedMessage resumableSessionUriChangedMessage)
        {
            if (resumableSessionUriChangedMessage.Upload == this.upload)
            {
                this.raisePropertyChanged("ControlsEnabled");
                this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
            }
        }

        private void uploadStatusChanged(UploadStartingMessage uploadStartingMessage)
        {
            this.updateUploadProperties(uploadStartingMessage.Upload);
        }

        private void uploadStatusChanged(UploadFinishedMessage uploadFinishedMessage)
        {
            this.updateUploadProperties(uploadFinishedMessage.Upload);
        }

        private void uploadStatusChanged(UploadStatusChangedMessage newUploadStatusChangedMessage)
        {
            this.updateUploadProperties(newUploadStatusChangedMessage.Upload);
        }

        private void updateUploadProperties(Upload upload)
        {
            if (upload == this.upload)
            {
                this.raiseUploadStatusProperties();
                this.raisePropertyChanged("UploadStatusColorAnimation");
                this.raisePropertyChanged("UploadStart");
                this.raisePropertyChanged("UploadEnd");
                this.raisePropertyChanged("StateCommandsEnabled");
                this.raisePropertyChanged("ResetStateCommandEnabled");
                this.raisePropertyChanged("WarningColor");
                this.raisePropertyChanged("UploadErrorMessage");
                this.raisePropertyChanged("ControlsEnabled");
                this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
                this.raisePropertyChanged("UploadedTotalInfo");
            }
        }

        private void resumableSessionUriChanged(ResumableSessionUriChangedMessage resumableSessionUriChangedMessage)
        {
            this.raisePropertyChanged("ControlsEnabled");
            this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
        }

        private void publishAtChanged(PublishAtChangedMessage publishAtChangedMessage)
        {
            if (this.upload == publishAtChangedMessage.Upload)
            {
                this.raisePublishAtProperties();
            }
        }

        private void raisePublishAtProperties()
        {
            this.raisePropertyChanged("PublishAt");
            this.raisePropertyChanged("PublishAtDate");
            this.raisePropertyChanged("PublishAtTime");
            this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");
            this.raisePropertyChanged("SelectedVisibility");
        }

        private void raiseUploadStatusProperties()
        {
            this.raisePropertyChanged("UploadStatus");
            this.raisePropertyChanged("UploadStatusColor");
        }

        private void raiseTitleProperties()
        {
            this.raisePropertyChanged("Title");
            this.raisePropertyChanged("YtTitle");
            this.raisePropertyChanged("TitleColor");
            this.raisePropertyChanged("TitleCharacterCount");
            this.raisePropertyChanged("ResetStateCommandEnabled");
            this.raiseUploadStatusProperties();
            this.raiseUploadStatusProperties();
        }

        private void raiseDescriptionProperties()
        {
            this.raisePropertyChanged("Description");
            this.raisePropertyChanged("DescriptionColor");
            this.raisePropertyChanged("DescriptionCharacterCount");
            this.raiseUploadStatusProperties();
            this.raisePropertyChanged("ResetStateCommandEnabled");
        }
        private void raiseTagsProperties()
        {
            this.raisePropertyChanged("TagsAsString");
            this.raisePropertyChanged("TagsColor");
            this.raisePropertyChanged("TagsCharacterCount");
            this.raiseUploadStatusProperties();
            this.raisePropertyChanged("ResetStateCommandEnabled");
        }

        private void removeComboBoxValue(object parameter)
        {
            if (!this.ControlsEnabled)
            {
                MessageBox.Show("Upload cannot be modified if upload is started.");
                return;
            }

            switch (parameter)
            {
                case "template":
                    this.SelectedTemplate = null;
                    break;
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

        private void raisePropertyChanged(string propertyName)
        {
            // take a copy to prevent thread issues
            PropertyChangedEventHandler handler = this.PropertyChanged;
            if (handler != null)
            {
                handler(this, new PropertyChangedEventArgs(propertyName));
            }
        }

        private bool resetUploadStatus()
        {
            if (!this.upload.VerifyForUpload())
            {
                return false;
            }

            if (this.upload.BytesSent > 0 && this.upload.UploadStatus != UplStatus.Stopped &&
                this.upload.UploadStatus != UplStatus.Finished)
            {
                this.upload.UploadStatus = UplStatus.Stopped;
            }
            else
            {
                this.upload.UploadStatus = UplStatus.ReadyForUpload;
            }

            JsonSerializationContent.JsonSerializer.SerializeAllUploads();

            this.raiseUploadStatusProperties();
            this.raisePropertyChanged("UploadStatusColorAnimation");
            this.raisePropertyChanged("UploadedTotalInfo");
            this.raisePropertyChanged("UploadStart");
            this.raisePropertyChanged("UploadEnd");
            this.raisePropertyChanged("UploadErrorMessage");
            this.raisePropertyChanged("StateCommandsEnabled");
            this.raisePropertyChanged("ResetStateCommandEnabled");
            this.raisePropertyChanged("ControlsEnabled");
            this.raisePropertyChanged("PublishAtDateTimeControlsEnabled");

            return true;
        }

        private void setPausedUploadStatus()
        {
            this.upload.UploadStatus = UplStatus.Paused;
            JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            this.raiseUploadStatusProperties();
        }

        private void openThumbnailDialog()
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            OpenFileDialog fileDialog = new OpenFileDialog();

            if (!string.IsNullOrWhiteSpace(this.ThumbnailFilePath))
            {
                fileDialog.InitialDirectory = Path.GetDirectoryName(this.ThumbnailFilePath);
            }
            else
            {
                if (this.upload.Template != null)
                {
                    if (!string.IsNullOrWhiteSpace(this.upload.Template.ThumbnailFolderPath))
                    {
                        fileDialog.InitialDirectory = this.upload.Template.ThumbnailFolderPath;
                    }
                    else 
                    {
                        if (!string.IsNullOrWhiteSpace(this.upload.Template.RootFolderPath))
                        {
                            fileDialog.InitialDirectory = this.upload.Template.RootFolderPath;
                        }
                    }
                }
            }

            DialogResult result = fileDialog.ShowDialog();


            if (result == DialogResult.OK)
            {
                this.ThumbnailFilePath = fileDialog.FileName;
            }
        }

        private void resetThumbnail()
        {
            if (this.UploadStatus == UplStatus.Finished)
            {
                MessageBox.Show("Thumbnail cannot be set if upload is finished. Please clear upload list from finished uploads.");
                return;
            }

            string oldFilePath = this.upload.ThumbnailFilePath;
            this.ThumbnailFilePath = null;
        }

        private void resetToTemplateValue(object parameter)
        {
            if (this.upload.Template != null)
            { 
                switch (parameter)
                {
                    case "title":
                        this.upload.CopyTitleFromTemplate();
                        this.raiseTitleProperties();
                        break;
                    case "description":
                        this.upload.CopyDescriptionFromTemplate();
                        this.raiseDescriptionProperties();
                        break;
                    case "tags":
                        this.upload.CopyTagsFromtemplate();
                        this.raiseTagsProperties();
                        break;
                    case "visibility":
                        this.upload.CopyVisibilityFromTemplate();
                        this.raisePropertyChanged("SelectedVisibility");
                        break;
                    case "playlist":
                        this.upload.CopyPlaylistFromTemplate();
                        this.raisePropertyChanged("ShowPlaylistHint");
                        this.raisePropertyChanged("SelectedPlaylist");
                        break;
                    case "videolanguage":
                        this.upload.CopyVideoLanguageFromTemplate();
                        this.raisePropertyChanged("SelectedVideoLanguage");
                        break;
                    case "descriptionlanguage":
                        this.upload.CopyDescriptionLanguageFromTemplate();
                        this.raisePropertyChanged("SelectedDescriptionLanguage");
                        break;
                    case "publishat":
                        this.upload.AutoSetPublishAtDateTime();
                        this.raisePublishAtProperties();
                        break;
                    case "category":
                        this.upload.CopyCategoryFromTemplate();
                        this.raisePropertyChanged("SelectedCategory");
                        break;
                    case "account":
                        this.upload.CopyYoutubeAccountFromTemplate();
                        this.raisePropertyChanged("SelectedYoutubeAccount");
                        break;
                    case "all":
                        this.upload.CopyTemplateValues();
                        this.raisePropertyChanged(null);
                        break;
                    default:
                        throw new InvalidOperationException("No parameter for resetToTemplateValue specified.");
                        break;
                }

                JsonSerializationContent.JsonSerializer.SerializeAllUploads();
            }
        }

        public void Dispose()
        {
            if (this.attributeResetSubscription != null)
            {
                this.attributeResetSubscription.Dispose();
            }

            if (this.bytesSentSubscription != null)
            {
                this.bytesSentSubscription.Dispose();
            }

            if (this.uploadStartingSubscription != null)
            {
                this.uploadStartingSubscription.Dispose();
            }

            if (this.uploadFinishedSubscription != null)
            {
                this.uploadFinishedSubscription.Dispose();
            }

            if (this.uploadStatusChangedSubscription != null)
            {
                this.uploadStatusChangedSubscription.Dispose();
            }

            if (this.resumableSessionUriChangedSubscription != null)
            {
                this.resumableSessionUriChangedSubscription.Dispose();
            }

            if (this.publishAtChangedSubscription != null)
            {
                this.publishAtChangedSubscription.Dispose();
            }

            if (this.errorMessageChangedSubscription != null)
            {
                this.errorMessageChangedSubscription.Dispose();
            }
        }
    }
}
