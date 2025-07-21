using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Community
{
    /// <summary>
    /// BorderPage.xaml 的交互逻辑
    /// </summary>
    public partial class SettingsCardPage : Page
    {
        public SettingsCardPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void EnableToggle1_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void EnableToggle2_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }


        private void OnCardClicked(object sender, RoutedEventArgs e)
        {
            Process.Start(new ProcessStartInfo("https://www.inkore.net") { UseShellExecute = true });
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example2.CSharp = Example2CS;
        }

        public string Example1Xaml => $@"
<ikw:SimpleStackPanel Spacing=""4"">
    <ui:SettingsCard x:Name=""settingsCard""
        Description=""This is a default card, with the Header, HeaderIcon, Description and Content set.""
        Header=""This is the Header"" IsEnabled=""{EnableToggle1.IsOn}"">
        <ui:SettingsCard.HeaderIcon>
            <ui:FontIcon Glyph=""&#xE799;""/>
        </ui:SettingsCard.HeaderIcon>
        <ComboBox SelectedIndex=""0"">
            <ComboBoxItem>Option 1</ComboBoxItem>
            <ComboBoxItem>Option 2</ComboBoxItem>
            <ComboBoxItem>Option 3</ComboBoxItem>
        </ComboBox>
    </ui:SettingsCard>

    <ui:SettingsCard Description=""You can use a FontIcon, SymbolIcon or BitmapIcon to set the cards HeaderIcon.""
        Header=""Icon options"" IsEnabled=""{EnableToggle1.IsOn}"">
        <ui:SettingsCard.HeaderIcon>
            <Image Width=""20"" Height=""20"" Source=""/Assets/WpfLibrary_256w.png""
                    RenderOptions.BitmapScalingMode=""HighQuality""/>
        </ui:SettingsCard.HeaderIcon>
        <ui:ToggleSwitch />
    </ui:SettingsCard>

    <ui:SettingsCard Header=""A card with custom objects as its Description""
        IsEnabled=""{EnableToggle1.IsOn}"">
        <ui:SettingsCard.Description>
            <ui:HyperlinkButton Content=""Learn more about Inkways"" />
        </ui:SettingsCard.Description>
        <Button Content=""Open Inkways Editor"" Style=""{{StaticResource {{x:Static ui:ThemeKeys.AccentButtonStyleKey}}}}"" />
    </ui:SettingsCard>

    <ui:SettingsCard Description=""When resizing a SettingsCard, the Content will wrap vertically. You can override this breakpoint by setting the SettingsCardWrapThreshold resource. For edge cases, you can also hide the icon by setting SettingsCardWrapNoIconThreshold.""
        Header=""Adaptive layouts"" IsEnabled=""{EnableToggle1.IsOn}"">
        <ui:SettingsCard.HeaderIcon>
            <ui:FontIcon Glyph=""&#xE745;""/>
        </ui:SettingsCard.HeaderIcon>

        <Button Content=""This control will wrap vertically!""/>
    </ui:SettingsCard>

    <ui:SettingsCard Header=""This is a card with a Header only"" 
        IsEnabled=""{EnableToggle1.IsOn}"" />
</ikw:SimpleStackPanel>
";

        public string Example2Xaml => $@"
<ui:SettingsCard Click=""OnCardClicked"" IsEnabled=""{EnableToggle2.IsOn}""
    Description=""A SettingsCard can be made clickable and you can leverage the Command property or Click event.""
    Header=""A clickable SettingsCard"" IsClickEnabled=""True"">
    <ui:SettingsCard.HeaderIcon>
        <ui:FontIcon Glyph=""&#xE799;""/>
    </ui:SettingsCard.HeaderIcon>
    <TextBlock Foreground=""{{DynamicResource {{x:Static ui:ThemeKeys.TextFillColorSecondaryBrushKey}}}}""
    Text=""This is content"" />
</ui:SettingsCard>

<ui:SettingsCard ActionIconToolTip=""Open in new window""
    Click=""OnCardClicked"" IsClickEnabled=""True""
    Description=""You can customize the ActionIcon and ActionIconToolTip.""
    Header=""Customizing the ActionIcon"" IsEnabled=""{EnableToggle2.IsOn}"">
    <ui:SettingsCard.HeaderIcon>
        <ui:FontIcon Glyph=""&#xE774;""/>
    </ui:SettingsCard.HeaderIcon>
    <ui:SettingsCard.ActionIcon>
        <ui:FontIcon Glyph=""&#xE8A7;""/>
    </ui:SettingsCard.ActionIcon>
</ui:SettingsCard>

<ui:SettingsCard Click=""OnCardClicked""
    IsActionIconVisible=""False"" IsClickEnabled=""True""
    Header=""Hiding the ActionIcon"" IsEnabled=""{EnableToggle2.IsOn}"">
    <ui:SettingsCard.HeaderIcon>
        <ui:FontIcon Glyph=""&#xE72E;""/>
    </ui:SettingsCard.HeaderIcon>
</ui:SettingsCard>
";

        public string Example2CS => $@"
private void OnCardClicked(object sender, RoutedEventArgs e)
{{
    Process.Start(new ProcessStartInfo(""https://www.inkore.net"") {{ UseShellExecute = true }});
}}
";

        #endregion
    }
}
