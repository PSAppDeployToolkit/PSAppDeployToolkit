using iNKORE.UI.WPF.Modern.Media.Animation;
using System.Windows;
using Page = System.Windows.Controls.Page;

namespace SettingsNavigationTest
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow
    {
        public readonly Page Main = new MainPage();
        public readonly Page Appearance = new AppearancePage();
        public MainWindow()
        {
            InitializeComponent();
            SettingsFrame.Navigate(Main, new SlideNavigationTransitionInfo { Effect = SlideNavigationTransitionEffect.FromRight });
        }
    }
}
