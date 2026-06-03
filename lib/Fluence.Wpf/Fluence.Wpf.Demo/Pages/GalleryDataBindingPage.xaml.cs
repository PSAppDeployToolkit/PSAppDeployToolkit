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

using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Gallery page demonstrating ObservableCollection binding and ListView SelectionMode variants.
    /// </summary>
    public partial class GalleryDataBindingPage : UserControl
    {
        private const string ObservableCollectionListViewXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.DataBinding.ObservableCollectionListView""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <Grid>
        <Grid.RowDefinitions>
            <RowDefinition Height=""Auto"" />
            <RowDefinition Height=""8"" />
            <RowDefinition Height=""220"" />
        </Grid.RowDefinitions>

        <Grid>
            <Grid.ColumnDefinitions>
                <ColumnDefinition Width=""*"" />
                <ColumnDefinition Width=""8"" />
                <ColumnDefinition Width=""Auto"" />
                <ColumnDefinition Width=""4"" />
                <ColumnDefinition Width=""Auto"" />
            </Grid.ColumnDefinitions>
            <ui:TextBox
                x:Name=""NewItemBox""
                KeyDown=""NewItemBox_KeyDown""
                PlaceholderText=""New item name..."" />
            <ui:Button
                Grid.Column=""2""
                Appearance=""Accent""
                Click=""AddItem_Click""
                Content=""Add"" />
            <ui:Button
                Grid.Column=""4""
                Click=""RemoveItem_Click""
                Content=""Remove selected"" />
        </Grid>

        <ui:ListView
            x:Name=""BoundListView""
            Grid.Row=""2""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1""
            SelectionMode=""Single"">
            <ui:ListView.ItemTemplate>
                <DataTemplate>
                    <Grid>
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""Auto"" />
                            <ColumnDefinition Width=""12"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>
                        <ui:FontIcon
                            VerticalAlignment=""Center""
                            Foreground=""{DynamicResource AccentTextFillColorPrimaryBrush}""
                            Glyph=""&#xE8A5;""
                            IconFontSize=""16"" />
                        <StackPanel
                            Grid.Column=""2""
                            VerticalAlignment=""Center""
                            Orientation=""Horizontal"">
                            <TextBlock
                                VerticalAlignment=""Center""
                                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                                Text=""{Binding Name}"" />
                            <TextBlock
                                Margin=""{DynamicResource DemoDataBindingSecondaryTextMargin}""
                                VerticalAlignment=""Center""
                                Style=""{StaticResource CaptionTextBlockStyle}""
                                Foreground=""{DynamicResource TextFillColorTertiaryBrush}""
                                Text=""{Binding AddedAt}"" />
                        </StackPanel>
                    </Grid>
                </DataTemplate>
            </ui:ListView.ItemTemplate>
        </ui:ListView>
    </Grid>
</UserControl>
";

        private const string ObservableCollectionListViewCSharpSource = @"using System;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Demo.Pages.DataBinding
{
    public partial class ObservableCollectionListView : UserControl
    {
        private readonly ObservableCollection<DataBindingSampleItem> _items = new ObservableCollection<DataBindingSampleItem>();

        public ObservableCollectionListView()
        {
            InitializeComponent();

            BoundListView.ItemsSource = _items;
            AddDemoItem(""Fluence.Wpf"");
            AddDemoItem(""WinUI 3 parity controls"");
            AddDemoItem(""net472 + net10.0-windows"");
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (NewItemBox is null)
            {
                return;
            }

            string text = (NewItemBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            AddDemoItem(text);
            NewItemBox.Text = string.Empty;
            NewItemBox.Focus();
        }

        private void NewItemBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddItem_Click(sender, e);
                e.Handled = true;
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (BoundListView.SelectedItem is DataBindingSampleItem selected)
            {
                BoundListView.AnimateRemove(selected, null);
            }
        }

        private void AddDemoItem(string name)
        {
            _items.Add(new DataBindingSampleItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString(""HH:mm:ss"", CultureInfo.CurrentCulture)
            });
        }

    }

    public sealed class DataBindingSampleItem
    {
        public string Name { get; set; } = string.Empty;

        public string AddedAt { get; set; } = string.Empty;
    }
}
";
        private const string ListViewSelectionModeXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.DataBinding.ListViewSelectionMode""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <StackPanel Margin=""0,0,0,12"" Orientation=""Horizontal"">
            <ui:RadioButton
                x:Name=""SingleModeRadio""
                Margin=""0,0,16,0""
                Checked=""SelectionMode_Changed""
                Content=""Single""
                GroupName=""SelectionModeGroup""
                IsChecked=""True"" />
            <ui:RadioButton
                x:Name=""MultipleModeRadio""
                Margin=""0,0,16,0""
                Checked=""SelectionMode_Changed""
                Content=""Multiple""
                GroupName=""SelectionModeGroup"" />
            <ui:RadioButton
                x:Name=""ExtendedModeRadio""
                Checked=""SelectionMode_Changed""
                Content=""Extended (Shift+Click)""
                GroupName=""SelectionModeGroup"" />
        </StackPanel>
        <ui:ListView
            x:Name=""SelectionModeListView""
            Height=""200""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1""
            SelectionChanged=""SelectionModeListView_SelectionChanged""
            SelectionMode=""Single"">
            <ListViewItem Content=""Alpha"" />
            <ListViewItem Content=""Bravo"" />
            <ListViewItem Content=""Charlie"" />
            <ListViewItem Content=""Delta"" />
            <ListViewItem Content=""Echo"" />
            <ListViewItem Content=""Foxtrot"" />
            <ListViewItem Content=""Golf"" />
            <ListViewItem Content=""Hotel"" />
        </ui:ListView>
        <TextBlock
            x:Name=""SelectionCountLabel""
            Margin=""0,8,0,0""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Selected: none"" />
    </StackPanel>
