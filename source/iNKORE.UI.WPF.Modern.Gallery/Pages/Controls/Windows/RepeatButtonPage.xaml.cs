using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// RepeatButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class RepeatButtonPage : Page
    {
        public RepeatButtonPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void DisableControl1_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private static int _clicks = 0;
        private void RepeatButton_Click(object sender, RoutedEventArgs e)
        {
            _clicks += 1;
            Control1Output.Text = "Number of clicks: " + _clicks;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<RepeatButton x:Name=""Control1""
    Content=""Click and hold"" IsEnabled=""{!DisableControl1.IsChecked}"" />
";

        #endregion
    }
}
