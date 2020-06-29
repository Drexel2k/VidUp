#region

using System;
using System.Windows.Controls;
using Drexel.VidUp.UI.ViewModels;

#endregion

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für TemplateControl.xaml
    /// </summary>
    /// 

    public partial class TemplateControl : UserControl
    {
        public TemplateControl()
        {
            InitializeComponent();
        }

        private void ComboBoxQuarterHourSelect_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            TemplateViewModel templateViewModel = (TemplateViewModel)comboBox.DataContext;
            QuarterHourViewModel selectedQuarterHour = (QuarterHourViewModel)comboBox.SelectedItem;
            if (templateViewModel.Template.DefaultPublishAtTime != selectedQuarterHour.QuarterHour)
            {
                templateViewModel.SetDefaultPublishAtTime(selectedQuarterHour.QuarterHour);
            }
        }


    }
}
