using iNKORE.UI.WPF.Modern.Controls;
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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// InfoBarPage.xaml 的交互逻辑
    /// </summary>
    public partial class InfoBarPage : Page
    {
        public InfoBarPage()
        {
            InitializeComponent();
        }

        string example2ActionButtonXaml = null;

        private void SeverityComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string severityName = e.AddedItems[0].ToString();

            switch (severityName)
            {
                case "Error":
                    TestInfoBar1.Severity = InfoBarSeverity.Error;
                    break;

                case "Warning":
                    TestInfoBar1.Severity = InfoBarSeverity.Warning;
                    break;

                case "Success":
                    TestInfoBar1.Severity = InfoBarSeverity.Success;
                    break;

                case "Informational":
                default:
                    TestInfoBar1.Severity = InfoBarSeverity.Informational;
                    break;
            }

            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void InfoBar_Closed(InfoBar sender, InfoBarClosedEventArgs args)
        {
            UpdateExampleCode();
        }


        private void MessageComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestInfoBar2 == null) return;

            if (MessageComboBox.SelectedIndex == 0) // short
            {
                string shortMessage = "A short essential app message.";
                TestInfoBar2.Message = shortMessage;
            }
            else if (MessageComboBox.SelectedIndex == 1) //long
            {
                TestInfoBar2.Message = @"A long essential app message for your users to be informed of, acknowledge, or take action on. Lorem ipsum dolor sit amet, consectetur adipiscing elit. Proin dapibus dolor vitae justo rutrum, ut lobortis nibh mattis. Aenean id elit commodo, semper felis nec.";
            }

            UpdateExampleCode();
        }

        private void ActionButtonComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (TestInfoBar2 == null) return;

            if (ActionButtonComboBox.SelectedIndex == 0) // none
            {
                TestInfoBar2.ActionButton = null;
                example2ActionButtonXaml = null;
            }
            else if (ActionButtonComboBox.SelectedIndex == 1) // button
            {
                var button = new Button();
                button.Content = "Action";
                TestInfoBar2.ActionButton = button;
                example2ActionButtonXaml = @"
    <ui:InfoBar.ActionButton>
        <Button Content=""Action"" Click=""InfoBarButton_Click"" />
    </ui:InfoBar.ActionButton> ";

            }
            else if (ActionButtonComboBox.SelectedIndex == 2) // hyperlink
            {
                var href = "https://docs.inkore.net/ui-wpf-modern/components/status/info-bar";
                var link = new HyperlinkButton();
                link.NavigateUri = new Uri(href);
                link.Content = "Informational link";
                TestInfoBar2.ActionButton = link;
                example2ActionButtonXaml = $@"
    <ui:InfoBar.ActionButton>
        <ui:HyperlinkButton Content=""Informational link"" NavigateUri=""{href}"" />
    </ui:InfoBar.ActionButton>";
            }

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ui:InfoBar x:Name=""TestInfoBar1"" Title=""Title"" IsOpen=""{TestInfoBar1.IsOpen}"" Severity=""{TestInfoBar1.Severity}""
    Message=""Essential app message for your users to be informed of, acknowledge, or take action on."" />
";

        public string Example2Xaml => $@"
<ui:InfoBar x:Name=""TestInfoBar2"" 
    Title=""Title"" IsOpen=""{TestInfoBar2.IsOpen}""
    Message=""{TestInfoBar2.Message}""> { (example2ActionButtonXaml != null ? "\r\n" : null) + example2ActionButtonXaml }
</ui:InfoBar>
";

        public string Example3Xaml => $@"
<ui:InfoBar x:Name=""TestInfoBar3"" Title=""Title""
    IsClosable=""{TestInfoBar3.IsClosable}"" IsIconVisible=""{TestInfoBar3.IsIconVisible}"" IsOpen=""{TestInfoBar3.IsOpen}""
    Message=""Essential app message for your users to be informed of, acknowledge, or take action on."" />
";

        #endregion
    }
}
