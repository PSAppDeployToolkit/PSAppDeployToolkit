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
        private const string ObservableCollectionListViewXamlSource = "<UserControl\n" +
                                                                      "    x:Class=\"Fluence.Wpf.Demo.Pages.DataBinding.ObservableCollectionListView\"\n" +
                                                                      "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                                      "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                                      "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                                      "    <Grid>\n" +
                                                                      "        <Grid.RowDefinitions>\n" +
                                                                      "            <RowDefinition Height=\"Auto\" />\n" +
                                                                      "            <RowDefinition Height=\"8\" />\n" +
                                                                      "            <RowDefinition Height=\"220\" />\n" +
                                                                      "        </Grid.RowDefinitions>\n" +
                                                                      "\n" +
                                                                      "        <Grid>\n" +
                                                                      "            <Grid.ColumnDefinitions>\n" +
                                                                      "                <ColumnDefinition Width=\"*\" />\n" +
                                                                      "                <ColumnDefinition Width=\"8\" />\n" +
                                                                      "                <ColumnDefinition Width=\"Auto\" />\n" +
                                                                      "                <ColumnDefinition Width=\"4\" />\n" +
                                                                      "                <ColumnDefinition Width=\"Auto\" />\n" +
                                                                      "            </Grid.ColumnDefinitions>\n" +
                                                                      "            <ui:TextBox\n" +
                                                                      "                x:Name=\"NewItemBox\"\n" +
                                                                      "                KeyDown=\"NewItemBox_KeyDown\"\n" +
                                                                      "                PlaceholderText=\"New item name...\" />\n" +
                                                                      "            <ui:Button\n" +
                                                                      "                Grid.Column=\"2\"\n" +
                                                                      "                Appearance=\"Accent\"\n" +
                                                                      "                Click=\"AddItem_Click\"\n" +
                                                                      "                Content=\"Add\" />\n" +
                                                                      "            <ui:Button\n" +
                                                                      "                Grid.Column=\"4\"\n" +
                                                                      "                Click=\"RemoveItem_Click\"\n" +
                                                                      "                Content=\"Remove selected\" />\n" +
                                                                      "        </Grid>\n" +
                                                                      "\n" +
                                                                      "        <ui:ListView\n" +
                                                                      "            x:Name=\"BoundListView\"\n" +
                                                                      "            Grid.Row=\"2\"\n" +
                                                                      "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                                      "            BorderThickness=\"1\"\n" +
                                                                      "            SelectionMode=\"Single\">\n" +
                                                                      "            <ui:ListView.ItemTemplate>\n" +
                                                                      "                <DataTemplate>\n" +
                                                                      "                    <Grid>\n" +
                                                                      "                        <Grid.ColumnDefinitions>\n" +
                                                                      "                            <ColumnDefinition Width=\"Auto\" />\n" +
                                                                      "                            <ColumnDefinition Width=\"12\" />\n" +
                                                                      "                            <ColumnDefinition Width=\"*\" />\n" +
                                                                      "                        </Grid.ColumnDefinitions>\n" +
                                                                      "                        <ui:FontIcon\n" +
                                                                      "                            VerticalAlignment=\"Center\"\n" +
                                                                      "                            Foreground=\"{DynamicResource AccentTextFillColorPrimaryBrush}\"\n" +
                                                                      "                            Glyph=\"&#xE8A5;\"\n" +
                                                                      "                            IconFontSize=\"16\" />\n" +
                                                                      "                        <StackPanel\n" +
                                                                      "                            Grid.Column=\"2\"\n" +
                                                                      "                            VerticalAlignment=\"Center\"\n" +
                                                                      "                            Orientation=\"Horizontal\">\n" +
                                                                      "                            <TextBlock\n" +
                                                                      "                                VerticalAlignment=\"Center\"\n" +
                                                                      "                                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                                      "                                Text=\"{Binding Name}\" />\n" +
                                                                      "                            <TextBlock\n" +
                                                                      "                                Margin=\"{DynamicResource DemoDataBindingSecondaryTextMargin}\"\n" +
                                                                      "                                VerticalAlignment=\"Center\"\n" +
                                                                      "                                Style=\"{StaticResource CaptionTextBlockStyle}\"\n" +
                                                                      "                                Foreground=\"{DynamicResource TextFillColorTertiaryBrush}\"\n" +
                                                                      "                                Text=\"{Binding AddedAt}\" />\n" +
                                                                      "                        </StackPanel>\n" +
                                                                      "                    </Grid>\n" +
                                                                      "                </DataTemplate>\n" +
                                                                      "            </ui:ListView.ItemTemplate>\n" +
                                                                      "        </ui:ListView>\n" +
                                                                      "    </Grid>\n" +
                                                                      "</UserControl>\n";

        private const string ObservableCollectionListViewCSharpSource = "using System;\n" +
                                                                        "using System.Collections.ObjectModel;\n" +
                                                                        "using System.Globalization;\n" +
                                                                        "using System.Windows;\n" +
                                                                        "using System.Windows.Controls;\n" +
                                                                        "using System.Windows.Input;\n" +
                                                                        "\n" +
                                                                        "namespace Fluence.Wpf.Demo.Pages.DataBinding\n" +
                                                                        "{\n" +
                                                                        "    public partial class ObservableCollectionListView : UserControl\n" +
                                                                        "    {\n" +
                                                                        "        private readonly ObservableCollection<DataBindingSampleItem> _items = new ObservableCollection<DataBindingSampleItem>();\n" +
                                                                        "\n" +
                                                                        "        public ObservableCollectionListView()\n" +
                                                                        "        {\n" +
                                                                        "            InitializeComponent();\n" +
                                                                        "\n" +
                                                                        "            BoundListView.ItemsSource = _items;\n" +
                                                                        "            AddDemoItem(\"Fluence.Wpf\");\n" +
                                                                        "            AddDemoItem(\"WinUI 3 parity controls\");\n" +
                                                                        "            AddDemoItem(\"net472 + net10.0-windows\");\n" +
                                                                        "        }\n" +
                                                                        "\n" +
                                                                        "        private void AddItem_Click(object sender, RoutedEventArgs e)\n" +
                                                                        "        {\n" +
                                                                        "            if (NewItemBox is null)\n" +
                                                                        "            {\n" +
                                                                        "                return;\n" +
                                                                        "            }\n" +
                                                                        "\n" +
                                                                        "            string text = (NewItemBox.Text ?? string.Empty).Trim();\n" +
                                                                        "            if (string.IsNullOrWhiteSpace(text))\n" +
                                                                        "            {\n" +
                                                                        "                return;\n" +
                                                                        "            }\n" +
                                                                        "\n" +
                                                                        "            AddDemoItem(text);\n" +
                                                                        "            NewItemBox.Text = string.Empty;\n" +
                                                                        "            NewItemBox.Focus();\n" +
                                                                        "        }\n" +
                                                                        "\n" +
                                                                        "        private void NewItemBox_KeyDown(object sender, KeyEventArgs e)\n" +
                                                                        "        {\n" +
                                                                        "            if (e.Key == Key.Enter)\n" +
                                                                        "            {\n" +
                                                                        "                AddItem_Click(sender, e);\n" +
                                                                        "                e.Handled = true;\n" +
                                                                        "            }\n" +
                                                                        "        }\n" +
                                                                        "\n" +
                                                                        "        private void RemoveItem_Click(object sender, RoutedEventArgs e)\n" +
                                                                        "        {\n" +
                                                                        "            if (BoundListView.SelectedItem is DataBindingSampleItem selected)\n" +
                                                                        "            {\n" +
                                                                        "                BoundListView.AnimateRemove(selected, null);\n" +
                                                                        "            }\n" +
                                                                        "        }\n" +
                                                                        "\n" +
                                                                        "        private void AddDemoItem(string name)\n" +
                                                                        "        {\n" +
                                                                        "            _items.Add(new DataBindingSampleItem\n" +
                                                                        "            {\n" +
                                                                        "                Name = name,\n" +
                                                                        "                AddedAt = DateTime.Now.ToString(\"HH:mm:ss\", CultureInfo.CurrentCulture)\n" +
                                                                        "            });\n" +
                                                                        "        }\n" +
                                                                        "\n" +
                                                                        "    }\n" +
                                                                        "\n" +
                                                                        "    public sealed class DataBindingSampleItem\n" +
                                                                        "    {\n" +
                                                                        "        public string Name { get; set; } = string.Empty;\n" +
                                                                        "\n" +
                                                                        "        public string AddedAt { get; set; } = string.Empty;\n" +
                                                                        "    }\n" +
                                                                        "}\n";
        private const string ListViewSelectionModeXamlSource = "<UserControl\n" +
                                                               "    x:Class=\"Fluence.Wpf.Demo.Pages.DataBinding.ListViewSelectionMode\"\n" +
                                                               "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                               "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                               "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                               "    <StackPanel>\n" +
                                                               "        <StackPanel Margin=\"0,0,0,12\" Orientation=\"Horizontal\">\n" +
                                                               "            <ui:RadioButton\n" +
                                                               "                x:Name=\"SingleModeRadio\"\n" +
                                                               "                Margin=\"0,0,16,0\"\n" +
                                                               "                Checked=\"SelectionMode_Changed\"\n" +
                                                               "                Content=\"Single\"\n" +
                                                               "                GroupName=\"SelectionModeGroup\"\n" +
                                                               "                IsChecked=\"True\" />\n" +
                                                               "            <ui:RadioButton\n" +
                                                               "                x:Name=\"MultipleModeRadio\"\n" +
                                                               "                Margin=\"0,0,16,0\"\n" +
                                                               "                Checked=\"SelectionMode_Changed\"\n" +
                                                               "                Content=\"Multiple\"\n" +
                                                               "                GroupName=\"SelectionModeGroup\" />\n" +
                                                               "            <ui:RadioButton\n" +
                                                               "                x:Name=\"ExtendedModeRadio\"\n" +
                                                               "                Checked=\"SelectionMode_Changed\"\n" +
                                                               "                Content=\"Extended (Shift+Click)\"\n" +
                                                               "                GroupName=\"SelectionModeGroup\" />\n" +
                                                               "        </StackPanel>\n" +
                                                               "        <ui:ListView\n" +
                                                               "            x:Name=\"SelectionModeListView\"\n" +
                                                               "            Height=\"200\"\n" +
                                                               "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                               "            BorderThickness=\"1\"\n" +
                                                               "            SelectionChanged=\"SelectionModeListView_SelectionChanged\"\n" +
                                                               "            SelectionMode=\"Single\">\n" +
                                                               "            <ListViewItem Content=\"Alpha\" />\n" +
                                                               "            <ListViewItem Content=\"Bravo\" />\n" +
                                                               "            <ListViewItem Content=\"Charlie\" />\n" +
                                                               "            <ListViewItem Content=\"Delta\" />\n" +
                                                               "            <ListViewItem Content=\"Echo\" />\n" +
                                                               "            <ListViewItem Content=\"Foxtrot\" />\n" +
                                                               "            <ListViewItem Content=\"Golf\" />\n" +
                                                               "            <ListViewItem Content=\"Hotel\" />\n" +
                                                               "        </ui:ListView>\n" +
                                                               "        <TextBlock\n" +
                                                               "            x:Name=\"SelectionCountLabel\"\n" +
                                                               "            Margin=\"0,8,0,0\"\n" +
                                                               "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                               "            Text=\"Selected: none\" />\n" +
                                                               "    </StackPanel>\n" +
                                                               "</UserControl>\n";

        private const string ListViewSelectionModeCSharpSource = "using System;\n" +
                                                                 "using System.Windows;\n" +
                                                                 "using System.Windows.Controls;\n" +
                                                                 "\n" +
                                                                 "namespace Fluence.Wpf.Demo.Pages.DataBinding\n" +
                                                                 "{\n" +
                                                                 "    public partial class ListViewSelectionMode : UserControl\n" +
                                                                 "    {\n" +
                                                                 "        public ListViewSelectionMode()\n" +
                                                                 "        {\n" +
                                                                 "            InitializeComponent();\n" +
                                                                 "        }\n" +
                                                                 "\n" +
                                                                 "        private void SelectionMode_Changed(object sender, RoutedEventArgs e)\n" +
                                                                 "        {\n" +
                                                                 "            if (SelectionModeListView is null)\n" +
                                                                 "            {\n" +
                                                                 "                return;\n" +
                                                                 "            }\n" +
                                                                 "\n" +
                                                                 "            if (MultipleModeRadio is not null && MultipleModeRadio.IsChecked == true)\n" +
                                                                 "            {\n" +
                                                                 "                SelectionModeListView.SelectionMode = SelectionMode.Multiple;\n" +
                                                                 "            }\n" +
                                                                 "            else if (ExtendedModeRadio is not null && ExtendedModeRadio.IsChecked == true)\n" +
                                                                 "            {\n" +
                                                                 "                SelectionModeListView.SelectionMode = SelectionMode.Extended;\n" +
                                                                 "            }\n" +
                                                                 "            else\n" +
                                                                 "            {\n" +
                                                                 "                SelectionModeListView.SelectionMode = SelectionMode.Single;\n" +
                                                                 "            }\n" +
                                                                 "\n" +
                                                                 "            SelectionModeListView.UnselectAll();\n" +
                                                                 "            UpdateSelectionLabel();\n" +
                                                                 "        }\n" +
                                                                 "\n" +
                                                                 "        private void SelectionModeListView_SelectionChanged(object sender, SelectionChangedEventArgs e)\n" +
                                                                 "        {\n" +
                                                                 "            UpdateSelectionLabel();\n" +
                                                                 "        }\n" +
                                                                 "\n" +
                                                                 "        private void UpdateSelectionLabel()\n" +
                                                                 "        {\n" +
                                                                 "            if (SelectionCountLabel is null || SelectionModeListView is null)\n" +
                                                                 "            {\n" +
                                                                 "                return;\n" +
                                                                 "            }\n" +
                                                                 "\n" +
                                                                 "            int count = SelectionModeListView.SelectedItems.Count;\n" +
                                                                 "            if (count == 0)\n" +
                                                                 "            {\n" +
                                                                 "                SelectionCountLabel.Text = \"Selected: none\";\n" +
                                                                 "                return;\n" +
                                                                 "            }\n" +
                                                                 "\n" +
                                                                 "            SelectionCountLabel.Text = count == 1\n" +
                                                                 "                ? string.Format(\"Selected: {0}\", (SelectionModeListView.SelectedItem as ListViewItem)?.Content ?? \"?\")\n" +
                                                                 "                : string.Format(\"Selected: {0} items\", count);\n" +
                                                                 "        }\n" +
                                                                 "    }\n" +
                                                                 "}\n";
        private const string DataTemplateRowXamlSource = "<UserControl\n" +
                                                         "    x:Class=\"Fluence.Wpf.Demo.Pages.DataBinding.DataTemplateRow\"\n" +
                                                         "    xmlns=\"http://schemas.microsoft.com/winfx/2006/xaml/presentation\"\n" +
                                                         "    xmlns:x=\"http://schemas.microsoft.com/winfx/2006/xaml\"\n" +
                                                         "    xmlns:ui=\"clr-namespace:Fluence.Wpf.Controls;assembly=Fluence.Wpf\">\n" +
                                                         "    <StackPanel>\n" +
                                                         "        <ui:ListView\n" +
                                                         "            x:Name=\"DataTemplateListView\"\n" +
                                                         "            Height=\"180\"\n" +
                                                         "            BorderBrush=\"{DynamicResource CardStrokeColorDefaultBrush}\"\n" +
                                                         "            BorderThickness=\"1\"\n" +
                                                         "            SelectionMode=\"Single\">\n" +
                                                         "            <ui:ListView.ItemTemplate>\n" +
                                                         "                <DataTemplate>\n" +
                                                         "                    <Grid Margin=\"0,2\">\n" +
                                                         "                        <Grid.ColumnDefinitions>\n" +
                                                         "                            <ColumnDefinition Width=\"Auto\" />\n" +
                                                         "                            <ColumnDefinition Width=\"12\" />\n" +
                                                         "                            <ColumnDefinition Width=\"*\" />\n" +
                                                         "                        </Grid.ColumnDefinitions>\n" +
                                                         "                        <ui:FontIcon\n" +
                                                         "                            VerticalAlignment=\"Center\"\n" +
                                                         "                            Foreground=\"{DynamicResource AccentTextFillColorPrimaryBrush}\"\n" +
                                                         "                            Glyph=\"&#xE8A5;\"\n" +
                                                         "                            IconFontSize=\"16\" />\n" +
                                                         "                        <StackPanel\n" +
                                                         "                            Grid.Column=\"2\"\n" +
                                                         "                            VerticalAlignment=\"Center\"\n" +
                                                         "                            Orientation=\"Horizontal\">\n" +
                                                         "                            <TextBlock\n" +
                                                         "                                VerticalAlignment=\"Center\"\n" +
                                                         "                                Foreground=\"{DynamicResource TextFillColorPrimaryBrush}\"\n" +
                                                         "                                Text=\"{Binding Name}\" />\n" +
                                                         "                            <TextBlock\n" +
                                                         "                                Margin=\"8,0,0,0\"\n" +
                                                         "                                VerticalAlignment=\"Center\"\n" +
                                                         "                                FontSize=\"12\"\n" +
                                                         "                                Foreground=\"{DynamicResource TextFillColorTertiaryBrush}\"\n" +
                                                         "                                Text=\"{Binding AddedAt}\" />\n" +
                                                         "                        </StackPanel>\n" +
                                                         "                    </Grid>\n" +
                                                         "                </DataTemplate>\n" +
                                                         "            </ui:ListView.ItemTemplate>\n" +
                                                         "        </ui:ListView>\n" +
                                                         "        <TextBlock\n" +
                                                         "            Margin=\"0,8,0,0\"\n" +
                                                         "            Foreground=\"{DynamicResource TextFillColorSecondaryBrush}\"\n" +
                                                         "            Text=\"Name and AddedAt are simple properties on each bound item.\" />\n" +
                                                         "    </StackPanel>\n" +
                                                         "</UserControl>\n";

        private const string DataTemplateRowCSharpSource = "using System;\n" +
                                                           "using System.Collections.ObjectModel;\n" +
                                                           "using System.Windows.Controls;\n" +
                                                           "\n" +
                                                           "namespace Fluence.Wpf.Demo.Pages.DataBinding\n" +
                                                           "{\n" +
                                                           "    public partial class DataTemplateRow : UserControl\n" +
                                                           "    {\n" +
                                                           "        public DataTemplateRow()\n" +
                                                           "        {\n" +
                                                           "            InitializeComponent();\n" +
                                                           "\n" +
                                                           "            DataTemplateListView.ItemsSource = new ObservableCollection<DataBindingTemplateItem>\n" +
                                                           "            {\n" +
                                                           "                new DataBindingTemplateItem { Name = \"Release notes\", AddedAt = DateTime.Now.ToString(\"HH:mm:ss\") },\n" +
                                                           "                new DataBindingTemplateItem { Name = \"Design tokens\", AddedAt = DateTime.Now.ToString(\"HH:mm:ss\") },\n" +
                                                           "                new DataBindingTemplateItem { Name = \"Control states\", AddedAt = DateTime.Now.ToString(\"HH:mm:ss\") }\n" +
                                                           "            };\n" +
                                                           "        }\n" +
                                                           "    }\n" +
                                                           "\n" +
                                                           "    public sealed class DataBindingTemplateItem\n" +
                                                           "    {\n" +
                                                           "        public string Name { get; set; } = string.Empty;\n" +
                                                           "\n" +
                                                           "        public string AddedAt { get; set; } = string.Empty;\n" +
                                                           "    }\n" +
                                                           "}\n";

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
            if (e.Key is Key.Enter)
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
                BoundListView.AnimateRemove(selected, onCompleted: null);
            }
        }

        private void AddDemoItem(string name)
        {
            _items.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
            });
        }

        private void AddDataTemplateItem(string name)
        {
            _templateItems.Add(new DemoItem
            {
                Name = name,
                AddedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.CurrentCulture),
            });
        }

        private void SelectionMode_Changed(object sender, RoutedEventArgs e)
        {
            if (SelectionModeListView is null)
            {
                return;
            }

            SelectionModeListView.SelectionMode = (MultipleModeRadio?.IsChecked) is true
                ? SelectionMode.Multiple
                : (ExtendedModeRadio?.IsChecked) is true ? SelectionMode.Extended : SelectionMode.Single;

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
            if (count is 0)
            {
                SelectionCountLabel.Text = "Selected: none";
                return;
            }

            SelectionCountLabel.Text = count is 1
                ? string.Format(CultureInfo.CurrentCulture, "Selected: {0}", (SelectionModeListView.SelectedItem as ListViewItem)?.Content ?? "?")
                : string.Format(CultureInfo.CurrentCulture, "Selected: {0} items", count);
        }
    }
}
