using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class AppBarButtonPage : Page
    {
        private AppBarToggleButton compactButton = null;
        private AppBarSeparator separator = null;

        public AppBarButtonPage()
        {
            InitializeComponent();
            Loaded += AppBarButtonPage_Loaded;
            Unloaded += AppBarButtonPage_Unloaded;
        }

        private void AppBarButtonPage_Unloaded(object sender, RoutedEventArgs e)
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

                UpdateExampleCode();
            }
        }

        private void AppBarButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                string name = b.Name;

                switch (name)
                {
                    case "Button3":
                        Control3Output.Text = "You clicked: " + name;
                        break;
                    case "Button4":
                        Control4Output.Text = "You clicked: " + name;
                        break;
                    case "Button5":
                        Control5Output.Text = "You clicked: " + name;
                        break;
                }
            }
        }

        private void Save_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
        }

        private string isCompactProp => compactButton.IsChecked == true
            ? @"IsCompact=""True""" : "";

        public string Example3Xaml => $@"                    
<ui:AppBarButton x:Name=""Button3"" {isCompactProp}
    Label=""FontIcon"" Click=""AppBarButton_Click"">
    <ui:AppBarButton.Icon>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Home}}""/>
    </ui:AppBarButton.Icon>
</ui:AppBarButton>
";
        public string Example4Xaml => $@"
<ui:AppBarButton x:Name=""Button4"" {isCompactProp}
    Label=""PathIcon"" Click=""AppBarButton_Click"" >
    <ui:AppBarButton.Icon>
        <ui:PathIcon Data=""F1 M 20,20L 24,10L 24,24L 5,24"" />
    </ui:AppBarButton.Icon>
</ui:AppBarButton>
";


        public string Example5Xaml => $@"
<ui:AppBarButton x:Name=""Button5"" {isCompactProp}
    Click=""AppBarButton_Click""
    Command=""Save"" Label=""Save"">
    <ui:AppBarButton.CommandBindings>
        <CommandBinding CanExecute=""Save_CanExecute"" Command=""Save"" />
    </ui:AppBarButton.CommandBindings>
    <ui:AppBarButton.Icon>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Save}}""/>
    </ui:AppBarButton.Icon>
</ui:AppBarButton>

";

        #endregion
    }
}
