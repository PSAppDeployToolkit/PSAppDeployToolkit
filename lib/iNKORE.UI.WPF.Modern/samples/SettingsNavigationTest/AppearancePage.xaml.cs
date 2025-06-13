using iNKORE.UI.WPF.Modern.Media.Animation;

namespace SettingsNavigationTest
{
    /// <summary>
    /// AppearancePage.xaml 的交互逻辑
    /// </summary>
    public partial class AppearancePage
    {
        public AppearancePage()
        {
            InitializeComponent();
        }

        private void BackToMain(object sender, System.Windows.RoutedEventArgs e)
        {
            var parent = this.TryFindParent<MainWindow>();
            parent.SettingsFrame.Navigate(parent.Main, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromLeft });
        }
    }
}
