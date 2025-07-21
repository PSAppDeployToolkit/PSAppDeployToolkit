using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls
{
    public partial class TileGallery : UserControl
    {
        public TileGallery()
        {
            InitializeComponent();

            scroller.ScrollChanged += Scroller_ScrollChanged;
            scroller.SizeChanged += Scroller_SizeChanged;

            ScrollBackBtn.Click    += ScrollBackBtn_Click;
            ScrollForwardBtn.Click += ScrollForwardBtn_Click;
        }

        private void Scroller_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            UpdateScrollButtonsVisibility();
        }

        private void ScrollBackBtn_Click(object sender, RoutedEventArgs e)
        {
            scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset - scroller.ViewportWidth);

            ScrollForwardBtn.Focus();
        }

        private void ScrollForwardBtn_Click(object sender, RoutedEventArgs e)
        {
            scroller.ScrollToHorizontalOffset(scroller.HorizontalOffset + scroller.ViewportWidth);

            ScrollBackBtn.Focus();
        }

        private void Scroller_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateScrollButtonsVisibility();
        }

        private void UpdateScrollButtonsVisibility()
        {
            if (scroller.HorizontalOffset < 1)
            {
                ScrollBackBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                ScrollBackBtn.Visibility = Visibility.Visible;
            }

            if (scroller.HorizontalOffset > scroller.ScrollableWidth - 1 || scroller.ScrollableWidth <= 0)
            {
                ScrollForwardBtn.Visibility = Visibility.Collapsed;
            }
            else
            {
                ScrollForwardBtn.Visibility = Visibility.Visible;
            }
        }
    }
}
