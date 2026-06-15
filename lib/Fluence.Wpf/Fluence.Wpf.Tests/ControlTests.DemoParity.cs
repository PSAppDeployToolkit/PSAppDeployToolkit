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

using Fluence.Wpf.Demo.Pages;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GalleryInputsPage_SliderSamplesIncludeHorizontalAndVerticalTicks()
        {
            RunDemoPageTest(static () => new GalleryInputsPage(), static window =>
            {
                Controls.Slider? horizontal = FindVisualChildByName<Controls.Slider>(window, "HorizontalTickSlider");
                Controls.Slider? vertical = FindVisualChildByName<Controls.Slider>(window, "VerticalTickSlider");

                Assert.IsNotNull(horizontal, "Inputs page should include a named horizontal slider with tick marks.");
                Assert.IsNotNull(vertical, "Inputs page should include a named vertical slider with tick marks.");
                Assert.AreNotEqual(System.Windows.Controls.Primitives.TickPlacement.None, horizontal.TickPlacement);
                Assert.AreNotEqual(System.Windows.Controls.Primitives.TickPlacement.None, vertical.TickPlacement);
                Assert.IsTrue(horizontal.TickFrequency > 0);
                Assert.IsTrue(vertical.TickFrequency > 0);
            });
        }

        [TestMethod]
        public void GalleryButtonsPage_RepeatButtonIncrementsNearbyCountText()
        {
            RunDemoPageTest(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.RepeatButton? button = FindVisualChildByName<Controls.RepeatButton>(window, "RepeatCounterButton");
                TextBlock? count = FindVisualChildByName<TextBlock>(window, "RepeatButtonCountText");
                Controls.RepeatButton? accentRepeat = FindRepeatButtonByContent(window, "Accent repeat");

                Assert.IsNotNull(button, "Buttons page should include a repeat button wired to a count label.");
                Assert.IsNotNull(count, "Buttons page should include a nearby repeat count text block.");
                Assert.IsNull(accentRepeat, "Buttons page should not include the extra Accent repeat sample.");
                Assert.AreEqual("Clicks: 0", count.Text);

                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                button.RaiseEvent(new RoutedEventArgs(System.Windows.Controls.Primitives.ButtonBase.ClickEvent));
                DrainDispatcher(window.Dispatcher);

                Assert.AreEqual("Clicks: 2", count.Text);
            });
        }

        [TestMethod]
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

                Assert.IsNotNull(twoState, "Selection page should include a two-state CheckBox example.");
                Assert.IsFalse(twoState.IsThreeState);
                Assert.IsNotNull(threeState, "Selection page should include a three-state CheckBox example.");
                Assert.IsTrue(threeState.IsThreeState);
                Assert.IsNotNull(selectAll, "Selection page should include a tri-state Select All CheckBox.");
                Assert.IsNotNull(optionOne);
                Assert.IsNotNull(optionTwo);
                Assert.IsNotNull(optionThree);

                selectAll.IsChecked = true;
                DrainDispatcher(window.Dispatcher);

                Assert.IsTrue(optionOne.IsChecked.GetValueOrDefault());
                Assert.IsTrue(optionTwo.IsChecked.GetValueOrDefault());
                Assert.IsTrue(optionThree.IsChecked.GetValueOrDefault());

                optionTwo.IsChecked = false;
                DrainDispatcher(window.Dispatcher);

                Assert.IsNull(selectAll.IsChecked,
                    "Select All should become indeterminate when only some child options are checked.");
            });
        }

        [TestMethod]
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

                Assert.IsNotNull(rating, "Selection page should include an editable RatingControl example.");
                Assert.IsNotNull(readOnlyRating, "Selection page should include a read-only RatingControl example.");
                Assert.AreEqual(1, CountVisualChildren<Controls.ToggleSwitch>(window),
                    "Selection page ToggleSwitch section should contain only the work ToggleSwitch sample.");
                Assert.IsNull(FindVisualChildByName<Controls.ToggleSwitch>(window, "SimpleToggleSwitch"),
                    "Selection page should remove the first simple ToggleSwitch sample.");
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "SimpleToggleStateText"),
                    "Selection page should remove the first simple ToggleSwitch state label.");
                Assert.IsNotNull(workHeader, "Selection page should include the Toggle work header text.");
                Assert.AreEqual("Toggle work", workHeader.Text);
                Assert.IsNotNull(workToggle, "Selection page should include the Toggle work ToggleSwitch.");
                Assert.IsNotNull(workLabel, "Selection page should include the Toggle work state label.");
                Assert.IsTrue(workToggle.IsChecked.GetValueOrDefault());
                Assert.AreEqual("On", workLabel.Text);
                Assert.IsNotNull(ring, "Selection page should include a ProgressRing bound to the Toggle work ToggleSwitch.");
                Assert.IsTrue(ring.IsIndeterminate);
                Assert.AreEqual(new Thickness(24, 0, 0, 0), ring.Margin);

                workToggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.IsFalse(ring.IsActive);

                workToggle.IsChecked = true;
                DrainDispatcher(window.Dispatcher);
                Assert.IsTrue(ring.IsActive);
            });
        }

        [TestMethod]
        public void GalleryTreesPage_IncludesMultipleSelectionTreeView()
        {
            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                Controls.TreeView? treeView = FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView");

                Assert.IsNotNull(treeView, "Trees page should include a TreeView with multi-select checkboxes.");
                Assert.AreEqual(TreeViewSelectionMode.Multiple, treeView.SelectionMode);
            });
        }

        [TestMethod]
        public void GalleryLayoutPage_ExpanderStartsCollapsed()
        {
            RunDemoPageTest(static () => new GalleryLayoutPage(), static window =>
            {
                Controls.Expander? expander = FindVisualChildByName<Controls.Expander>(window, "AdvancedOptionsExpander");

                Assert.IsNotNull(expander, "Layout page should expose the Advanced options Expander.");
                Assert.IsFalse(expander.IsExpanded, "Layout page Expander sample should be collapsed by default.");
            });
        }

        [TestMethod]
        public void GalleryDataPage_ListBoxSamplesExposeSelectionModes()
        {
            RunDemoPageTest(static () => new GalleryDataPage(), static window =>
            {
                Controls.ListBox? singleSelect = FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox");
                Controls.ListBox? multiSelect = FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox");

                Assert.IsNotNull(singleSelect, "Data page should include a single-selection ListBox sample.");
                Assert.IsNotNull(multiSelect, "Data page should include a multi-selection ListBox sample.");
                Assert.AreEqual(SelectionMode.Single, singleSelect.SelectionMode,
                    "The first ListBox sample should keep the default single selection mode.");
                Assert.AreEqual(SelectionMode.Extended, multiSelect.SelectionMode,
                    "The second ListBox sample should demonstrate extended multi-selection.");
                Assert.IsTrue(singleSelect.Items.Count > 0, "Single-selection ListBox sample should contain items.");
                Assert.IsTrue(multiSelect.SelectedItems.Count >= 2,
                    "Multi-selection ListBox sample should start with multiple items selected.");
            });
        }

        [TestMethod]
        public void GalleryDataAndTreeSamplesExposeThemedBorders()
        {
            RunDemoPageTest(static () => new GalleryDataPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "SimpleListView"), "SimpleListView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "RichListView"), "RichListView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListBox>(window, "SingleSelectListBox"), "SingleSelectListBox");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListBox>(window, "MultiSelectListBox"), "MultiSelectListBox");
            });

            RunDemoPageTest(static () => new GalleryDataBindingPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "BoundListView"), "BoundListView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "SelectionModeListView"), "SelectionModeListView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.ListView>(window, "DataTemplateListView"), "DataTemplateListView");
            });

            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "HierarchyTreeView"), "HierarchyTreeView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "SelectionTreeView"), "SelectionTreeView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "MultiSelectTreeView"), "MultiSelectTreeView");
                AssertControlHasThemedBorder(FindVisualChildByName<Controls.TreeView>(window, "ExpansionTreeView"), "ExpansionTreeView");
            });
        }

        private static void AssertControlHasThemedBorder(Control? control, string name)
        {
            Assert.IsNotNull(control, name + " should exist in the demo page.");
            Assert.AreEqual(new Thickness(1), control.BorderThickness, name + " should expose a visible 1px border.");
            Assert.IsNotNull(control.BorderBrush, name + " should use a themed BorderBrush.");
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
