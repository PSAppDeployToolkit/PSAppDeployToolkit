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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using WpfButton = System.Windows.Controls.Button;
using WpfButtonBase = System.Windows.Controls.Primitives.ButtonBase;
using WpfOrientation = System.Windows.Controls.Orientation;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfToggleButton = System.Windows.Controls.Primitives.ToggleButton;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.PipsPager"/>.
    /// </summary>
    public partial class ControlTests
    {
        private static WpfToggleButton? GetPipAt(WpfStackPanel host, int offset)
        {
            return offset >= 0 && offset < host.Children.Count
                ? host.Children[offset] as WpfToggleButton
                : null;
        }

        [TestMethod]
        public void PipsPager_DefaultStyle_AppliesAndTemplatePartsResolve()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.PipsPager)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.PipsPager.");

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new();

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(0, pager.NumberOfPages, "NumberOfPages must default to 0.");
                    Assert.AreEqual(0, pager.SelectedPageIndex, "SelectedPageIndex must default to 0.");
                    Assert.AreEqual(5, pager.MaxVisiblePips, "MaxVisiblePips must default to 5, matching WinUI.");
                    Assert.AreEqual(WpfOrientation.Horizontal, pager.Orientation,
                        "Orientation must default to Horizontal.");
                    Assert.AreEqual(PipsPagerButtonVisibility.Collapsed, pager.PreviousButtonVisibility,
                        "PreviousButtonVisibility must default to Collapsed, matching WinUI.");
                    Assert.AreEqual(PipsPagerButtonVisibility.Collapsed, pager.NextButtonVisibility,
                        "NextButtonVisibility must default to Collapsed, matching WinUI.");

                    WpfButton? previous = FindVisualChildByName<WpfButton>(pager, "PART_PreviousButton");
                    WpfButton? next = FindVisualChildByName<WpfButton>(pager, "PART_NextButton");
                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(previous, "PART_PreviousButton must be present in the PipsPager template.");
                    Assert.IsNotNull(next, "PART_NextButton must be present in the PipsPager template.");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");
                    Assert.AreEqual(0, host.Children.Count, "An empty pager must not realize any pips.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_FivePages_RendersFivePips()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");
                    Assert.AreEqual(5, host.Children.Count, "NumberOfPages=5 must render five pips.");

                    for (int offset = 0; offset < 5; offset++)
                    {
                        WpfToggleButton? pip = GetPipAt(host, offset);
                        Assert.IsNotNull(pip,
                            string.Format("The pip at offset {0} must be a ToggleButton.", offset));
                        Assert.AreEqual(offset == 0, pip.IsChecked,
                            string.Format("Only the selected (first) pip must be checked; offset {0}.", offset));
                        Assert.AreEqual(
                            string.Format("Page {0}", offset + 1),
                            AutomationProperties.GetName(pip),
                            string.Format("The pip at offset {0} must carry its accessible page name.", offset));
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_PipClick_SelectsPageAndRaisesSelectedIndexChanged()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    int oldIndex = -1;
                    int newIndex = -1;
                    int raiseCount = 0;
                    pager.SelectedIndexChanged += (_, args) =>
                    {
                        oldIndex = args.OldIndex;
                        newIndex = args.NewIndex;
                        raiseCount++;
                    };

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");

                    WpfToggleButton? pip = GetPipAt(host, 3);
                    Assert.IsNotNull(pip, "The pip at offset 3 must be a ToggleButton.");

                    pip.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, pip));
                    Assert.AreEqual(3, pager.SelectedPageIndex, "Clicking pip 3 must select page index 3.");
                    Assert.AreEqual(1, raiseCount, "Clicking a pip must raise SelectedIndexChanged once.");
                    Assert.AreEqual(0, oldIndex, "SelectedIndexChanged must carry the previous index.");
                    Assert.AreEqual(3, newIndex, "SelectedIndexChanged must carry the new index.");

                    Assert.IsTrue(GetPipAt(host, 3)?.IsChecked,
                        "The clicked pip must render as the selected pip.");
                    Assert.IsFalse(GetPipAt(host, 0)?.IsChecked,
                        "The previously selected pip must uncheck.");

                    // Re-clicking the selected pip must not move the selection or re-raise.
                    pip.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, pip));
                    Assert.AreEqual(3, pager.SelectedPageIndex,
                        "Re-clicking the selected pip must keep its page selected.");
                    Assert.AreEqual(1, raiseCount,
                        "Re-clicking the selected pip must not raise SelectedIndexChanged again.");
                    Assert.IsTrue(GetPipAt(host, 3)?.IsChecked,
                        "Re-clicking the selected pip must keep it checked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_NavigationButtons_ChangeSelectionAndRespectBounds()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfButton? previous = FindVisualChildByName<WpfButton>(pager, "PART_PreviousButton");
                    WpfButton? next = FindVisualChildByName<WpfButton>(pager, "PART_NextButton");
                    Assert.IsNotNull(previous, "PART_PreviousButton must be present in the PipsPager template.");
                    Assert.IsNotNull(next, "PART_NextButton must be present in the PipsPager template.");

                    Assert.IsFalse(previous.IsEnabled, "The previous button must be disabled at the first page.");
                    Assert.IsTrue(next.IsEnabled, "The next button must be enabled while pages remain ahead.");

                    next.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, next));
                    Assert.AreEqual(1, pager.SelectedPageIndex, "Clicking next must advance the selection.");
                    Assert.IsTrue(previous.IsEnabled, "The previous button must enable once off the first page.");

                    next.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, next));
                    Assert.AreEqual(2, pager.SelectedPageIndex, "Clicking next again must reach the last page.");
                    Assert.IsFalse(next.IsEnabled, "The next button must be disabled at the last page.");

                    // Raising Click bypasses IsEnabled, so this also proves the coercion clamp.
                    next.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, next));
                    Assert.AreEqual(2, pager.SelectedPageIndex,
                        "Advancing past the last page must clamp the selection.");

                    previous.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, previous));
                    Assert.AreEqual(1, pager.SelectedPageIndex, "Clicking previous must step the selection back.");

                    previous.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, previous));
                    previous.RaiseEvent(new RoutedEventArgs(WpfButtonBase.ClickEvent, previous));
                    Assert.AreEqual(0, pager.SelectedPageIndex,
                        "Stepping back past the first page must clamp the selection.");
                    Assert.IsFalse(previous.IsEnabled,
                        "The previous button must be disabled again at the first page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_MaxVisiblePips_WindowsAroundSelection()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");

                    // Selection at the leading edge: the window clamps to the first pages.
                    Assert.AreEqual(3, host.Children.Count, "MaxVisiblePips=3 must realize three pips.");
                    Assert.AreEqual("Page 1", AutomationProperties.GetName(GetPipAt(host, 0)!),
                        "At the first page the window must start at page 1.");
                    Assert.IsTrue(GetPipAt(host, 0)?.IsChecked, "The first pip must be checked at page 1.");

                    // Mid-range selection: the window centers on the selected page.
                    pager.SelectedPageIndex = 5;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(3, host.Children.Count, "The window must stay at three pips mid-range.");
                    Assert.AreEqual("Page 5", AutomationProperties.GetName(GetPipAt(host, 0)!),
                        "A mid-range selection must center the window (pages 5..7 around page 6).");
                    Assert.AreEqual("Page 7", AutomationProperties.GetName(GetPipAt(host, 2)!),
                        "A mid-range selection must center the window (pages 5..7 around page 6).");
                    Assert.IsTrue(GetPipAt(host, 1)?.IsChecked,
                        "The centered window must check its middle pip.");

                    // Selection at the trailing edge: the window clamps to the last pages.
                    pager.SelectedPageIndex = 9;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(3, host.Children.Count, "The window must stay at three pips at the end.");
                    Assert.AreEqual("Page 8", AutomationProperties.GetName(GetPipAt(host, 0)!),
                        "At the last page the window must clamp to the final pages.");
                    Assert.AreEqual("Page 10", AutomationProperties.GetName(GetPipAt(host, 2)!),
                        "At the last page the window must end at the final page.");
                    Assert.IsTrue(GetPipAt(host, 2)?.IsChecked,
                        "The clamped window must check its last pip for the last page.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_SelectedPageIndex_CoercesIntoRange()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.PipsPager pager = new() { NumberOfPages = 5 };
                Assert.AreEqual(0, pager.SelectedPageIndex, "The selection must start at the first page.");

                pager.SelectedPageIndex = -3;
                Assert.AreEqual(0, pager.SelectedPageIndex, "Negative indices must coerce to 0.");

                pager.SelectedPageIndex = 99;
                Assert.AreEqual(4, pager.SelectedPageIndex,
                    "Indices at or beyond NumberOfPages must coerce to the last page.");

                int oldIndex = -1;
                int newIndex = -1;
                pager.SelectedIndexChanged += (_, args) =>
                {
                    oldIndex = args.OldIndex;
                    newIndex = args.NewIndex;
                };

                pager.NumberOfPages = 3;
                Assert.AreEqual(2, pager.SelectedPageIndex,
                    "Shrinking NumberOfPages must re-coerce the selection to the new last page.");
                Assert.AreEqual(4, oldIndex, "The re-coercion must raise SelectedIndexChanged with the old index.");
                Assert.AreEqual(2, newIndex, "The re-coercion must raise SelectedIndexChanged with the new index.");

                pager.NumberOfPages = 0;
                Assert.AreEqual(0, pager.SelectedPageIndex,
                    "An empty pager must coerce the selection to 0.");

                pager.NumberOfPages = -7;
                Assert.AreEqual(0, pager.NumberOfPages, "Negative page counts must coerce to 0.");
            });
        }

        [TestMethod]
        public void PipsPager_VerticalOrientation_StacksPipsVerticallyAndSwapsChevrons()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 400 };

                // The chevron FontIcons are button content, so they only enter the visual tree
                // once the navigation buttons are visible and have applied their templates.
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Controls.FontIcon? previousGlyph = FindVisualChildByName<Controls.FontIcon>(pager, "PreviousGlyph");
                    Controls.FontIcon? nextGlyph = FindVisualChildByName<Controls.FontIcon>(pager, "NextGlyph");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");
                    Assert.IsNotNull(previousGlyph, "The previous chevron FontIcon must be present.");
                    Assert.IsNotNull(nextGlyph, "The next chevron FontIcon must be present.");

                    Assert.AreEqual(WpfOrientation.Horizontal, host.Orientation,
                        "The default pager must stack its pips horizontally.");
                    Assert.AreEqual("\uE76B", previousGlyph.Glyph,
                        "The horizontal previous chevron must be ChevronLeft (E76B).");
                    Assert.AreEqual("\uE76C", nextGlyph.Glyph,
                        "The horizontal next chevron must be ChevronRight (E76C).");

                    pager.Orientation = WpfOrientation.Vertical;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(WpfOrientation.Vertical, host.Orientation,
                        "A vertical pager must stack its pips vertically.");
                    Assert.AreEqual("\uE70E", previousGlyph.Glyph,
                        "The vertical previous chevron must be ChevronUp (E70E).");
                    Assert.AreEqual("\uE70D", nextGlyph.Glyph,
                        "The vertical next chevron must be ChevronDown (E70D).");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_ButtonVisibilityEnum_ControlsNavigationButtonVisibility()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfButton? previous = FindVisualChildByName<WpfButton>(pager, "PART_PreviousButton");
                    WpfButton? next = FindVisualChildByName<WpfButton>(pager, "PART_NextButton");
                    Assert.IsNotNull(previous, "PART_PreviousButton must be present in the PipsPager template.");
                    Assert.IsNotNull(next, "PART_NextButton must be present in the PipsPager template.");

                    Assert.AreEqual(Visibility.Collapsed, previous.Visibility,
                        "The previous button must default to Collapsed, matching WinUI.");
                    Assert.AreEqual(Visibility.Collapsed, next.Visibility,
                        "The next button must default to Collapsed, matching WinUI.");

                    pager.PreviousButtonVisibility = PipsPagerButtonVisibility.Visible;
                    pager.NextButtonVisibility = PipsPagerButtonVisibility.Visible;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, previous.Visibility,
                        "PipsPagerButtonVisibility.Visible must show the previous button.");
                    Assert.AreEqual(Visibility.Visible, next.Visibility,
                        "PipsPagerButtonVisibility.Visible must show the next button.");

                    // VisibleOnPointerOver shows the buttons only while the pointer is over the
                    // pager (template MultiTrigger on IsMouseOver); without hover they collapse.
                    pager.PreviousButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    pager.NextButtonVisibility = PipsPagerButtonVisibility.VisibleOnPointerOver;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(pager.IsMouseOver, "The pager must not be hovered in this headless test.");
                    Assert.AreEqual(Visibility.Collapsed, previous.Visibility,
                        "VisibleOnPointerOver must keep the previous button collapsed without hover.");
                    Assert.AreEqual(Visibility.Collapsed, next.Visibility,
                        "VisibleOnPointerOver must keep the next button collapsed without hover.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_ArrowKeys_MoveSelectionWhileFocusIsInside()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 5 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");

                    WpfToggleButton? pip = GetPipAt(host, 0);
                    Assert.IsNotNull(pip, "The pip at offset 0 must be a ToggleButton.");
                    _ = pip.Focus();

                    PresentationSource? source = PresentationSource.FromVisual(pip);
                    Assert.IsNotNull(source, "The pip must have a presentation source once the window is shown.");

                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Right)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.AreEqual(1, pager.SelectedPageIndex,
                        "Right arrow inside the pager must advance the selection.");

                    pip = GetPipAt(host, 1);
                    Assert.IsNotNull(pip, "The pip at offset 1 must be a ToggleButton.");
                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.AreEqual(0, pager.SelectedPageIndex,
                        "Left arrow inside the pager must step the selection back.");

                    pip = GetPipAt(host, 0);
                    Assert.IsNotNull(pip, "The pip at offset 0 must be a ToggleButton.");
                    pip.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Left)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.AreEqual(0, pager.SelectedPageIndex,
                        "Left arrow at the first page must clamp the selection.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_ThemeCycle_PipBrushesResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "ControlStrongFillColorDefaultBrush",
                    "ControlStrongFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in PipsPager theme cycle step: {1}", key, theme));
                    }
                }
            });
        }

        [TestMethod]
        public void PipsPager_PipFills_UseNeutralStrongFillRoles()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new()
                {
                    NumberOfPages = 3,
                    PreviousButtonVisibility = PipsPagerButtonVisibility.Visible,
                    NextButtonVisibility = PipsPagerButtonVisibility.Visible,
                };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");

                    object? strongFill = app?.TryFindResource("ControlStrongFillColorDefaultBrush");
                    Assert.IsNotNull(strongFill, "ControlStrongFillColorDefaultBrush must resolve.");

                    // WinUI maps PipsPagerNavigationButtonForeground at rest to
                    // ControlStrongFillColorDefaultBrush; the chevron buttons must share the same
                    // neutral strong fill as the pips when not hovered or pressed. The next button
                    // is enabled at the first page, so its Foreground reflects the rest setter
                    // (the previous button is disabled at page 0 and shows the disabled brush).
                    WpfButton? nextButton = FindVisualChildByName<WpfButton>(pager, "PART_NextButton");
                    Assert.IsNotNull(nextButton, "PART_NextButton must be present in the PipsPager template.");
                    Assert.IsTrue(nextButton.IsEnabled, "The next button must be enabled at the first page.");
                    Assert.AreSame(strongFill, nextButton.Foreground,
                        "The navigation button rest Foreground must be ControlStrongFillColorDefaultBrush.");

                    // WinUI maps PipsPagerNavigationButtonForegroundDisabled to
                    // ControlStrongFillColorDisabledBrush (not the text disabled fill). The
                    // previous button is disabled at page 0, so its Foreground must reflect that
                    // disabled setter.
                    object? strongFillDisabled = app?.TryFindResource("ControlStrongFillColorDisabledBrush");
                    Assert.IsNotNull(strongFillDisabled, "ControlStrongFillColorDisabledBrush must resolve.");
                    WpfButton? previousButton = FindVisualChildByName<WpfButton>(pager, "PART_PreviousButton");
                    Assert.IsNotNull(previousButton, "PART_PreviousButton must be present in the PipsPager template.");
                    Assert.IsFalse(previousButton.IsEnabled, "The previous button must be disabled at the first page.");
                    Assert.AreSame(strongFillDisabled, previousButton.Foreground,
                        "The disabled navigation button Foreground must be ControlStrongFillColorDisabledBrush.");

                    WpfToggleButton? selectedPip = GetPipAt(host, 0);
                    WpfToggleButton? restPip = GetPipAt(host, 1);
                    Assert.IsNotNull(selectedPip, "The selected pip must be a ToggleButton.");
                    Assert.IsNotNull(restPip, "The rest pip must be a ToggleButton.");

                    System.Windows.Shapes.Ellipse? selectedDot =
                        FindVisualChildByName<System.Windows.Shapes.Ellipse>(selectedPip, "Pip");
                    System.Windows.Shapes.Ellipse? restDot =
                        FindVisualChildByName<System.Windows.Shapes.Ellipse>(restPip, "Pip");
                    Assert.IsNotNull(selectedDot, "The selected pip must render its dot Ellipse.");
                    Assert.IsNotNull(restDot, "The rest pip must render its dot Ellipse.");

                    // WinUI PipsPager pips are neutral: rest and selected dots both use the
                    // strong fill (PipsPagerSelectionIndicatorForeground / ...Selected); the
                    // selected pip is distinguished by size, not by the accent color.
                    Assert.AreSame(strongFill, restDot.Fill,
                        "The rest pip must fill with ControlStrongFillColorDefaultBrush.");
                    Assert.AreSame(strongFill, selectedDot.Fill,
                        "The selected pip must fill with the same neutral ControlStrongFillColorDefaultBrush (no accent).");
                    Assert.AreEqual(4.0, restDot.Width, 0.01, "The rest pip dot must stay at the 4px rest size.");

                    // The selected size is animated (83ms ControlFasterAnimationDuration), so
                    // sample the dot until the storyboard settles at the 6px selected size.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => Math.Abs(selectedDot.Width - 6.0) < 0.01),
                        "The selected pip dot must grow to the 6px selected size.");

                    pager.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreSame(app?.TryFindResource("ControlStrongFillColorDisabledBrush"), restDot.Fill,
                        "Disabled pips must fill with ControlStrongFillColorDisabledBrush.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_PipSizeMorph_AnimatesSelectionAndSurvivesWindowRebuild()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 10, MaxVisiblePips = 3 };

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfStackPanel? host = FindVisualChildByName<WpfStackPanel>(pager, "PART_PipsHost");
                    Assert.IsNotNull(host, "PART_PipsHost must be present in the PipsPager template.");

                    static System.Windows.Shapes.Ellipse? DotAt(WpfStackPanel pipsHost, int offset)
                    {
                        WpfToggleButton? pip = GetPipAt(pipsHost, offset);
                        return pip is null
                            ? null
                            : FindVisualChildByName<System.Windows.Shapes.Ellipse>(pip, "Pip");
                    }

                    static bool IsDotSize(System.Windows.Shapes.Ellipse? dot, double size)
                    {
                        return dot is not null
                            && Math.Abs(dot.Width - size) < 0.01
                            && Math.Abs(dot.Height - size) < 0.01;
                    }

                    // Pips are created with IsChecked already true, so the IsChecked
                    // EnterActions must run when the template applies and settle the
                    // selected dot at 6x6 (83ms ControlFasterAnimationDuration morph).
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 0), 6.0)),
                        "The initially selected pip must animate to the 6px selected size at load.");
                    Assert.IsTrue(IsDotSize(DotAt(host, 1), 4.0), "An unselected pip must rest at 4px.");

                    // In-place selection change (the window stays clamped at the start):
                    // the old pip's ExitActions shrink it back to 4 while the new pip grows to 6.
                    pager.SelectedPageIndex = 1;
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 6.0)),
                        "The newly selected pip must animate up to the 6px selected size.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 0), 4.0)),
                        "The previously selected pip must animate back to the 4px rest size.");

                    // Window rebuild (mid-range selection recreates the pips): the recreated
                    // selected pip must still land at 6x6 because its trigger condition is
                    // already true when the recreated template applies.
                    pager.SelectedPageIndex = 5;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual("Page 5", AutomationProperties.GetName(GetPipAt(host, 0)!),
                        "A mid-range selection must rebuild the pip window.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => IsDotSize(DotAt(host, 1), 6.0)),
                        "A recreated selected pip must animate to the 6px selected size.");
                    Assert.IsTrue(IsDotSize(DotAt(host, 0), 4.0), "A recreated unselected pip must rest at 4px.");
                    Assert.IsTrue(IsDotSize(DotAt(host, 2), 4.0), "A recreated unselected pip must rest at 4px.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PipsPager_AutomationPeer_ReportsGroupClassNameAndName()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.PipsPager pager = new() { NumberOfPages = 3 };
                AutomationProperties.SetName(pager, "Gallery pager");

                try
                {
                    window.Content = pager;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(pager);
                    Assert.IsNotNull(peer, "PipsPager must create an automation peer.");
                    _ = Assert.IsInstanceOfType<Automation.PipsPagerAutomationPeer>(peer,
                        "PipsPager must expose the PipsPagerAutomationPeer.");
                    Assert.AreEqual("PipsPager", peer.GetClassName(),
                        "The peer must report the PipsPager class name.");
                    Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType(),
                        "The peer must report the Group control type.");
                    Assert.AreEqual("Gallery pager", peer.GetName(),
                        "The peer name must come from AutomationProperties.Name.");
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
