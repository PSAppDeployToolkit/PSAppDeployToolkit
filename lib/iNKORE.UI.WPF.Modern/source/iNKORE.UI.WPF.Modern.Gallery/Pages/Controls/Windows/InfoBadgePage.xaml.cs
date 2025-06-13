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
    /// InfoBadgePage.xaml 的交互逻辑
    /// </summary>
    public partial class InfoBadgePage : Page
    {
        public InfoBadgePage()
        {
            InitializeComponent();
        }

        public double InfoBadgeOpacity
        {
            get { return (double)GetValue(InfoBadgeOpacityProperty); }
            set { SetValue(InfoBadgeOpacityProperty, value); }
        }

        public static readonly DependencyProperty InfoBadgeOpacityProperty =
            DependencyProperty.Register(
                "ShadowOpacity",
                typeof(double),
                typeof(InfoBadgePage),
                new PropertyMetadata(0.0));

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        public void NavigationViewDisplayMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string paneDisplayMode = e.AddedItems[0].ToString();

            switch (paneDisplayMode)
            {
                case "LeftExpanded":
                    nvSample1.PaneDisplayMode = NavigationViewPaneDisplayMode.Left;
                    nvSample1.IsPaneOpen = true;
                    break;

                case "LeftCompact":
                    nvSample1.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;
                    nvSample1.IsPaneOpen = false;
                    break;

                case "Top":
                    nvSample1.PaneDisplayMode = NavigationViewPaneDisplayMode.Top;
                    nvSample1.IsPaneOpen = true;
                    break;
            }
        }

        private void ToggleInfoBadgeOpacity_Toggled(object sender, RoutedEventArgs e)
        {
            InfoBadgeOpacity = (InfoBadgeOpacity == 0.0) ? 1.0 : 0.0;

            UpdateExampleCode();
        }

        string infoBadge2StyleKey = "AttentionIconInfoBadgeStyle";
        string infoBadge3StyleKey = "AttentionValueInfoBadgeStyle";
        string infoBadge4StyleKey = "AttentionDotInfoBadgeStyle";

        public void InfoBadgeStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            string infoBadgeStyle = e.AddedItems[0].ToString();
            ResourceDictionary Resources = new ResourceDictionary { Source = new Uri("/iNKORE.UI.WPF.Modern.Controls;component/Controls/Windows/InfoBadge/InfoBadge.xaml", UriKind.RelativeOrAbsolute) };

            switch (infoBadgeStyle)
            {
                case "Attention":
                    infoBadge2StyleKey = "AttentionIconInfoBadgeStyle";
                    infoBadge3StyleKey = "AttentionValueInfoBadgeStyle";
                    infoBadge4StyleKey = "AttentionDotInfoBadgeStyle";
                    break;

                case "Informational":
                    infoBadge2StyleKey = "InformationalIconInfoBadgeStyle";
                    infoBadge3StyleKey = "InformationalValueInfoBadgeStyle";
                    infoBadge4StyleKey = "InformationalDotInfoBadgeStyle";
                    break;

                case "Success":
                    infoBadge2StyleKey = "SuccessIconInfoBadgeStyle";
                    infoBadge3StyleKey = "SuccessValueInfoBadgeStyle";
                    infoBadge4StyleKey = "SuccessDotInfoBadgeStyle";
                    break;

                case "Critical":
                    infoBadge2StyleKey = "CriticalIconInfoBadgeStyle";
                    infoBadge3StyleKey = "CriticalValueInfoBadgeStyle";
                    infoBadge4StyleKey = "CriticalDotInfoBadgeStyle";
                    break;
            }

            infoBadge2.Style = Resources[infoBadge2StyleKey] as Style;
            infoBadge3.Style = Resources[infoBadge3StyleKey] as Style;
            infoBadge4.Style = Resources[infoBadge4StyleKey] as Style;

            UpdateExampleCode();
        }

        private void ValueNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            if ((int)args.NewValue >= -1)
            {
                DynamicInfoBadge.Value = (int)args.NewValue;
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
            Example4.Xaml = Example4Xaml;
        }

        public string Example1Xaml => $@"
<ui:NavigationViewItem
    x:Name=""InboxPage""
    Content=""Inbox"">
    <ui:NavigationViewItem.Icon>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Mail}}""/>
    </ui:NavigationViewItem.Icon>
    <ui:NavigationViewItem.InfoBadge>
        <ui:InfoBadge x:Name=""infoBadge1""
            Opacity=""{InfoBadgeOpacity}"" Value=""5"" />
    </ui:NavigationViewItem.InfoBadge>
</ui:NavigationViewItem>
";

        public string Example2Xaml => $@"
<ikw:SimpleStackPanel
    HorizontalAlignment=""Center""
    Orientation=""Horizontal""
    Spacing=""20"">
    <ui:InfoBadge
        x:Name=""infoBadge2""
        HorizontalAlignment=""Right""
        Style=""{{DynamicResource {infoBadge2StyleKey}}}"" />
    <ui:InfoBadge
        x:Name=""infoBadge3""
        HorizontalAlignment=""Right""
        Style=""{{DynamicResource {infoBadge3StyleKey}}}""
        Value=""10"" />
    <ui:InfoBadge
        x:Name=""infoBadge4""
        VerticalAlignment=""Center""
        Style=""{{DynamicResource {infoBadge4StyleKey}}}"" />
</ikw:SimpleStackPanel>
";

        public string Example3Xaml => $@"
<Button Padding=""0""
    HorizontalContentAlignment=""Stretch""
    VerticalContentAlignment=""Stretch"">
    <Grid Width=""Auto"" Height=""Auto""
        HorizontalAlignment=""Stretch""
        VerticalAlignment=""Stretch"">
        <ui:FontIcon HorizontalAlignment=""Center"" Icon=""{{x:Static ui:SegoeFluentIcons.Sync}}""/>
        <ui:InfoBadge
            HorizontalAlignment=""Right""
            VerticalAlignment=""Top""
            Background=""#C42B1C"">
            <ui:InfoBadge.IconSource>
                <ui:FontIconSource FontFamily=""Segoe MDL2 Assets"" Glyph=""&#xF13C;"" />
            </ui:InfoBadge.IconSource>
        </ui:InfoBadge>
    </Grid>
</Button>
";

        public string Example4Xaml => $@"
<ui:InfoBadge x:Name=""DynamicInfoBadge"" Value=""{DynamicInfoBadge.Value.ToString()}""/>
";

        #endregion
    }
}
