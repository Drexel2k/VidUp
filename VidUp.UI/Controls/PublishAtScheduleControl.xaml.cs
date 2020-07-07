#region

using System;
using System.Linq;
using System.Windows.Controls;
using Drexel.VidUp.UI.ViewModels;

#endregion

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für TemplateControl.xaml
    /// </summary>
    /// 

    public partial class PublishAtScheduleControl : UserControl
    {
        public PublishAtScheduleControl()
        {
            InitializeComponent();
        }

        private void UCTemplate_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            PublishAtScheduleViewModel viewModel = (PublishAtScheduleViewModel) this.DataContext;
            viewModel.Initializing = false;
        }
    }
}
