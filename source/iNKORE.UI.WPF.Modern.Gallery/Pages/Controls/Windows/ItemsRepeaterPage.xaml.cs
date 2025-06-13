using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ItemsRepeaterPage
    {
        private Random random = new Random();
        private int MaxLength = 425;
        private bool isHorizontal = false;

        private StackLayout VerticalStackLayout;
        private StackLayout HorizontalStackLayout;
        private UniformGridLayout UniformGridLayout;

        public ObservableCollection<Bar> BarItems;

        public ItemsRepeaterPage()
        {
            InitializeComponent();

            VerticalStackLayout = (StackLayout)Resources[nameof(VerticalStackLayout)];
            HorizontalStackLayout = (StackLayout)Resources[nameof(HorizontalStackLayout)];
            UniformGridLayout = (UniformGridLayout)Resources[nameof(UniformGridLayout)];

            InitializeData();
            repeater2.ItemsSource = Enumerable.Range(0, 500);
            repeater.ItemsSource = BarItems;

            // UpdateExampleCode();
            VStackBtn.RaiseEvent(new RoutedEventArgs(RadioButton.ClickEvent));
            Ex2_CustomLayout.RaiseEvent(new RoutedEventArgs(RadioButton.ClickEvent));
        }

        ~ItemsRepeaterPage()
        {
        }

        private void InitializeData()
        {
            if (BarItems == null) BarItems = new ObservableCollection<Bar>();

            BarItems.Add(new Bar(300, this.MaxLength));
            BarItems.Add(new Bar(25, this.MaxLength));
            BarItems.Add(new Bar(175, this.MaxLength));

            List<object> basicData =
            [
                64,
                "Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua.",
                128,
                "Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat.",
                256,
                "Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur.",
                512,
                "Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum.",
                1024,
            ];
            MixedTypeRepeater.ItemsSource = basicData;

            List<NestedCategory> nestedCategories =
            [
                new NestedCategory("Fruits", new ObservableCollection<string>
                {
                    "Apricots", "Bananas", "Grapes", "Strawberries",
                    "Watermelon", "Plums", "Blueberries"
                }),
                new NestedCategory("Vegetables", new ObservableCollection<string>
                {
                    "Broccoli", "Spinach", "Sweet potato", "Cauliflower",
                    "Onion", "Brussel sprouts", "Carrots"
                }),
                new NestedCategory("Grains", new ObservableCollection<string>
                {
                    "Rice", "Quinoa", "Pasta",  "Bread",
                    "Farro", "Oats", "Barley"
                }),
                new NestedCategory("Proteins", new ObservableCollection<string>
                {
                    "Steak", "Chicken", "Tofu", "Salmon",
                    "Pork", "Chickpeas", "Eggs"
                }),
            ];

            outerRepeater.ItemsSource = nestedCategories;
        }

        private void AddBtn_Click(object sender, RoutedEventArgs e)
        {
            BarItems.Add(new Bar(random.Next(this.MaxLength), this.MaxLength));
            DeleteBtn.IsEnabled = true;
        }

        private void DeleteBtn_Click(object sender, RoutedEventArgs e)
        {
            if (BarItems.Count > 0)
            {
                BarItems.RemoveAt(0);
                if (BarItems.Count == 0)
                {
                    DeleteBtn.IsEnabled = false;
                }
            }
        }

        private void OrientationBtn_Click(object sender, RoutedEventArgs e)
        {
            string layoutKey = String.Empty, itemTemplateKey = String.Empty;

            if (isHorizontal)
            {
                layoutKey = "VerticalStackLayout";
                itemTemplateKey = "HorizontalBarTemplate";
            }
            else
            {
                layoutKey = "HorizontalStackLayout";
                itemTemplateKey = "VerticalBarTemplate";
            }

            repeater.Layout = Resources[layoutKey] as VirtualizingLayout;
            repeater.ItemTemplate = Resources[itemTemplateKey] as DataTemplate;
            repeater.ItemsSource = BarItems;

            isHorizontal = !isHorizontal;
            UpdateExampleCode();
        }


        private string _repeater2_Layout = "";
        private void LayoutBtn_Click(object sender, RoutedEventArgs e)
        {
            string layoutKey = ((FrameworkElement)sender).Tag as string;
            if (layoutKey.Equals(nameof(this.VerticalStackLayout))) // we used x:Name in the resources which both acts as the x:Key value and creates a member field by the same name
            {
                _repeater2_Layout = _code_VerticalStackLayout;
            }
            else if (layoutKey.Equals(nameof(this.HorizontalStackLayout)))
            {
                _repeater2_Layout = _code_HorizontalStackLayout;
            }
            else if (layoutKey.Equals(nameof(this.UniformGridLayout)))
            {
                _repeater2_Layout = _code_UniformGridLayout;
            }

            repeater2.Layout = Resources[layoutKey] as VirtualizingLayout;
            UpdateExampleCode();
        }

        private string repeaterLayoutCode = "";
        private string repeaterItemTemplate = "";

        private void RadioBtn_Click(object sender, RoutedEventArgs e)
        {
            string itemTemplateKey = String.Empty;
            var layoutKey = ((FrameworkElement)sender).Tag as string;

            if (layoutKey.Equals(nameof(this.VerticalStackLayout))) // we used x:Name in the resources which both acts as the x:Key value and creates a member field by the same name
            {
                itemTemplateKey = "HorizontalBarTemplate";
                repeaterItemTemplate = _code_HorizontalBarTemplate;
                repeaterLayoutCode = _code_VerticalStackLayout;

                repeater.MaxWidth = MaxLength + 12;
            }
            else if (layoutKey.Equals(nameof(this.HorizontalStackLayout)))
            {
                itemTemplateKey = "VerticalBarTemplate";
                repeaterItemTemplate = _code_VerticalBarTemplate;
                repeaterLayoutCode = _code_HorizontalStackLayout;

                repeater.MaxWidth = 6000;
            }
            else if (layoutKey.Equals(nameof(this.UniformGridLayout)))
            {
                itemTemplateKey = "CircularTemplate";
                repeaterItemTemplate = _code_CircularTemplate;
                repeaterLayoutCode = _code_UniformGridLayout;

                repeater.MaxWidth = 540;
            }

            repeater.Layout = Resources[layoutKey] as VirtualizingLayout;
            repeater.ItemTemplate = Resources[itemTemplateKey] as DataTemplate;
            repeater.ItemsSource = BarItems;

            UpdateExampleCode();
        }

        private async void OnLoaded(object sender, RoutedEventArgs e)
        {
            DataContext = await Contact.GetContactsAsync();
        }

        private void ChangeFirstItemButton_Click(object sender, RoutedEventArgs e)
        {
            var contacts = (ObservableCollection<Contact>)DataContext;
            contacts[0] = new Contact("First", "Last", "Line 1\nLine 2");
        }

        private void ModifyFirstItemButton_Click(object sender, RoutedEventArgs e)
        {
            var contacts = (ObservableCollection<Contact>)DataContext;
            var firstContact = contacts[0];
            if (firstContact.Company.Contains("\n"))
            {
                firstContact.ChangeCompany("Line 1");
            }
            else
            {
                firstContact.ChangeCompany("Line 1\nLine 2");
            }
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateExampleCode();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
            Example2.Xaml = Example2Xaml;
            Example2.CSharp = Example2CS;
            Example3.Xaml = Example3Xaml;
            Example3.CSharp = Example3CS;
            Example4.Xaml = Example4Xaml;
            Example4.CSharp = Example4CS;

            Example5.Xaml = Example5Xaml;
            Example5.CSharp = Example5CS;
            Example6.Xaml = Example6Xaml;
            Example6.CSharp = Example6CS;
            Example7.Xaml = Example7Xaml;
            Example7.CSharp = Example7CS;
        }

        #region Page Overview

        private static readonly string _code_HorizontalBarTemplate = $@"
<DataTemplate x:Key=""HorizontalBarTemplate"">
    <Border
        Width=""{{Binding MaxLength, Mode=OneTime}}""
        Background=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
        Tag=""{{DynamicResource SystemChromeLowColor}}"">
        <Rectangle
            Width=""{{Binding Length, Mode=OneTime}}""
            Height=""24""
            HorizontalAlignment=""Left""
            Fill=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemAccentColor}}"" />
    </Border>
