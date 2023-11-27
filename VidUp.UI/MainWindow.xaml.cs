using System;
using System.Threading;
using System.Windows;
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

            MainWindowViewModel minWindowViewModel = ((MainWindowViewModel)this.DataContext);
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            Tracer.Write($"MainWindow.fileDrop: Dropped {files.Length} files.");
            minWindowViewModel.AddFiles(files, false);
            Tracer.Write($"MainWindow.fileDrop: End.");
        }

        private void closed(object sender, EventArgs e)
        {
            CountdownEvent serializationContentProcessesCount = ((MainWindowViewModel)this.DataContext).StopSerializationContent();
            serializationContentProcessesCount.Wait();
            CountdownEvent serializationSettingsProcessesCount = ((MainWindowViewModel)this.DataContext).StopSerializationSettings();
            serializationSettingsProcessesCount.Wait();
            ((MainWindowViewModel) this.DataContext).Close();
        }
    }
}
