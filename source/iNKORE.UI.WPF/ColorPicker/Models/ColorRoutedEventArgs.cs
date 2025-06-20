using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.ColorPicker.Models
{
    public class ColorRoutedEventArgs : RoutedEventArgs
    {
        public ColorRoutedEventArgs(RoutedEvent routedEvent, Color color) : base(routedEvent)
        {
            Color = color;
        }

        public Color Color { get; private set; }
    }
}