</DataTemplate>
";
        private static readonly string _code_VerticalBarTemplate = $@"
<DataTemplate x:Key=""VerticalBarTemplate"">
    <Border
        Height=""{{Binding MaxHeight, Mode=OneTime}}""
        Background=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
        Tag=""{{DynamicResource SystemChromeLowColor}}"">
        <Rectangle
            Width=""48""
            Height=""{{Binding Height, Mode=OneTime}}""
            VerticalAlignment=""Top""
            Fill=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemAccentColor}}"" />
    </Border>
</DataTemplate>
";
        private static readonly string _code_CircularTemplate = $@"
<DataTemplate x:Key=""CircularTemplate"">
    <Grid>
        <Ellipse
            Width=""{{Binding MaxDiameter, Mode=OneTime}}""
            Height=""{{Binding MaxDiameter, Mode=OneTime}}""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Fill=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemChromeLowColor}}"" />
        <Ellipse
            Width=""{{Binding Diameter, Mode=OneTime}}""
            Height=""{{Binding Diameter, Mode=OneTime}}""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Fill=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemAccentColor}}"" />
    </Grid>
</DataTemplate>
";

        private static readonly string _code_VerticalStackLayout = $@"
<ui:StackLayout x:Key=""VerticalStackLayout""
    Orientation=""Vertical"" Spacing=""8"" />
