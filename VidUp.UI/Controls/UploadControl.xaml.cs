using Drexel.VidUp.Business;
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
    /// Interaktionslogik für UploadControl.xaml
    /// </summary>
    public partial class UploadControl : UserControl
    {
        public UploadControl()
        {
            InitializeComponent();
        }

        //FU MVVM, Combox or whatever! When usercontrol view unloads, ComboBox sets selectedItem to null
        //and so template is set to null on upload even if before a template was selected
        private void ComboBoxTemplateSelect_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            UploadViewModel uploadViewModel = (UploadViewModel)comboBox.DataContext;
            Template selectedTemplate = (Template)comboBox.SelectedItem;
            if (uploadViewModel.Template != selectedTemplate)
            {
                uploadViewModel.Template = selectedTemplate;
            }
        }

        private void ComboBoxQuarterHourSelect_DropDownClosed(object sender, EventArgs e)
        {
            ComboBox comboBox = (ComboBox)sender;
            UploadViewModel uploadViewModel = (UploadViewModel)comboBox.DataContext;
            QuarterHourViewModel selectedQuarterHour = (QuarterHourViewModel)comboBox.SelectedItem;
            if (uploadViewModel.Upload.PublishAtTime != selectedQuarterHour.QuarterHour)
            {
                uploadViewModel.SetPublishAtTime(selectedQuarterHour.QuarterHour);
            }
        }
    }
}
