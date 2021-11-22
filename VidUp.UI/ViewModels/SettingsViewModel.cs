using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using Drexel.VidUp.Utils;
using Drexel.VidUp.Youtube.Authentication;
using MaterialDesignThemes.Wpf;


namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CultureViewModel> observableCultureInfoViewModels;
        private string searchText = string.Empty;

        private YoutubeAccountList youtubeAccounts;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccountComboboxViewModel selectedYoutubeAccount;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newYoutubeAccountCommand;
        private GenericCommand youtubeAccountAuthenticateCommand;
        private GenericCommand youtubeAccountDeleteCommand;
        private object authenticateYoutubeAccountLock = new object();
        private bool authenticatingYoutubeAccount = false;

        #region properties

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

        public YoutubeAccountComboboxViewModel SelectedYoutubeAccount
        {
            get => this.selectedYoutubeAccount;
            set
            {
                this.selectedYoutubeAccount = value;
                this.raisePropertyChanged("SelectedYoutubeAccount");
                this.raisePropertyChanged("SelectedYoutubeAccountName");
                this.raisePropertyChanged("SelectedYoutubeAccountFilePath");
            }
        }

        public ObservableYoutubeAccountViewModels ObservableYoutubeAccountViewModels
        {
            get
            {
                return this.observableYoutubeAccountViewModels;
            }
        }

        public string SelectedYoutubeAccountName
        {
            get => this.selectedYoutubeAccount.YoutubeAccount.Name;
            set
            { 
                this.selectedYoutubeAccount.YoutubeAccount.Name = value;
                this.raisePropertyChanged("SelectedYoutubeAccountName");
                this.raisePropertyChanged("SelectedYoutubeAccountFilePath");
            }
        }

        public string SelectedYoutubeAccountFilePath
        {
            get => this.selectedYoutubeAccount.YoutubeAccount.FilePath;
        }

        public GenericCommand NewYoutubeAccountCommand
        {
            get => this.newYoutubeAccountCommand;
        }

        public GenericCommand YoutubeAccountAuthenticateCommand
        {
            get => this.youtubeAccountAuthenticateCommand;
        }

        public GenericCommand YoutubeAccountDeleteCommand
        {
            get => this.youtubeAccountDeleteCommand;
        }

        public bool AuthenticatingYoutubeAccount
        {
            get => authenticatingYoutubeAccount;
        }

        #endregion properties

        public SettingsViewModel(YoutubeAccountList youtubeAccountList, ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels)
        {
            if (youtubeAccountList == null)
            {
                throw new ArgumentException("YoutubeAccountList must not be null.");
            }

            if (observableYoutubeAccountViewModels == null)
            {
                throw new ArgumentException("ObservableYoutubeAccountViewModels action must not be null.");
            }

            this.youtubeAccounts = youtubeAccountList;
            this.observableYoutubeAccountViewModels = observableYoutubeAccountViewModels;
            this.selectedYoutubeAccount = this.observableYoutubeAccountViewModels[0];

            this.newYoutubeAccountCommand = new GenericCommand(this.openNewYoutubeAccountDialogAsync);
            this.youtubeAccountAuthenticateCommand = new GenericCommand(this.authenticateYoutubeAccountAsync);
            this.youtubeAccountDeleteCommand = new GenericCommand(this.deleteYoutubeAccountAsync);

            this.observableCultureInfoViewModels = new ObservableCollection<CultureViewModel>();

            this.refreshObservableCultureInfoViewmodels();
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

        public async void openNewYoutubeAccountDialogAsync(object obj)
        {
            var view = new NewYoutubeAccountControl
            {
                DataContext = new NewYoutubeAccountViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                string finalName;
                NewYoutubeAccountViewModel data = (NewYoutubeAccountViewModel)view.DataContext;
                string name = string.Concat(data.Name.Split(Path.GetInvalidFileNameChars()));

                //is needed if file aready exists as the original name shall not be overwritten including the suffix
                //as long as counting up
                finalName = name;

                if (string.IsNullOrWhiteSpace(name))
                {
                    
                    name = "default";
                    finalName = name;
                }

                string newFilePath = Path.Combine(Settings.Instance.StorageFolder, $"uploadrefreshtoken_{name}");

                int index = 1;
                while (File.Exists(newFilePath))
                {
                    newFilePath = Path.Combine(Settings.Instance.StorageFolder, $"uploadrefreshtoken_{name}{index}");
                    finalName = $"{name}{index}";
                    index++;
                }

                File.Create(newFilePath);
                YoutubeAccount youtubeAccount = new YoutubeAccount(newFilePath, finalName);
                List<YoutubeAccount> list = new List<YoutubeAccount>();
                list.Add(youtubeAccount);
                this.youtubeAccounts.AddYoutubeAccounts(new List<YoutubeAccount>(list));

                this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels.GetViewModel(youtubeAccount);
            }
        }

        private async void authenticateYoutubeAccountAsync(object obj)
        {
            lock (this.authenticateYoutubeAccountLock)
            {
                if (this.authenticatingYoutubeAccount)
                {
                    return;
                }

                this.authenticatingYoutubeAccount = true;
                this.raisePropertyChanged("AuthenticatingYoutubeAccount");
            }

            await YoutubeAuthentication.GetRefreshTokenAsync(this.SelectedYoutubeAccountName).ConfigureAwait(false);

            this.authenticatingYoutubeAccount = false;
            this.raisePropertyChanged("AuthenticatingYoutubeAccount");
        }

        private async void deleteYoutubeAccountAsync(object obj)
        {
            if (this.youtubeAccounts.AccountCount <= 1)
            {
                ConfirmControl control = new ConfirmControl(
                    $"You cannot delete the last Youtube account, at least one account must be left. Rename or reauthenticate (relink) the account or add a new account first.", false);

                await DialogHost.Show(control, "RootDialog").ConfigureAwait(false);
            }
            else
            {
                ConfirmControl control = new ConfirmControl(
                    $"WARNING! If you delete an account, all content (uploads, templates, playlists) belonging to this account will be deleted!", true);

                //a collection is change later, so we must return to the Gui thread.
                bool result = (bool) await DialogHost.Show(control, "RootDialog").ConfigureAwait(true);
                if (result)
                {
                    string accountName = this.selectedYoutubeAccount.YoutubeAccount.Name;
                    if (this.observableYoutubeAccountViewModels[0].YoutubeAccount.Name == this.selectedYoutubeAccount.YoutubeAccount.Name)
                    {
                        this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels[1];
                    }
                    else
                    {
                        this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels[0];
                    }

                    EventAggregator.Instance.Publish(new BeforeYoutubeAccountDeleteMessage(this.youtubeAccounts.GetYoutubeAccount(accountName)));
                    this.youtubeAccounts.Remove(accountName);
                    JsonSerializationContent.JsonSerializer.SerializePlaylistList();
                    JsonSerializationContent.JsonSerializer.SerializeTemplateList();
                    JsonSerializationContent.JsonSerializer.SerializeAllUploads();
                    JsonSerializationContent.JsonSerializer.SerializeUploadList();
                }
            }
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