";
        private static readonly string _code_HorizontalStackLayout = $@"
<ui:StackLayout x:Key=""HorizontalStackLayout""
    Orientation=""Horizontal"" Spacing=""8"" />
";
        private static readonly string _code_UniformGridLayout = $@"
<ui:UniformGridLayout x:Key=""UniformGridLayout""
    MinColumnSpacing=""8"" MinRowSpacing=""8"" />
";

        public string Example1Xaml => $@"
<ui:ItemsRepeaterScrollHost MaxHeight=""500"">
    <ui:ScrollViewerEx HorizontalScrollBarVisibility=""Auto"">
        <ui:ItemsRepeater x:Name=""repeater"">
            <ui:ItemsRepeater.ItemTemplate>
                {repeaterItemTemplate.fIndent(4)}
            </ui:ItemsRepeater.ItemTemplate>
            <ui:ItemsRepeater.Layout>
                {repeaterLayoutCode.fIndent(4)}
            </ui:ItemsRepeater.Layout>
        </ui:ItemsRepeater>
    </ui:ScrollViewerEx>
</ui:ItemsRepeaterScrollHost>
";
        public string Example1CS => $@"
public class Bar
{{
    public Bar(double length, int max)
    {{
        Length = length;
        MaxLength = max;

        Height = length / 4;
        MaxHeight = max / 4;

        Diameter = length / 6;
        MaxDiameter = max / 6;
    }}
    public double Length {{ get; set; }}
    public int MaxLength {{ get; set; }}

    public double Height {{ get; set; }}
    public double MaxHeight {{ get; set; }}

    public double Diameter {{ get; set; }}
    public double MaxDiameter {{ get; set; }}
}}

private void InitializeData()
{{
    if (BarItems == null) BarItems = new ObservableCollection<Bar>();
    BarItems.Add(new Bar(300, this.MaxLength));
    BarItems.Add(new Bar(25, this.MaxLength));
    BarItems.Add(new Bar(175, this.MaxLength));
}}
";

        public string Example2Xaml => $@"
