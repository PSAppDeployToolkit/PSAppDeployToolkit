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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public Task GalleryButtonsPage_EnableCheckBoxControlsOnlyVisibleButtonVariantsAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.CheckBox enable = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "ButtonEnableCheckBox"), exactMatch: false);
                Controls.Button standard = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Standard"), exactMatch: false);
                Controls.Button accent = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Accent"), exactMatch: false);
                Controls.Button subtle = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Subtle"), exactMatch: false);
                Controls.Button? disabled = FindFluentButtonByContent(window, "Disabled");

                Assert.Null(disabled);

                Assert.True(standard.IsEnabled);
                Assert.True(accent.IsEnabled);
                Assert.True(subtle.IsEnabled);

                enable.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.False(standard.IsEnabled, "Enable toggle should disable the Standard button.");
                Assert.False(accent.IsEnabled, "Enable toggle should disable the Accent button.");
                Assert.False(subtle.IsEnabled, "Enable toggle should disable the Subtle button.");
            });
        }

        [Fact]
        public Task DemoSampleControl_SourceExpander_ReopensAfterCollapseAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(application);
                MergeDemoSharedStyles(application);

                DemoSampleControl sample = new()
                {
                    SampleDescription = "Sample",
                    DemoContent = new TextBlock { Text = "Body" },
                    XamlSource = "<Grid />",
                    CSharpSource = "public void Demo() { }",
                };
                Window window = new()
                {
                    Content = sample,
                    Width = 480,
                    Height = 360,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Controls.Expander expander = Assert.IsType<Controls.Expander>(sample.FindName("SourceExpander"));

                    expander.IsExpanded = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) > 1d).ConfigureAwait(true),
                        "First expand should open the source content row.");

                    expander.IsExpanded = false;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) <= 0.5d).ConfigureAwait(true),
                        "Collapse should close the source content row.");

                    // Regression guard: re-expanding after a collapse must reopen the row.
                    // The collapse animation keeps filling and so holds the row closed, and a
                    // filling animation outranks the trigger setter, so the expand path has to
                    // re-animate the row back open or the dropdown never comes back.
                    expander.IsExpanded = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, milliseconds: 4000, () => SourceContentRowHeight(expander) > 1d).ConfigureAwait(true),
                        "Re-expanding after a collapse must reopen the source content row.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        private static double SourceContentRowHeight(Controls.Expander expander)
        {
            FrameworkElement? clip = FindVisualChildByName<FrameworkElement>(expander, "SourceContentClip");
            return clip?.ActualHeight ?? 0d;
        }

        [Fact]
        public Task GalleryButtonsPage_SubtleButtonsUseWinUiTransparentRestBorderAndToggleButtonSampleIsRemovedAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.Button subtle = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Subtle"), exactMatch: false);
                Controls.Button refresh = Assert.IsType<Controls.Button>(FindFluentButtonByContent(window, "Refresh"), exactMatch: false);

                AssertBrushIsTransparent(subtle.BorderBrush);
                AssertBrushIsTransparent(refresh.BorderBrush);
                Assert.Null(FindToggleButtonByContent(window, "Bold"));
                Assert.Null(FindToggleButtonByContent(window, "Pinned"));
            });
        }

        [Fact]
        public Task GalleryIconsPage_IconographyHeaderAndSearchFollowWinUiGalleryAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                List<TextBlock> titles = [.. FindVisualChildren<TextBlock>(window)
                    .Where(static text => string.Equals(text.Text, "Iconography", StringComparison.Ordinal))];
                Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);

                _ = Assert.Single(titles);
                Assert.Equal("Search icons by name, code, or tags", search.PlaceholderText, StringComparer.Ordinal);
                Assert.Equal(420.0, search.Width, 0.1);
                Assert.Empty(FindVisualChildren<DemoSampleControl>(window));
            });
        }

        [Fact]
        public Task GalleryIconsPage_SearchFiltersCatalogAndSelectsFirstResultAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);
                Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "IconCatalogList"), exactMatch: false);

                int totalIcons = GetIconCatalogItems(list).Count;
                Assert.True(totalIcons > 500, "Catalog should load the full Segoe Fluent Icons set before filtering.");

                search.Text = "zoom";
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                List<GalleryIconsPage.IconCatalogItem> filtered = GetIconCatalogItems(list);
                Assert.True(filtered.Count > 0, "Searching for zoom should keep matching icons.");
                Assert.True(filtered.Count < totalIcons, "Searching for zoom should remove non-matching icons.");
                foreach (GalleryIconsPage.IconCatalogItem item in filtered)
                {
                    Assert.True(item.Name.Contains("zoom", StringComparison.OrdinalIgnoreCase),
                        "Filtered icons should match the search term: " + item.Name);
                }

                TextBlock nameValue = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "IconNameValueText"), exactMatch: false);
                Assert.Equal(filtered[0].Name, nameValue.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryIconsPage_ClickingTileSelectsIconAndPopulatesSidebarAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.ListView list = Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "IconCatalogList"), exactMatch: false);

                List<Button> tiles = [.. FindVisualChildren<Button>(list)
                    .Where(static tile => tile.DataContext is GalleryIconsPage.IconCatalogItem)];
                Assert.True(tiles.Count >= 2, "The initial viewport should realize icon tiles.");

                GalleryIconsPage.IconCatalogItem first = (GalleryIconsPage.IconCatalogItem)tiles[0].DataContext;
                GalleryIconsPage.IconCatalogItem second = (GalleryIconsPage.IconCatalogItem)tiles[1].DataContext;
                Assert.True(first.IsSelected, "The first icon should be selected initially so the sidebar is never empty.");
                Assert.False(second.IsSelected, "The second icon should start unselected.");

                tiles[1].RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(second.IsSelected, "Clicking a tile should select its icon.");
                Assert.False(first.IsSelected, "Selecting a tile should clear the previous selection.");

                TextBlock nameValue = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "IconNameValueText"), exactMatch: false);
                Controls.FontIcon preview = Assert.IsType<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(window, "IconPreviewGlyph"), exactMatch: false);
                Assert.Equal(second.Name, nameValue.Text, StringComparer.Ordinal);
                Assert.Equal(second.Glyph, preview.Glyph, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryIconsPage_SidebarGlyphFieldsMatchWinUiGalleryFormatsAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryIconsPage(), static window =>
            {
                Controls.AutoSuggestBox search = Assert.IsType<Controls.AutoSuggestBox>(FindVisualChildByName<Controls.AutoSuggestBox>(window, "IconSearchBox"), exactMatch: false);

                search.Text = "E71F";
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                AssertIconSidebarValue(window, "IconNameValueText", "CopyIconNameButton", "ZoomOut");
                AssertIconSidebarValue(window, "IconTextGlyphValueText", "CopyTextGlyphButton", "&#xE71F;");
                AssertIconSidebarValue(window, "IconCodeGlyphValueText", "CopyCodeGlyphButton", "\\uE71F");
                AssertIconSidebarValue(window, "IconXamlValueText", "CopyXamlButton", "<fluence:FontIcon Glyph=\"&#xE71F;\" />");
                AssertIconSidebarValue(window, "IconCSharpValueText", "CopyCSharpButton",
                    "FontIcon icon = new FontIcon();" + Environment.NewLine + "icon.Glyph = \"\\uE71F\";");
            });
        }

        private static List<GalleryIconsPage.IconCatalogItem> GetIconCatalogItems(Controls.ListView list)
        {
            List<GalleryIconsPage.IconCatalogItem> items = [];
            if (list.ItemsSource is IEnumerable<GalleryIconsPage.IconCatalogRow> rows)
            {
                foreach (GalleryIconsPage.IconCatalogRow row in rows)
                {
                    items.AddRange(row.Items);
                }
            }

            return items;
        }

        private static void AssertIconSidebarValue(Window window, string valueName, string buttonName, string expected)
        {
            TextBlock value = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, valueName), exactMatch: false);
            Controls.Button copy = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, buttonName), exactMatch: false);
            Assert.Equal(expected, value.Text, StringComparer.Ordinal);
            Assert.Equal(expected, copy.Tag as string, StringComparer.Ordinal);
        }

        [Fact]
        public Task GalleryButtonsPage_DemoContentPresenterCentersButtonGroupsAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                List<DemoSampleControl> samples = [.. FindVisualChildren<DemoSampleControl>(window)];
                Assert.True(samples.Count > 0, "Buttons page should render DemoSampleControl samples.");

                foreach (DemoSampleControl sample in samples)
                {
                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(sample.FindName("DemoContentPresenter"));
                    Assert.Equal(VerticalAlignment.Center, presenter.VerticalAlignment);
                    Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
                }
            });
        }

        [Fact]
        public Task GallerySelectionPage_BasicRadioGroupStartsAtGroupLeftEdgeAsync()
        {
            return RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RadioButton optionA = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option A"), exactMatch: false);
                Controls.RadioButton optionB = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option B"), exactMatch: false);
                Controls.RadioButton optionC = Assert.IsType<Controls.RadioButton>(FindRadioButtonByContent(window, "Option C"), exactMatch: false);

                Assert.Equal(0.0, optionA.Margin.Left);
                Assert.Equal(16.0, optionA.Margin.Right);
                Assert.Equal(0.0, optionB.Margin.Left);
                Assert.Equal(0.0, optionC.Margin.Left);
            });
        }

        [Fact]
        public Task GalleryDataBindingPage_AddItemRailIsWiderAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                Controls.TextBox newItemBox = Assert.IsType<Controls.TextBox>(FindVisualChildByName<Controls.TextBox>(window, "NewItemBox"), exactMatch: false);
                StackPanel rightRailStack = Assert.IsType<StackPanel>(newItemBox.Parent);

                Assert.Equal(320.0, rightRailStack.MinWidth, 0.1);
                Assert.Equal(320.0, newItemBox.Width, 0.1);
            });
        }

        [Fact]
        public Task GalleryNavigationPage_CompactSampleShowsBackAndPaneToggleButtonsAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView compact = Assert.IsType<Controls.NavigationView>(FindVisualChildByName<Controls.NavigationView>(window, "CompactNavigationDemo"), exactMatch: false);
                Controls.CheckBox backEnabled = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "BackEnabledToggle"), exactMatch: false);

                Assert.True(backEnabled.IsChecked.GetValueOrDefault(),
                    "Compact navigation sample should start with the back button enabled.");
                Assert.True(compact.IsPaneToggleButtonVisible,
                    "Compact navigation sample should explicitly show the pane toggle button.");

                Button back = Assert.IsType<Button>(compact.Template.FindName(Controls.NavigationView.PartBackButton, compact));
                Button paneToggle = Assert.IsType<Button>(compact.Template.FindName(Controls.NavigationView.PartPaneToggleButton, compact));
                Assert.Equal(Visibility.Visible, back.Visibility);
                Assert.Equal(Visibility.Visible, paneToggle.Visibility);
                Assert.Null(FindVisualChildByName<Controls.Button>(window, "CompactPaneToggleButton"));

                Assert.False(compact.IsPaneOpen,
                    "Compact navigation sample should start with the compact pane closed.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(compact.IsPaneOpen,
                    "The built-in pane toggle should open the compact pane.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(compact.IsPaneOpen,
                    "The built-in pane toggle should close the compact pane on subsequent clicks.");
            });
        }

        [Fact]
        public Task GalleryFormsPage_ActionsAlignAndOutputHasStableSpaceAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryFormsPage(), static window =>
            {
                Controls.Button signIn = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, "SignInButton"), exactMatch: false);
                StackPanel? checkoutButtons = FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel");
                Controls.Button placeOrder = Assert.IsType<Controls.Button>(checkoutButtons?.Children.OfType<Controls.Button>().FirstOrDefault(), exactMatch: false);
                List<Border> outputRegions = [.. FindVisualChildren<DemoSampleControl>(window)
                    .Select(static sample => sample.FindName("OutputRegion") as Border)
                    .Where(static border => border is not null)
                    .Cast<Border>()];

                Assert.Equal(0.0, signIn.Margin.Left);
                Assert.Equal(0.0, placeOrder.Margin.Left);
                Assert.True(outputRegions.Count > 0, "Forms page should expose output regions.");
                Assert.True(outputRegions.TrueForAll(static region => region.MinWidth >= 220.0),
                    "Output regions should reserve enough room for status text.");
            });
        }

        [Fact]
        public async Task GalleryPages_RemoveRequestedOutputRegionsAsync()
        {
            await RunDemoPageTestAsync(static () => new GalleryInputsPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "CharCountLabel"))).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "ItemCountLabel"))).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "TreeSelectionLabel"))).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
                Assert.Null(FindVisualChildByName<TextBlock>(window, "CompactNavigationOutputText"))).ConfigureAwait(true);
        }

        [Fact]
        public Task GalleryStatusPage_NumberBoxDrivesFirstProgressBarAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryStatusPage(), static window =>
            {
                Controls.NumberBox numberBox = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox"), exactMatch: false);
                Controls.ProgressBar progressBar = Assert.IsType<Controls.ProgressBar>(FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar"), exactMatch: false);
                Controls.ToggleSwitch indeterminateToggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "IndeterminateToggle"), exactMatch: false);
                Controls.NumberBox progressRingValueBox = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "ProgressRingValueBox"), exactMatch: false);

                Assert.Equal(HorizontalAlignment.Center, numberBox.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, numberBox.VerticalAlignment);
                Assert.Equal(HorizontalAlignment.Center, indeterminateToggle.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, indeterminateToggle.VerticalAlignment);
                Assert.Equal(HorizontalAlignment.Center, progressRingValueBox.HorizontalAlignment);
                Assert.Equal("On / Off", indeterminateToggle.OnContent as string, StringComparer.Ordinal);
                Assert.Equal("On / Off", indeterminateToggle.OffContent as string, StringComparer.Ordinal);
                Assert.Equal(0d, numberBox.Minimum);
                Assert.Equal(100d, numberBox.Maximum);
                Assert.Null(FindVisualChildByName<Controls.Slider>(window, "ProgressSlider"));
                Assert.Null(FindVisualChildByName<TextBlock>(window, "SliderValueLabel"));

                numberBox.Value = 73d;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.Equal(73d, progressBar.Value, 0.1);

                numberBox.Value = 0d;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.Equal(0d, progressBar.Value, 0.1);

                indeterminateToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("On / Off", indeterminateToggle.OffContent as string, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryFormsPage_CheckoutFieldsUseStableNamesAndAlignOptionalInputAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryFormsPage(), static window =>
            {
                Grid checkoutGrid = Assert.IsType<Grid>(FindVisualChildByName<Grid>(window, "CheckoutFieldsGrid"), exactMatch: false);
                Controls.NumberBox quantity = Assert.IsType<Controls.NumberBox>(FindVisualChildByName<Controls.NumberBox>(window, "QuantityNumberBox"), exactMatch: false);
                Controls.TextBox optional = Assert.IsType<Controls.TextBox>(FindVisualChildByName<Controls.TextBox>(window, "OptionalTextBox"), exactMatch: false);
                Controls.CheckBox gift = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "GiftCheckBox"), exactMatch: false);
                StackPanel actions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel"), exactMatch: false);

                Assert.Equal(3, checkoutGrid.ColumnDefinitions.Count);
                Assert.Equal(0, Grid.GetColumn(quantity));
                Assert.Equal(2, Grid.GetColumn(optional));
                Assert.Equal(VerticalAlignment.Bottom, optional.VerticalAlignment);
            });
        }

        [Fact]
        public Task GalleryDataPage_ListBackgroundsAndPersonPicturesUseExpectedAssetsAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                Border simpleBackground = Assert.IsType<Border>(FindVisualChildByName<Border>(window, "SimpleListViewBackground"), exactMatch: false);
                Border richBackground = Assert.IsType<Border>(FindVisualChildByName<Border>(window, "RichListViewBackground"), exactMatch: false);
                StackPanel emptyStateActions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "EmptyStateActionsPanel"), exactMatch: false);

                Assert.Equal(HorizontalAlignment.Center, emptyStateActions.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, emptyStateActions.VerticalAlignment);
                Assert.True(emptyStateActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "EmptyContent action buttons should be wider than the default compact command width.");

                List<Controls.PersonPicture> personPictures = [.. FindVisualChildren<Controls.PersonPicture>(window)];
                WrapPanel personPicturePanel = Assert.IsType<WrapPanel>(personPictures.FirstOrDefault()?.Parent);
                Assert.Equal(5, personPictures.Count);
                Assert.Equal(5, personPictures.Count(static picture => picture.ProfilePicture is not null));
                Assert.Equal(HorizontalAlignment.Center, personPicturePanel.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, personPicturePanel.VerticalAlignment);
                Assert.True(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureMadisonButler.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should include the Madison Butler portrait asset.");
                Assert.False(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureOscarWard.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should remove the extra Oscar Ward portrait.");
                Assert.False(personPictures.Exists(static picture => !string.IsNullOrWhiteSpace(picture.Initials)),
                    "PersonPicture sample should remove the initials fallback entry.");
                Assert.False(personPictures.Exists(static picture => picture.IsGroup),
                    "PersonPicture sample should remove the invalid group glyph entry.");
            });
        }

        [Fact]
        public async Task GalleryPages_RightRailControlsUseRequestedAlignmentAsync()
        {
            await RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                StackPanel selectionRail = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "SelectionModeRail"), exactMatch: false);
                Assert.Equal(VerticalAlignment.Center, selectionRail.VerticalAlignment);
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                StackPanel treeExpansionActions = Assert.IsType<StackPanel>(FindVisualChildByName<StackPanel>(window, "TreeExpansionActionsPanel"), exactMatch: false);
                Assert.Equal(HorizontalAlignment.Center, treeExpansionActions.HorizontalAlignment);
                Assert.Equal(VerticalAlignment.Center, treeExpansionActions.VerticalAlignment);
                Assert.True(treeExpansionActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "Tree expansion buttons should be wider than the default compact command width.");
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryAccessibilityPage(), static window =>
            {
                string[] buttonNames =
                [
                    "AutomationNewDocumentButton",
                    "AutomationOpenFileButton",
                    "AutomationSaveButton",
                    "AutomationDeleteButton",
                    "AutomationShareButton",
                ];

                foreach (string buttonName in buttonNames)
                {
                    Controls.Button button = Assert.IsType<Controls.Button>(FindVisualChildByName<Controls.Button>(window, buttonName), exactMatch: false);
                    Assert.Equal(36.0, button.Width, 0.1);
                    Assert.Equal(36.0, button.Height, 0.1);
                    Assert.Equal(36.0, button.MinWidth, 0.1);
                    Assert.Equal(0.0, button.Padding.Left, 0.1);
                }
            }).ConfigureAwait(true);
        }

        [Fact]
        public Task GalleryNavigationPage_IconsAreDefaultSizeAndInfoBadgePaneStartsExpandedAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView leftNavigation = Assert.IsType<Controls.NavigationView>(FindVisualChildByName<Controls.NavigationView>(window, "LeftNavigationDemo"), exactMatch: false);

                List<Controls.FontIcon> leftIcons = [.. FindVisualChildren<Controls.FontIcon>(leftNavigation)];
                Assert.True(leftIcons.Count >= 3, "Left navigation sample should expose item icons.");
                Assert.True(leftIcons.TrueForAll(static icon => Math.Abs(icon.IconFontSize - 16d) < 0.1),
                    "NavigationView item icons should align with the compact pane glyph size.");

                Controls.NavigationView badgeNavigation = Assert.IsType<Controls.NavigationView>(FindVisualChildren<Controls.NavigationView>(window).FirstOrDefault(static nav => string.Equals(nav.Header as string, "Inbox", StringComparison.Ordinal)), exactMatch: false);
                Assert.Equal(NavigationViewPaneDisplayMode.Left, badgeNavigation.PaneDisplayMode);
                Assert.True(badgeNavigation.IsPaneOpen,
                    "InfoBadge NavigationView sample should keep the pane open.");
            });
        }

        [Fact]
        public Task GalleryTabsPage_PlacementSampleUsesLeftPlacementOnlyAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryTabsPage(), static window =>
            {
                Dictionary<string, TabItem> items = FindVisualChildren<TabItem>(window)
                    .Where(static item => item.Header is string)
                    .ToDictionary(static item => (string)item.Header, StringComparer.Ordinal);

                double infoWidth = GetExplicitHeaderWidth(items, "Inbox");
                double archiveWidth = GetExplicitHeaderWidth(items, "Archive");

                Assert.Equal(infoWidth, archiveWidth, 0.1);
                Assert.True(infoWidth > 0.0, "Placement sample tab headers should use an explicit shared width.");

                TabControl leftTabs = Assert.IsType<TabControl>(FindVisualChildByName<TabControl>(window, "LeftPlacementTabs"), exactMatch: false);
                Assert.Equal(Dock.Left, leftTabs.TabStripPlacement);

                TabControl? bottomTabs = FindVisualChildByName<TabControl>(window, "BottomPlacementTabs");
                Assert.Null(bottomTabs);
            });
        }

        [Fact]
        public Task GalleryLayoutPage_SeparatesStructuralPrimitiveSamplesAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryLayoutPage(), static window =>
            {
                List<string> descriptions = [.. FindVisualChildren<DemoSampleControl>(window).Select(static sample => sample.SampleDescription)];

                Assert.True(descriptions.Exists(static description => description.Contains("Separator", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated Separator DemoSampleControl.");
                Assert.True(descriptions.Exists(static description => description.Contains("DockPanel", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated DockPanel DemoSampleControl.");
                Assert.True(descriptions.Exists(static description => description.Contains("Expander", StringComparison.OrdinalIgnoreCase)),
                    "Layout page should have a dedicated Expander DemoSampleControl.");

                Controls.Expander dockPanelExpander = Assert.IsType<Controls.Expander>(FindVisualChildByName<Controls.Expander>(window, "DockPanelOptionsExpander"), exactMatch: false);
                _ = Assert.IsType<DockPanel>(dockPanelExpander.Header, exactMatch: false);
                _ = Assert.IsType<DockPanel>(dockPanelExpander.Content, exactMatch: false);
            });
        }

        [Fact]
        public Task GalleryAccessibilityPage_RtlSampleDefaultsOnAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryAccessibilityPage(), static window =>
            {
                Controls.ToggleSwitch toggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "RtlToggle"), exactMatch: false);
                Controls.Card card = Assert.IsType<Controls.Card>(FindVisualChildByName<Controls.Card>(window, "RtlDemoCard"), exactMatch: false);

                Assert.True(toggle.IsChecked.GetValueOrDefault(),
                    "RTL sample should default to On.");
                Assert.Equal(FlowDirection.RightToLeft, card.FlowDirection);
            });
        }

        private static double GetExplicitHeaderWidth(IDictionary<string, TabItem> items, string header)
        {
            Assert.True(items.TryGetValue(header, out TabItem? item), "TabItem should exist: " + header);
            return double.IsNaN(item.Width) ? item.MinWidth : item.Width;
        }

        private static void AssertBrushIsTransparent(Brush brush)
        {
            if (brush is SolidColorBrush solid)
            {
                Assert.Equal(0, solid.Color.A);
            }
        }

        private static Controls.ToggleButton? FindToggleButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.ToggleButton>(root).FirstOrDefault(button => string.Equals(button.Content as string, content, StringComparison.Ordinal));
        }

        private static Controls.RadioButton? FindRadioButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.RadioButton>(root).FirstOrDefault(radioButton => string.Equals(radioButton.Content as string, content, StringComparison.Ordinal));
        }
    }
}
