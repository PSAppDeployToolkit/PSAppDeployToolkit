using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls
{
    /// <summary>
    /// HeaderTile.xaml 的交互逻辑
    /// </summary>
    public partial class HeaderTile : UserControl
    {
        public string Title
        {
            get => (string)GetValue(TitleProperty);
            set => SetValue(TitleProperty, value);
        }

        public static readonly DependencyProperty TitleProperty =
            DependencyProperty.Register("Title", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

        public string Description
        {
            get => (string)GetValue(DescriptionProperty);
            set => SetValue(DescriptionProperty, value);
        }

        public static readonly DependencyProperty DescriptionProperty =
            DependencyProperty.Register("Description", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

        public object Source
        {
            get => GetValue(SourceProperty);
            set => SetValue(SourceProperty, value);
        }

        public static readonly DependencyProperty SourceProperty =
            DependencyProperty.Register("Source", typeof(object), typeof(HeaderTile), new PropertyMetadata(null));

        public string Link
        {
            get => (string)GetValue(LinkProperty);
            set => SetValue(LinkProperty, value);
        }

        public static readonly DependencyProperty LinkProperty =
            DependencyProperty.Register("Link", typeof(string), typeof(HeaderTile), new PropertyMetadata(null));

        public object Icon
        {
            get => GetValue(IconProperty);
            set => SetValue(IconProperty, value);
        }

        public static readonly DependencyProperty IconProperty =
            DependencyProperty.Register(nameof(Icon), typeof(object), typeof(HeaderTile), new PropertyMetadata(null));

        public HeaderTile()
        {
            InitializeComponent();
        }
    }
}