<FrameworkElement.Resources>
    <DataTemplate x:Key=""NormalItemTemplate"">
        <Button
            HorizontalAlignment=""Stretch""
            VerticalAlignment=""Stretch""
            Background=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemChromeLowColor}}"">
            <TextBlock Text=""{{Binding}}"" />
        </Button>
    </DataTemplate>
    <DataTemplate x:Key=""AccentItemTemplate"">
        <Button
            HorizontalAlignment=""Stretch""
            VerticalAlignment=""Stretch""
            Background=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource ColorToBrushConverter}}}}""
            Tag=""{{DynamicResource SystemAccentColor}}"">
            <TextBlock Text=""{{Binding}}"" />
        </Button>
    </DataTemplate>

    <l:MyDataTemplateSelector x:Key=""MyDataTemplateSelector""
        Accent=""{{StaticResource AccentItemTemplate}}""
        Normal=""{{StaticResource NormalItemTemplate}}"" />

</FrameworkElement.Resources>

<ui:ItemsRepeaterScrollHost>
    <ui:ScrollViewerEx x:Name=""scrollViewer""
        Height=""400"" Padding=""0,0,16,0"">

        <ui:ItemsRepeater x:Name=""repeater2""
            Margin=""0,0,12,0"" HorizontalAlignment=""Stretch""
            ItemTemplate=""{{StaticResource MyDataTemplateSelector}}"">
            <ui:ItemsRepeater.Layout>
                {_repeater2_Layout.fIndent(4)}
            </ui:ItemsRepeater.Layout>
        </ui:ItemsRepeater>

    </ui:ScrollViewerEx>
</ui:ItemsRepeaterScrollHost>
";
        public string Example2CS => $@"
// using: {ThemeManager.Link_GithubRepo}/blob/main/source/iNKORE.UI.WPF.Modern.Gallery/Common/ActivityFeedLayout.cs

public Page()
{{
    repeater2.ItemsSource = Enumerable.Range(0, 500);
}}

public class MyDataTemplateSelector : DataTemplateSelector
{{
    public DataTemplate Normal {{ get; set; }}
    public DataTemplate Accent {{ get; set; }}

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {{
        if ((int)item % 2 == 0)
        {{
            return Normal;
        }}
        else
        {{
            return Accent;
        }}
    }}
}}
";

        public string Example3Xaml => $@"
<FrameworkElement.Resources>
    <DataTemplate x:Key=""StringDataTemplate"">
        <Grid Margin=""10"" Background=""{{DynamicResource SystemControlBackgroundAccentBrush}}"">
            <TextBlock
                Padding=""10""
                HorizontalAlignment=""Center""
                VerticalAlignment=""Center""
                Foreground=""{{DynamicResource SystemControlForegroundChromeWhiteBrush}}""
                Text=""{{Binding}}""
                TextWrapping=""Wrap"" />
        </Grid>
    </DataTemplate>
    <DataTemplate x:Key=""IntDataTemplate"">
        <Grid Margin=""10"" Background=""{{DynamicResource SystemControlBackgroundChromeMediumBrush}}"">
            <TextBlock
                Padding=""10""
                HorizontalAlignment=""Center""
                VerticalAlignment=""Center""
                Style=""{{StaticResource HeaderTextBlockStyle}}""
                Text=""{{Binding}}"" />
        </Grid>
    </DataTemplate>

    <l:StringOrIntTemplateSelector x:Key=""StringOrIntTemplateSelector""
        IntTemplate=""{{StaticResource IntDataTemplate}}""
        StringTemplate=""{{StaticResource StringDataTemplate}}"" />
</FrameworkElement.Resources>

<ui:ItemsRepeater x:Name=""MixedTypeRepeater""
    Margin=""0,0,12,0"" HorizontalAlignment=""Stretch""
    ItemTemplate=""{{StaticResource StringOrIntTemplateSelector}}"">

    <ui:ItemsRepeater.Layout>
        <ui:UniformGridLayout MinItemHeight=""200"" MinItemWidth=""200"" />
    </ui:ItemsRepeater.Layout>
