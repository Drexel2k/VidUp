using System.Windows;
using System.Windows.Controls;
using Drexel.VidUp.UI.ViewModels;

namespace Drexel.VidUp.UI.Controls
{
    /// <summary>
    /// Interaktionslogik für UploadsControl.xaml
    /// </summary>
    public partial class UploadListControl : UserControl
    {
        public UploadListControl()
        {
            InitializeComponent();
        }

        private void CUpload_Dropped(object sender, DragEventArgs e)
        {
            base.OnDrop(e);

            UploadControl uploadControlToMove = (UploadControl)e.Data.GetData("UploadControl");
            if (uploadControlToMove != null)
            {
                UploadControl uploadControlAtTargetPosition = (UploadControl) sender;
                UploadListViewModel uploadListViewModel = (UploadListViewModel) this.DataContext;
                uploadListViewModel.Reorder(((UploadViewModel) uploadControlToMove.DataContext).Upload, ((UploadViewModel) uploadControlAtTargetPosition.DataContext).Upload);
                e.Handled = true;
            }
        }
    }
}
