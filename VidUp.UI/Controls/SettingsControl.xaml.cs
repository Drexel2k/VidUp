#region

using System;
using System.Diagnostics;
using System.Windows.Controls;
using Drexel.VidUp.UI.ViewModels;

#endregion

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für TemplateControl.xaml
    /// </summary>
    /// 

    public partial class SettingsControl : UserControl
    {
        public SettingsControl()
        {
            InitializeComponent();
        }

        private void hyperlinkRequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }
    }
}
