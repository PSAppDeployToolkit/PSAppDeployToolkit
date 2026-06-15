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
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void GalleryButtonsPage_EnableCheckBoxControlsOnlyVisibleButtonVariants()
        {
            RunDemoPageTest(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.CheckBox? enable = FindVisualChildByName<Controls.CheckBox>(window, "ButtonEnableCheckBox");
                Controls.Button? standard = FindFluentButtonByContent(window, "Standard");
                Controls.Button? accent = FindFluentButtonByContent(window, "Accent");
                Controls.Button? subtle = FindFluentButtonByContent(window, "Subtle");
                Controls.Button? disabled = FindFluentButtonByContent(window, "Disabled");

                Assert.IsNotNull(enable, "Buttons page should expose the enable toggle in the right rail.");
                Assert.IsNotNull(standard, "Standard button sample should exist.");
                Assert.IsNotNull(accent, "Accent button sample should exist.");
                Assert.IsNotNull(subtle, "Subtle button sample should exist.");
                Assert.IsNull(disabled, "The explicit Disabled button should be removed from the first sample.");

                Assert.IsTrue(standard.IsEnabled);
                Assert.IsTrue(accent.IsEnabled);
                Assert.IsTrue(subtle.IsEnabled);

                enable.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.IsFalse(standard.IsEnabled, "Enable toggle should disable the Standard button.");
                Assert.IsFalse(accent.IsEnabled, "Enable toggle should disable the Accent button.");
                Assert.IsFalse(subtle.IsEnabled, "Enable toggle should disable the Subtle button.");
            });
        }

        [TestMethod]
        public void GalleryButtonsPage_SubtleButtonsUseWinUiTransparentRestBorderAndToggleButtonSampleIsRemoved()
        {
            RunDemoPageTest(static () => new GalleryButtonsPage(), static window =>
            {
                Controls.Button? subtle = FindFluentButtonByContent(window, "Subtle");
                Controls.Button? refresh = FindFluentButtonByContent(window, "Refresh");

                Assert.IsNotNull(subtle, "Subtle button sample should exist.");
                Assert.IsNotNull(refresh, "Refresh button sample should exist.");
                AssertBrushIsTransparent(subtle.BorderBrush, "Subtle button should use the WinUI transparent rest border.");
                AssertBrushIsTransparent(refresh.BorderBrush, "Refresh subtle button should use the WinUI transparent rest border.");
                Assert.IsNull(FindToggleButtonByContent(window, "Bold"),
                    "Buttons page should remove the Bold ToggleButton sample.");
                Assert.IsNull(FindToggleButtonByContent(window, "Pinned"),
                    "Buttons page should remove the Pinned ToggleButton sample.");
            });
        }

        [TestMethod]
        public void GalleryIconsPage_FontIconDescriptionIsUniqueAndSampleRowIsCentered()
        {
            RunDemoPageTest(static () => new GalleryIconsPage(), static window =>
            {
                List<TextBlock> duplicateDescriptions = [.. FindVisualChildren<TextBlock>(window)
                    .Where(static text => string.Equals(
                        text.Text,
                        "FontIcon uses glyph codes to render icons from the 'Segoe Fluent Icons' font.",
                        StringComparison.Ordinal))];
                Grid? sampleRow = FindVisualChildByName<Grid>(window, "FontIconSampleContent");

                Assert.AreEqual(1, duplicateDescriptions.Count,
                    "Icons page should show the FontIcon guidance sentence once.");
                Assert.IsNotNull(sampleRow, "Icons page should expose the FontIcon sample row.");
                Assert.AreEqual(VerticalAlignment.Center, sampleRow.VerticalAlignment,
                    "FontIcon sample row should be centered inside the sample surface.");
                Assert.IsTrue(sampleRow.MinHeight >= 48.0,
                    "FontIcon sample row should reserve enough height for vertical centering.");

                Controls.FontIcon? glyph = FindVisualChildren<Controls.FontIcon>(sampleRow)
                    .FirstOrDefault(static icon => string.Equals(icon.Glyph, "\uE713", StringComparison.Ordinal));
                TextBlock? label = FindVisualChildren<TextBlock>(sampleRow)
                    .FirstOrDefault(static text => string.Equals(text.Text, "Settings", StringComparison.Ordinal));

                Assert.IsNotNull(glyph, "Settings glyph should exist.");
                Assert.IsNotNull(label, "Settings text should exist.");
                Assert.AreEqual(VerticalAlignment.Center, glyph.VerticalAlignment,
                    "Settings glyph should be vertically centered.");
                Assert.AreEqual(VerticalAlignment.Center, label.VerticalAlignment,
                    "Settings text should be vertically centered.");
            });
        }

        [TestMethod]
        public void GalleryButtonsPage_DemoContentPresenterCentersButtonGroups()
        {
            RunDemoPageTest(static () => new GalleryButtonsPage(), static window =>
            {
                List<DemoSampleControl> samples = [.. FindVisualChildren<DemoSampleControl>(window)];
                Assert.IsTrue(samples.Count > 0, "Buttons page should render DemoSampleControl samples.");

                foreach (DemoSampleControl sample in samples)
                {
                    ContentPresenter? presenter = sample.FindName("DemoContentPresenter") as ContentPresenter;
                    Assert.IsNotNull(presenter, "DemoSampleControl should expose DemoContentPresenter.");
                    Assert.AreEqual(VerticalAlignment.Center, presenter.VerticalAlignment,
                        "Button sample content should be vertically centered in the sample surface.");
                    Assert.AreEqual(HorizontalAlignment.Stretch, presenter.HorizontalAlignment,
                        "Button sample content should keep equal horizontal room from the sample edges.");
                }
            });
        }

        [TestMethod]
        public void GallerySelectionPage_BasicRadioGroupStartsAtGroupLeftEdge()
        {
            RunDemoPageTest(static () => new GallerySelectionPage(), static window =>
            {
                Controls.RadioButton? optionA = FindRadioButtonByContent(window, "Option A");
                Controls.RadioButton? optionB = FindRadioButtonByContent(window, "Option B");
                Controls.RadioButton? optionC = FindRadioButtonByContent(window, "Option C");

                Assert.IsNotNull(optionA, "Basic radio option A should exist.");
                Assert.IsNotNull(optionB, "Basic radio option B should exist.");
                Assert.IsNotNull(optionC, "Basic radio option C should exist.");
                Assert.AreEqual(0.0, optionA.Margin.Left,
                    "The first Basic group radio button should align to the group label.");
                Assert.AreEqual(16.0, optionA.Margin.Right,
                    "Basic group radio buttons should keep a trailing gap.");
                Assert.AreEqual(0.0, optionB.Margin.Left,
                    "Subsequent Basic group radio buttons should use trailing gaps instead of left indentation.");
                Assert.AreEqual(0.0, optionC.Margin.Left,
                    "The final Basic group radio button should not be left-indented.");
            });
        }

        [TestMethod]
        public void GalleryDataBindingPage_AddItemRailIsWider()
        {
            RunDemoPageTest(static () => new GalleryDataBindingPage(), static window =>
            {
                Controls.TextBox? newItemBox = FindVisualChildByName<Controls.TextBox>(window, "NewItemBox");
                StackPanel? rightRailStack = newItemBox?.Parent as StackPanel;

                Assert.IsNotNull(newItemBox, "Data Binding add-item TextBox should exist.");
                Assert.IsNotNull(rightRailStack, "Data Binding add-item controls should live in a right rail stack.");
                Assert.AreEqual(320.0, rightRailStack.MinWidth, 0.1,
                    "Data Binding right rail should be widened by 100px.");
                Assert.AreEqual(320.0, newItemBox.Width, 0.1,
                    "Data Binding add-item TextBox should use the wider right rail width.");
            });
        }

        [TestMethod]
        public void GalleryNavigationPage_CompactSampleShowsBackAndPaneToggleButtons()
        {
            RunDemoPageTest(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView? compact = FindVisualChildByName<Controls.NavigationView>(window, "CompactNavigationDemo");
                Controls.CheckBox? backEnabled = FindVisualChildByName<Controls.CheckBox>(window, "BackEnabledToggle");

                Assert.IsNotNull(compact, "Navigation page should expose CompactNavigationDemo.");
                Assert.IsNotNull(backEnabled, "Navigation compact sample should expose the back-enabled toggle.");
                Assert.IsTrue(backEnabled.IsChecked.GetValueOrDefault(),
                    "Compact navigation sample should start with the back button enabled.");
                Assert.IsTrue(compact.IsPaneToggleButtonVisible,
                    "Compact navigation sample should explicitly show the pane toggle button.");

                Button? back = compact.Template.FindName(Controls.NavigationView.PartBackButton, compact) as Button;
                Button? paneToggle = compact.Template.FindName(Controls.NavigationView.PartPaneToggleButton, compact) as Button;
                Assert.IsNotNull(back, "Compact NavigationView template should expose PART_BackButton.");
                Assert.IsNotNull(paneToggle, "Compact NavigationView template should expose PART_PaneToggleButton.");
                Assert.AreEqual(Visibility.Visible, back.Visibility,
                    "Compact navigation sample should show the back button.");
                Assert.AreEqual(Visibility.Visible, paneToggle.Visibility,
                    "Compact navigation sample should show the pane toggle/collapse button.");
                Assert.IsNull(FindVisualChildByName<Controls.Button>(window, "CompactPaneToggleButton"),
                    "Compact navigation sample should use NavigationView's built-in pane toggle button.");

                Assert.IsFalse(compact.IsPaneOpen,
                    "Compact navigation sample should start with the compact pane closed.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                DrainDispatcher(window.Dispatcher);
                Assert.IsTrue(compact.IsPaneOpen,
                    "The built-in pane toggle should open the compact pane.");
                paneToggle.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent, paneToggle));
                DrainDispatcher(window.Dispatcher);
                Assert.IsFalse(compact.IsPaneOpen,
                    "The built-in pane toggle should close the compact pane on subsequent clicks.");
            });
        }

        [TestMethod]
        public void GalleryFormsPage_ActionsAlignAndOutputHasStableSpace()
        {
            RunDemoPageTest(static () => new GalleryFormsPage(), static window =>
            {
                Controls.Button? signIn = FindVisualChildByName<Controls.Button>(window, "SignInButton");
                StackPanel? checkoutButtons = FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel");
                Controls.Button? placeOrder = checkoutButtons?.Children.OfType<Controls.Button>().FirstOrDefault();
                List<Border> outputRegions = [.. FindVisualChildren<DemoSampleControl>(window)
                    .Select(static sample => sample.FindName("OutputRegion") as Border)
                    .Where(static border => border is not null)
                    .Cast<Border>()];

                Assert.IsNotNull(signIn, "Forms sign-in sample should expose SignInButton.");
                Assert.IsNotNull(placeOrder, "Forms checkout sample should expose the primary checkout button.");
                Assert.AreEqual(0.0, signIn.Margin.Left,
                    "First sign-in action should align with the form fields.");
                Assert.AreEqual(0.0, placeOrder.Margin.Left,
                    "First checkout action should align with the form fields.");
                Assert.IsTrue(outputRegions.Count > 0, "Forms page should expose output regions.");
                Assert.IsTrue(outputRegions.TrueForAll(static region => region.MinWidth >= 220.0),
                    "Output regions should reserve enough room for status text.");
            });
        }

        [TestMethod]
        public void GalleryPages_RemoveRequestedOutputRegions()
        {
            RunDemoPageTest(static () => new GalleryInputsPage(), static window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "CharCountLabel"),
                    "Inputs TextBox sample should no longer expose a character-count output.");
            });

            RunDemoPageTest(static () => new GalleryDataBindingPage(), static window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "ItemCountLabel"),
                    "DataBinding first sample should no longer expose an item-count output.");
            });

            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "TreeSelectionLabel"),
                    "Trees second sample should no longer expose a selection output.");
            });

            RunDemoPageTest(static () => new GalleryNavigationPage(), static window =>
            {
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "CompactNavigationOutputText"),
                    "Navigation compact sample should no longer expose an output text region.");
            });
        }

        [TestMethod]
        public void GalleryStatusPage_NumberBoxDrivesFirstProgressBar()
        {
            RunDemoPageTest(static () => new GalleryStatusPage(), static window =>
            {
                Controls.NumberBox? numberBox = FindVisualChildByName<Controls.NumberBox>(window, "ProgressValueNumberBox");
                Controls.ProgressBar? progressBar = FindVisualChildByName<Controls.ProgressBar>(window, "StandardProgressBar");
                Controls.ToggleSwitch? indeterminateToggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "IndeterminateToggle");
                Controls.NumberBox? progressRingValueBox = FindVisualChildByName<Controls.NumberBox>(window, "ProgressRingValueBox");

                Assert.IsNotNull(numberBox, "Status page should use a NumberBox for the first ProgressBar value.");
                Assert.IsNotNull(progressBar, "Status page should expose the first ProgressBar.");
                Assert.IsNotNull(indeterminateToggle, "Status page should expose the indeterminate ProgressBar toggle.");
                Assert.IsNotNull(progressRingValueBox, "Status page should expose the determinate ProgressRing NumberBox.");
                Assert.AreEqual(HorizontalAlignment.Center, numberBox.HorizontalAlignment,
                    "Progress value NumberBox should be centered in the right rail.");
                Assert.AreEqual(VerticalAlignment.Center, numberBox.VerticalAlignment,
                    "Progress value NumberBox should be vertically centered in the right rail.");
                Assert.AreEqual(HorizontalAlignment.Center, indeterminateToggle.HorizontalAlignment,
                    "Indeterminate toggle should be centered in the right rail.");
                Assert.AreEqual(VerticalAlignment.Center, indeterminateToggle.VerticalAlignment,
                    "Indeterminate toggle should be vertically centered in the right rail.");
                Assert.AreEqual(HorizontalAlignment.Center, progressRingValueBox.HorizontalAlignment,
                    "ProgressRing value NumberBox should remain centered.");
                Assert.AreEqual("On / Off", indeterminateToggle.OnContent as string,
                    "Indeterminate toggle text should not switch to a state-specific label.");
                Assert.AreEqual("On / Off", indeterminateToggle.OffContent as string,
                    "Indeterminate toggle text should not switch to a state-specific label.");
                Assert.AreEqual(0d, numberBox.Minimum, "Progress value NumberBox should allow the ProgressBar's empty state.");
                Assert.AreEqual(100d, numberBox.Maximum, "Progress value NumberBox should cap at 100.");
                Assert.IsNull(FindVisualChildByName<Controls.Slider>(window, "ProgressSlider"),
                    "The first ProgressBar should no longer be driven by a Slider.");
                Assert.IsNull(FindVisualChildByName<TextBlock>(window, "SliderValueLabel"),
                    "The first ProgressBar should no longer show an output value label.");

                numberBox.Value = 73d;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.AreEqual(73d, progressBar.Value, 0.1,
                    "Changing the NumberBox should update the first ProgressBar.");

                numberBox.Value = 0d;
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                Assert.AreEqual(0d, progressBar.Value, 0.1,
                    "Changing the NumberBox to 0 should show the ProgressBar empty state.");

                indeterminateToggle.IsChecked = false;
                DrainDispatcher(window.Dispatcher);
                Assert.AreEqual("On / Off", indeterminateToggle.OffContent as string,
                    "Indeterminate toggle text should remain fixed after toggling off.");
            });
        }

        [TestMethod]
        public void GalleryFormsPage_CheckoutFieldsUseStableNamesAndAlignOptionalInput()
        {
            RunDemoPageTest(static () => new GalleryFormsPage(), static window =>
            {
                Grid? checkoutGrid = FindVisualChildByName<Grid>(window, "CheckoutFieldsGrid");
                Controls.NumberBox? quantity = FindVisualChildByName<Controls.NumberBox>(window, "QuantityNumberBox");
                Controls.TextBox? optional = FindVisualChildByName<Controls.TextBox>(window, "OptionalTextBox");
                Controls.CheckBox? gift = FindVisualChildByName<Controls.CheckBox>(window, "GiftCheckBox");
                StackPanel? actions = FindVisualChildByName<StackPanel>(window, "CheckoutButtonsPanel");

                Assert.IsNotNull(checkoutGrid, "Checkout sample should expose the quantity/options grid.");
                Assert.IsNotNull(quantity, "Checkout sample should expose the Quantity NumberBox.");
                Assert.IsNotNull(optional, "Checkout sample should expose the Optional TextBox.");
                Assert.IsNotNull(gift, "Checkout sample should expose the gift CheckBox.");
                Assert.IsNotNull(actions, "Checkout sample should expose the action button row.");
                Assert.AreEqual(3, checkoutGrid.ColumnDefinitions.Count,
                    "Checkout field grid should preserve the quantity, spacer, and optional columns.");
                Assert.AreEqual(0, Grid.GetColumn(quantity),
                    "Quantity NumberBox should remain in the first column.");
                Assert.AreEqual(2, Grid.GetColumn(optional),
                    "Optional TextBox should remain in the aligned right column.");
                Assert.AreEqual(VerticalAlignment.Bottom, optional.VerticalAlignment,
                    "Optional TextBox should align with the Quantity input row.");
            });
        }

        [TestMethod]
        public void GalleryDataPage_ListBackgroundsAndPersonPicturesUseExpectedAssets()
        {
            RunDemoPageTest(static () => new GalleryDataPage(), static window =>
            {
                Border? simpleBackground = FindVisualChildByName<Border>(window, "SimpleListViewBackground");
                Border? richBackground = FindVisualChildByName<Border>(window, "RichListViewBackground");
                StackPanel? emptyStateActions = FindVisualChildByName<StackPanel>(window, "EmptyStateActionsPanel");

                Assert.IsNotNull(simpleBackground, "Simple ListView sample should have a named background wrapper.");
                Assert.IsNotNull(richBackground, "Rich ListView sample should have a named background wrapper.");
                Assert.IsNotNull(emptyStateActions, "EmptyContent sample should expose the action button panel.");
                Assert.AreEqual(HorizontalAlignment.Center, emptyStateActions.HorizontalAlignment,
                    "EmptyContent action buttons should be horizontally centered.");
                Assert.AreEqual(VerticalAlignment.Center, emptyStateActions.VerticalAlignment,
                    "EmptyContent action buttons should be vertically centered.");
                Assert.IsTrue(emptyStateActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "EmptyContent action buttons should be wider than the default compact command width.");

                List<Controls.PersonPicture> personPictures = [.. FindVisualChildren<Controls.PersonPicture>(window)];
                WrapPanel? personPicturePanel = personPictures.FirstOrDefault()?.Parent as WrapPanel;
                Assert.AreEqual(5, personPictures.Count,
                    "PersonPicture sample should be reduced to five people.");
                Assert.AreEqual(5, personPictures.Count(static picture => picture.ProfilePicture is not null),
                    "PersonPicture sample should show five image-backed portraits.");
                Assert.IsNotNull(personPicturePanel, "PersonPicture sample should render in a WrapPanel.");
                Assert.AreEqual(HorizontalAlignment.Center, personPicturePanel.HorizontalAlignment,
                    "PersonPicture sample should be horizontally centered.");
                Assert.AreEqual(VerticalAlignment.Center, personPicturePanel.VerticalAlignment,
                    "PersonPicture sample should be vertically centered.");
                Assert.IsTrue(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureMadisonButler.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should include the Madison Butler portrait asset.");
                Assert.IsFalse(personPictures.Exists(static picture => picture.ProfilePicture?.ToString(CultureInfo.InvariantCulture).IndexOf("PersonPictureOscarWard.png", StringComparison.Ordinal) >= 0),
                    "PersonPicture sample should remove the extra Oscar Ward portrait.");
                Assert.IsFalse(personPictures.Exists(static picture => !string.IsNullOrWhiteSpace(picture.Initials)),
                    "PersonPicture sample should remove the initials fallback entry.");
                Assert.IsFalse(personPictures.Exists(static picture => picture.IsGroup),
                    "PersonPicture sample should remove the invalid group glyph entry.");
            });
        }

        [TestMethod]
        public void GalleryPages_RightRailControlsUseRequestedAlignment()
        {
            RunDemoPageTest(static () => new GalleryDataBindingPage(), static window =>
            {
                StackPanel? selectionRail = FindVisualChildByName<StackPanel>(window, "SelectionModeRail");
                Assert.IsNotNull(selectionRail, "Data Binding selection sample should expose its right rail.");
                Assert.AreEqual(VerticalAlignment.Center, selectionRail.VerticalAlignment,
                    "Data Binding selection options should be vertically centered.");
            });

            RunDemoPageTest(static () => new GalleryTreesPage(), static window =>
            {
                StackPanel? treeExpansionActions = FindVisualChildByName<StackPanel>(window, "TreeExpansionActionsPanel");
                Assert.IsNotNull(treeExpansionActions, "Tree expansion sample should expose the action button panel.");
                Assert.AreEqual(HorizontalAlignment.Center, treeExpansionActions.HorizontalAlignment,
                    "Tree expansion buttons should be horizontally centered.");
                Assert.AreEqual(VerticalAlignment.Center, treeExpansionActions.VerticalAlignment,
                    "Tree expansion buttons should be vertically centered.");
                Assert.IsTrue(treeExpansionActions.Children.OfType<Controls.Button>().All(static button => button.MinWidth >= 140.0),
                    "Tree expansion buttons should be wider than the default compact command width.");
            });

            RunDemoPageTest(static () => new GalleryAccessibilityPage(), static window =>
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
                    Controls.Button? button = FindVisualChildByName<Controls.Button>(window, buttonName);
                    Assert.IsNotNull(button, "Automation properties sample should expose " + buttonName + ".");
                    Assert.AreEqual(36.0, button.Width, 0.1,
                        "Automation properties icon buttons should use a square width.");
                    Assert.AreEqual(36.0, button.Height, 0.1,
                        "Automation properties icon buttons should use a square height.");
                    Assert.AreEqual(36.0, button.MinWidth, 0.1,
                        "Automation properties icon buttons should avoid the default text button MinWidth.");
                    Assert.AreEqual(0.0, button.Padding.Left, 0.1,
                        "Automation properties icon buttons should center the glyph within the focus visual.");
                }
            });
        }

        [TestMethod]
        public void GalleryNavigationPage_IconsAreDefaultSizeAndInfoBadgePaneStartsExpanded()
        {
            RunDemoPageTest(static () => new GalleryNavigationPage(), static window =>
            {
                Controls.NavigationView? leftNavigation = FindVisualChildByName<Controls.NavigationView>(window, "LeftNavigationDemo");
                Assert.IsNotNull(leftNavigation, "Navigation page should expose the left mode sample.");

                List<Controls.FontIcon> leftIcons = [.. FindVisualChildren<Controls.FontIcon>(leftNavigation)];
                Assert.IsTrue(leftIcons.Count >= 3, "Left navigation sample should expose item icons.");
                Assert.IsTrue(leftIcons.TrueForAll(static icon => Math.Abs(icon.IconFontSize - 16d) < 0.1),
                    "NavigationView item icons should align with the compact pane glyph size.");

                Controls.NavigationView? badgeNavigation = FindVisualChildren<Controls.NavigationView>(window)
                    .FirstOrDefault(static nav => string.Equals(nav.Header as string, "Inbox", StringComparison.Ordinal));
                Assert.IsNotNull(badgeNavigation, "Navigation page should expose the InfoBadge NavigationView sample.");
                Assert.AreEqual(NavigationViewPaneDisplayMode.Left, badgeNavigation.PaneDisplayMode,
                    "InfoBadge NavigationView sample should start expanded.");
                Assert.IsTrue(badgeNavigation.IsPaneOpen,
                    "InfoBadge NavigationView sample should keep the pane open.");
            });
        }

        [TestMethod]
        public void GalleryTabsPage_PlacementSampleUsesLeftPlacementOnly()
        {
            RunDemoPageTest(static () => new GalleryTabsPage(), static window =>
            {
                Dictionary<string, TabItem> items = FindVisualChildren<TabItem>(window)
                    .Where(static item => item.Header is string)
                    .ToDictionary(static item => (string)item.Header, StringComparer.Ordinal);

                double infoWidth = GetExplicitHeaderWidth(items, "Inbox");
                double archiveWidth = GetExplicitHeaderWidth(items, "Archive");

                Assert.AreEqual(infoWidth, archiveWidth, 0.1);
                Assert.IsTrue(infoWidth > 0.0, "Placement sample tab headers should use an explicit shared width.");

                TabControl? leftTabs = FindVisualChildByName<TabControl>(window, "LeftPlacementTabs");
                Assert.IsNotNull(leftTabs, "Placement sample should keep the left TabControl.");
                Assert.AreEqual(Dock.Left, leftTabs.TabStripPlacement,
                    "Placement sample should demonstrate the left-hand TabStripPlacement.");

                TabControl? bottomTabs = FindVisualChildByName<TabControl>(window, "BottomPlacementTabs");
                Assert.IsNull(bottomTabs, "Placement sample should not include the removed bottom-row TabControl.");
            });
        }

        [TestMethod]
        public void GalleryLayoutPage_SeparatesStructuralPrimitiveSamples()
        {
            RunDemoPageTest(static () => new GalleryLayoutPage(), static window =>
            {
                List<string> descriptions = [.. FindVisualChildren<DemoSampleControl>(window).Select(static sample => sample.SampleDescription)];

                Assert.IsTrue(descriptions.Exists(static description => description.IndexOf("Separator", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated Separator DemoSampleControl.");
                Assert.IsTrue(descriptions.Exists(static description => description.IndexOf("DockPanel", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated DockPanel DemoSampleControl.");
                Assert.IsTrue(descriptions.Exists(static description => description.IndexOf("Expander", StringComparison.OrdinalIgnoreCase) >= 0),
                    "Layout page should have a dedicated Expander DemoSampleControl.");

                Controls.Expander? dockPanelExpander = FindVisualChildByName<Controls.Expander>(window, "DockPanelOptionsExpander");
                Assert.IsNotNull(dockPanelExpander, "Layout page should expose the DockPanel Expander sample.");
                Assert.IsInstanceOfType(dockPanelExpander.Header, typeof(DockPanel),
                    "DockPanel Expander sample should use DockPanel in the collapsed header.");
                Assert.IsInstanceOfType(dockPanelExpander.Content, typeof(DockPanel),
                    "DockPanel Expander sample should use DockPanel in the expanded content.");
            });
        }

        [TestMethod]
        public void GalleryAccessibilityPage_RtlSampleDefaultsOn()
        {
            RunDemoPageTest(static () => new GalleryAccessibilityPage(), static window =>
            {
                Controls.ToggleSwitch? toggle = FindVisualChildByName<Controls.ToggleSwitch>(window, "RtlToggle");
                Controls.Card? card = FindVisualChildByName<Controls.Card>(window, "RtlDemoCard");

                Assert.IsNotNull(toggle, "Accessibility page should expose the RTL toggle.");
                Assert.IsNotNull(card, "Accessibility page should expose the RTL demo card.");
                Assert.IsTrue(toggle.IsChecked.GetValueOrDefault(),
                    "RTL sample should default to On.");
                Assert.AreEqual(FlowDirection.RightToLeft, card.FlowDirection,
                    "RTL demo card should default to RightToLeft.");
            });
        }

        private static double GetExplicitHeaderWidth(IDictionary<string, TabItem> items, string header)
        {
            Assert.IsTrue(items.TryGetValue(header, out TabItem? item), "TabItem should exist: " + header);
            return double.IsNaN(item.Width) ? item.MinWidth : item.Width;
        }

        private static void AssertBrushIsTransparent(Brush? brush, string message)
        {
            Assert.IsNotNull(brush, message);
            if (brush is SolidColorBrush solid)
            {
                Assert.AreEqual(0, solid.Color.A, message);
            }
        }

        private static Controls.ToggleButton? FindToggleButtonByContent(DependencyObject root, string content)
        {
            foreach (Controls.ToggleButton button in FindVisualChildren<Controls.ToggleButton>(root))
            {
                if (string.Equals(button.Content as string, content, StringComparison.Ordinal))
                {
                    return button;
                }
            }

            return null;
        }

        private static Controls.RadioButton? FindRadioButtonByContent(DependencyObject root, string content)
        {
            foreach (Controls.RadioButton radioButton in FindVisualChildren<Controls.RadioButton>(root))
            {
                if (string.Equals(radioButton.Content as string, content, StringComparison.Ordinal))
                {
                    return radioButton;
                }
            }

            return null;
        }
    }
}
