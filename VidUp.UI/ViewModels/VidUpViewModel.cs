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
            this.aboutCommand = new GenericCommand(this.openAboutDialog);
            this.donateCommand = new GenericCommand(this.openDonateDialog);
        }

        private async void openAboutDialog(object obj)
        {
            var view = new AboutControl
            {
                DataContext = new AboutViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
        }

        private async void openDonateDialog(object obj)
        {
            var view = new DonateControl
            {
                // DataContext = new DonateViewModel()
            };

            bool result = (bool)await DialogHost.Show(view, "RootDialog");
        }
    }
}
