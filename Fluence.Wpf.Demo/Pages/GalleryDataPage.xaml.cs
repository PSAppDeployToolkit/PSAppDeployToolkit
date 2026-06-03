/*
 * Copyright 2026 Dan Cunningham
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *
 * 1. Redistributions of source code must retain the above copyright notice,
 *    this list of conditions and the following disclaimer.
 * 2. Redistributions in binary form must reproduce the above copyright notice,
 *    this list of conditions and the following disclaimer in the documentation
 *    and/or other materials provided with the distribution.
 * 3. Neither the name of the copyright holder nor the names of its contributors
 *    may be used to endorse or promote products derived from this software
 *    without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS"
 * AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE
 * IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE
 * ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT HOLDER OR CONTRIBUTORS BE
 * LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR
 * CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF
 * SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS
 * INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF LIABILITY, WHETHER IN
 * CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING NEGLIGENCE OR OTHERWISE)
 * ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED OF
 * THE POSSIBILITY OF SUCH DAMAGE.
 */

using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryDataPage : UserControl
    {
        private const string ListViewItemsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Data.ListViewItems""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.ColumnDefinitions>
            <ColumnDefinition Width=""*"" />
            <ColumnDefinition Width=""20"" />
            <ColumnDefinition Width=""*"" />
        </Grid.ColumnDefinitions>
        <Border
            x:Name=""SimpleListViewBackground""
            CornerRadius=""{DynamicResource ControlCornerRadius}"">
            <ui:ListView
                x:Name=""SimpleListView""
                Height=""230""
                BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
                BorderThickness=""1"">
                <ListViewItem Content=""Ana Bowman"" />
                <ListViewItem Content=""Shawn Hughes"" />
                <ListViewItem Content=""Oscar Ward"" />
                <ListViewItem Content=""Madison Butler"" />
                <ListViewItem Content=""Graham Barnes"" />
            </ui:ListView>
        </Border>
        <Border
            x:Name=""RichListViewBackground""
            Grid.Column=""2""
            CornerRadius=""{DynamicResource ControlCornerRadius}"">
            <ui:ListView
                x:Name=""RichListView""
                Height=""230""
                BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
                BorderThickness=""1"">
                <ListViewItem>
                    <Grid Margin=""0,4"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""36"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>
                        <ui:FontIcon
                            VerticalAlignment=""Center""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Glyph=""&#xE77B;""
                            IconFontSize=""20"" />
                        <StackPanel Grid.Column=""1"">
                            <TextBlock FontWeight=""SemiBold"" Text=""Ana Bowman"" />
                            <TextBlock
                                FontSize=""12""
                                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                                Text=""Support Engineer"" />
                        </StackPanel>
                    </Grid>
                </ListViewItem>
                <ListViewItem>
                    <Grid Margin=""0,4"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""36"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>
                        <ui:FontIcon
                            VerticalAlignment=""Center""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Glyph=""&#xE77B;""
                            IconFontSize=""20"" />
                        <StackPanel Grid.Column=""1"">
                            <TextBlock FontWeight=""SemiBold"" Text=""Shawn Hughes"" />
                            <TextBlock
                                FontSize=""12""
                                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                                Text=""Platform Specialist"" />
                        </StackPanel>
                    </Grid>
                </ListViewItem>
                <ListViewItem>
                    <Grid Margin=""0,4"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""36"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>
                        <ui:FontIcon
                            VerticalAlignment=""Center""
                            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                            Glyph=""&#xE77B;""
                            IconFontSize=""20"" />
                        <StackPanel Grid.Column=""1"">
                            <TextBlock FontWeight=""SemiBold"" Text=""Oscar Ward"" />
                            <TextBlock
                                FontSize=""12""
                                Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                                Text=""DevOps Lead"" />
                        </StackPanel>
                    </Grid>
                </ListViewItem>
            </ui:ListView>
        </Border>
    </Grid>
</UserControl>
";

        private const string ListViewItemsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Data
{
    public partial class ListViewItems : UserControl
    {
        public ListViewItems()
        {
            InitializeComponent();
        }
    }
}
";
        private const string ListViewEmptyStateXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Data.ListViewEmptyState""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ListView
            x:Name=""EmptyStateListView""
            Height=""180""
            Margin=""0,0,0,12""
            Background=""{DynamicResource CardBackgroundFillColorDefaultBrush}""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1"">
            <ui:ListView.EmptyContent>
                <TextBlock
                    HorizontalAlignment=""Center""
                    VerticalAlignment=""Center""
                    Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
                    Text=""No items. Add one to begin."" />
            </ui:ListView.EmptyContent>
        </ui:ListView>
        <StackPanel
            x:Name=""EmptyStateActionsPanel""
            HorizontalAlignment=""Center""
            VerticalAlignment=""Center""
            Orientation=""Horizontal"">
            <ui:Button
                Margin=""0,0,8,0""
                Appearance=""Accent""
                Click=""AddListItem_Click""
                Content=""Add item""
                MinWidth=""140"" />
            <ui:Button
                Click=""RemoveListItem_Click""
                Content=""Remove item""
                MinWidth=""140"" />
        </StackPanel>
    </StackPanel>
</UserControl>
";

        private const string ListViewEmptyStateCSharpSource = @"using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Data
{
    public partial class ListViewEmptyState : UserControl
    {
        private int _addCounter;

        private static readonly string[] SampleNames =
        {
            ""Liam Torres"",
            ""Nora Fischer"",
            ""Eli Nakamura"",
            ""Priya Kapoor"",
            ""Dante Reeves""
        };

        public ListViewEmptyState()
        {
            InitializeComponent();
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            string name = SampleNames[_addCounter % SampleNames.Length];
            _addCounter++;

            EmptyStateListView.Items.Add(new ListViewItem { Content = name });
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView.Items.Count == 0)
            {
                return;
            }

            object lastItem = EmptyStateListView.Items[EmptyStateListView.Items.Count - 1];
            EmptyStateListView.AnimateRemove(lastItem, null);
        }
    }
}
";
        private const string CardVariantsXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Data.CardVariants""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf""
    xmlns:uicore=""clr-namespace:Fluence.Wpf;assembly=Fluence.Wpf"">
    <UniformGrid Columns=""2"">
        <ui:Card
            MinHeight=""110""
            Margin=""0,0,16,16""
            Padding=""18""
            Header=""Default""
            Variant=""{x:Static uicore:CardVariant.Default}"">
            <TextBlock Text=""Standard surface for grouped content."" TextWrapping=""Wrap"" />
        </ui:Card>
        <ui:Card
            MinHeight=""110""
            Margin=""0,0,0,16""
            Padding=""18""
            Header=""Outlined""
            Variant=""{x:Static uicore:CardVariant.Outlined}"">
            <TextBlock Text=""Emphasizes the boundary over fill."" TextWrapping=""Wrap"" />
        </ui:Card>
        <ui:Card
            MinHeight=""110""
            Margin=""0,0,16,0""
            Padding=""18""
            Header=""Filled""
            Variant=""{x:Static uicore:CardVariant.Filled}"">
            <TextBlock Text=""Adds stronger container presence."" TextWrapping=""Wrap"" />
        </ui:Card>
        <ui:Card
            MinHeight=""110""
            Padding=""18""
            Header=""Subtle""
            Variant=""{x:Static uicore:CardVariant.Subtle}"">
            <TextBlock Text=""Keeps low-emphasis supporting content grouped."" TextWrapping=""Wrap"" />
        </ui:Card>
    </UniformGrid>
