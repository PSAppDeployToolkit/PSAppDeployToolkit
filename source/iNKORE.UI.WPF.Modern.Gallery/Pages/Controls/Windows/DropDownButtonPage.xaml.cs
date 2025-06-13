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
    /// DropDownButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class DropDownButtonPage : Page
    {
        public DropDownButtonPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:DropDownButton Content=""Email"">
    <ui:DropDownButton.Flyout>
        <ui:MenuFlyout Placement=""Bottom"">
            <MenuItem Header=""Send"" />
            <MenuItem Header=""Reply"" />
            <MenuItem Header=""Reply All"" />
        </ui:MenuFlyout>
    </ui:DropDownButton.Flyout>
</ui:DropDownButton>
";

        public string Example2Xaml => $@"
<ui:DropDownButton AutomationProperties.Name=""Email"">
    <ui:DropDownButton.Content>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Mail}}"" />
    </ui:DropDownButton.Content>
    <ui:DropDownButton.Flyout>
        <ui:MenuFlyout Placement=""Bottom"">
            <MenuItem Header=""Send"">
                <MenuItem.Icon>
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Send}}"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Reply"">
                <MenuItem.Icon>
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.MailReply}}"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Reply All"">
                <MenuItem.Icon>
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.MailReplyAll}}"" />
                </MenuItem.Icon>
            </MenuItem>
        </ui:MenuFlyout>
    </ui:DropDownButton.Flyout>
</ui:DropDownButton>
";

        #endregion

    }
}
