using System;
using System.Collections.Generic;
using System.Windows;
using Drexel.VidUp.Business;
using Drexel.VidUp.UI.ViewModels;
using Drexel.VidUp.Utils;

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
            AppDomain.CurrentDomain.UnhandledException += new UnhandledExceptionEventHandler(this.exHandler);
            InitializeComponent();
        }

        private void exHandler(object sender, UnhandledExceptionEventArgs args)
        {
            Exception e = (Exception)args.ExceptionObject;
            Tracer.Write(e.ToString());
            MessageBox.Show(e.ToString(), "PRESS CTRL+C TO COPY!");
        }

        private void fileDrop(object sender, DragEventArgs e)
        {
            Tracer.Write($"MainWindow.fileDrop: Start.");
            base.OnDrop(e);

            UploadListViewModel uploadListViewModel = (UploadListViewModel)((MainWindowViewModel)this.DataContext).CurrentViewModel;
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            Tracer.Write($"MainWindow.fileDrop: Dropped {files.Length} files.");
            List<Upload> uploads = new List<Upload>();
            foreach (string file in files)
            {
                uploads.Add(new Upload(file,new YoutubeAccount("","")));
            }

            uploadListViewModel.AddUploads(uploads);
            Tracer.Write($"MainWindow.fileDrop: End.");
        }

        private void closed(object sender, EventArgs e)
        {
            ((MainWindowViewModel) this.DataContext).Close();
        }
    }
}