</UserControl>
";

        private const string ListViewSelectionModeCSharpSource = @"using System;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.DataBinding
{
    public partial class ListViewSelectionMode : UserControl
    {
        public ListViewSelectionMode()
        {
            InitializeComponent();
        }

        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeListView is null)
            {
                return;
            }

            if (MultipleModeRadio is not null && MultipleModeRadio.IsChecked == true)
            {
                SelectionModeListView.SelectionMode = SelectionMode.Multiple;
            }
            else if (ExtendedModeRadio is not null && ExtendedModeRadio.IsChecked == true)
            {
                SelectionModeListView.SelectionMode = SelectionMode.Extended;
            }
            else
            {
                SelectionModeListView.SelectionMode = SelectionMode.Single;
            }

            SelectionModeListView.UnselectAll();
            UpdateSelectionLabel();
        }

        private void SelectionModeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionLabel();
        }

        private void UpdateSelectionLabel()
        {
            if (SelectionCountLabel is null || SelectionModeListView is null)
            {
                return;
            }

            int count = SelectionModeListView.SelectedItems.Count;
            if (count == 0)
            {
                SelectionCountLabel.Text = ""Selected: none"";
                return;
            }

            SelectionCountLabel.Text = count == 1
                ? string.Format(""Selected: {0}"", (SelectionModeListView.SelectedItem as ListViewItem)?.Content ?? ""?"")
                : string.Format(""Selected: {0} items"", count);
        }
    }
}
";
        private const string DataTemplateRowXamlSource = @"<UserControl
    x:Class=""Fluence.Wpf.Demo.Pages.DataBinding.DataTemplateRow""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:ui=""clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf"">
    <StackPanel>
        <ui:ListView
            x:Name=""DataTemplateListView""
            Height=""180""
            BorderBrush=""{DynamicResource CardStrokeColorDefaultBrush}""
            BorderThickness=""1""
            SelectionMode=""Single"">
            <ui:ListView.ItemTemplate>
                <DataTemplate>
                    <Grid Margin=""0,2"">
                        <Grid.ColumnDefinitions>
                            <ColumnDefinition Width=""Auto"" />
                            <ColumnDefinition Width=""12"" />
                            <ColumnDefinition Width=""*"" />
                        </Grid.ColumnDefinitions>
                        <ui:FontIcon
                            VerticalAlignment=""Center""
                            Foreground=""{DynamicResource AccentTextFillColorPrimaryBrush}""
                            Glyph=""&#xE8A5;""
                            IconFontSize=""16"" />
                        <StackPanel
                            Grid.Column=""2""
                            VerticalAlignment=""Center""
                            Orientation=""Horizontal"">
                            <TextBlock
                                VerticalAlignment=""Center""
                                Foreground=""{DynamicResource TextFillColorPrimaryBrush}""
                                Text=""{Binding Name}"" />
                            <TextBlock
                                Margin=""8,0,0,0""
                                VerticalAlignment=""Center""
                                FontSize=""12""
                                Foreground=""{DynamicResource TextFillColorTertiaryBrush}""
                                Text=""{Binding AddedAt}"" />
                        </StackPanel>
                    </Grid>
                </DataTemplate>
            </ui:ListView.ItemTemplate>
        </ui:ListView>
        <TextBlock
            Margin=""0,8,0,0""
            Foreground=""{DynamicResource TextFillColorSecondaryBrush}""
            Text=""Name and AddedAt are simple properties on each bound item."" />
    </StackPanel>
</UserControl>
";

        private const string DataTemplateRowCSharpSource = @"using System;