</ui:ItemsRepeater>
";
        public string Example3CS => $@"
public class StringOrIntTemplateSelector : DataTemplateSelector
{{
    // Define the (currently empty) data templates to return
    // These will be ""filled-in"" in the XAML code.
    public DataTemplate StringTemplate {{ get; set; }}

    public DataTemplate IntTemplate {{ get; set; }}

    public override DataTemplate SelectTemplate(object item, DependencyObject container)
    {{
        // Return the correct data template based on the item's type.
        if (item.GetType() == typeof(String)) return StringTemplate;
        else if (item.GetType() == typeof(int)) return IntTemplate;
        else return null;
    }}
}}
";

        public string Example4Xaml => $@"
<FrameworkElement.Resources>
    <DataTemplate x:Key=""CategoryTemplate"">
        <StackPanel>
            <TextBlock
                Padding=""8""
                Style=""{{StaticResource TitleTextBlockStyle}}""
                Text=""{{Binding CategoryName, Mode=OneTime}}"" />
            <ui:ItemsRepeater
                x:Name=""innerRepeater""
                ItemTemplate=""{{StaticResource StringDataTemplate}}""
                ItemsSource=""{{Binding CategoryItems, Mode=OneTime}}"">
                <ui:ItemsRepeater.Layout>
                    <ui:StackLayout Orientation=""Horizontal"" />
                </ui:ItemsRepeater.Layout>
            </ui:ItemsRepeater>
        </StackPanel>
    </DataTemplate>
</FrameworkElement.Resources>

<ui:ItemsRepeater x:Name=""outerRepeater""
    VerticalAlignment=""Top""
    ItemTemplate=""{{StaticResource CategoryTemplate}}"">

    <ui:ItemsRepeater.Layout>
        <ui:StackLayout Orientation=""Vertical"" />
    </ui:ItemsRepeater.Layout>

</ui:ItemsRepeater>
";
        public string Example4CS => $@"
private void InitializeData()
{{
    List<object> basicData =
    [
        64,
        ""Lorem ipsum dolor sit amet, consectetur adipiscing elit, sed do eiusmod tempor incididunt ut labore et dolore magna aliqua."",
        128,
        ""Ut enim ad minim veniam, quis nostrud exercitation ullamco laboris nisi ut aliquip ex ea commodo consequat."",
        256,
        ""Duis aute irure dolor in reprehenderit in voluptate velit esse cillum dolore eu fugiat nulla pariatur."",
        512,
        ""Excepteur sint occaecat cupidatat non proident, sunt in culpa qui officia deserunt mollit anim id est laborum."",
        1024,
    ];
    MixedTypeRepeater.ItemsSource = basicData;

    List<NestedCategory> nestedCategories =
    [
        new NestedCategory(""Fruits"", new ObservableCollection<string>
        {{
            ""Apricots"", ""Bananas"", ""Grapes"", ""Strawberries"",
            ""Watermelon"", ""Plums"", ""Blueberries""
        }}),
        new NestedCategory(""Vegetables"", new ObservableCollection<string>
        {{
            ""Broccoli"", ""Spinach"", ""Sweet potato"", ""Cauliflower"",
            ""Onion"", ""Brussel sprouts"", ""Carrots""
        }}),
        new NestedCategory(""Grains"", new ObservableCollection<string>
        {{
            ""Rice"", ""Quinoa"", ""Pasta"",  ""Bread"", 
            ""Farro"", ""Oats"", ""Barley""
        }}),
        new NestedCategory(""Proteins"", new ObservableCollection<string>
        {{
            ""Steak"", ""Chicken"", ""Tofu"", ""Salmon"",
            ""Pork"", ""Chickpeas"", ""Eggs""
        }}),
    ];

    outerRepeater.ItemsSource = nestedCategories;
}}
";

        #endregion

        #region Other Pages

        private readonly string _thisDataContext_ = $@"
