using Drexel.VidUp.UI.Controls;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    public class VidUpViewModel
    {
        private GenericCommand aboutCommand;
        private GenericCommand donateCommand;

        public GenericCommand AboutCommand
        {
            get
            {
                return this.aboutCommand;
            }
        }

        public GenericCommand DonateCommand
        {
            get
            {
                return this.donateCommand;
            }
        }

        public VidUpViewModel()
        {
            this.aboutCommand = new GenericCommand(this.openAboutDialogAsync);
            this.donateCommand = new GenericCommand(this.openDonateDialogAsync);
        }

        private async void openAboutDialogAsync(object obj)
        {
            var view = new AboutControl
            {
                DataContext = new AboutViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog").ConfigureAwait(false);
        }

        private async void openDonateDialogAsync(object obj)
        {
            var view = new DonateControl
            {
                // DataContext = new DonateViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog").ConfigureAwait(false);
        }
    }
}
