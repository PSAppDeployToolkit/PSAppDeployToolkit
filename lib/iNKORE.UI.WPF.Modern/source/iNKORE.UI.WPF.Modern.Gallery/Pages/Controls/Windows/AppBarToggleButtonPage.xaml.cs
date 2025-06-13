using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class AppBarToggleButtonPage : Page
    {
        AppBarToggleButton compactButton = null;
        AppBarSeparator separator = null;

        public AppBarToggleButtonPage()
        {
            InitializeComponent();
            Loaded += AppBarButtonPage_Loaded;
            Unloaded += AppBarToggleButtonPage_Unloaded;
        }

        private void AppBarToggleButtonPage_Unloaded(object sender, RoutedEventArgs e)
        {
            CommandBar appBar = NavigationRootPage.Current.PageHeader.TopCommandBar;
            compactButton.Click -= CompactButton_Click;
            appBar.PrimaryCommands.Remove(compactButton);
            appBar.PrimaryCommands.Remove(separator);
        }

        void AppBarButtonPage_Loaded(object sender, RoutedEventArgs e)
        {
            // Add compact button to the command bar. It provides functionality specific
            // to this page, and is removed when leaving the page.

            CommandBar appBar = NavigationRootPage.Current.PageHeader.TopCommandBar;
            separator = new AppBarSeparator();
            appBar.PrimaryCommands.Insert(0, separator);

            compactButton = new AppBarToggleButton
            {
                Icon = new FontIcon(SegoeFluentIcons.FontSize),
                Label = "IsCompact"
            };
            compactButton.Click += CompactButton_Click;
            appBar.PrimaryCommands.Insert(0, compactButton);

            UpdateExampleCode();
        }

        private void CompactButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is ToggleButton toggle && toggle.IsChecked != null)
            {
                //Button1.IsCompact =
                //Button2.IsCompact =
                Button3.IsCompact =
                Button4.IsCompact = (bool)toggle.IsChecked;
            }

            UpdateExampleCode();
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is AppBarToggleButton b)
            {
                string name = b.Name;

                switch (name)
                {
                    case "Button3":
                        Control3Output.Text = "IsChecked = " + b.IsChecked.ToString();
                        break;
                    case "Button4":
                        Control4Output.Text = "IsChecked = " + b.IsChecked.ToString();
                        break;
                }
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
        }

        private string isCompactProp => compactButton.IsChecked == true
            ? @"IsCompact=""True""" : "";

        public string Example3Xaml => $@"
<ui:AppBarToggleButton x:Name=""Button3"" {isCompactProp}
    Click=""AppBarButton_Click"" Label=""FontIcon"">
    <ui:AppBarToggleButton.Icon>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Home}}"" />
    </ui:AppBarToggleButton.Icon>
</ui:AppBarToggleButton>
";

        public string Example4Xaml => $@"
<ui:AppBarToggleButton x:Name=""Button4""
    Click=""AppBarButton_Click"" {isCompactProp}
    IsThreeState=""True"" Label=""PathIcon"">
    <ui:AppBarToggleButton.Icon>
        <ui:PathIcon Data=""F1 M 20,20L 24,10L 24,24L 5,24"" />
    </ui:AppBarToggleButton.Icon>
</ui:AppBarToggleButton>
";

        #endregion
    }
}
