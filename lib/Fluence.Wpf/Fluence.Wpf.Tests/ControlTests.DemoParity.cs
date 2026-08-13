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
using System.Windows;
using System.Windows.Controls;
using Fluence.Wpf.Demo.Pages;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [Fact]
        public void GalleryInputsPage_SliderSamplesIncludeHorizontalAndVerticalTicks()
        {
            RunDemoPageTest(static () => new GalleryInputsPage(), static window =>
            {
                Controls.Slider? horizontal = FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider");
                Controls.Slider? vertical = FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider");

                Assert.NotNull(horizontal);
                Assert.NotNull(vertical);
                Assert.NotEqual(System.Windows.Controls.Primitives.TickPlacement.None, horizontal.TickPlacement);
                Assert.NotEqual(System.Windows.Controls.Primitives.TickPlacement.None, vertical.TickPlacement);
                Assert.True(horizontal.TickFrequency > 0);
                Assert.True(vertical.TickFrequency > 0);
            });
        }

        [Fact]
        public void GalleryButtonsPage_RepeatButtonIncrementsNearbyCountText()
        {
            RunDemoPageTest(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.RepeatButton? button = FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton");
                TextBlock? count = FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText");
                Controls.RepeatButton? accentRepeat = FindRepeatButtonByContent(window, "Accent repeat");

                Assert.NotNull(button);
                Assert.NotNull(count);
                Assert.Null(accentRepeat);
                Assert.Equal("Clicks: 0", count.Text, StringComparer.Ordinal);

                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DrainDispatcher(window.Dispatcher);

                Assert.Equal("Clicks: 2", count.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void GalleryButtonsPage_ToggleButtonSampleUpdatesStateText()
        {
            RunDemoPageTest(() => new GalleryButtonsPage(), window =>
            {
                Controls.ToggleButton? wrapToggle = FindVisualChildByName<Controls.ToggleButton>(window, "WrapToggleButton");
                Controls.ToggleButton? threeStateToggle = FindVisualChildByName<Controls.ToggleButton>(window, "ThreeStateToggleButton");
                TextBlock? stateText = FindVisualChildByName<TextBlock>(window, "ToggleButtonStateText");

                Assert.NotNull(wrapToggle);
                Assert.NotNull(threeStateToggle);
                Assert.NotNull(stateText);
                Assert.True(threeStateToggle.IsThreeState, "The three-state sample should opt into IsThreeState.");
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = true;
                DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: On", stateText.Text, StringComparer.Ordinal);

                wrapToggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.Equal("Wrap text: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void GalleryButtonsPage_ToggleSplitButtonSampleTogglesStateText()
        {
            RunDemoPageTest(() => new GalleryButtonsPage(), window =>
            {
                Controls.ToggleSplitButton? listToggle = FindVisualChildByName<Controls.ToggleSplitButton>(window, "ListToggleSplitButton");
                TextBlock? stateText = FindVisualChildByName<TextBlock>(window, "ToggleSplitButtonStateText");

                Assert.NotNull(listToggle);
                Assert.NotNull(stateText);
                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);

                Button? primary = listToggle.Template?.FindName("PART_PrimaryButton", listToggle) as Button;
                Assert.NotNull(primary);

                primary.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DrainDispatcher(window.Dispatcher);

                Assert.True(listToggle.IsChecked, "Clicking the primary half should check the sample.");
                Assert.Equal("List formatting: Bulleted list", stateText.Text, StringComparer.Ordinal);

                primary.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DrainDispatcher(window.Dispatcher);

                Assert.False(listToggle.IsChecked, "A second primary click should uncheck the sample.");
                Assert.Equal("List formatting: Off", stateText.Text, StringComparer.Ordinal);
            });
        }

        [Fact]
        public void GallerySelectionPage_CheckBoxSamplesMatchWinUIGalleryStates()
        {
            RunDemoPageTest(static () => new GallerySelectionPage(), static window =>
            {
                Controls.CheckBox? twoState = FindVisualChildByName<Controls.CheckBox>(window, "TwoStateCheckBox");
                Controls.CheckBox? threeState = FindVisualChildByName<Controls.CheckBox>(window, "ThreeStateCheckBox");
                Controls.CheckBox? selectAll = FindVisualChildByName<Controls.CheckBox>(window, "SelectAllCheckBox");
                Controls.CheckBox? optionOne = FindVisualChildByName<Controls.CheckBox>(window, "OptionOneCheckBox");
                Controls.CheckBox? optionTwo = FindVisualChildByName<Controls.CheckBox>(window, "OptionTwoCheckBox");
                Controls.CheckBox? optionThree = FindVisualChildByName<Controls.CheckBox>(window, "OptionThreeCheckBox");

                Assert.NotNull(twoState);
                Assert.False(twoState.IsThreeState);
                Assert.NotNull(threeState);
                Assert.True(threeState.IsThreeState);
                Assert.NotNull(selectAll);
                Assert.NotNull(optionOne);
                Assert.NotNull(optionTwo);
                Assert.NotNull(optionThree);

                selectAll.IsChecked = true;
                DrainDispatcher(window.Dispatcher);

                Assert.True(optionOne.IsChecked.GetValueOrDefault());
                Assert.True(optionTwo.IsChecked.GetValueOrDefault());
                Assert.True(optionThree.IsChecked.GetValueOrDefault());

                optionTwo.IsChecked = false;
                DrainDispatcher(window.Dispatcher);

                Assert.Null(selectAll.IsChecked);
            });
        }

        [Fact]
        public void GallerySelectionPage_RatingAndRequestedToggleSamplesArePresent()
        {
            RunDemoPageTest(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RatingControl? rating = FindVisualChildByName<Controls.RatingControl>(window, "RatingSample");
                Controls.RatingControl? readOnlyRating = FindVisualChildByName<Controls.RatingControl>(window, "ReadOnlyRatingSample");
                TextBlock? workHeader = FindVisualChildByName<TextBlock>(window, "WorkToggleHeaderText");
                Controls.ToggleSwitch? workToggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "WorkToggleSwitch");
                TextBlock? workLabel = FindVisualChildByName<TextBlock>(window, "WorkToggleStateText");
                Controls.ProgressRing? ring = FindVisualChildByName<Controls.ProgressRing>(window, "WorkToggleProgressRing");

                Assert.NotNull(rating);
                Assert.NotNull(readOnlyRating);
                Assert.Equal(1, CountVisualChildren<Controls.ToggleSwitch>(window));
                Assert.Null(FindVisualChildByName<Controls.ToggleSwitch>(window, "SimpleToggleSwitch"));
                Assert.Null(FindVisualChildByName<TextBlock>(window, "SimpleToggleStateText"));
                Assert.NotNull(workHeader);
                Assert.Equal("Toggle work", workHeader.Text, StringComparer.Ordinal);
                Assert.NotNull(workToggle);
                Assert.NotNull(workLabel);
                Assert.True(workToggle.IsChecked.GetValueOrDefault());
                Assert.Equal("On", workLabel.Text, StringComparer.Ordinal);
                Assert.NotNull(ring);
                Assert.True(ring.IsIndeterminate);
                Assert.Equal(new Thickness(24, 0, 0, 0), ring.Margin);

                workToggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.False(ring.IsActive);

                workToggle.IsChecked = true;
                DrainDispatcher(window.Dispatcher);
                Assert.True(ring.IsActive);
            });
        }

        [Fact]
        public void GalleryTreesPage_IncludesMultipleSelectionTreeView()
        {
            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                Controls.TreeView? treeView = FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView");

                Assert.NotNull(treeView);
                Assert.Equal(TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        [Fact]
        public void GalleryLayoutPage_ExpanderStartsCollapsed()
        {
            RunDemoPageTest(static () => new GalleryLayoutPage(), static window =>
            {
                Controls.Expander? expander = FindVisualChildByName<Controls.Expander>(window, "AdvancedOptionsExpander");

                Assert.NotNull(expander);
                Assert.False(expander.IsExpanded, "Layout page Expander sample should be collapsed by default.");
            });
        }

        [Fact]
        public void GalleryDataPage_ListBoxSamplesExposeSelectionModes()
        {
            RunDemoPageTest(static () => new GalleryDataPage(), static window =>
            {
                Controls.ListBox? singleSelect = FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox");
                Controls.ListBox? multiSelect = FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox");

                Assert.NotNull(singleSelect);
                Assert.NotNull(multiSelect);
                Assert.Equal(SelectionMode.Single, singleSelect.SelectionMode);
                Assert.Equal(SelectionMode.Extended, multiSelect.SelectionMode);
                Assert.True(singleSelect.Items.Count > 0, "Single-selection ListBox sample should contain items.");
                Assert.True(multiSelect.SelectedItems.Count >= 2,
                    "Multi-selection ListBox sample should start with multiple items selected.");
            });
        }

        [Fact]
        public void GalleryDataAndTreeSamplesExposeThemedBorders()
        {
            RunDemoPageTest(static () => new GalleryDataPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "SimpleListView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "RichListView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"));
            });

            RunDemoPageTest(static () => new GalleryDataBindingPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "BoundListView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "SelectionModeListView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "DataTemplateListView"));
            });

            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "HierarchyTreeView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "SelectionTreeView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"));
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "ExpansionTreeView"));
            });
        }

        private static void AssertControlHasThemedBorder(Control? control)
        {
            Assert.NotNull(control);
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
            foreach (Controls.RepeatButton repeatButton in FindVisualChildren<Controls.RepeatButton>(root))
            {
                if (string.Equals(repeatButton.Content as string, content, StringComparison.Ordinal))
                {
                    return repeatButton;
                }
            }

            return null;
        }

        private static void RunDemoPageTest(Func<UserControl> createPage, Action<Window> verify)
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    verify(window);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
