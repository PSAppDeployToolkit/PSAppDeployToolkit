using iNKORE.UI.WPF.Modern.Common.IconKeys;
using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class AppBarSeparatorPage : Page
    {
        private AppBarToggleButton compactButton = null;
        private AppBarSeparator separator = null;

        public AppBarSeparatorPage()
        {
            InitializeComponent();
            Loaded += AppBarButtonPage_Loaded;
            Unloaded += AppBarSeparatorPage_Unloaded;
        }

        private void AppBarSeparatorPage_Unloaded(object sender, RoutedEventArgs e)
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
            if ((sender as AppBarToggleButton).IsChecked == true)
            {
                Control1.DefaultLabelPosition = CommandBarDefaultLabelPosition.Collapsed;
            }
            else
            {
                Control1.DefaultLabelPosition = CommandBarDefaultLabelPosition.Bottom;
            }

            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ui:CommandBar x:Name=""Control1""
    DefaultLabelPosition=""{Control1.DefaultLabelPosition.ToString()}"">
    <ui:CommandBar.PrimaryCommands>
        <ui:AppBarButton Label=""Attach Camera"" >
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.AttachCamera}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
                            
        <ui:AppBarSeparator />
                            
        <ui:AppBarButton Label=""Like"" >
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Like}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
                            
        <ui:AppBarButton Label=""Dislike"" >
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Dislike}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
                            
        <ui:AppBarSeparator />
                            
        <ui:AppBarButton Label=""Orientation"" >
            <ui:AppBarButton.Icon>
                <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Orientation}}""/>
            </ui:AppBarButton.Icon>
        </ui:AppBarButton>
    </ui:CommandBar.PrimaryCommands>
</ui:CommandBar>
";

        #endregion
    }
}
