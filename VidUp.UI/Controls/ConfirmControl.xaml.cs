using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
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
