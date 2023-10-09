using System;
using System.ComponentModel;
using Drexel.VidUp.Business;
using Drexel.VidUp.Json.Content;
using Drexel.VidUp.UI.Controls;
using Drexel.VidUp.UI.EventAggregation;
using MaterialDesignThemes.Wpf;


namespace Drexel.VidUp.UI.ViewModels
{
    public class SettingsRibbonViewModel : INotifyPropertyChanged
    {
        private YoutubeAccountList youtubeAccountList;
        private ObservableYoutubeAccountViewModels observableYoutubeAccountViewModels;
        private YoutubeAccountComboboxViewModel selectedYoutubeAccount;

        public event PropertyChangedEventHandler PropertyChanged;

        private GenericCommand newYoutubeAccountCommand;

        #region properties

        public YoutubeAccountComboboxViewModel SelectedYoutubeAccount
        {
            get => this.selectedYoutubeAccount;
            set
            {
                YoutubeAccount oldYoutubeAccount = this.selectedYoutubeAccount.YoutubeAccount;
                this.selectedYoutubeAccount = value;            
                EventAggregator.Instance.Publish(new SelectedYoutubeAccountChangedMessage(oldYoutubeAccount, value.YoutubeAccount));
                this.raisePropertyChanged("SelectedYoutubeAccount");
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

                JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();

                this.raisePropertyChanged("SelectedYoutubeAccountName");

            }
        }

        public GenericCommand NewYoutubeAccountCommand
        {
            get => this.newYoutubeAccountCommand;
        }

        #endregion properties

        public SettingsRibbonViewModel(YoutubeAccountList youtubeAccountList)
        {
            if (youtubeAccountList == null)
            {
                throw new ArgumentException("YoutubeAccountList must not be null.");
            }

            this.youtubeAccountList = youtubeAccountList;
            this.observableYoutubeAccountViewModels = new ObservableYoutubeAccountViewModels(this.youtubeAccountList, false);
            this.selectedYoutubeAccount = this.observableYoutubeAccountViewModels[0];

            this.newYoutubeAccountCommand = new GenericCommand(this.openNewYoutubeAccountDialogAsync);

            EventAggregator.Instance.Subscribe<YoutubeAccountDeleteMessage>(this.deleteYoutubeAccountAsync);
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
                NewYoutubeAccountViewModel data = (NewYoutubeAccountViewModel)view.DataContext;
                
                YoutubeAccount youtubeAccount = new YoutubeAccount(data.Name);
                this.youtubeAccountList.AddYoutubeAccount(youtubeAccount);
                JsonSerializationContent.JsonSerializer.SerializeYoutubeAccountList();

                this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels.GetViewModel(youtubeAccount);
            }
        }

        private async void deleteYoutubeAccountAsync(YoutubeAccountDeleteMessage youtubeAccountDeleteMessage)
        {
            if (this.youtubeAccountList.AccountCount <= 1)
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
                bool result = (bool) await DialogHost.Show(control, "RootDialog");
                if (result)
                {
                    if (this.observableYoutubeAccountViewModels[0].YoutubeAccount.Name == this.selectedYoutubeAccount.YoutubeAccount.Name)
                    {
                        this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels[1];
                    }
                    else
                    {
                        this.SelectedYoutubeAccount = this.observableYoutubeAccountViewModels[0];
                    }

                    EventAggregator.Instance.Publish(new BeforeYoutubeAccountDeleteMessage(youtubeAccountDeleteMessage.YoutubeAccount));
                    this.youtubeAccountList.Delete(youtubeAccountDeleteMessage.YoutubeAccount);
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