</UserControl>
";

        private const string CardVariantsCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Data
{
    public partial class CardVariants : UserControl
    {
        public CardVariants()
        {
            InitializeComponent();
        }
    }
}
";
        private const string PersonPictureXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.Data.PersonPictureSample""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <WrapPanel
        HorizontalAlignment=""Center""
        VerticalAlignment=""Center"">
        <ui:PersonPicture
            Width=""56""
            Height=""56""
            Margin=""0,0,12,12""
            DisplayName=""Ana Bowman""
            ProfilePicture=""pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureAnaBowman.png"" />
        <ui:PersonPicture
            Width=""56""
            Height=""56""
            Margin=""0,0,12,12""
            DisplayName=""Shawn Hughes""
            ProfilePicture=""pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureShawnHughes.png""
            BadgeNumber=""3"" />
        <ui:PersonPicture
            Width=""56""
            Height=""56""
            Margin=""0,0,12,12""
            DisplayName=""Priya Kapoor""
            ProfilePicture=""pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPicturePriyaKapoor.png"" />
        <ui:PersonPicture
            Width=""56""
            Height=""56""
            Margin=""0,0,12,12""
            DisplayName=""Mateo Rivera""
            ProfilePicture=""pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureMateoRivera.png"" />
        <ui:PersonPicture
            Width=""56""
            Height=""56""
            Margin=""0,0,12,12""
            DisplayName=""Madison Butler""
            ProfilePicture=""pack://application:,,,/Fluence.Wpf.Demo;component/Resources/ControlImages/PersonPictureMadisonButler.png"" />
    </WrapPanel>
</UserControl>
";

        private const string PersonPictureCSharpSource = @"using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.Data
{
    public partial class PersonPictureSample : UserControl
    {
        public PersonPictureSample()
        {
            InitializeComponent();
        }
    }
}
";

        private int _addCounter;

        private static readonly string[] SampleNames =
        [
            "Liam Torres",
            "Nora Fischer",
            "Eli Nakamura",
            "Priya Kapoor",
            "Dante Reeves"
        ];

        public GalleryDataPage()
        {
            InitializeComponent();

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
            new DemoSampleSource(1, ListViewItemsXamlSource, ListViewItemsCSharpSource),
            new DemoSampleSource(2, ListViewEmptyStateXamlSource, ListViewEmptyStateCSharpSource),
            new DemoSampleSource(3, PersonPictureXamlSource, PersonPictureCSharpSource),
                new DemoSampleSource(4, CardVariantsXamlSource, CardVariantsCSharpSource));
        }

        private void AddListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView is null)
            {
                return;
            }

            string name = SampleNames[_addCounter % SampleNames.Length];
            _addCounter++;

            _ = EmptyStateListView.Items.Add(new ListViewItem { Content = name });
        }

        private void RemoveListItem_Click(object sender, RoutedEventArgs e)
        {
            if (EmptyStateListView is null || EmptyStateListView.Items.Count == 0)
            {
                return;
            }

            object lastItem = EmptyStateListView.Items[EmptyStateListView.Items.Count - 1];
            EmptyStateListView.AnimateRemove(lastItem, null);
        }
    }
}
