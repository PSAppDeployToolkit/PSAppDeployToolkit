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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading;

namespace Fluence.Wpf.Demo.Pages
{
    public partial class GalleryDataGridPage : Page
    {
        private static readonly string BasicXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.DataGrid.BasicDataGrid",
            "    <fluence:DataGrid\n" +
            "        x:Name=\"BasicGrid\"\n" +
            "        Height=\"320\"\n" +
            "        AutoGenerateColumns=\"False\"\n" +
            "        FrozenColumnCount=\"1\"\n" +
            "        IsReadOnly=\"True\">\n" +
            "        <fluence:DataGrid.Columns>\n" +
            "            <DataGridTextColumn Width=\"160\" Binding=\"{Binding Name}\" Header=\"Product\" />\n" +
            "            <DataGridTextColumn Width=\"140\" Binding=\"{Binding Category}\" Header=\"Category\" />\n" +
            "            <DataGridTextColumn Width=\"140\" Binding=\"{Binding Supplier}\" Header=\"Supplier\" />\n" +
            "            <DataGridTextColumn Width=\"100\" Binding=\"{Binding Quantity}\" Header=\"Quantity\" />\n" +
            "            <DataGridCheckBoxColumn Width=\"100\" Binding=\"{Binding InStock}\" Header=\"In stock\" />\n" +
            "        </fluence:DataGrid.Columns>\n" +
            "    </fluence:DataGrid>\n");

        private const string BasicCSharpSource = "using System.Windows.Controls;\n" +
                                                 "\n" +
                                                 "namespace Fluence.Wpf.Demo.Pages.DataGrid\n" +
                                                 "{\n" +
                                                 "    public partial class BasicDataGrid : UserControl\n" +
                                                 "    {\n" +
                                                 "        public BasicDataGrid()\n" +
                                                 "        {\n" +
                                                 "            InitializeComponent();\n" +
                                                 "\n" +
                                                 "            // DataGridSampleItem is the gallery's product row model.\n" +
                                                 "            BasicGrid.ItemsSource = DataGridSampleItem.CreateCatalog(1000);\n" +
                                                 "\n" +
                                                 "            // IsItemSelectable = false is the WinUI SelectionMode=\"None\" case.\n" +
                                                 "            BasicGrid.IsItemSelectable = true;\n" +
                                                 "            BasicGrid.GridLinesVisibility = DataGridGridLinesVisibility.Horizontal;\n" +
                                                 "        }\n" +
                                                 "    }\n" +
                                                 "}\n";

