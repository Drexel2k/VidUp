using System;
using System.Windows.Media;

namespace Drexel.VidUp.UI.ViewModels
{
    public struct DisplayScheduleColorTemplateNameViewModel
    {
        public Color Color { get; set; }
        public string TemplateName { get; set; }

        public SolidColorBrush BackgroundSolidColorBrush
        {
            get => new SolidColorBrush(this.Color);
        }
    }
}
