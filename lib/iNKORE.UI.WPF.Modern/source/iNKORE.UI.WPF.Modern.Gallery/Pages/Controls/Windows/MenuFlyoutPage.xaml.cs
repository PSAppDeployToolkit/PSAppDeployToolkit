using iNKORE.UI.WPF.Modern.Controls;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class MenuFlyoutPage : Page
    {
        public MenuFlyoutPage()
        {
            InitializeComponent();
        }

        private void MenuFlyoutItem_Click(object sender, RoutedEventArgs e)
        {
            if (sender is MenuItem selectedItem)
            {
                string sortOption = selectedItem.Tag.ToString();
                switch (sortOption)
                {
                    case "rating":
                        //SortByRating();
                        break;
                    case "match":
                        //SortByMatch();
                        break;
                    case "distance":
                        //SortByDistance();
                        break;
                }
                Control1Output.Text = "Sort by: " + sortOption;
            }
        }

        private void Example5_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();

            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "RepeatToggle",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = RepeatToggleMenuFlyoutItem,
                Path = new PropertyPath("IsChecked"),
            });
            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "ShuffleToggle",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = ShuffleToggleMenuFlyoutItem,
                Path = new PropertyPath("IsChecked"),
            });
            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution> { Substitution1, Substitution2 };
            Example2.Substitutions = Substitutions;
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
        }

        public string Example1Xaml => $@"
<ui:AppBarButton Label=""Sort"">
    <ui:AppBarButton.Icon>
        <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Sort}}""/>
    </ui:AppBarButton.Icon>
    <ui:AppBarButton.Flyout>
        <ui:MenuFlyout>
            <MenuItem
                Click=""MenuFlyoutItem_Click""
                Header=""By rating""
                Tag=""rating"" />
            <MenuItem
                Click=""MenuFlyoutItem_Click""
                Header=""By match""
                Tag=""match"" />
            <MenuItem
                Click=""MenuFlyoutItem_Click""
                Header=""By distance""
                Tag=""distance"" />
        </ui:MenuFlyout>
    </ui:AppBarButton.Flyout>
</ui:AppBarButton>
";

        public string Example2Xaml => $@"
<Button x:Name=""Control2"" Content=""Options"">
    <ui:FlyoutService.Flyout>
        <ui:MenuFlyout>
            <MenuItem Header=""Reset"" />
            <Separator />
            <MenuItem
                x:Name=""RepeatToggleMenuFlyoutItem""
                Header=""Repeat""
                IsCheckable=""True""
                IsChecked=""True"" />
            <MenuItem
                x:Name=""ShuffleToggleMenuFlyoutItem""
                Header=""Shuffle""
                IsCheckable=""True""
                IsChecked=""True"" />
        </ui:MenuFlyout>
    </ui:FlyoutService.Flyout>
</Button>
";

        public string Example3Xaml => $@"
<Button x:Name=""Control3"" Content=""File Options"">
    <ui:FlyoutService.Flyout>
        <ui:MenuFlyout>
            <MenuItem Header=""Open"" />
            <MenuItem Header=""Send to"">
                <MenuItem Header=""Bluetooth"" />
                <MenuItem Header=""Desktop (shortcut)"" />
                <MenuItem Header=""Compressed file"">
                    <MenuItem Header=""Compress and email"" />
                    <MenuItem Header=""Compress to .7z"" />
                    <MenuItem Header=""Compress to .zip"" />
                </MenuItem>
            </MenuItem>
        </ui:MenuFlyout>
    </ui:FlyoutService.Flyout>
</Button>
";

        public string Example4Xaml => $@"
<Button x:Name=""Control4"" Content=""Edit Options"">
    <ui:FlyoutService.Flyout>
        <ui:MenuFlyout>
            <MenuItem Header=""Share"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE72D;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Copy"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE16F;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem Header=""Delete"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE107;"" />
                </MenuItem.Icon>
            </MenuItem>
            <Separator />
            <MenuItem Header=""Rename"" />
            <MenuItem Header=""Select"" />
        </ui:MenuFlyout>
    </ui:FlyoutService.Flyout>
</Button>
";

        public string Example5Xaml => $@"
<Button x:Name=""Control5"" Content=""Edit Options"">
    <ui:FlyoutService.Flyout>
        <ui:MenuFlyout>
            <MenuItem Header=""Share"" InputGestureText=""Ctrl+S"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE72D;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem
                FontFamily=""Consolas""
                Header=""Copy""
                InputGestureText=""Ctrl+C"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE16F;"" />
                </MenuItem.Icon>
            </MenuItem>
            <MenuItem
                FontFamily=""Segoe UI""
                Header=""Delete""
                InputGestureText=""Delete"">
                <MenuItem.Icon>
                    <ui:FontIcon Glyph=""&#xE107;"" />
                </MenuItem.Icon>
            </MenuItem>
            <Separator />
            <MenuItem Header=""Rename"" />
            <MenuItem Header=""Select"" />
        </ui:MenuFlyout>
    </ui:FlyoutService.Flyout>
</Button>
";

        #endregion

    }
}
