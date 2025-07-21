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
    /// SplitButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class SplitButtonPage : Page
    {
        private Color currentColor = Colors.Green;

        public SplitButtonPage()
        {
            InitializeComponent();
            myRichEditBox.Foreground = new SolidColorBrush(currentColor);
            myRichEditBox.Selection.Text=
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, " +
                "sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Tempor commodo ullamcorper a lacus.";
        }

        private void GridView_ItemClick(object sender, ItemClickEventArgs e)
        {
            var rect = (Rectangle)e.ClickedItem;
            var color = ((SolidColorBrush)rect.Fill).Color;
            myRichEditBox.Foreground = new SolidColorBrush(color);
            CurrentColor.Background = new SolidColorBrush(color);

            myRichEditBox.Focus();
            currentColor = color;

            // Delay required to circumvent GridView bug: https://github.com/microsoft/microsoft-ui-xaml/issues/6350
            Task.Delay(10).ContinueWith(_ => myColorButton.Flyout.Hide(), TaskScheduler.FromCurrentSynchronizationContext());
        }

        private void RevealColorButton_Click(object sender, RoutedEventArgs e)
        {
            myColorButtonReveal.Flyout.Hide();
        }

        private void myColorButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
        {
            var border = (Border)sender.Content;
            var color = ((SolidColorBrush)border.Background).Color;

            myRichEditBox.Foreground = new SolidColorBrush(color);
            currentColor = color;
        }

        private void MyRichEditBox_TextChanged(object sender, RoutedEventArgs e)
        {
            if (((SolidColorBrush)myRichEditBox.Foreground).Color != currentColor)
            {
                myRichEditBox.Foreground = new SolidColorBrush(currentColor);
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:SplitButton x:Name=""myColorButton""
    AutomationProperties.Name=""Font color""
    Click=""myColorButton_Click"">
    <Border x:Name=""CurrentColor""
        Width=""{{StaticResource SwatchSize}}""
        Height=""{{StaticResource SwatchSize}}""
        Margin=""0"" Background=""Green""
        CornerRadius=""4,0,0,4"" />
    <ui:SplitButton.Flyout>
        <ui:Flyout Placement=""Bottom"">
            <ui:GridView IsItemClickEnabled=""True"" ItemClick=""GridView_ItemClick"">
                <ui:GridView.ItemsPanel>
                    <ItemsPanelTemplate>
                        <primitives:UniformGrid Columns=""3"" />
                    </ItemsPanelTemplate>
                </ui:GridView.ItemsPanel>
                <ui:GridView.Resources>
                    <Style TargetType=""Rectangle"">
                        <Setter Property=""Width"" Value=""{{StaticResource SwatchSize}}"" />
                        <Setter Property=""Height"" Value=""{{StaticResource SwatchSize}}"" />
                        <Setter Property=""RadiusX"" Value=""4"" />
                        <Setter Property=""RadiusY"" Value=""4"" />
                    </Style>
                </ui:GridView.Resources>
                <ui:GridView.Items>
                    <Rectangle Fill=""Red"" />
                    <Rectangle Fill=""Orange"" />
                    <Rectangle Fill=""Yellow"" />
                    <Rectangle Fill=""Green"" />
                    <Rectangle Fill=""Blue"" />
                    <Rectangle Fill=""Indigo"" />
                    <Rectangle Fill=""Violet"" />
                    <Rectangle Fill=""Gray"" />
                </ui:GridView.Items>
            </ui:GridView>

        </ui:Flyout>
    </ui:SplitButton.Flyout>
</ui:SplitButton>

<RichTextBox x:Name=""myRichEditBox""
    ui:ControlHelper.PlaceholderText=""Type something here""
    TextChanged=""MyRichEditBox_TextChanged"" />
";

        public string Example1CS => $@"
public Page()
{{
    InitializeComponent();
    myRichEditBox.Foreground = new SolidColorBrush(currentColor);
    myRichEditBox.Selection.Text=
        ""Lorem ipsum dolor sit amet, consectetur adipiscing elit, "" +
        ""sed do eiusmod tempor incididunt ut labore et dolore magna aliqua. Tempor commodo ullamcorper a lacus."";
}}

private void GridView_ItemClick(object sender, ItemClickEventArgs e)
{{
    var rect = (Rectangle)e.ClickedItem;
    var color = ((SolidColorBrush)rect.Fill).Color;
    myRichEditBox.Foreground = new SolidColorBrush(color);
    CurrentColor.Background = new SolidColorBrush(color);

    myRichEditBox.Focus();
    currentColor = color;

    // Delay required to circumvent GridView bug: https://github.com/microsoft/microsoft-ui-xaml/issues/6350
    Task.Delay(10).ContinueWith(_ => myColorButton.Flyout.Hide(), TaskScheduler.FromCurrentSynchronizationContext());
}}

private void RevealColorButton_Click(object sender, RoutedEventArgs e)
{{
    myColorButtonReveal.Flyout.Hide();
}}

private void myColorButton_Click(SplitButton sender, SplitButtonClickEventArgs args)
{{
    var border = (Border)sender.Content;
    var color = ((SolidColorBrush)border.Background).Color;

    myRichEditBox.Foreground = new SolidColorBrush(color);
    currentColor = color;
}}

private void MyRichEditBox_TextChanged(object sender, RoutedEventArgs e)
{{
    if (((SolidColorBrush)myRichEditBox.Foreground).Color != currentColor)
    {{
        myRichEditBox.Foreground = new SolidColorBrush(currentColor);
    }}
}}
";

        public string Example2Xaml => $@"
<ui:SplitButton x:Name=""myColorButtonReveal""
    AutomationProperties.Name=""Font color"">
    Choose color
    <ui:SplitButton.Flyout>
        <ui:Flyout Placement=""Bottom"">
            <primitives:UniformGrid Columns=""3"">
                <primitives:UniformGrid.Resources>
                        <Style TargetType=""Rectangle"">
                            <Setter Property=""Width"" Value=""{{StaticResource SwatchSize}}"" />
                            <Setter Property=""Height"" Value=""{{StaticResource SwatchSize}}"" />
                            <Setter Property=""RadiusX"" Value=""4"" />
                            <Setter Property=""RadiusY"" Value=""4"" />
                        </Style>
                        <Style BasedOn=""{{StaticResource DefaultButtonStyle}}"" TargetType=""Button"">
                            <Setter Property=""Padding"" Value=""0"" />
                            <Setter Property=""MinWidth"" Value=""0"" />
                            <Setter Property=""MinHeight"" Value=""0"" />
                            <Setter Property=""Margin"" Value=""6"" />
                            <Setter Property=""ui:ControlHelper.CornerRadius"" Value=""{{DynamicResource ControlCornerRadius}}"" />
                        </Style>
                </primitives:UniformGrid.Resources>
                <Button AutomationProperties.Name=""Red"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Red"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Orange"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Orange"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Yellow"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Yellow"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Green"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Green"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Blue"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Blue"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Indigo"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Indigo"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Violet"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Violet"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Gray"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Gray"" />
                    </Button.Content>
                </Button>
                <Button AutomationProperties.Name=""Black"" Click=""RevealColorButton_Click"">
                    <Button.Content>
                        <Rectangle Fill=""Black"" />
                    </Button.Content>
                </Button>
            </primitives:UniformGrid>
        </ui:Flyout>
    </ui:SplitButton.Flyout>
</ui:SplitButton>
";


        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }
    }
}
