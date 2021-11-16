using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.IO;
using System.Linq;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.Utils;
using MaterialDesignThemes.Wpf;


namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CultureViewModel> observableCultureInfoViewModels;
        private string searchText = string.Empty;

        private YouTubeAccountList youTubeAccounts;
        private ObservableYouTubeAccountViewModels observableYouTubeAccountViewModels;
        private YouTubeAccountComboboxViewModel selectedYouTubeAccount;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newYouTubeAccountCommand;

        #region properties

        public GenericCommand NewYouTubeAccountCommand
        {
            get
            {
                return this.newYouTubeAccountCommand;
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

        public YouTubeAccountComboboxViewModel SelectedYouTubeAccount
        {
            get => this.selectedYouTubeAccount;
            set
            {
                this.selectedYouTubeAccount = value;
                this.raisePropertyChanged("SelectedYouTubeAccount");
                this.raisePropertyChanged("SelectedYouTubeAccountName");
                this.raisePropertyChanged("SelectedYouTubeAccountFilePath");
            }
        }

        public ObservableYouTubeAccountViewModels ObservableYouTubeAccountViewModels
        {
            get
            {
                return this.observableYouTubeAccountViewModels;
            }
        }

        public string SelectedYouTubeAccountName
        {
            get => this.selectedYouTubeAccount.YouTubeAccountName;
            set
            { 
                this.selectedYouTubeAccount.YouTubeAccountName = value;
                this.raisePropertyChanged("SelectedYouTubeAccountName");
                this.raisePropertyChanged("SelectedYouTubeAccountFilePath");
            }
        }

        public string SelectedYouTubeAccountFilePath
        {
            get => this.selectedYouTubeAccount.YouTubeAccountFilePath;
        }

        #endregion properties

        public SettingsViewModel(YouTubeAccountList youTubeAccountList, ObservableYouTubeAccountViewModels observableYouTubeAccountViewModels)
        {
            if (youTubeAccountList == null)
            {
                throw new ArgumentException("YouTubeAccountList must not be null.");
            }

            if (observableYouTubeAccountViewModels == null)
            {
                throw new ArgumentException("ObservableYouTubeAccountViewModels action must not be null.");
            }

            this.youTubeAccounts = youTubeAccountList;
            this.observableYouTubeAccountViewModels = observableYouTubeAccountViewModels;
            this.selectedYouTubeAccount = this.observableYouTubeAccountViewModels[0];

            this.newYouTubeAccountCommand = new GenericCommand(this.openNewYouTubeAccountDialohAsync);

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

        public async void openNewYouTubeAccountDialohAsync(object obj)
        {
            var view = new NewYouTubeAccountControl
            {
                DataContext = new NewYouTubeAccountViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
            if (result)
            {
                string finalName;
                NewYouTubeAccountViewModel data = (NewYouTubeAccountViewModel)view.DataContext;
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
                YouTubeAccount youTubeAccount = new YouTubeAccount(newFilePath, finalName);
                List<YouTubeAccount> list = new List<YouTubeAccount>();
                list.Add(youTubeAccount);
                this.youTubeAccounts.AddYouTubeAccounts(new List<YouTubeAccount>(list));

                this.SelectedYouTubeAccount = this.observableYouTubeAccountViewModels.GetViewModel(youTubeAccount);
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
