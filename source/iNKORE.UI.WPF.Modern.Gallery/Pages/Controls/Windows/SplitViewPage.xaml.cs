using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public sealed partial class SplitViewPage
    {
        private ObservableCollection<NavLink> _navLinks = new ObservableCollection<NavLink>()
        {
            new NavLink() { Label = "People", Symbol = Symbol.People  },
            new NavLink() { Label = "Globe", Symbol = Symbol.Globe },
            new NavLink() { Label = "Message", Symbol = Symbol.Message },
            new NavLink() { Label = "Mail", Symbol = Symbol.Mail },
        };

        public ObservableCollection<NavLink> NavLinks
        {
            get { return _navLinks; }
        }

        public SplitViewPage()
        {
            this.InitializeComponent();
        }

        private void togglePaneButton_Click(object sender, RoutedEventArgs e)
        {
            //if (Application.Current.MainWindow.ActualWidth >= 640)
            //{
            //    if (splitView.IsPaneOpen)
            //    {
            //        splitView.DisplayMode = SplitViewDisplayMode.CompactOverlay;
            //        splitView.IsPaneOpen = false;
            //    }
            //    else
            //    {
            //        splitView.IsPaneOpen = true;
            //        splitView.DisplayMode = SplitViewDisplayMode.Inline;
            //    }
            //}
            //else
            //{
            //    splitView.IsPaneOpen = !splitView.IsPaneOpen;
            //}

            UpdateExampleCode();
        }

        private void PanePlacement_Toggled(object sender, RoutedEventArgs e)
        {
            var ts = sender as ToggleSwitch;
            if (ts.IsOn)
            {
                splitView.PanePlacement = SplitViewPanePlacement.Right;
            }
            else
            {
                splitView.PanePlacement = SplitViewPanePlacement.Left;
            }

            UpdateExampleCode();
        }

        private void NavLinksList_ItemClick(object sender, ItemClickEventArgs e)
        {
            content.Text = (e.ClickedItem as NavLink).Label + " Page";
            UpdateExampleCode();
        }

        private void displayModeCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            //if (!this.IsLoaded) return;

            UpdateExampleCode();
        }

        private void paneBackgroundCombobox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void openPaneLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void compactPaneLengthSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
        }

        public string Example1Xaml => $@"
<DataTemplate x:Key=""NavLinkItemTemplate"">
    <StackPanel
        Margin=""2,0,0,0""
        AutomationProperties.Name=""{{Binding Label, Mode=OneTime}}""
        Orientation=""Horizontal"">
        <ui:SymbolIcon Symbol=""{{Binding Symbol, Mode=OneTime}}"" />
        <TextBlock
            Margin=""24,0,0,0""
            VerticalAlignment=""Center""
            Text=""{{Binding Label, Mode=OneTime}}"" />
    </StackPanel>
</DataTemplate>

<ui:SplitView x:Name=""splitView""
    CompactPaneLength=""{compactPaneLengthSlider.Value}""
    DisplayMode=""{splitView.DisplayMode}"" IsTabStop=""False""
    PanePlacement=""{splitView.PanePlacement}""
    OpenPaneLength=""{openPaneLengthSlider.Value}""
    PaneBackground=""{{DynamicResource {{x:Static ui:ThemeKeys.SystemControlBackgroundChromeMediumLowBrushKey}}}}"">
    <ui:SplitView.Pane>
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height=""Auto"" />
                <RowDefinition Height=""*"" />
                <RowDefinition Height=""Auto"" />
            </Grid.RowDefinitions>
            <TextBlock x:Name=""PaneHeader""
                Margin=""60,12,0,0""
                Style=""{{StaticResource BaseTextBlockStyle}}""
                Text=""PANE CONTENT"" />
            <ui:ListView x:Name=""NavLinksList""
                Grid.Row=""1"" Margin=""0,12,0,0""
                VerticalAlignment=""Stretch""
                IsItemClickEnabled=""True""
                ItemClick=""NavLinksList_ItemClick""
                ItemTemplate=""{{StaticResource NavLinkItemTemplate}}""
                ItemsSource=""{{Binding NavLinks}}"" />
        </Grid>
    </ui:SplitView.Pane>
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""*"" />
        </Grid.RowDefinitions>
        <TextBlock
            Margin=""12,12,0,0""
            Style=""{{StaticResource {{x:Static ui:ThemeKeys.BaseTextBlockStyleKey}}}}""
            Text=""SPLITVIEW CONTENT"" />
        <TextBlock
            x:Name=""content""
            Grid.Row=""1""
            Margin=""12,12,0,0""
            Style=""{{StaticResource {{x:Static ui:ThemeKeys.BodyTextBlockStyleKey}}}}"" />
    </Grid>
</ui:SplitView>
";

        public string Example1CS => $@" 
public class NavLink
{{
    public string Label {{ get; set; }}
    public Symbol Symbol {{ get; set; }}
}}

private ObservableCollection<NavLink> _navLinks = new ObservableCollection<NavLink>()
{{
    new NavLink() {{ Label = ""People"", Symbol = Symbol.People  }},
    new NavLink() {{ Label = ""Globe"", Symbol = Symbol.Globe }},
    new NavLink() {{ Label = ""Message"", Symbol = Symbol.Message }},
    new NavLink() {{ Label = ""Mail"", Symbol = Symbol.Mail }},
}};

public ObservableCollection<NavLink> NavLinks
{{
    get {{ return _navLinks; }}
}}

private void NavLinksList_ItemClick(object sender, ItemClickEventArgs e)
{{
    content.Text = (e.ClickedItem as NavLink).Label + "" Page"";
}}
";

        #endregion

    }

    public class NavLink
    {
        public string Label { get; set; }
        public Symbol Symbol { get; set; }
    }
}
