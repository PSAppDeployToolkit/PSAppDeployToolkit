using iNKORE.UI.WPF.Modern.Controls;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace NavigationViewExample
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        public Pages.AppsPage Page_Apps = new Pages.AppsPage();
        public Pages.HomePage Page_Home = new Pages.HomePage();
        public Pages.GamesPage Page_Games = new Pages.GamesPage();

        private void NavigationView_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            var item = sender.SelectedItem;
            Page? page = null;

            if(item == NavigationViewItem_Home)
            {
                page = Page_Home;
            }
            else if (item == NavigationViewItem_Games)
            {
                page = Page_Games;
            }
            else if (item == NavigationViewItem_Apps)
            {
                 page = Page_Apps;
            }

            if(page != null)
            {
                NavigationView_Root.Header = page.Title;
                Frame_Main.Navigate(page);
            }
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            NavigationView_Root.SelectedItem = NavigationViewItem_Home;
        }
    }
}