using System.Collections.ObjectModel;
using System.Windows.Controls;

namespace Fluence.Wpf.Demo.Pages.DataBinding
{
    public partial class DataTemplateRow : UserControl
    {
        public DataTemplateRow()
        {
            InitializeComponent();

            DataTemplateListView.ItemsSource = new ObservableCollection<DataBindingTemplateItem>
            {
                new DataBindingTemplateItem { Name = ""Release notes"", AddedAt = DateTime.Now.ToString(""HH:mm:ss"") },
                new DataBindingTemplateItem { Name = ""Design tokens"", AddedAt = DateTime.Now.ToString(""HH:mm:ss"") },
                new DataBindingTemplateItem { Name = ""Control states"", AddedAt = DateTime.Now.ToString(""HH:mm:ss"") }
            };
        }
    }

    public sealed class DataBindingTemplateItem
    {
        public string Name { get; set; } = string.Empty;

        public string AddedAt { get; set; } = string.Empty;
    }
}
";

        private readonly ObservableCollection<DemoItem> _items = [];
        private readonly ObservableCollection<DemoItem> _templateItems = [];

        /// <summary>
        /// Initializes a new instance of <see cref="GalleryDataBindingPage"/>.
        /// </summary>
        public GalleryDataBindingPage()
        {
            InitializeComponent();

            // Move each hidden slot's control into its DemoSampleControl card and attach the
            // XAML/C# source shown in the expander. The Nth source maps to DemoSampleSlot{N}. See
            // DemoSamplePageWiring for the slot-naming contract.
            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, ObservableCollectionListViewXamlSource, ObservableCollectionListViewCSharpSource),
                new DemoSampleSource(2, ListViewSelectionModeXamlSource, ListViewSelectionModeCSharpSource),
                new DemoSampleSource(3, DataTemplateRowXamlSource, DataTemplateRowCSharpSource));

            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            BoundListView.ItemsSource = _items;
            DataTemplateListView.ItemsSource = _templateItems;

            // Seed a few items so the list is not empty on first load.
            AddDemoItem("Fluence.Wpf");
            AddDemoItem("WinUI 3 parity controls");
            AddDemoItem("net472 + net10.0-windows");
            AddDataTemplateItem("Release notes");
            AddDataTemplateItem("Design tokens");
            AddDataTemplateItem("Control states");
        }

        private void AddItem_Click(object sender, RoutedEventArgs e)
        {
            if (NewItemBox is null)
            {
                return;
            }

            string text = (NewItemBox.Text ?? string.Empty).Trim();
            if (string.IsNullOrWhiteSpace(text))
            {
                return;
            }

            AddDemoItem(text);
            NewItemBox.Text = string.Empty;
            _ = NewItemBox.Focus();
        }

        private void NewItemBox_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                AddItem_Click(sender, e);
                e.Handled = true;
            }
        }

        private void RemoveItem_Click(object sender, RoutedEventArgs e)
        {
            if (BoundListView is null)
            {
                return;
            }

            if (BoundListView.SelectedItem is DemoItem selected)
            {
                BoundListView.AnimateRemove(selected, null);
            }
        }

        private void AddDemoItem(string name)
        {
            _items.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture)
            });
        }

        private void AddDataTemplateItem(string name)
        {
            _templateItems.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture)
            });
        }

        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeListView is null)
            {
                return;
            }

            SelectionModeListView.SelectionMode = MultipleModeRadio?.IsChecked == true
                ? SelectionMode.Multiple
                : ExtendedModeRadio?.IsChecked == true ? SelectionMode.Extended : SelectionMode.Single;

            SelectionModeListView.UnselectAll();
            UpdateSelectionLabel();
        }

        private void SelectionModeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateSelectionLabel();
        }

        private void UpdateSelectionLabel()
        {
            if (SelectionCountLabel is null || SelectionModeListView is null)
            {
                return;
            }

            int count = SelectionModeListView.SelectedItems.Count;
            if (count == 0)
            {
                SelectionCountLabel.Text = "Selected: none";
                return;
            }

            SelectionCountLabel.Text = count == 1
                ? string.Format(CultureInfo.CurrentCulture, "Selected: {0}", (SelectionModeListView.SelectedItem as ListViewItem)?.Content ?? "?")
                : string.Format(CultureInfo.CurrentCulture, "Selected: {0} items", count);
        }
    }

    /// <summary>
    /// Simple view-model item for the bound list demo.
    /// </summary>
    public sealed class DemoItem
    {
        /// <summary>
        /// Gets or sets the display name.
        /// </summary>
        public string? Name { get; set; }

        /// <summary>
        /// Gets or sets the time the item was added (formatted string).
        /// </summary>
        public string? AddedAt { get; set; }
    }
}
