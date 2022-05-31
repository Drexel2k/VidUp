using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.AuthenticationService;
using Drexel.VidUp.Youtube.Http;
using MaterialDesignThemes.Wpf;


namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CultureViewModel> observableCultureInfoViewModels;
        private string searchText = string.Empty;

        private YoutubeAccount youtubeAccount;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand parameterlessCommand;
        private object authenticateYoutubeAccountLock = new object();
        private bool authenticatingYoutubeAccount = false;

        #region properties

        public YoutubeAccount YoutubeAccount
        {
            get
            {
                return this.youtubeAccount;
            }
            set
            {
                if (this.youtubeAccount != value)
                {
                    this.youtubeAccount = value;
                    //all properties changed
                    this.raisePropertyChanged(null);
                }
            }
        }

        public string YoutubeAccountName
        {
            get => this.youtubeAccount.Name;
            set
            {
                this.youtubeAccount.Name = value;
                JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();

                this.raisePropertyChanged("YoutubeAccountName");
                EventAggregator.Instance.Publish(new YoutubeAccountDisplayPropertyChangedMessage("name"));
            }
        }

        public bool Tracing
        {
            get { return Settings.Instance.UserSettings.Trace; }
            set
            {
                Settings.Instance.UserSettings.Trace = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("Tracing");
            }
        }

        public bool UseCustomYouTubeApiCredentials
        {
            get { return Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials; }
            set
            {
                if (value != Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials)
                {
                    HttpHelper.ClearAccessTokens();
                }

                Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("UseCustomYouTubeApiCredentials");
            }
        }

        public string ClientId
        {
            get => Settings.Instance.UserSettings.ClientId;
            set
            {
                if (value != Settings.Instance.UserSettings.ClientId && Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials)
                {
                    HttpHelper.ClearAccessTokens();
                }

                Settings.Instance.UserSettings.ClientId = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("ClientId");
            }
        }

        public string ClientSecret
        {
            get => Settings.Instance.UserSettings.ClientSecret;
            set
            {
                if (value != Settings.Instance.UserSettings.ClientSecret && Settings.Instance.UserSettings.UseCustomYouTubeApiCredentials)
                {
                    HttpHelper.ClearAccessTokens();
                }

                Settings.Instance.UserSettings.ClientSecret = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("ClientSecret");
            }
        }

        public TraceLevel SelectedTraceLevel
        {
            get { return Settings.Instance.UserSettings.TraceLevel; }
            set
            {
                Settings.Instance.UserSettings.TraceLevel = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("SelectedTraceLevel");
            }
        }

        public Array TraceLevels
        {
            get
            {
                return Enum.GetValues(typeof(TraceLevel));
            }
        }

        public string SearchText
        {
            get => this.searchText;
            set
            {
                this.searchText = value;
                this.refreshObservableCultureInfoViewmodels();
            }
        }

        public ObservableCollection<CultureViewModel> ObservableCultureInfoViewModels
        {
            get => this.observableCultureInfoViewModels;
        }

        public GenericCommand ParameterlessCommand
        {
            get => this.parameterlessCommand;
        }

        #endregion properties

        public SettingsViewModel(YoutubeAccount youtubeAccount)
        {
            this.youtubeAccount = youtubeAccount;

            this.parameterlessCommand = new GenericCommand(this.parameterlessCommandAction);

            this.observableCultureInfoViewModels = new ObservableCollection<CultureViewModel>();

            this.refreshObservableCultureInfoViewmodels();

            EventAggregator.Instance.Subscribe<SelectedYoutubeAccountChangedMessage>(this.selectedYoutubeAccountChanged);
        }

        private void selectedYoutubeAccountChanged(SelectedYoutubeAccountChangedMessage selectedYoutubeAccountChangedMessage)
        {
            //raises all properties changed
            this.YoutubeAccount = selectedYoutubeAccountChangedMessage.NewYoutubeAccount;
        }

        private void cultureViewModelPropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "IsChecked")
            {
                CultureViewModel cultureViewModel = (CultureViewModel)sender;
                int currentIndex = this.observableCultureInfoViewModels.IndexOf(cultureViewModel);
                this.observableCultureInfoViewModels.RemoveAt(currentIndex);

                if (cultureViewModel.IsChecked)
                {
                    List<CultureViewModel> checkedCultureViewModels = this.observableCultureInfoViewModels.Where(cultureViewModel => cultureViewModel.IsChecked).ToList();
                    if (checkedCultureViewModels.Count == 0)
                    {
                        this.observableCultureInfoViewModels.Insert(0, cultureViewModel);
                    }
                    else
                    {
                        checkedCultureViewModels.Add(cultureViewModel);
                        checkedCultureViewModels.Sort((cultureViewModel1, cultureViewModel2) => cultureViewModel1.Name.CompareTo(cultureViewModel2.Name));

                        bool inserted = false;
                        for (int index = 0; index < this.observableCultureInfoViewModels.Count; index++)
                        {
                            if (this.observableCultureInfoViewModels[index] != checkedCultureViewModels[index])
                            {
                                this.observableCultureInfoViewModels.Insert(index, checkedCultureViewModels[index]);
                                inserted = true;
                                break;
                            }
                        }

                        //all items selected, so new checked playlist must be at the end
                        if (!inserted)
                        {
                            this.observableCultureInfoViewModels.Add(cultureViewModel);
                        }
                    }
                }
                else
                {
                    bool inserted = false;
                    for (int index = 0; index < this.observableCultureInfoViewModels.Count; index++)
                    {
                        if (!this.observableCultureInfoViewModels[index].IsChecked)
                        {
                            if (this.observableCultureInfoViewModels[index].Name.CompareTo(cultureViewModel.Name) > 0)
                            {
                                this.observableCultureInfoViewModels.Insert(index, cultureViewModel);
                                inserted = true;
                                break;
                            }
                        }
                    }

                    //if unchecked cultureViewModel is the last position in order
                    if (!inserted)
                    {
                        this.observableCultureInfoViewModels.Add(cultureViewModel);
                    }
                }
                
                List<CultureViewModel> checkedCultureViewModelsUpdated = this.observableCultureInfoViewModels.Where(cultureViewModel => cultureViewModel.IsChecked).ToList();

                if (Settings.Instance.UserSettings.VideoLanguagesFilter == null)
                {
                    Settings.Instance.UserSettings.VideoLanguagesFilter = new List<string>();
                }
                else
                {
                    Settings.Instance.UserSettings.VideoLanguagesFilter.Clear();
                }

                if (checkedCultureViewModelsUpdated.Count >= 0)
                {
                    foreach (CultureViewModel cultureViewModelUpdated in checkedCultureViewModelsUpdated)
                    {
                        Settings.Instance.UserSettings.VideoLanguagesFilter.Add(cultureViewModelUpdated.Name);
                    }
                }

                Cultures.SetRelevantCultures();
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
            }
        }

        private void refreshObservableCultureInfoViewmodels()
        {
            this.observableCultureInfoViewModels.Clear();

            string searchText = this.searchText.ToLower();
            List<CultureViewModel> selectedViewModels = new List<CultureViewModel>();
            List<CultureViewModel> notSelectedViewModels = new List<CultureViewModel>();
            foreach (CultureInfo cultureInfo in Cultures.AllCultureInfos)
            {
                CultureViewModel cultureViewModel;
                if (Settings.Instance.UserSettings.VideoLanguagesFilter != null &&
                    Settings.Instance.UserSettings.VideoLanguagesFilter.Contains(cultureInfo.Name))
                {
                    cultureViewModel = new CultureViewModel(cultureInfo, true);
                    cultureViewModel.PropertyChanged += cultureViewModelPropertyChanged;
                    selectedViewModels.Add(cultureViewModel);
                }
                else
                {
                    if (cultureInfo.Name.ToLower().Contains(searchText) || cultureInfo.EnglishName.ToLower().Contains(searchText))
                    {
                        cultureViewModel = new CultureViewModel(cultureInfo, false);
                        cultureViewModel.PropertyChanged += cultureViewModelPropertyChanged;
                        notSelectedViewModels.Add(cultureViewModel);
                    }
                }
            }

            foreach (CultureViewModel selectedViewModel in selectedViewModels)
            {
                this.observableCultureInfoViewModels.Add(selectedViewModel);
            }

            foreach (CultureViewModel notSelectedViewModel in notSelectedViewModels)
            {
                this.observableCultureInfoViewModels.Add(notSelectedViewModel);
            }
        }

        private void parameterlessCommandAction(object target)
        {
            switch (target)
            {
                case "signin":
                    this.signInYoutubeAccountAsync();
                    break;
                case "signout":
                    this.signOutYoutubeAccount();
                    break;
                case "delete":
                    this.deleteYoutubeAccount();
                    break;
                default:
                    throw new InvalidOperationException("Invalid parameter for parameterlessCommandAction.");
                    break;
            }
        }

        private async void signInYoutubeAccountAsync()
        {
            Tracer.Write($"SettingsViewModel.signInYoutubeAccountAsync: Start.");
            lock (this.authenticateYoutubeAccountLock)
            {
                if (this.authenticatingYoutubeAccount)
                {
                    return;
                }

                this.authenticatingYoutubeAccount = true;
            }

            try
            {
                await YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync(this.youtubeAccount);
            }
            catch (Exception e)
            {
                string message = "Authentication error";
                AuthenticationException authenticationException = e as AuthenticationException;
                if (authenticationException != null)
                {
                    if (authenticationException.IsApiResponseError)
                    {
                        message += ", server denied authentication";
                    }
                }

                Tracer.Write($"SettingsViewModel.signInYoutubeAccountAsync: YoutubeAuthentication.SetRefreshTokenOnYoutubeAccountAsync Exception: {e.ToString()}.");
                ConfirmControl control = new ConfirmControl($"{message}: {e.GetType().Name}: {e.Message}.", false);

                await DialogHost.Show(control, "RootDialog").ConfigureAwait(false);
            }

            this.authenticatingYoutubeAccount = false;
            EventAggregator.Instance.Publish(new YoutubeAccountStatusChangedMessage());

            Tracer.Write($"SettingsViewModel.signInYoutubeAccountAsync: End.");
        }

        private void signOutYoutubeAccount()
        {
            Tracer.Write($"SettingsViewModel.signOutYoutubeAccountAsync: Start.");
            YoutubeAuthentication.DeleteRefreshTokenOnYoutubeAccountAsync(this.youtubeAccount);
            JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();
            EventAggregator.Instance.Publish(new YoutubeAccountStatusChangedMessage());
            Tracer.Write($"SettingsViewModel.signOutYoutubeAccountAsync: End.");
        }

        private void deleteYoutubeAccount()
        {
            EventAggregator.Instance.Publish(new YoutubeAccountDeleteMessage(this.youtubeAccount));
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
    }
}
