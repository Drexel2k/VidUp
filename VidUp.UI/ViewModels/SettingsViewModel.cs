using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Windows.Documents;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Settings;
using Drexel.VidUp.Utils;


namespace Drexel.VidUp.UI.ViewModels
{
    //todo: move ribbon properties to separate view model
    public class SettingsViewModel : INotifyPropertyChanged
    {
        private ObservableCollection<CultureViewModel> observableCultureInfoViewModels;
        private string searchText = string.Empty;
        public event PropertyChangedEventHandler PropertyChanged;

        #region properties

        public bool Tracing
        {
            get { return Settings.SettingsInstance.UserSettings.Trace; }
            set
            {
                Settings.SettingsInstance.UserSettings.Trace = value;
                JsonSerializationSettings.JsonSerializer.SerializeSettings();
                this.raisePropertyChanged("Tracing");
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
        
        #endregion properties

        public SettingsViewModel()
        {
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

                        for (int index = 0; index < checkedCultureViewModels.Count; index++)
                        {
                            if (this.observableCultureInfoViewModels[index] != checkedCultureViewModels[index])
                            {
                                this.observableCultureInfoViewModels.Insert(index, checkedCultureViewModels[index]);
                            }
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
                Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Clear();
                if (checkedCultureViewModelsUpdated.Count >= 0)
                {
                    foreach (CultureViewModel cultureViewModelUpdated in checkedCultureViewModelsUpdated)
                    {
                        Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Add(cultureViewModelUpdated.Name);
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
                if (Settings.SettingsInstance.UserSettings.VideoLanguagesFilter != null)
                {
                    if (Settings.SettingsInstance.UserSettings.VideoLanguagesFilter.Contains(cultureInfo.Name))
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
