using System.Windows;
using System.Windows.Controls;

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaction logic for ConfirmControl.xaml
    /// </summary>
    public partial class ConfirmControl : UserControl
    {
        public ConfirmControl(string confirmationText, bool showCancelButton)
        {
            InitializeComponent();
            this.ConfirmationTextTitle.Text = "Confirmation";
            this.ConfirmationTextContent.Text = confirmationText;
            this.ContentGrid.Width = 300;

            if (!showCancelButton)
            {
                this.CancelButton.Visibility = Visibility.Hidden;
            }
        }

        public ConfirmControl(string title, string confirmationText, bool showCancelButton, int width)
        {
            InitializeComponent();
            this.ConfirmationTextTitle.Text = title;
            this.ConfirmationTextContent.Text = confirmationText;
            this.ContentGrid.Width = width;

            if (!showCancelButton)
            {
                this.CancelButton.Visibility = Visibility.Hidden;
            }
        }
    }
}
