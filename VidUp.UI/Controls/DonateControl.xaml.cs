using System.Diagnostics;
using System.Windows.Controls;
using System.Windows.Navigation;

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

        private void hyperlinkRequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