// using: {ThemeManager.Link_GithubRepo}/blob/main/source/iNKORE.UI.WPF.Modern.Gallery/Pages/Controls/Windows/ListViewPage.xaml.cs

private async void OnLoaded(object sender, RoutedEventArgs e)
{{
    DataContext = await Contact.GetContactsAsync();
}}
";


        public string Example5Xaml => $@"
<FrameworkElement.Resources>
    <DataTemplate x:Key=""ContactListViewTemplate"">
        <Grid>
            <Grid.RowDefinitions>
                <RowDefinition Height=""*"" />
                <RowDefinition Height=""*"" />
            </Grid.RowDefinitions>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""Auto"" />
                <ColumnDefinition Width=""*"" />
            </Grid.ColumnDefinitions>
            <Ellipse
                x:Name=""Ellipse""
                Grid.RowSpan=""2""
                Width=""32""
                Height=""32""
                Margin=""6""
                HorizontalAlignment=""Center""
                VerticalAlignment=""Center""
                Fill=""Gray"" />
            <TextBlock
                Grid.Column=""1""
                Margin=""12,6,0,0""
                Style=""{{StaticResource BaseTextBlockStyle}}""
                Text=""{{Binding Name, Mode=OneTime}}"" />
            <TextBlock
                Grid.Row=""1""
                Grid.Column=""1""
                Margin=""12,0,0,6""
                Style=""{{StaticResource BodyTextBlockStyle}}""
                Text=""{{Binding Company}}"" />
        </Grid>
    </DataTemplate>

    <ui:StackLayout x:Key=""stackLayout"" Spacing=""{Example5Spacing.Value}""/>
    <l:SpacingConverter x:Key=""SpacingConverter"" />
</FrameworkElement.Resources>

<ui:ItemsRepeaterScrollHost>
    <ui:ScrollViewerEx>
        <ui:ItemsRepeater ItemsSource=""{{Binding}}""
            ItemTemplate=""{{StaticResource ContactListViewTemplate}}""
            Layout=""{{StaticResource stackLayout}}"" />
    </ui:ScrollViewerEx>
</ui:ItemsRepeaterScrollHost>
";
        public string Example5CS => $@"
{_thisDataContext_}
";

        public string Example6Xaml => $@"
<FrameworkElement.Resources>
    <ui:UniformGridLayout x:Key=""uniformGridLayout""
        MinItemWidth=""{Ex6_MinItemWidth.Value}""  MinItemHeight=""{Ex6_MinItemHeight.Value}""
        MinRowSpacing=""{Ex6_MinRowSpacing.Value}"" MinColumnSpacing=""{Ex6_MinColumnSpacing.Value}""
        MaximumRowsOrColumns=""{Ex6_MaximumRowsOrColumns.Value}"" Orientation=""{Ex6_Orientation.SelectedItem}""
        ItemsJustification=""{Ex6_ItemsJustification.SelectedItem}"" ItemsStretch=""{Ex6_ItemsStretch.SelectedItem}""/>
</FrameworkElement.Resources>

<ui:ItemsRepeaterScrollHost>
    <ui:ScrollViewerEx>
        <ScrollViewer.Style>
            <Style BasedOn=""{{StaticResource {{x:Type ScrollViewer}}}}"" TargetType=""ScrollViewer"">
                <Setter Property=""HorizontalScrollBarVisibility"" Value=""Disabled"" />
                <Setter Property=""VerticalScrollBarVisibility"" Value=""Auto"" />
                <Style.Triggers>
                    <DataTrigger Binding=""{{Binding Source={{StaticResource uniformGridLayout}}, Path=Orientation}}"" Value=""Vertical"">
                        <Setter Property=""HorizontalScrollBarVisibility"" Value=""Auto"" />
                        <Setter Property=""VerticalScrollBarVisibility"" Value=""Disabled"" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </ScrollViewer.Style>
        <ui:ItemsRepeater x:Name=""UniformGridLayoutRepeater""
            ItemTemplate=""{{StaticResource ContactListViewTemplate}}""
            ItemsSource=""{{Binding}}""
            Layout=""{{StaticResource uniformGridLayout}}"" />
    </ui:ScrollViewerEx>
