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
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.BrushAssert;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Covers <see cref="Controls.DataGrid"/>, the Fluent table that maps the WinUI TableView
    /// scenarios onto the WPF DataGrid.
    /// Authority: in-tree ListView item recipe; native WPF DataGrid part contract; .NET WPF Fluent DataGrid theme.
    /// </summary>
    public sealed class DataGridTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task DataGrid_DefaultStyle_AppliesFluentContainerStylesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                _ = Assert.IsType<Style>(application.TryFindResource(typeof(Controls.DataGrid)));

                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    Assert.Same(application.TryFindResource("DataGridRowStyle"), grid.RowStyle);
                    Assert.Same(application.TryFindResource("DataGridCellStyle"), grid.CellStyle);
                    Assert.Same(application.TryFindResource("DataGridColumnHeaderStyle"), grid.ColumnHeaderStyle);
                    Assert.Same(application.TryFindResource("DataGridRowHeaderStyle"), grid.RowHeaderStyle);
                    Assert.Equal(DataGridHeadersVisibility.Column, grid.HeadersVisibility);
                    Assert.Equal(DataGridGridLinesVisibility.Horizontal, grid.GridLinesVisibility);
                    Assert.Equal(32.0, grid.MinRowHeight, 0.001);
                    AssertBrushColor(grid.HorizontalGridLinesBrush, "DividerStrokeColorDefaultBrush");
                    Assert.True(grid.IsItemSelectable);
                    Assert.Null(grid.EmptyContent);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_Template_KeepsNativeScrollAndHeaderPartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    _ = Assert.IsType<ScrollViewer>(FindVisualChildByName<ScrollViewer>(grid, "DG_ScrollViewer"), exactMatch: false);
                    _ = Assert.IsType<DataGridColumnHeadersPresenter>(FindVisualChildByName<DataGridColumnHeadersPresenter>(grid, "PART_ColumnHeadersPresenter"));
                    _ = Assert.IsType<DataGridColumnHeader>(FindVisualChildByName<DataGridColumnHeader>(grid, "PART_FillerColumnHeader"));

                    ScrollBar vertical = Assert.IsType<ScrollBar>(FindVisualChildByName<ScrollBar>(grid, "PART_VerticalScrollBar"));
                    ScrollBar horizontal = Assert.IsType<ScrollBar>(FindVisualChildByName<ScrollBar>(grid, "PART_HorizontalScrollBar"));
                    Assert.Same(application.TryFindResource("VerticalScrollBarStyle"), vertical.Style);
                    Assert.Same(application.TryFindResource("HorizontalScrollBarStyle"), horizontal.Style);

                    Border outer = Assert.IsType<Border>(FindVisualChildByName<Border>(grid, "OuterBorder"));
                    Assert.Equal(new CornerRadius(4), outer.CornerRadius);
                    AssertBrushColor(outer.BorderBrush, "CardStrokeColorDefaultBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_AutomationPeer_ReportsDataGridAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(1));

                AutomationPeer peer = Assert.IsType<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(grid), exactMatch: false);

                Assert.Equal("DataGrid", peer.GetClassName(), StringComparer.Ordinal);
                Assert.Equal(AutomationControlType.DataGrid, peer.GetAutomationControlType());
            });
        }

        [Fact]
        public Task DataGrid_TenThousandRows_RealizesOnlyVisibleRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(10000));
                Window window = ShowInWindow(grid);

                try
                {
                    int realized = FindVisualChildren<DataGridRow>(grid).Count();
                    Assert.InRange(realized, 1, 40);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_SortDirection_ShowsMatchingSortGlyphAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridColumn column = grid.Columns[0];
                    TextBlock glyph = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(HeaderFor(grid, column), "SortGlyph"));
                    Assert.Equal(Visibility.Hidden, glyph.Visibility);

                    column.SortDirection = ListSortDirection.Ascending;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, glyph.Visibility);
                    Assert.Equal("\uF090", glyph.Text, StringComparer.Ordinal);

                    column.SortDirection = ListSortDirection.Descending;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("\uF08E", glyph.Text, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_FrozenColumnCount_KeepsLeadingHeaderInPlaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(5));
                grid.Width = 300;
                grid.FrozenColumnCount = 1;
                foreach (DataGridColumn column in grid.Columns)
                {
                    column.Width = new DataGridLength(200);
                }

                Window window = ShowInWindow(grid);

                try
                {
                    DataGridColumnHeader frozen = HeaderFor(grid, grid.Columns[0]);
                    DataGridColumnHeader scrolling = HeaderFor(grid, grid.Columns[1]);
                    double frozenBefore = frozen.TranslatePoint(new Point(0, 0), grid).X;
                    double scrollingBefore = scrolling.TranslatePoint(new Point(0, 0), grid).X;

                    ScrollViewer scroll = Assert.IsType<ScrollViewer>(FindVisualChildByName<ScrollViewer>(grid, "DG_ScrollViewer"), exactMatch: false);
                    scroll.ScrollToHorizontalOffset(150);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(frozenBefore, frozen.TranslatePoint(new Point(0, 0), grid).X, 0.5);
                    Assert.True(scrolling.TranslatePoint(new Point(0, 0), grid).X < scrollingBefore - 100, "Unfrozen headers must scroll.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_SelectedRow_ShowsOverlayAndSelectionPillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    grid.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    DataGridRow row = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
                    Border pill = Assert.IsType<Border>(FindVisualChildByName<Border>(row, "SelectionIndicator"));
                    Border overlay = Assert.IsType<Border>(FindVisualChildByName<Border>(row, "SelectedOverlay"));

                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => pill.Opacity > 0.99).ConfigureAwait(true),
                        "The selection pill must fade in for the selected row.");
                    AssertBrushColor(overlay.Background, "SubtleFillColorSecondaryBrush");
                    AssertBrushColor(pill.Background, "NavigationViewSelectionIndicatorForeground");

                    // Regression: the native Aero2 cell triggers turned selected text white. The cell
                    // style overrides the default style, so the cell keeps the primary text color.
                    DataGridCell cell = Assert.IsType<DataGridCell>(FindVisualChild<DataGridCell>(row));
                    Assert.True(cell.IsSelected, "Row selection must select the cells of the row.");
                    AssertBrushColor(cell.Foreground, "TextFillColorPrimaryBrush");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_IsItemSelectableFalse_ClearsAndBlocksSelectionAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    grid.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(grid.SelectedItem);

                    grid.IsItemSelectable = false;
                    Assert.Null(grid.SelectedItem);

                    grid.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Null(grid.SelectedItem);
                    Assert.Empty(grid.SelectedCells);

                    grid.IsItemSelectable = true;
                    grid.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.NotNull(grid.SelectedItem);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_AlternatingRowBackground_PaintsOddRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                SolidColorBrush alternate = new(Colors.Gainsboro);
                Controls.DataGrid grid = CreateGrid(CreateRows(4));
                grid.AlternatingRowBackground = alternate;
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridRow even = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
                    DataGridRow odd = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(1));

                    Assert.Same(grid.RowBackground, Assert.IsType<Border>(FindVisualChildByName<Border>(even, "DGR_Border")).Background);
                    Assert.Same(alternate, Assert.IsType<Border>(FindVisualChildByName<Border>(odd, "DGR_Border")).Background);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_EmptyContent_VisibleOnlyWithoutRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ObservableCollection<SampleRow> rows = new(CreateRows(2));
                Controls.DataGrid grid = CreateGrid(rows);
                grid.CanUserAddRows = false;
                grid.EmptyContent = "No rows";
                Window window = ShowInWindow(grid);

                try
                {
                    ContentPresenter empty = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(grid, "EmptyContentPresenter"));
                    Assert.Equal(Visibility.Collapsed, empty.Visibility);

                    rows.Clear();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, empty.Visibility);

                    grid.EmptyContent = null;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, empty.Visibility);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_DeclaredColumns_ReceiveFluenceStylesUnlessSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(2);
                Controls.DataGrid grid = CreateGrid(rows);
                Style custom = new(typeof(TextBlock));
                DataGridTextColumn explicitColumn = new()
                {
                    Header = "Custom",
                    Binding = new Binding(nameof(SampleRow.Name)),
                    ElementStyle = custom,
                };
                grid.Columns.Add(explicitColumn);
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridTextColumn name = Assert.IsType<DataGridTextColumn>(grid.Columns[0]);
                    DataGridCheckBoxColumn stock = Assert.IsType<DataGridCheckBoxColumn>(grid.Columns[2]);

                    Assert.Same(application.TryFindResource("DataGridTextColumnElementStyle"), name.ElementStyle);
                    Assert.Same(application.TryFindResource("DataGridTextColumnEditingElementStyle"), name.EditingElementStyle);
                    Assert.Same(application.TryFindResource("DataGridCheckBoxColumnElementStyle"), stock.ElementStyle);
                    Assert.Same(application.TryFindResource("DataGridCheckBoxColumnEditingElementStyle"), stock.EditingElementStyle);
                    Assert.Same(custom, explicitColumn.ElementStyle);

                    TextBlock text = Assert.IsType<TextBlock>(name.GetCellContent(rows[0]));
                    Assert.Same(name.ElementStyle, text.Style);
                    CheckBox check = Assert.IsType<CheckBox>(stock.GetCellContent(rows[0]));
                    Assert.False(check.IsHitTestVisible, "The display check box keeps the native non-interactive default.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_AutoGeneratedColumns_ReceiveFluenceStylesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Controls.DataGrid grid = new()
                {
                    AutoGenerateColumns = true,
                    Width = 480,
                    Height = 320,
                    ItemsSource = CreateRows(3),
                };
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridCheckBoxColumn stock = Assert.Single(grid.Columns.OfType<DataGridCheckBoxColumn>());
                    Assert.Same(application.TryFindResource("DataGridCheckBoxColumnElementStyle"), stock.ElementStyle);

                    object? textStyle = application.TryFindResource("DataGridTextColumnElementStyle");
                    List<DataGridTextColumn> textColumns = [.. grid.Columns.OfType<DataGridTextColumn>()];
                    Assert.Equal(3, textColumns.Count);
                    Assert.All(textColumns, column => Assert.Same(textStyle, column.ElementStyle));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_BeginEdit_UsesFluenceEditorAndCancelRestoresValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(3);
                Controls.DataGrid grid = CreateGrid(rows);
                Window window = ShowInWindow(grid);

                try
                {
                    grid.CurrentCell = new DataGridCellInfo(rows[0], grid.Columns[0]);
                    Assert.True(grid.BeginEdit(), "BeginEdit should start editing the current cell.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TextBox editor = Assert.IsType<TextBox>(grid.Columns[0].GetCellContent(rows[0]));
                    Assert.Same(application.TryFindResource("DataGridTextColumnEditingElementStyle"), editor.Style);

                    editor.Text = "Changed";
                    Assert.True(grid.CancelEdit(DataGridEditingUnit.Row));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal("Item 0", rows[0].Name, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_NotifyDataErrorInfo_ShowsFluentErrorTemplateAndToolTipAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ValidatedRow invalid = new() { Name = string.Empty };
                Controls.DataGrid grid = new()
                {
                    AutoGenerateColumns = false,
                    Width = 480,
                    Height = 320,
                    ItemsSource = new[] { invalid, new ValidatedRow { Name = "Valid" } },
                };
                grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(ValidatedRow.Name)) });
                Window window = ShowInWindow(grid);

                try
                {
                    TextBlock text = Assert.IsType<TextBlock>(grid.Columns[0].GetCellContent(invalid));
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => Validation.GetHasError(text)).ConfigureAwait(true),
                        "The cell text must report the INotifyDataErrorInfo error.");
                    Assert.Same(application.TryFindResource("DataGridValidationErrorTemplate"), Validation.GetErrorTemplate(text));
                    Assert.Equal(ValidatedRow.NameRequired, text.ToolTip);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_Grouping_AddsDefaultGroupStyleAndTogglesAllGroupsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ListCollectionView view = new(CreateRows(6));
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleRow.Category)));
                Controls.DataGrid grid = CreateGrid(view);
                Window window = ShowInWindow(grid);

                try
                {
                    GroupStyle groupStyle = Assert.Single(grid.GroupStyle);
                    Assert.Same(application.TryFindResource("DataGridGroupItemStyle"), groupStyle.ContainerStyle);
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChildren<GroupItem>(grid).Take(3).Count() is 2).ConfigureAwait(true),
                        "Two category groups should be realized.");

                    grid.CollapseAllGroups();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.All(GroupExpanders(grid), static expander => Assert.False(expander.IsExpanded));

                    grid.ExpandAllGroups();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.All(GroupExpanders(grid), static expander => Assert.True(expander.IsExpanded));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_GroupedRows_InheritGridForegroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                List<SampleRow> rows = CreateRows(6);
                ListCollectionView view = new(rows);
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleRow.Category)));
                Controls.DataGrid grid = CreateGrid(view);
                Window window = ShowInWindow(grid);

                try
                {
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => grid.Columns[0].GetCellContent(rows[0]) is TextBlock).ConfigureAwait(true),
                        "The first grouped row should be realized.");

                    // The group Expander is a native Expander; its Aero2 theme style sets Foreground to
                    // ControlText, which grouped rows inherited instead of the grid's Fluent text color.
                    Color primary = ResolvedColor(application, "TextFillColorPrimaryBrush");
                    Assert.Equal(primary, SolidColor(Assert.Single(GroupExpanders(grid).Take(1)).Foreground));
                    TextBlock text = Assert.IsType<TextBlock>(grid.Columns[0].GetCellContent(rows[0]), exactMatch: false);
                    Assert.Equal(primary, SolidColor(text.Foreground));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_GroupHeader_ReportsItsSizeToGroupVirtualizationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                ListCollectionView view = new(CreateRows(6));
                view.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleRow.Category)));
                Controls.DataGrid grid = CreateGrid(view);
                VirtualizingPanel.SetIsVirtualizingWhenGrouping(grid, value: true);
                Window window = ShowInWindow(grid);

                try
                {
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChildren<GroupItem>(grid).Any()).ConfigureAwait(true),
                        "A category group should be realized.");

                    // Virtualized grouping measures each group's header through the Expander template part
                    // named HeaderSite; without it the header counts as zero height in the scroll extent.
                    GroupItem groupItem = FindVisualChildren<GroupItem>(grid).First();
                    Size header = ((IHierarchicalVirtualizationAndScrollInfo)groupItem).HeaderDesiredSizes.PixelSize;
                    Assert.True(header.Height >= 32, "The group header must report its 32 DIP height to group virtualization.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_GroupCommands_LeaveNestedGridGroupsAloneAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                ListCollectionView innerView = new(CreateRows(6));
                innerView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleRow.Category)));
                FrameworkElementFactory inner = new(typeof(Controls.DataGrid));
                inner.SetValue(ItemsControl.ItemsSourceProperty, innerView);
                inner.SetValue(FrameworkElement.HeightProperty, 160d);

                ListCollectionView outerView = new(CreateRows(3));
                outerView.GroupDescriptions.Add(new PropertyGroupDescription(nameof(SampleRow.Category)));
                Controls.DataGrid outer = CreateGrid(outerView);
                outer.RowDetailsVisibilityMode = DataGridRowDetailsVisibilityMode.Visible;
                outer.RowDetailsTemplate = new DataTemplate { VisualTree = inner };
                Window window = ShowInWindow(outer);

                try
                {
                    List<Controls.DataGrid> nested = [];
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () =>
                        {
                            nested = [.. FindVisualChildren<Controls.DataGrid>(outer).Where(grid => !ReferenceEquals(grid, outer))];
                            return nested.Count is 3 && nested.TrueForAll(static grid => FindVisualChildren<GroupItem>(grid).Any());
                        }).ConfigureAwait(true),
                        "Each of the three rows should realize a grouped grid in its details.");

                    // The grid's commands act on its own groups only, never on a grid nested in its rows.
                    List<Expander> nestedExpanders = [.. nested.SelectMany(static grid => GroupExpanders(grid))];
                    outer.CollapseAllGroups();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.All(GroupExpanders(outer).Except(nestedExpanders), static expander => Assert.False(expander.IsExpanded));
                    Assert.All(nestedExpanders, static expander => Assert.True(expander.IsExpanded));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_ThemeCycle_ReresolvesGridLineAndHeaderBrushesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                Window window = ShowInWindow(grid);

                try
                {
                    ThemeTestHelpers.ApplyStandardThemeCycle();
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);

                    DataGridColumnHeader header = HeaderFor(grid, grid.Columns[0]);
                    Assert.True(
                        await WaitUntilAsync(
                            window.Dispatcher,
                            2000,
                            () => SolidColor(grid.HorizontalGridLinesBrush) == ResolvedColor(application, "DividerStrokeColorDefaultBrush")
                                && SolidColor(header.Foreground) == ResolvedColor(application, "TextFillColorPrimaryBrush")).ConfigureAwait(true),
                        "Grid lines and header text must follow the theme after the cycle ends in Dark.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_HighContrastSelectedCell_UsesHighlightFillAndTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(3);
                Controls.DataGrid grid = CreateGrid(rows);
                grid.SelectionUnit = DataGridSelectionUnit.Cell;
                Window window = ShowInWindow(grid);

                try
                {
                    // Row containers are regenerated when the theme changes, so apply High Contrast first.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    grid.SelectedCells.Add(new DataGridCellInfo(rows[0], grid.Columns[0]));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    DataGridCell selected = CellFor(grid, rows[0], grid.Columns[0]);
                    DataGridCell neighbor = CellFor(grid, rows[0], grid.Columns[1]);
                    Assert.True(selected.IsSelected, "The added cell must be selected.");
                    Assert.False(neighbor.IsSelected);

                    // The old subtle fill equals the grid background in High Contrast, so the selection was invisible.
                    Assert.Equal(SystemColors.HighlightColor, SolidColor(selected.Background));
                    Assert.NotEqual(SolidColor(grid.Background), SolidColor(selected.Background));
                    Assert.Equal(SystemColors.HighlightTextColor, SolidColor(selected.Foreground));

                    // Row and header hover use this fill; on the old card background both were SystemColors.Control
                    // in High Contrast, so hover was invisible.
                    Assert.NotEqual(ResolvedColor(application, "SubtleFillColorSecondaryBrush"), SolidColor(grid.Background));

                    // The display text follows the cell Foreground instead of setting its own.
                    TextBlock text = Assert.IsType<TextBlock>(grid.Columns[0].GetCellContent(rows[0]), exactMatch: false);
                    Assert.Equal(SystemColors.HighlightTextColor, SolidColor(text.Foreground));
                    TextBlock neighborText = Assert.IsType<TextBlock>(grid.Columns[1].GetCellContent(rows[0]), exactMatch: false);
                    Assert.NotEqual(SystemColors.HighlightTextColor, SolidColor(neighborText.Foreground));
                    Assert.NotEqual(SystemColors.HighlightColor, SolidColor(neighbor.Background));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_Disabled_DimsRowsAndUsesDisabledSelectionPillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Controls.DataGrid grid = CreateGrid(CreateRows(3));
                grid.IsEnabled = false;
                Window window = ShowInWindow(grid);

                try
                {
                    grid.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    DataGridRow selected = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
                    DataGridRow other = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(1));
                    Assert.False(selected.IsEnabled, "A disabled grid disables its rows.");
                    Assert.True(selected.IsSelected, "Programmatic selection still selects the row.");
                    Assert.Equal(0.3, selected.Opacity, 0.001);
                    Assert.Equal(0.3, other.Opacity, 0.001);

                    Border pill = Assert.IsType<Border>(FindVisualChildByName<Border>(selected, "SelectionIndicator"));
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => pill.Opacity > 0.99).ConfigureAwait(true),
                        "The disabled selection pill must be fully visible.");
                    AssertBrushColor(pill.Background, "SystemControlDisabledAccentBrush");

                    grid.IsEnabled = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(1.0, selected.Opacity, 0.001);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_EditingTextBox_UsesGridValidationTemplateAndHighContrastFocusEdgeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(3);
                Controls.DataGrid grid = CreateGrid(rows);
                Window window = ShowInWindow(grid);

                try
                {
                    // Applying a theme regenerates the rows and ends an open edit, so switch before editing.
                    ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    grid.CurrentCell = new DataGridCellInfo(rows[0], grid.Columns[0]);
                    Assert.True(grid.BeginEdit(), "BeginEdit should start editing the current cell.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    TextBox editor = Assert.IsType<TextBox>(grid.Columns[0].GetCellContent(rows[0]));
                    Assert.Same(application.TryFindResource("DataGridValidationErrorTemplate"), Validation.GetErrorTemplate(editor));

                    Border edge = Assert.IsType<Border>(FindVisualChildByName<Border>(editor, "EditorBorder"));
                    Assert.Equal(SystemColors.HighlightColor, SolidColor(edge.BorderBrush));
                    Assert.Equal((Thickness)application.TryFindResource("TextControlBorderThemeThicknessFocused"), edge.BorderThickness);
                    Assert.NotEqual(SolidColor(editor.Background), SolidColor(edge.BorderBrush));

                    Assert.True(grid.CancelEdit(DataGridEditingUnit.Row));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_EditingTextBox_ShowsValidationErrorToolTipAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                List<SampleRow> rows = CreateRows(3);
                Controls.DataGrid grid = CreateGrid(rows);
                Window window = ShowInWindow(grid);

                try
                {
                    grid.CurrentCell = new DataGridCellInfo(rows[0], grid.Columns[1]);
                    Assert.True(grid.BeginEdit(), "BeginEdit should start editing the current cell.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    TextBox editor = Assert.IsType<TextBox>(grid.Columns[1].GetCellContent(rows[0]));

                    // Text the Quantity binding cannot convert fails the commit and keeps the editor open,
                    // so the editor itself must explain the error.
                    editor.Text = "abc";
                    _ = grid.CommitEdit(DataGridEditingUnit.Cell, exitEditingMode: true);
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => Validation.GetHasError(editor)).ConfigureAwait(true),
                        "The unconvertible text should leave a validation error on the open editor.");
                    Assert.Same(editor, grid.Columns[1].GetCellContent(rows[0]));
                    Assert.Equal(Validation.GetErrors(editor)[0].ErrorContent, editor.ToolTip);

                    Assert.True(grid.CancelEdit(DataGridEditingUnit.Row));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_Cell_UsesInsetCollectionFocusVisualAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(2);
                Controls.DataGrid grid = CreateGrid(rows);
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridCell cell = CellFor(grid, rows[0], grid.Columns[0]);
                    Assert.Same(application.TryFindResource("DefaultCollectionFocusVisualStyle"), cell.FocusVisualStyle);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DataGrid_ThemeChange_KeepsOpenCellEditAndRepaintsGridLinesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                List<SampleRow> rows = CreateRows(3);
                Controls.DataGrid grid = CreateGrid(rows);
                Window window = ShowInWindow(grid);

                try
                {
                    DataGridRow rowBefore = Assert.IsType<DataGridRow>(grid.ItemContainerGenerator.ContainerFromIndex(0));
                    Color lightLine = SolidColor(grid.HorizontalGridLinesBrush);

                    grid.CurrentCell = new DataGridCellInfo(rows[0], grid.Columns[0]);
                    Assert.True(grid.BeginEdit(), "BeginEdit should start editing the current cell.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    TextBox editor = Assert.IsType<TextBox>(grid.Columns[0].GetCellContent(rows[0]));
                    editor.Text = "Typed during edit";

                    // The grid-line brushes are DynamicResource values, so a theme change used to replace them,
                    // and the native grid answered by regenerating every row container and ending the edit.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Color darkLine = ResolvedColor(application, "DividerStrokeColorDefaultBrush");
                    Assert.NotEqual(lightLine, darkLine);
                    Assert.Equal(darkLine, SolidColor(grid.HorizontalGridLinesBrush));

                    Assert.Same(rowBefore, grid.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.True(CellFor(grid, rows[0], grid.Columns[0]).IsEditing, "The cell must still be editing.");
                    Assert.Same(editor, grid.Columns[0].GetCellContent(rows[0]));
                    Assert.Equal("Typed during edit", editor.Text, StringComparer.Ordinal);

                    Assert.True(grid.CommitEdit(DataGridEditingUnit.Row, exitEditingMode: true), "The open edit must commit.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Typed during edit", rows[0].Name, StringComparer.Ordinal);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        // Helpers

        private static DataGridCell CellFor(DependencyObject grid, object item, DataGridColumn column)
        {
            return Assert.Single(
                FindVisualChildren<DataGridCell>(grid),
                cell => ReferenceEquals(cell.Column, column) && ReferenceEquals(cell.DataContext, item));
        }

        private static IEnumerable<Expander> GroupExpanders(DependencyObject grid)
        {
            return FindVisualChildren<GroupItem>(grid)
                .Select(static groupItem => Assert.IsType<Expander>(groupItem.Template.FindName("GroupExpander", groupItem)));
        }

        private static DataGridColumnHeader HeaderFor(DependencyObject grid, DataGridColumn column)
        {
            return Assert.Single(FindVisualChildren<DataGridColumnHeader>(grid), header => ReferenceEquals(header.Column, column));
        }

        private static Controls.DataGrid CreateGrid(IEnumerable items)
        {
            Controls.DataGrid grid = new()
            {
                AutoGenerateColumns = false,
                Width = 480,
                Height = 320,
                ItemsSource = items,
            };
            grid.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(SampleRow.Name)) });
            grid.Columns.Add(new DataGridTextColumn { Header = "Quantity", Binding = new Binding(nameof(SampleRow.Quantity)) });
            grid.Columns.Add(new DataGridCheckBoxColumn { Header = "In stock", Binding = new Binding(nameof(SampleRow.InStock)) });
            return grid;
        }

        private static List<SampleRow> CreateRows(int count)
        {
            List<SampleRow> rows = new(count);
            for (int i = 0; i < count; i++)
            {
                rows.Add(new SampleRow
                {
                    Name = "Item " + i.ToString(CultureInfo.InvariantCulture),
                    Quantity = i,
                    InStock = i % 2 is 0,
                    Category = i % 3 is 0 ? "Tools" : "Parts",
                });
            }

            return rows;
        }

        [Fact]
        public Task DataGrid_MismatchedTargetTypeAppResource_KeepsNativeColumnStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                const string key = "DataGridTextColumnElementStyle";
                object? original = application.Resources.Contains(key) ? application.Resources[key] : null;
                bool hadOriginal = application.Resources.Contains(key);
                Style mismatched = new(typeof(Button));
                application.Resources[key] = mismatched;
                Window? window = null;

                try
                {
                    List<SampleRow> rows = CreateRows(2);
                    Controls.DataGrid grid = CreateGrid(rows);
                    window = ShowInWindow(grid);

                    DataGridTextColumn name = Assert.IsType<DataGridTextColumn>(grid.Columns[0]);
                    Assert.NotSame(mismatched, name.ElementStyle);
                    _ = Assert.IsType<TextBlock>(name.GetCellContent(rows[0]));
                }
                finally
                {
                    if (window is not null)
                    {
                        CloseWindowAndDrain(window);
                    }

                    if (hadOriginal)
                    {
                        application.Resources[key] = original;
                    }
                    else
                    {
                        application.Resources.Remove(key);
                    }
                }
            });
        }

        [Fact]
        public Task DataGrid_NativeDataGrid_KeepsNativeLookWithLibraryThemeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                DataGrid native = new()
                {
                    AutoGenerateColumns = false,
                    Width = 480,
                    Height = 320,
                    ItemsSource = CreateRows(2),
                };
                native.Columns.Add(new DataGridTextColumn { Header = "Name", Binding = new Binding(nameof(SampleRow.Name)) });
                Window window = ShowInWindow(native);

                try
                {
                    Assert.Null(native.RowStyle);
                    Assert.Null(native.CellStyle);
                    Assert.Null(native.ColumnHeaderStyle);
                    Assert.Null(FindVisualChildByName<Border>(native, "OuterBorder"));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static Window ShowInWindow(FrameworkElement content)
        {
            // Off-screen, so a resting mouse pointer cannot hover a row and change its visual state.
            Window window = new()
            {
                Content = content,
                Width = 640,
                Height = 480,
                Left = -20000,
                Top = -20000,
                WindowStartupLocation = WindowStartupLocation.Manual,
                ShowInTaskbar = false,
            };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            return window;
        }

        private sealed class SampleRow
        {
            public string Name { get; set; } = string.Empty;

            public int Quantity { get; set; }

            public bool InStock { get; set; }

            public string Category { get; set; } = string.Empty;
        }

        private sealed class ValidatedRow : INotifyDataErrorInfo
        {
            public const string NameRequired = "Name is required.";

            public event EventHandler<DataErrorsChangedEventArgs>? ErrorsChanged;

            public string Name
            {
                get;
                set
                {
                    field = value;
                    ErrorsChanged?.Invoke(this, new DataErrorsChangedEventArgs(nameof(Name)));
                }
            } = string.Empty;

            public bool HasErrors => string.IsNullOrWhiteSpace(Name);

            public IEnumerable GetErrors(string? propertyName)
            {
                if (string.Equals(propertyName, nameof(Name), StringComparison.Ordinal) && HasErrors)
                {
                    yield return NameRequired;
                }
            }
        }
    }
}
