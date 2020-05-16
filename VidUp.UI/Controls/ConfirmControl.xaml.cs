#region

using System.Windows.Controls;

#endregion

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaction logic for ConfirmControl.xaml
    /// </summary>
    public partial class ConfirmControl : UserControl
    {
        public ConfirmControl(string confirmationText)
        {
            InitializeComponent();
            this.ConfirmationTextTextBlock.Text = confirmationText;
        }
    }
}
