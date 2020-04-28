using Drexel.VidUp.Business;
using Drexel.VidUp.UI.DllImport;
using Drexel.VidUp.UI.ViewModels;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Drexel.VidUp.UI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    /// 
    public partial class MainWindow
    {
        public MainWindow()
        {
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(exHandler);
            InitializeComponent();
        }

        private void exHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            MessageBox.Show(e.ToString(), "PRESS CTRL+C TO COPY!");
        }

        private void Image_Drop(object sender, DragEventArgs e)
        {
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)this.DataContext;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<Upload> uploads = new List<Upload>();
            foreach (string file in files)
            {
                uploads.Add(new Upload(file));
            }

            mainWindowViewModel.AddUploads(uploads);
        }

        private void CMainWindow_Activated(object sender, EventArgs e)
        {
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)this.DataContext;
            mainWindowViewModel.WindowActivated();
        }

        private void CMainWindow_Deactivated(object sender, EventArgs e)
        {
            MainWindowViewModel mainWindowViewModel = (MainWindowViewModel)this.DataContext;
            mainWindowViewModel.WindowDeactivated();
        }
    }
}
