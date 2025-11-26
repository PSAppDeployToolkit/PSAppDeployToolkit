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
    /// RelativePanelPage.xaml 的交互逻辑
    /// </summary>
    public partial class RelativePanelPage : Page
    {
        public RelativePanelPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ui:RelativePanel Width=""300"">
    <Rectangle
        x:Name=""Rectangle1""
        Width=""50""
        Height=""50""
        Fill=""Red"" />
    <Rectangle
        x:Name=""Rectangle2""
        Width=""50""
        Height=""50""
        Margin=""8,0,0,0""
        ui:RelativePanel.RightOf=""Rectangle1""
        Fill=""Blue"" />
    <Rectangle
        x:Name=""Rectangle3""
        Width=""50""
        Height=""50""
        ui:RelativePanel.AlignRightWithPanel=""True""
        Fill=""Green"" />
    <Rectangle
        x:Name=""Rectangle4""
        Width=""50""
        Height=""50""
        Fill=""Yellow""
        ui:RelativePanel.AlignHorizontalCenterWith=""Rectangle3""
        ui:RelativePanel.Below=""Rectangle3"" 
        Margin=""0,8,0,0"" />
</ui:RelativePanel>
";

        #endregion
    }
}
