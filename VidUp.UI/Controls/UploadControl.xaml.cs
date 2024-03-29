﻿using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;


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
        private void controlGotFocus(object sender, RoutedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            if(control.Name == "Description")
            {
                control.MinHeight = 200;
            }

            if (control.Name == "Tags")
            {
                control.MinHeight = 100;
            }

            UploadControl uploadControl = (UploadControl)((GroupBox)((Grid)((StackPanel)control.Parent).Parent).Parent).Parent;
            uploadControl.Minimize.Visibility = Visibility.Visible;
        }

        private void controlLostFocus(object sender, RoutedEventArgs e)
        {
            TextBox control = (TextBox)sender;
            control.MinHeight = 0;

            UploadControl uploadControl = (UploadControl)((GroupBox)((Grid)((StackPanel)control.Parent).Parent).Parent).Parent;
            if(uploadControl.Description.MinHeight == 0 && uploadControl.Tags.MinHeight == 0)
            {
                uploadControl.Minimize.Visibility = Visibility.Collapsed;
            }

        }

        private void minimizeClick(object sender, RoutedEventArgs e)
        {
            UploadControl control = (UploadControl)((GroupBox)((StackPanel)((Button)sender).Parent).Parent).Parent;
            control.Description.MinHeight = 0;
            control.Tags.MinHeight = 0;
            control.Minimize.Visibility = Visibility.Collapsed;
        }

        private void CUpload_MouseMoved(object sender, MouseEventArgs e)
        {
            base.OnMouseMove(e);
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                // Package the data.
                DataObject data = new DataObject();
                data.SetData("UploadControl", this);

                // Inititate the drag-and-drop operation.
                DragDrop.DoDragDrop(this, data, DragDropEffects.Move);
                //continues in UploadListControl.xaml.cs
            }
        }
    }
}
