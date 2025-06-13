using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
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
    /// ToggleSplitButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class ToggleSplitButtonPage : Page
    {
        private string _type = "•";
        public ToggleSplitButtonPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void BulletButton_Click(object sender, RoutedEventArgs e)
        {
            Button clickedBullet = (Button)sender;
            var symbol = (FontIcon)clickedBullet.Content;

            if (symbol.Glyph == SegoeFluentIcons.List.Glyph)
            {
                _type = "•";
                mySymbolIcon.Icon = SegoeFluentIcons.List;
                myListButton.SetValue(AutomationProperties.NameProperty, "Bullets");
            }
            else if (symbol.Glyph == SegoeFluentIcons.BulletedList.Glyph)
            {
                _type = "I)";
                mySymbolIcon.Icon = SegoeFluentIcons.BulletedList;
                myListButton.SetValue(AutomationProperties.NameProperty, "Roman Numerals");
            }
            myRichEditBox.Selection.Text = _type;

            myListButton.IsChecked = true;
            myListButton.Flyout.Hide();
            myRichEditBox.Focus();
        }

        private void MyListButton_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
        {
            if (sender.IsChecked)
            {
                //add bulleted list
                myRichEditBox.Selection.Text = _type;
            }
            else
            {
                //remove bulleted list
                myRichEditBox.Selection.Text = "";
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
        }

        public string Example1Xaml => $@"
<ui:ToggleSplitButton x:Name=""myListButton""
    AutomationProperties.Name=""Bullets""
    IsCheckedChanged=""MyListButton_IsCheckedChanged"">
    <ui:FontIcon x:Name=""mySymbolIcon"" Icon=""{{x:Static ui:SegoeFluentIcons.List}}"" Margin=""4""/>
    <ui:ToggleSplitButton.Flyout>
        <ui:Flyout Placement=""Bottom"">
            <StackPanel Orientation=""Horizontal"">
                <StackPanel.Resources>
                    <Style TargetType=""Button"" BasedOn=""{{StaticResource DefaultButtonStyle}}"">
                        <Setter Property=""Padding"" Value=""4"" />
                        <Setter Property=""MinWidth"" Value=""0"" />
                        <Setter Property=""MinHeight"" Value=""0"" />
                        <Setter Property=""Margin"" Value=""6"" />
                        <Setter Property=""ui:ControlHelper.CornerRadius"" Value=""{{DynamicResource ControlCornerRadius}}"" />
                    </Style>
                </StackPanel.Resources>
                <Button AutomationProperties.Name=""Bulleted list"" Click=""BulletButton_Click"">
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.List}}"" />
                </Button>
                <Button AutomationProperties.Name=""Roman numerals list"" Click=""BulletButton_Click"">
                    <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.BulletedList}}"" />
                </Button>
            </StackPanel>
        </ui:Flyout>
    </ui:ToggleSplitButton.Flyout>
</ui:ToggleSplitButton>

<RichTextBox x:Name=""myRichEditBox""
    AutomationProperties.Name=""Text entry"" />
";

        public string Example1CS => $@"
private void BulletButton_Click(object sender, RoutedEventArgs e)
{{
    Button clickedBullet = (Button)sender;
    var symbol = (FontIcon)clickedBullet.Content;

    if (symbol.Glyph == SegoeFluentIcons.List.Glyph)
    {{
        _type = ""•"";
        mySymbolIcon.Icon = SegoeFluentIcons.List;
        myListButton.SetValue(AutomationProperties.NameProperty, ""Bullets"");
    }}
    else if (symbol.Glyph == SegoeFluentIcons.BulletedList.Glyph)
    {{
        _type = ""I)"";
        mySymbolIcon.Icon = SegoeFluentIcons.BulletedList;
        myListButton.SetValue(AutomationProperties.NameProperty, ""Roman Numerals"");
    }}
    myRichEditBox.Selection.Text = _type;

    myListButton.IsChecked = true;
    myListButton.Flyout.Hide();
    myRichEditBox.Focus();
}}

private void MyListButton_IsCheckedChanged(ToggleSplitButton sender, ToggleSplitButtonIsCheckedChangedEventArgs args)
{{
    if (sender.IsChecked)
    {{
        //add bulleted list
        myRichEditBox.Selection.Text = _type;
    }}
    else
    {{
        //remove bulleted list
        myRichEditBox.Selection.Text = """";
    }}
}}
";

        #endregion

    }
}
