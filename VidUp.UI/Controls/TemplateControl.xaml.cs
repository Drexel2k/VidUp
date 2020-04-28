using Drexel.VidUp.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für TemplateControl.xaml
    /// </summary>
    /// 

    //todo: make selection of picture file path, root folder path and thumbnail folder path clearable
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
