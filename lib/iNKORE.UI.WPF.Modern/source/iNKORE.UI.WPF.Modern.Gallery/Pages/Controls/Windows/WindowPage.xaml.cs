using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class WindowPage
    {
        public WindowPage()
        {
            InitializeComponent();
        }

        private void ResizeWindow(object sender, RoutedEventArgs e)
        {
            if (e.OriginalSource is MenuItem menuItem)
            {
                var size = Size.Parse(((string)menuItem.Header).Replace('×', ','));
                var window = Window.GetWindow(this);
                window.Width = size.Width;
                window.Height = size.Height;
            }
        }

        private void OpenWindow(object sender, RoutedEventArgs e)
        {
            var window = new WindowWithCustomTitleBar
            {
                Owner = Window.GetWindow(this)
            };
            window.ShowDialog();
        }
    }
}
