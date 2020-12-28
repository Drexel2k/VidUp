#region

using System;
using System.Collections.Generic;
using System.Windows;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;

#endregion

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
            UploadListViewModel uploadListViewModel = (UploadListViewModel)((MainWindowViewModel)this.DataContext).CurrentViewModel;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
            List<Upload> uploads = new List<Upload>();
            foreach (string file in files)
            {
                uploads.Add(new Upload(file));
            }

            uploadListViewModel.AddUploads(uploads);
        }
    }
}
