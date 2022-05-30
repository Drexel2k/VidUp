using Drexel.VidUp.UI.Controls;
using MaterialDesignThemes.Wpf;

namespace Drexel.VidUp.UI.ViewModels
{
    public class VidUpRibbonViewModel
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

        public VidUpRibbonViewModel()
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

            await DialogHost.Show(view, "RootDialog").ConfigureAwait(false);
        }

        private async void openDonateDialogAsync(object obj)
        {
            var view = new DonateControl
            {
                // DataContext = new DonateViewModel()
            };

            await DialogHost.Show(view, "RootDialog").ConfigureAwait(false);
        }
    }
}
