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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public Task GalleryInputsPage_SliderSamplesIncludeHorizontalAndVerticalTicksAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryInputsPage(), static window =>
            {
                Controls.Slider horizontal = Assert.IsType<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider"), exactMatch: false);
                Controls.Slider vertical = Assert.IsType<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider"), exactMatch: false);

                Assert.NotEqual(System.Windows.Controls.Primitives.TickPlacement.None, horizontal.TickPlacement);
                Assert.NotEqual(System.Windows.Controls.Primitives.TickPlacement.None, vertical.TickPlacement);
                Assert.True(horizontal.TickFrequency > 0);
                Assert.True(vertical.TickFrequency > 0);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_RepeatButtonIncrementsNearbyCountTextAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.RepeatButton button = Assert.IsType<Controls.RepeatButton>(FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton"), exactMatch: false);
                TextBlock count = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText"), exactMatch: false);
                Controls.RepeatButton? accentRepeat = FindRepeatButtonByContent(window, "Accent repeat");

                Assert.Null(accentRepeat);
                Assert.Equal("Clicks: 0", count.Text, StringComparer.Ordinal);

                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Equal("Clicks: 2", count.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_ToggleButtonSampleUpdatesStateTextAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.ToggleButton wrapToggle = Assert.IsType<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "WrapToggleButton"), exactMatch: false);
                Controls.ToggleButton threeStateToggle = Assert.IsType<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "ThreeStateToggleButton"), exactMatch: false);
                TextBlock stateText = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleButtonStateText"), exactMatch: false);

                Assert.True(threeStateToggle.IsThreeState, "The three-state sample should opt into IsThreeState.");
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: On", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GalleryButtonsPage_ToggleSplitButtonSampleTogglesStateTextAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.ToggleSplitButton listToggle = Assert.IsType<Controls.ToggleSplitButton>(FindVisualChildByName<Controls.ToggleSplitButton>(window, "ListToggleSplitButton"), exactMatch: false);
                TextBlock stateText = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleSplitButtonStateText"), exactMatch: false);

                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);

                Button primary = Assert.IsType<Button>(listToggle.Template?.FindName("PART_PrimaryButton", listToggle));

                primary.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(listToggle.IsChecked, "Clicking the primary half should check the sample.");
                Assert.Equal("List formatting: Bulleted list", stateText.Text, StringComparer.Ordinal);

                primary.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.False(listToggle.IsChecked, "A second primary click should uncheck the sample.");
                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task GallerySelectionPage_CheckBoxSamplesMatchWinUIGalleryStatesAsync()
        {
            return RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.CheckBox twoState = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "TwoStateCheckBox"), exactMatch: false);
                Controls.CheckBox threeState = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "ThreeStateCheckBox"), exactMatch: false);
                Controls.CheckBox selectAll = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "SelectAllCheckBox"), exactMatch: false);
                Controls.CheckBox optionOne = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionOneCheckBox"), exactMatch: false);
                Controls.CheckBox optionTwo = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionTwoCheckBox"), exactMatch: false);
                Controls.CheckBox optionThree = Assert.IsType<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionThreeCheckBox"), exactMatch: false);

                Assert.False(twoState.IsThreeState);
                Assert.True(threeState.IsThreeState);

                selectAll.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.True(optionOne.IsChecked.GetValueOrDefault());
                Assert.True(optionTwo.IsChecked.GetValueOrDefault());
                Assert.True(optionThree.IsChecked.GetValueOrDefault());

                optionTwo.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                Assert.Null(selectAll.IsChecked);
            });
        }

        [Fact]
        public Task GallerySelectionPage_RatingAndRequestedToggleSamplesArePresentAsync()
        {
            return RunDemoPageTestAsync(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RatingControl rating = Assert.IsType<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "RatingSample"), exactMatch: false);
                Controls.RatingControl readOnlyRating = Assert.IsType<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "ReadOnlyRatingSample"), exactMatch: false);
                TextBlock workHeader = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleHeaderText"), exactMatch: false);
                Controls.ToggleSwitch workToggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"), exactMatch: false);
                TextBlock workLabel = Assert.IsType<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleStateText"), exactMatch: false);
                Controls.ProgressRing ring = Assert.IsType<Controls.ProgressRing>(FindVisualChildByName<Controls.ProgressRing>(window, "WorkToggleProgressRing"), exactMatch: false);

                Assert.Equal(1, CountVisualChildren<Controls.ToggleSwitch>(window));
                Assert.Null(FindVisualChildByName<Controls.ToggleSwitch>(window, "SimpleToggleSwitch"));
                Assert.Null(FindVisualChildByName<TextBlock>(window, "SimpleToggleStateText"));
                Assert.Equal("Toggle work", workHeader.Text, StringComparer.Ordinal);
                Assert.True(workToggle.IsChecked.GetValueOrDefault());
                Assert.Equal("On", workLabel.Text, StringComparer.Ordinal);
                Assert.True(ring.IsIndeterminate);
                Assert.Equal(new Thickness(24, 0, 0, 0), ring.Margin);

                workToggle.IsChecked = false;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.False(ring.IsActive);

                workToggle.IsChecked = true;
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                Assert.True(ring.IsActive);
            });
        }

        [Fact]
        public Task GalleryTreesPage_IncludesMultipleSelectionTreeViewAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                Controls.TreeView treeView = Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"), exactMatch: false);

                Assert.Equal(TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        [Fact]
        public Task GalleryLayoutPage_ExpanderStartsCollapsedAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryLayoutPage(), static window =>
            {
                Controls.Expander expander = Assert.IsType<Controls.Expander>(FindVisualChildByName<Controls.Expander>(window, "AdvancedOptionsExpander"), exactMatch: false);

                Assert.False(expander.IsExpanded, "Layout page Expander sample should be collapsed by default.");
            });
        }

        [Fact]
        public Task GalleryDataPage_ListBoxSamplesExposeSelectionModesAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                Controls.ListBox singleSelect = Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"), exactMatch: false);
                Controls.ListBox multiSelect = Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"), exactMatch: false);

                Assert.Equal(SelectionMode.Single, singleSelect.SelectionMode);
                Assert.Equal(SelectionMode.Extended, multiSelect.SelectionMode);
                Assert.True(singleSelect.Items.Count > 0, "Single-selection ListBox sample should contain items.");
                Assert.True(multiSelect.SelectedItems.Count >= 2,
                    "Multi-selection ListBox sample should start with multiple items selected.");
            });
        }

        [Fact]
        public async Task GalleryDataAndTreeSamplesExposeThemedBordersAsync()
        {
            await RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SimpleListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "RichListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"), exactMatch: false));
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "BoundListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SelectionModeListView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "DataTemplateListView"), exactMatch: false));
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "HierarchyTreeView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "SelectionTreeView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"), exactMatch: false));
                AssertControlHasThemedBorder(Assert.IsType<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "ExpansionTreeView"), exactMatch: false));
            }).ConfigureAwait(true);
        }

        private static void AssertControlHasThemedBorder(Control control)
        {
            Assert.Equal(new Thickness(1), control.BorderThickness);
            Assert.NotNull(control.BorderBrush);
        }

        private static int CountVisualChildren<T>(DependencyObject root) where T : DependencyObject
        {
            int count = 0;
            foreach (T child in FindVisualChildren<T>(root))
            {
                count++;
            }

            return count;
        }

        private static Controls.RepeatButton? FindRepeatButtonByContent(DependencyObject root, string content)
        {
            return FindVisualChildren<Controls.RepeatButton>(root).FirstOrDefault(repeatButton => string.Equals(repeatButton.Content as string, content, StringComparison.Ordinal));
        }

        private static Task RunDemoPageTestAsync(Func<UserControl> createPage, Action<Window> verify)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                UserControl page = createPage();
                Window window = new()
                {
                    Width = 900,
                    Height = 700,
                    Content = page,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(window);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
