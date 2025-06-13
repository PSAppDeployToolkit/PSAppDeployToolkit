using iNKORE.UI.WPF.Modern.Media.Animation;

namespace SettingsNavigationTest
{
    /// <summary>
    /// MainPage.xaml 的交互逻辑
    /// </summary>
    public partial class MainPage
    {
        public MainPage()
        {
            InitializeComponent();
        }

        private void ToAppearancePage(object sender, System.Windows.RoutedEventArgs e)
        {
            var parent = this.TryFindParent<MainWindow>();
            parent.SettingsFrame.Navigate(parent.Appearance, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
