using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class CommandBarFlyoutPage : Page
    {
        private CommandBarFlyout CommandBarFlyout1;

        public CommandBarFlyoutPage()
        {
            InitializeComponent();
            CommandBarFlyout1 = (CommandBarFlyout)Resources[nameof(CommandBarFlyout1)];

            UpdateExampleCode();
        }

        private void OnElementClicked(object sender, RoutedEventArgs e)
        {
            // Do custom logic
            SelectedOptionText.Text = "You clicked: " + (sender as AppBarButton).Label;
        }

        private void ShowMenu(bool isTransient)
        {
            CommandBarFlyout1.ShowMode = isTransient ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
            CommandBarFlyout1.ShowAt(Image1);
        }

        private void MyImageButton_ContextMenuOpening(object sender, ContextMenuEventArgs e)
        {
            // Show a context menu in standard mode
            // Focus will move to the menu
            ShowMenu(false);
        }

        private void MyImageButton_Click(object sender, RoutedEventArgs e)
        {
            // Show a context menu in transient mode
            // Focus will not move to the menu
            ShowMenu(true);
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
        }

        public string Example1Xaml => $@"
<Page.Resources>
    <ui:CommandBarFlyout x:Key=""CommandBarFlyout1"" Placement=""RightEdgeAlignedTop"">
        <ui:AppBarButton
            Click=""OnElementClicked""
            Label=""Share""
            ToolTipService.ToolTip=""Share"">
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Share}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
        <ui:AppBarButton
            Click=""OnElementClicked""
            Label=""Save""
            ToolTipService.ToolTip=""Save"">
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Save}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
        <ui:AppBarButton
            Click=""OnElementClicked""
            Label=""Delete""
            ToolTipService.ToolTip=""Delete"">
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Delete}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
        <ui:CommandBarFlyout.SecondaryCommands>
            <ui:AppBarButton Click=""OnElementClicked"" Label=""Resize"" />
            <ui:AppBarButton Click=""OnElementClicked"" Label=""Move"" />
        </ui:CommandBarFlyout.SecondaryCommands>
    </ui:CommandBarFlyout>
</Page.Resources>

<Button
    x:Name=""MyImageButton""
    AutomationProperties.Name=""mountain""
    Click=""MyImageButton_Click""
    ContextMenuOpening=""MyImageButton_ContextMenuOpening"">
    <Image
        x:Name=""Image1""
        Height=""300""
        Source=""/Assets/SampleMedia/rainier.jpg"" />
</Button>
";

        public string Example1CS => $@"
private CommandBarFlyout CommandBarFlyout1;

public CommandBarFlyoutPage()
{{
    InitializeComponent();
    CommandBarFlyout1 = (CommandBarFlyout)Resources[nameof(CommandBarFlyout1)];
}}

private void OnElementClicked(object sender, RoutedEventArgs e)
{{
    // Do custom logic
    SelectedOptionText.Text = ""You clicked: "" + (sender as AppBarButton).Label;
}}

private void ShowMenu(bool isTransient)
{{
    CommandBarFlyout1.ShowMode = isTransient ? FlyoutShowMode.Transient : FlyoutShowMode.Standard;
    CommandBarFlyout1.ShowAt(Image1);
}}

private void MyImageButton_ContextMenuOpening(object sender, ContextMenuEventArgs e)
{{
    // Show a context menu in standard mode
    // Focus will move to the menu
    ShowMenu(false);
}}

private void MyImageButton_Click(object sender, RoutedEventArgs e)
{{
    // Show a context menu in transient mode
    // Focus will not move to the menu
    ShowMenu(true);
}}
";

        #endregion

    }
}