</ui:ItemsRepeaterScrollHost>
";
        public string Example6CS => $@"
{_thisDataContext_}
";

        public string Example7Xaml => $@"
<FrameworkElement.Resources>
    <ui:FlowLayout x:Key=""flowLayout"" Orientation=""{Ex7_Orientation.SelectedItem}""
        Ex7_MinRowSpacing=""{Ex7_MinRowSpacing.Value}"" Ex7_MinColumnSpacing=""{Ex7_MinColumnSpacing.Value}""
        LineAlignment=""{Ex7_LineAlignment.SelectedItem}"" />
</FrameworkElement.Resources>

<ui:ItemsRepeaterScrollHost>
    <ui:ScrollViewerEx>
        <ScrollViewer.Style>
            <Style BasedOn=""{{StaticResource {{x:Type ScrollViewer}}}}"" TargetType=""ScrollViewer"">
                <Setter Property=""HorizontalScrollBarVisibility"" Value=""Disabled"" />
                <Setter Property=""VerticalScrollBarVisibility"" Value=""Auto"" />
                <Style.Triggers>
                    <DataTrigger Binding=""{{Binding Source={{StaticResource flowLayout}}, Path=Orientation}}"" Value=""Vertical"">
                        <Setter Property=""HorizontalScrollBarVisibility"" Value=""Auto"" />
                        <Setter Property=""VerticalScrollBarVisibility"" Value=""Disabled"" />
                    </DataTrigger>
                </Style.Triggers>
            </Style>
        </ScrollViewer.Style>
        <ui:ItemsRepeater ItemsSource=""{{Binding}}""
            ItemTemplate=""{{StaticResource ContactListViewTemplate}}""
            Layout=""{{StaticResource flowLayout}}"" />
    </ui:ScrollViewerEx>
</ui:ItemsRepeaterScrollHost>
";
        public string Example7CS => $@"
{_thisDataContext_}
";

        #endregion

        #endregion

    }

    public class NestedCategory
    {
        public string CategoryName { get; set; }
        public ObservableCollection<string> CategoryItems { get; set; }
        public NestedCategory(string catName, ObservableCollection<string> catItems)
        {
            CategoryName = catName;
            CategoryItems = catItems;
        }
    }

    public class MyDataTemplateSelector : DataTemplateSelector
    {
        public DataTemplate Normal { get; set; }
        public DataTemplate Accent { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            if ((int)item % 2 == 0) return Normal;
            else return Accent;
        }
    }

    public class StringOrIntTemplateSelector : DataTemplateSelector
    {
        // Define the (currently empty) data templates to return
        // These will be "filled-in" in the XAML code.
        public DataTemplate StringTemplate { get; set; }

        public DataTemplate IntTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container)
        {
            // Return the correct data template based on the item's type.
            if (item.GetType() == typeof(String)) return StringTemplate;
            else if (item.GetType() == typeof(int)) return IntTemplate;
            else return null;
        }
    }

    public class Bar
    {
        public Bar(double length, int max)
        {
            Length = length;
            MaxLength = max;

            Height = length / 4;
            MaxHeight = max / 4;

            Diameter = length / 6;
            MaxDiameter = max / 6;
        }
        public double Length { get; set; }
        public int MaxLength { get; set; }

        public double Height { get; set; }
        public double MaxHeight { get; set; }

        public double Diameter { get; set; }
        public double MaxDiameter { get; set; }
    }

    public class SpacingConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return value;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double d && double.IsNaN(d))
            {
                return 0d;
            }

            return value;
        }
    }
}
