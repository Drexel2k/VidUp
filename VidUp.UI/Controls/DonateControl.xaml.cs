#region

using System.Windows.Controls;
using System.Windows.Navigation;

#endregion

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für DonateControl.xaml
    /// </summary>
    public partial class DonateControl : UserControl
    {
        public DonateControl()
        {
            InitializeComponent();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            System.Diagnostics.Process.Start("explorer.exe", e.Uri.AbsoluteUri);
            e.Handled = true;
        }
    }
}
