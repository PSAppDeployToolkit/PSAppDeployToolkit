using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Gallery.Controls.UserControls
{
    /// <summary>
    /// DocumentationPromotion.xaml 的交互逻辑
    /// </summary>
    public partial class DocumentationPromotion : UserControl
    {
        public DocumentationPromotion()
        {
            InitializeComponent();
        }

        private void HyperlinkButton_Repository_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb(ThemeManager.Link_GithubRepo);
        }

        private void HyperlinkButton_Package_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb(ThemeManager.Link_NugetPackage);
        }

        private void Hyperlink_DocumentationRepo_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb("https://github.com/iNKORE-NET/Documentation/tree/main/data/docs/ui.wpf.modern");
        }

        private void Hyperlink_Discord_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb(ThemeManager.Link_DiscordServer);
        }

        private void Hyperlink_Telegram_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb(ThemeManager.Link_TelegramGroup);

        }

        private void HyperlinkButton_Facebook_Click(object sender, RoutedEventArgs e)
        {
            App.BrowseWeb(ThemeManager.Link_FacebookPage);
        }

    }
}
