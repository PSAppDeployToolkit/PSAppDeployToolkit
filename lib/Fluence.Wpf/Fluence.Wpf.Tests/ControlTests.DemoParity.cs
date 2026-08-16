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
                Controls.Slider horizontal = Assert.IsAssignableFrom<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider"));
                Controls.Slider vertical = Assert.IsAssignableFrom<Controls.Slider>(FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider"));

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
                Controls.RepeatButton button = Assert.IsAssignableFrom<Controls.RepeatButton>(FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton"));
                TextBlock count = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText"));
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
            return RunDemoPageTestAsync(() => new GalleryButtonsPage(), window =>
            {
                Controls.ToggleButton wrapToggle = Assert.IsAssignableFrom<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "WrapToggleButton"));
                Controls.ToggleButton threeStateToggle = Assert.IsAssignableFrom<Controls.ToggleButton>(FindVisualChildByName<Controls.ToggleButton>(window, "ThreeStateToggleButton"));
                TextBlock stateText = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleButtonStateText"));

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
            return RunDemoPageTestAsync(() => new GalleryButtonsPage(), window =>
            {
                Controls.ToggleSplitButton listToggle = Assert.IsAssignableFrom<Controls.ToggleSplitButton>(FindVisualChildByName<Controls.ToggleSplitButton>(window, "ListToggleSplitButton"));
                TextBlock stateText = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "ToggleSplitButtonStateText"));

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
                Controls.CheckBox twoState = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "TwoStateCheckBox"));
                Controls.CheckBox threeState = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "ThreeStateCheckBox"));
                Controls.CheckBox selectAll = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "SelectAllCheckBox"));
                Controls.CheckBox optionOne = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionOneCheckBox"));
                Controls.CheckBox optionTwo = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionTwoCheckBox"));
                Controls.CheckBox optionThree = Assert.IsAssignableFrom<Controls.CheckBox>(FindVisualChildByName<Controls.CheckBox>(window, "OptionThreeCheckBox"));

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
                Controls.RatingControl rating = Assert.IsAssignableFrom<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "RatingSample"));
                Controls.RatingControl readOnlyRating = Assert.IsAssignableFrom<Controls.RatingControl>(FindVisualChildByName<Controls.RatingControl>(window, "ReadOnlyRatingSample"));
                TextBlock workHeader = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleHeaderText"));
                Controls.ToggleSwitch workToggle = Assert.IsAssignableFrom<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch"));
                TextBlock workLabel = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(window, "WorkToggleStateText"));
                Controls.ProgressRing ring = Assert.IsAssignableFrom<Controls.ProgressRing>(FindVisualChildByName<Controls.ProgressRing>(window, "WorkToggleProgressRing"));

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
                Controls.TreeView treeView = Assert.IsAssignableFrom<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"));

                Assert.Equal(TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        [Fact]
        public Task GalleryLayoutPage_ExpanderStartsCollapsedAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryLayoutPage(), static window =>
            {
                Controls.Expander expander = Assert.IsAssignableFrom<Controls.Expander>(FindVisualChildByName<Controls.Expander>(window, "AdvancedOptionsExpander"));

                Assert.False(expander.IsExpanded, "Layout page Expander sample should be collapsed by default.");
            });
        }

        [Fact]
        public Task GalleryDataPage_ListBoxSamplesExposeSelectionModesAsync()
        {
            return RunDemoPageTestAsync(static () => new GalleryDataPage(), static window =>
            {
                Controls.ListBox singleSelect = Assert.IsAssignableFrom<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"));
                Controls.ListBox multiSelect = Assert.IsAssignableFrom<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"));

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
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SimpleListView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "RichListView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListBox>(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox")));
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryDataBindingPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "BoundListView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "SelectionModeListView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.ListView>(FindVisualChildByName<Controls.ListView>(window, "DataTemplateListView")));
            }).ConfigureAwait(true);

            await RunDemoPageTestAsync(static () => new GalleryTreesPage(), static window =>
            {
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "HierarchyTreeView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "SelectionTreeView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView")));
                AssertControlHasThemedBorder(Assert.IsAssignableFrom<Controls.TreeView>(FindVisualChildByName<Controls.TreeView>(window, "ExpansionTreeView")));
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