        private static readonly string GroupedXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.DataGrid.GroupedDataGrid",
            "    <fluence:DataGrid\n" +
            "        x:Name=\"GroupedGrid\"\n" +
            "        Height=\"320\"\n" +
            "        AutoGenerateColumns=\"False\"\n" +
            "        IsReadOnly=\"True\">\n" +
            "        <fluence:DataGrid.Columns>\n" +
            "            <DataGridTextColumn Width=\"180\" Binding=\"{Binding Name}\" Header=\"Product\" />\n" +
            "            <DataGridTextColumn Width=\"140\" Binding=\"{Binding Supplier}\" Header=\"Supplier\" />\n" +
            "            <DataGridTextColumn Width=\"100\" Binding=\"{Binding Quantity}\" Header=\"Quantity\" />\n" +
            "        </fluence:DataGrid.Columns>\n" +
            "    </fluence:DataGrid>\n");

        private const string GroupedCSharpSource = "using System.Globalization;\n" +
                                                   "using System.Windows.Controls;\n" +
                                                   "using System.Windows.Data;\n" +
                                                   "\n" +
                                                   "namespace Fluence.Wpf.Demo.Pages.DataGrid\n" +
                                                   "{\n" +
                                                   "    public partial class GroupedDataGrid : UserControl\n" +
                                                   "    {\n" +
                                                   "        private readonly ListCollectionView _view;\n" +
                                                   "\n" +
                                                   "        public GroupedDataGrid()\n" +
                                                   "        {\n" +
                                                   "            InitializeComponent();\n" +
                                                   "\n" +
                                                   "            // DataGridSampleItem is the gallery's product row model.\n" +
                                                   "            _view = new ListCollectionView(DataGridSampleItem.CreateCatalog(40));\n" +
                                                   "            _view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DataGridSampleItem.Category)));\n" +
                                                   "            GroupedGrid.ItemsSource = _view;\n" +
                                                   "        }\n" +
                                                   "\n" +
                                                   "        // Call this as the user types in a filter box.\n" +
                                                   "        public void ApplyFilter(string filterText)\n" +
                                                   "        {\n" +
                                                   "            _view.Filter = filterText.Length is 0\n" +
                                                   "                ? null\n" +
                                                   "                : item => item is DataGridSampleItem product\n" +
                                                   "                    && CultureInfo.InvariantCulture.CompareInfo.IndexOf(product.Name, filterText, CompareOptions.IgnoreCase) >= 0;\n" +
                                                   "        }\n" +
                                                   "\n" +
                                                   "        public void ExpandAll()\n" +
                                                   "        {\n" +
                                                   "            GroupedGrid.ExpandAllGroups();\n" +
                                                   "        }\n" +
                                                   "\n" +
                                                   "        public void CollapseAll()\n" +
                                                   "        {\n" +
                                                   "            GroupedGrid.CollapseAllGroups();\n" +
                                                   "        }\n" +
                                                   "    }\n" +
                                                   "}\n";

        private static readonly string EditableXamlSource = DemoSampleXaml.UserControl(
            "Fluence.Wpf.Demo.Pages.DataGrid.EditableDataGrid",
            "    <fluence:DataGrid\n" +
            "        x:Name=\"EditableGrid\"\n" +
            "        AutoGenerateColumns=\"False\"\n" +
            "        CanUserAddRows=\"False\">\n" +
            "        <fluence:DataGrid.EmptyContent>\n" +
            "            <TextBlock Text=\"No products.\" />\n" +
            "        </fluence:DataGrid.EmptyContent>\n" +
            "        <fluence:DataGrid.Columns>\n" +
            "            <DataGridTextColumn Binding=\"{Binding Name}\" Header=\"Product\" />\n" +
            "            <DataGridTextColumn Binding=\"{Binding Quantity}\" Header=\"Quantity\" />\n" +
            "            <DataGridCheckBoxColumn Binding=\"{Binding InStock}\" Header=\"In stock\" />\n" +
            "            <DataGridTemplateColumn Header=\"Rating\" SortMemberPath=\"Rating\">\n" +
            "                <DataGridTemplateColumn.CellTemplate>\n" +
            "                    <DataTemplate>\n" +
            "                        <fluence:RatingControl IsReadOnly=\"True\" Value=\"{Binding Rating}\" />\n" +
            "                    </DataTemplate>\n" +
            "                </DataGridTemplateColumn.CellTemplate>\n" +
            "            </DataGridTemplateColumn>\n" +
            "            <DataGridTemplateColumn Header=\"Fulfilment\" SortMemberPath=\"Progress\">\n" +
            "                <DataGridTemplateColumn.CellTemplate>\n" +
            "                    <DataTemplate>\n" +
            "                        <fluence:ProgressBar Maximum=\"100\" Value=\"{Binding Progress}\" />\n" +
            "                    </DataTemplate>\n" +
            "                </DataGridTemplateColumn.CellTemplate>\n" +
            "            </DataGridTemplateColumn>\n" +
            "        </fluence:DataGrid.Columns>\n" +
            "    </fluence:DataGrid>\n");

        private const string EditableCSharpSource = "using System.Collections.ObjectModel;\n" +
                                                    "using System.Windows.Controls;\n" +
                                                    "\n" +
                                                    "namespace Fluence.Wpf.Demo.Pages.DataGrid\n" +
                                                    "{\n" +
                                                    "    public partial class EditableDataGrid : UserControl\n" +
                                                    "    {\n" +
                                                    "        private readonly ObservableCollection<DataGridSampleItem> _rows = DataGridSampleItem.CreateCatalog(8);\n" +
                                                    "\n" +
                                                    "        public EditableDataGrid()\n" +
                                                    "        {\n" +
                                                    "            InitializeComponent();\n" +
                                                    "\n" +
                                                    "            // DataGridSampleItem is the gallery's product row model. It implements\n" +
                                                    "            // INotifyDataErrorInfo, so invalid edits show the Fluent error outline and tooltip.\n" +
                                                    "            EditableGrid.ItemsSource = _rows;\n" +
                                                    "        }\n" +
                                                    "\n" +
                                                    "        // EmptyContent shows when the rows are cleared.\n" +
                                                    "        public void ClearRows()\n" +
                                                    "        {\n" +
                                                    "            _rows.Clear();\n" +
                                                    "        }\n" +
                                                    "    }\n" +
                                                    "}\n";

        private readonly ListCollectionView _groupedView;
        private readonly ObservableCollection<DataGridSampleItem> _editableRows = [];
        private string? _rowEditColumn;

        public GalleryDataGridPage()
        {
            InitializeComponent();

            BasicDataGrid.ItemsSource = DataGridSampleItem.CreateCatalog(1000);

            _groupedView = new ListCollectionView(DataGridSampleItem.CreateCatalog(40));
            _groupedView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(DataGridSampleItem.Category)));
            GroupedDataGrid.ItemsSource = _groupedView;

            RestoreEditableRows();
            EditableDataGrid.ItemsSource = _editableRows;

            DemoSamplePageWiring.Apply(
                (DependencyObject)Content,
                new DemoSampleSource(1, BasicXamlSource, BasicCSharpSource),
                new DemoSampleSource(2, GroupedXamlSource, GroupedCSharpSource),
                new DemoSampleSource(3, EditableXamlSource, EditableCSharpSource));
        }

        private void GroupFilterTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            string filter = GroupFilterTextBox.Text.Trim();
            _groupedView.Filter = filter.Length is 0
                ? null
                : item => item is DataGridSampleItem product
                    && CultureInfo.InvariantCulture.CompareInfo.IndexOf(product.Name, filter, CompareOptions.IgnoreCase) >= 0;
        }

        private void ExpandAllGroups_Click(object sender, RoutedEventArgs e)
        {
            GroupedDataGrid.ExpandAllGroups();
        }

        private void CollapseAllGroups_Click(object sender, RoutedEventArgs e)
        {
            GroupedDataGrid.CollapseAllGroups();
        }

        private void EditableDataGrid_CellEditEnding(object sender, DataGridCellEditEndingEventArgs e)
        {
            if (e.EditAction is not DataGridEditAction.Commit)
            {
                EditResultText.Text = "Edit cancelled.";
                return;
            }

            // The ending events are raised before the commit, so the item still holds the old value and
            // validation has not run yet. Look again once the commit has settled.
            string column = e.Column.Header as string ?? string.Empty;
            DataGridCell? cell = e.EditingElement.Parent as DataGridCell;
            object row = e.Row.Item;
            _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => OnCellCommitSettled(cell, column, row)));
        }

        private void EditableDataGrid_RowEditEnding(object sender, DataGridRowEditEndingEventArgs e)
        {
            if (e.EditAction is DataGridEditAction.Commit)
            {
                object row = e.Row.Item;
                _ = Dispatcher.BeginInvoke(DispatcherPriority.Background, new Action(() => ReportEditResult(row)));
            }
            else
            {
                _rowEditColumn = null;
                EditResultText.Text = "Edit cancelled.";
            }
        }

        private void OnCellCommitSettled(DataGridCell? cell, string column, object row)
        {
            // A cell that is still editing failed to commit (for example "abc" in Quantity), so it is
            // not an edit. The column named is the first one committed since the row started editing.
            if (cell?.IsEditing is true)
            {
                return;
            }

            _rowEditColumn ??= column;
            ReportEditResult(row);
        }

        private void ReportEditResult(object row)
        {
            // Nothing recorded means no cell committed in this row session, for example Escape on the
            // only edit followed by leaving the row. A cell committed with its row still pending (for
            // example after Tab) leaves the source unchanged until the row commits, which reports it.
            if (_rowEditColumn is not string column
                || row is not DataGridSampleItem item
                || EditableDataGrid.ItemContainerGenerator.ContainerFromItem(item) is DataGridRow { IsEditing: true })
            {
                return;
            }

            _rowEditColumn = null;
            string? error = item.GetErrors(nameof(DataGridSampleItem.Name)).Cast<string>()
                .Concat(item.GetErrors(nameof(DataGridSampleItem.Quantity)).Cast<string>())
                .FirstOrDefault();
            EditResultText.Text = error is null
                ? "Edited " + column + " on " + item.Name + "."
                : "Validation error: " + error;
        }

        private void ClearRows_Click(object sender, RoutedEventArgs e)
        {
            _editableRows.Clear();
        }

        private void RestoreRows_Click(object sender, RoutedEventArgs e)
        {
            RestoreEditableRows();
        }

        private void RestoreEditableRows()
        {
            _editableRows.Clear();
            foreach (DataGridSampleItem item in DataGridSampleItem.CreateCatalog(8))
            {
                _editableRows.Add(item);
            }
        }
    }
}
