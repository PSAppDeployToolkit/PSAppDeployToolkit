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
    public partial class SettingsExpanderPage : Page
    {
        public SettingsExpanderPage()
        {
            InitializeComponent();
        }

        public ObservableCollection<MyDataModel> _myDataSet = new() 
        {
            new()
            {
                Name = "First Item",
                Info = "More about first item.",
                ItemType = "Item type: Button",
                LinkDescription = "Click here for more on first item.",
                Url = "https://www.inkore.net",
            },
            new()
            {
                Name = "Second Item",
                Info = "More about second item.",
                ItemType = "Item type: Link button",
                LinkDescription = "Click here for more on second item.",
                Url = "https://docs.inkore.net/zh-cn/ui-wpf-modern",
            },
            new()
            {
                Name = "Third Item",
                Info = "More about third item.",
                ItemType = "Item type: No button",
                LinkDescription = "Click here for more on third item.",
                Url = "https://mcskinn.inkore.app/",
            },
        };

        public ObservableCollection<MyDataModel> MyDataSet => _myDataSet;


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

        private void ExpandedToggle1_Toggled(object sender, RoutedEventArgs e)
        {

        }

        private void Button_Click(object sender, RoutedEventArgs e)
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
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ui:SettingsExpander x:Name=""settingsCard"" VerticalAlignment=""Top""
        Description=""The SettingsExpander has the same properties as a Card, and you can set SettingsCard as part of the Items collection.""
        Header=""SettingsExpander"" IsEnabled=""{EnableToggle1.IsOn}"">
    <ui:SettingsExpander.HeaderIcon>
        <ui:FontIcon Glyph=""&#xE91B;""/>
    </ui:SettingsExpander.HeaderIcon>
    <!--  TODO: This should be TwoWay bound but throws compile error in Uno.  -->
    <ComboBox SelectedIndex=""0"">
        <ComboBoxItem>Option 1</ComboBoxItem>
        <ComboBoxItem>Option 2</ComboBoxItem>
        <ComboBoxItem>Option 3</ComboBoxItem>
    </ComboBox>

    <ui:SettingsExpander.Items>
        <ui:SettingsCard Header=""A basic SettingsCard within an SettingsExpander"">
            <Button Content=""Button"" />
        </ui:SettingsCard>
        <ui:SettingsCard Description=""SettingsCard within an Expander can be made clickable too!""
            Header=""This item can be clicked""
            IsClickEnabled=""True"" />

        <ui:SettingsCard ContentAlignment=""Left"">
            <CheckBox Content=""Here the ContentAlignment is set to Left. This is great for e.g. CheckBoxes or RadioButtons."" />
        </ui:SettingsCard>

        <ui:SettingsCard HorizontalContentAlignment=""Left""
            ContentAlignment=""Vertical""
            Description=""You can also align your content vertically. Make sure to set the HorizontalAlignment to Left when you do!""
            Header=""Vertically aligned"">
            <ui:GridView SelectedIndex=""1"">
                <ui:GridViewItem>
                    <Border Width=""64"" Height=""64""
                        Background=""#0078D4"" CornerRadius=""4"" />
                </ui:GridViewItem>
                <ui:GridViewItem>
                    <Border Width=""64"" Height=""64""
                        Background=""#005EB7"" CornerRadius=""4"" />
                </ui:GridViewItem>
                <ui:GridViewItem>
                    <Border Width=""64"" Height=""64""
                        Background=""#003D92"" CornerRadius=""4"" />
                </ui:GridViewItem>
                <ui:GridViewItem>
                    <Border Width=""64"" Height=""64""
                        Background=""#001968"" CornerRadius=""4"" />
                </ui:GridViewItem>
            </ui:GridView>
        </ui:SettingsCard>
        <ui:SettingsCard Description=""You can override the Left indention of a SettingsCard by overriding the SettingsCardLeftIndention""
            Header=""Customization"">
            <ui:SettingsCard.Resources>
                <sys:Double x:Key=""SettingsCardLeftIndention"">40</sys:Double>
            </ui:SettingsCard.Resources>
        </ui:SettingsCard>
    </ui:SettingsExpander.Items>
</ui:SettingsExpander>
";

        public string Example2Xaml => $@"
<ikw:SimpleStackPanel Spacing=""4"">=
    <ui:SettingsExpander Description=""The SettingsExpander can use ItemsSource to define its Items.""
                    Header=""Settings Expander with ItemsSource""
                    DataContext=""{{Binding RelativeSource={{RelativeSource Mode=FindAncestor, AncestorType={{x:Type ui:Page}}}}}}""
                    ItemsSource=""{{Binding MyDataSet}}"">
        <ui:SettingsExpander.HeaderIcon>
                            
            <ui:FontIcon Glyph=""&#xEA37;""/>
        </ui:SettingsExpander.HeaderIcon>
        <ui:SettingsExpander.ItemTemplate>
            <DataTemplate>
                <ui:SettingsCard Description=""{{Binding Info}}""
                            Header=""{{Binding Name}}"">
                    <ui:HyperlinkButton Content=""{{Binding LinkDescription}}""
                            NavigateUri=""{{Binding Url}}"" />
                </ui:SettingsCard>
            </DataTemplate>
        </ui:SettingsExpander.ItemTemplate>
        <ui:SettingsExpander.ItemsHeader>
            <ui:InfoBar Title=""This is the ItemsHeader""
                BorderThickness=""0""
                CornerRadius=""0""
                IsIconVisible=""False""
                IsOpen=""True""
                Severity=""Success"">
                <ui:InfoBar.ActionButton>
                    <ui:HyperlinkButton Content=""It can host custom content"" />
                </ui:InfoBar.ActionButton>
            </ui:InfoBar>
        </ui:SettingsExpander.ItemsHeader>
    </ui:SettingsExpander>

    <ui:SettingsExpander Description=""SettingsExpander can use a DataTemplate, DataTemplateSelector, or IElementFactory for its ItemTemplate.""
                    Header=""Settings Expander with a custom ItemTemplate""
                    DataContext=""{{Binding RelativeSource={{RelativeSource Mode=FindAncestor, AncestorType={{x:Type ui:Page}}}}}}""
                    ItemsSource=""{{Binding MyDataSet}}"">
        <ui:SettingsExpander.HeaderIcon>
            <ui:FontIcon Glyph=""&#xE8FD;""/>
        </ui:SettingsExpander.HeaderIcon>
        <ui:SettingsExpander.ItemTemplate>
            <lc:MyDataModelTemplateSelector>
                <lc:MyDataModelTemplateSelector.ButtonTemplate>
                    <DataTemplate>
                        <ui:SettingsCard Description=""{{Binding ItemType}}""
                                    Header=""{{Binding Name}}"">
                            <Button Click=""Button_Click""
                        Content=""{{Binding LinkDescription}}"" />
                        </ui:SettingsCard>
                    </DataTemplate>
                </lc:MyDataModelTemplateSelector.ButtonTemplate>

                <lc:MyDataModelTemplateSelector.LinkButtonTemplate>
                    <DataTemplate>
                        <ui:SettingsCard Description=""{{Binding ItemType}}""
                                    Header=""{{Binding Name}}"">
                            <ui:HyperlinkButton Content=""{{Binding LinkDescription}}""
                                    NavigateUri=""{{Binding Url}}"" />
                        </ui:SettingsCard>
                    </DataTemplate>
                </lc:MyDataModelTemplateSelector.LinkButtonTemplate>

                <lc:MyDataModelTemplateSelector.NoButtonTemplate>
                    <DataTemplate>
                        <ui:SettingsCard Description=""{{Binding ItemType}}""
                                    Header=""{{Binding Name}}"" />
                    </DataTemplate>
                </lc:MyDataModelTemplateSelector.NoButtonTemplate>
            </lc:MyDataModelTemplateSelector>
        </ui:SettingsExpander.ItemTemplate>
    </ui:SettingsExpander>
</ikw:SimpleStackPanel>
";

        public string Example2CS => $@"
public ObservableCollection<MyDataModel> _myDataSet = new() 
{{
    new()
    {{
        Name = ""First Item"",
        Info = ""More about first item."",
        ItemType = ""Item type: Button"",
        LinkDescription = ""Click here for more on first item."",
        Url = ""https://www.inkore.net"",
    }},
    new()
    {{
        Name = ""Second Item"",
        Info = ""More about second item."",
        ItemType = ""Item type: Link button"",
        LinkDescription = ""Click here for more on second item."",
        Url = ""https://docs.inkore.net/zh-cn/ui-wpf-modern"",
    }},
    new()
    {{
        Name = ""Third Item"",
        Info = ""More about third item."",
        ItemType = ""Item type: No button"",
        LinkDescription = ""Click here for more on third item."",
        Url = ""https://mcskinn.inkore.app/"",
    }},
}};

public ObservableCollection<MyDataModel> MyDataSet => _myDataSet;

private void Button_Click(object sender, RoutedEventArgs e)
{{
    Process.Start(new ProcessStartInfo(""https://www.inkore.net"") {{ UseShellExecute = true }});
}}

public class MyDataModel
{{
    public string? Name {{ get; set; }}

    public string? Info {{ get; set; }}

    public string? ItemType {{ get; set; }}

    public string? LinkDescription {{ get; set; }}

    public string? Url {{ get; set; }}
}}

public class MyDataModelTemplateSelector : DataTemplateSelector
{{
    public DataTemplate? ButtonTemplate {{ get; set; }}
    public DataTemplate? LinkButtonTemplate {{ get; set; }}
    public DataTemplate? NoButtonTemplate {{ get; set; }}

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {{
        var itm = (MyDataModel)item;
        if (itm.ItemType?.EndsWith(""Button"") == true)
        {{
            return ButtonTemplate!;
        }}
        else if (itm.ItemType?.EndsWith(""Link button"") == true)
        {{
            return LinkButtonTemplate!;
        }}
        else
        {{
            return NoButtonTemplate!;
        }}
    }}
}}
}}
";

        public string Example3Xaml => $@"
<ScrollViewer Padding=""20,0"">
    <!--  These styles can be referenced to create a consistent SettingsPage layout  -->
    <FrameworkElement.Resources>
        <!--  Spacing between cards  -->
        <sys:Double x:Key=""SettingsCardSpacing"">4</sys:Double>
        <!--  Style (inc. the correct spacing) of a section header  -->
        <Style x:Key=""SettingsSectionHeaderTextBlockStyle""
            BasedOn=""{{StaticResource BodyStrongTextBlockStyle}}""
            TargetType=""TextBlock"">
            <Style.Setters>
                <Setter Property=""Margin"" Value=""1,30,0,6"" />
            </Style.Setters>
        </Style>
    </FrameworkElement.Resources>
    <Grid>
        <ikw:SimpleStackPanel MaxWidth=""1000""
            HorizontalAlignment=""Stretch""
            Spacing=""{{StaticResource SettingsCardSpacing}}"">
            <TextBlock Style=""{{StaticResource SettingsSectionHeaderTextBlockStyle}}""
                Text=""Section 1"" />
            <ui:SettingsCard Description=""This is a default card, with the Header, HeaderIcon, Description and Content set""
                    Header=""This is the Header"">
                <ui:SettingsCard.HeaderIcon>
                    <ui:FontIcon Glyph=""&#xE799;"" />
                </ui:SettingsCard.HeaderIcon>
                <ui:ToggleSwitch IsOn=""True"" />
            </ui:SettingsCard>

            <ui:SettingsExpander Description=""The SettingsExpander has the same properties as a SettingsCard""
                    Header=""SettingsExpander"">
                <ui:SettingsExpander.HeaderIcon>
                    <ui:FontIcon Glyph=""&#xE91B;"" />
                </ui:SettingsExpander.HeaderIcon>

                <Button Content=""Content"" Style=""{{StaticResource AccentButtonStyle}}"" />

                <ui:SettingsExpander.Items>
                    <ui:SettingsCard Header=""A basic SettingsCard within an SettingsExpander"">
                        <Button Content=""Button"" />
                    </ui:SettingsCard>
                    <ui:SettingsCard Description=""SettingsCard within an Expander can be made clickable too!""
                        Header=""This item can be clicked"" IsClickEnabled=""True"" />
                    <ui:SettingsCard ContentAlignment=""Left"">
                        <CheckBox Content=""Here the ContentAlignment is set to Left. This is great for e.g. CheckBoxes or RadioButtons"" />
                    </ui:SettingsCard>
                </ui:SettingsExpander.Items>
            </ui:SettingsExpander>

            <TextBlock Style=""{{StaticResource SettingsSectionHeaderTextBlockStyle}}"" Text=""Section 2"" />
            <ui:SettingsCard Description=""Another card to show grouping of cards""
                Header=""Another SettingsCard"">
                <ui:SettingsCard.HeaderIcon>
                    <ui:FontIcon Glyph=""&#xE799;"" />
                </ui:SettingsCard.HeaderIcon>

                <ComboBox SelectedIndex=""0"">
                    <ComboBoxItem>Option 1</ComboBoxItem>
                    <ComboBoxItem>Option 2</ComboBoxItem>
                    <ComboBoxItem>Option 3</ComboBoxItem>
                </ComboBox>
            </ui:SettingsCard>

            <ui:SettingsCard Description=""Another card to show grouping of cards""
                        Header=""Yet another SettingsCard"">
                <ui:SettingsCard.HeaderIcon>
                    <ui:FontIcon Glyph=""&#xE768;"" />
                </ui:SettingsCard.HeaderIcon>
                <Button Content=""Content"" />
            </ui:SettingsCard>

            <!--  Example 'About' section  -->
            <TextBlock Style=""{{StaticResource SettingsSectionHeaderTextBlockStyle}}"" Text=""About"" />

            <ui:SettingsExpander Description=""© iNKORE Studios 2024. All rights reserved.""
                            Header=""iNKORE.UI.WPF.Modern Gallery"">
                <ui:SettingsExpander.HeaderIcon>
                    <Image Source=""/Assets/WpfLibrary_256w.png""
                            Width=""20"" Height=""20"" RenderOptions.BitmapScalingMode=""HighQuality""/>
                </ui:SettingsExpander.HeaderIcon>
                <TextBlock Foreground=""{{DynamicResource {{x:Static ui:ThemeKeys.TextFillColorSecondaryBrushKey}}}}""
                    Text=""{ThemeManager.AssemblyVersion}"" />
                <ui:SettingsExpander.Items>
                    <ui:SettingsCard HorizontalContentAlignment=""Left""
                                ContentAlignment=""Left"">
                        <StackPanel Margin=""-12,0,0,0""
                        Orientation=""Vertical"">
                            <ui:HyperlinkButton Content=""Link 1"" />
                            <ui:HyperlinkButton Content=""Link 2"" />
                            <ui:HyperlinkButton Content=""Link 3"" />
                        </StackPanel>
                    </ui:SettingsCard>
                </ui:SettingsExpander.Items>
            </ui:SettingsExpander>
            <ui:HyperlinkButton Margin=""0,8,0,0""
                    Content=""Send feedback"" />
        </ikw:SimpleStackPanel>
    </Grid>
</ScrollViewer>
";


        #endregion
    }

    public class MyDataModel
    {
        public string? Name { get; set; }

        public string? Info { get; set; }

        public string? ItemType { get; set; }

        public string? LinkDescription { get; set; }

        public string? Url { get; set; }
    }

    public class MyDataModelTemplateSelector : DataTemplateSelector
    {
        public DataTemplate? ButtonTemplate { get; set; }
        public DataTemplate? LinkButtonTemplate { get; set; }
        public DataTemplate? NoButtonTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            var itm = (MyDataModel)item;
            if (itm.ItemType?.EndsWith("Button") == true)
            {
                return ButtonTemplate!;
            }
            else if (itm.ItemType?.EndsWith("Link button") == true)
            {
                return LinkButtonTemplate!;
            }
            else
            {
                return NoButtonTemplate!;
            }
        }
    }
}
