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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Input;
using WpfBorder = System.Windows.Controls.Border;
using WpfButton = System.Windows.Controls.Button;
using WpfGrid = System.Windows.Controls.Grid;
using WpfStackPanel = System.Windows.Controls.StackPanel;
using WpfTabControl = System.Windows.Controls.TabControl;
using WpfTabItem = System.Windows.Controls.TabItem;
using WpfTextBlock = System.Windows.Controls.TextBlock;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-3 A1-A4 tests: per-control focus visual style dedup.
    /// Verifies Button, CheckBox, RadioButton, and ToggleButton all use the
    /// shared <c>DefaultControlFocusVisualStyle</c> resource rather than
    /// per-control duplicates.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // WI-3 A1-A4  Focus visual dedup
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void FocusVisual_DefaultControlFocusVisualStyle_ResolvesInAllThemes()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    _ = MergeGenericDictionary(app);
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);

                    Style? style = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                    Assert.IsNotNull(style,
                        string.Format("DefaultControlFocusVisualStyle must resolve in theme: {0}", theme));
                }
            });
        }

        [TestMethod]
        public void FocusVisual_PerControlKeys_RemovedFromDictionary()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // These per-control duplicate keys must no longer exist now that
                // all four controls reference DefaultControlFocusVisualStyle.
                Assert.IsNull(app?.TryFindResource("ButtonFocusVisual"),
                    "ButtonFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("CheckBoxFocusVisual"),
                    "CheckBoxFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("RadioButtonFocusVisual"),
                    "RadioButtonFocusVisual per-control key must be removed.");
                Assert.IsNull(app?.TryFindResource("ToggleButtonFocusVisual"),
                    "ToggleButtonFocusVisual per-control key must be removed.");
            });
        }

        [TestMethod]
        public void FocusVisual_Button_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                Button btn = new();
                Window w = new() { Content = btn, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, btn.FocusVisualStyle,
                    "Button.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_CheckBox_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                CheckBox cb = new() { Content = "Test" };
                Window w = new() { Content = cb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, cb.FocusVisualStyle,
                    "CheckBox.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_RadioButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                RadioButton rb = new() { Content = "Option A" };
                Window w = new() { Content = rb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, rb.FocusVisualStyle,
                    "RadioButton.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_ToggleButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultControlFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultControlFocusVisualStyle must resolve.");

                ToggleButton tb = new() { Content = "Toggle" };
                Window w = new() { Content = tb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.AreSame(sharedStyle, tb.FocusVisualStyle,
                    "ToggleButton.FocusVisualStyle must reference the shared DefaultControlFocusVisualStyle.");
                w.Close();
            });
        }

        [TestMethod]
        public void FocusVisual_TabItem_UsesCollectionFocusStyleWithRightBreathingRoom()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? sharedStyle = app?.TryFindResource("DefaultCollectionFocusVisualStyle") as Style;
                Assert.IsNotNull(sharedStyle, "DefaultCollectionFocusVisualStyle must resolve.");

                WpfTabControl tabControl = new();
                _ = tabControl.Items.Add(new WpfTabItem { Header = "Text", Content = new WpfTextBlock { Text = "A" } });
                _ = tabControl.Items.Add(new WpfTabItem { Header = "Fill", Content = new WpfTextBlock { Text = "B" } });
                Window w = new() { Content = tabControl, Width = 360, Height = 180 };

                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    WpfTabItem? first = tabControl.ItemContainerGenerator.ContainerFromIndex(0) as WpfTabItem;
                    Assert.IsNotNull(first, "The first TabItem container should be generated.");
                    Assert.AreSame(sharedStyle, first.FocusVisualStyle,
                        "TabItem should use WPF keyboard focus cues instead of a pointer-sticky custom focus ring.");
                    Assert.IsTrue(first.Margin.Right >= 8.0,
                        "TabItem should reserve enough right margin so the focus rectangle is not clipped at the tab edge.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void TabControl_TabKeySelectsNextHeaderThenContinuesOut()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                WpfTabControl tabControl = new()
                {
                    Width = 320,
                    Height = 140,
                };
                _ = tabControl.Items.Add(new WpfTabItem { Header = "First", Content = new WpfTextBlock { Text = "One" } });
                _ = tabControl.Items.Add(new WpfTabItem { Header = "Second", Content = new WpfTextBlock { Text = "Two" } });

                WpfButton afterButton = new() { Content = "After" };
                WpfStackPanel root = new();
                _ = root.Children.Add(tabControl);
                _ = root.Children.Add(afterButton);

                Window window = new() { Content = root, Width = 420, Height = 240 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfTabItem? first = tabControl.ItemContainerGenerator.ContainerFromIndex(0) as WpfTabItem;
                    WpfTabItem? second = tabControl.ItemContainerGenerator.ContainerFromIndex(1) as WpfTabItem;
                    Assert.IsNotNull(first, "The first TabControl header should be generated.");
                    Assert.IsNotNull(second, "The second TabControl header should be generated.");

                    _ = Keyboard.Focus(first);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreSame(first, Keyboard.FocusedElement,
                        "Test setup should put keyboard focus on the first TabControl header.");

                    KeyEventArgs firstTabArgs = RaiseTabKey(first, window);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(firstTabArgs.Handled,
                        "Tab on the first TabControl header should be handled as header navigation.");
                    Assert.AreSame(second, Keyboard.FocusedElement,
                        "Tab should move focus to the next TabControl header.");
                    Assert.AreSame(second, tabControl.SelectedItem,
                        "Tabbing to the next TabControl header should select that tab.");

                    KeyEventArgs secondTabArgs = RaiseTabKey(second, window);
                    Assert.IsFalse(secondTabArgs.Handled,
                        "Tab on the final TabControl header should be left for normal focus navigation.");
                    bool movedOut = second.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(movedOut, "Tab should be able to move past the last TabControl header.");
                    Assert.AreSame(afterButton, Keyboard.FocusedElement,
                        "Tab should continue out of the TabControl after the final header.");
                }
                finally
                {
                    Keyboard.ClearFocus();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TabView_TabKeySelectsNextHeaderThenContinuesOut()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabViewItem first = new() { Header = "First", Content = new WpfTextBlock { Text = "One" } };
                TabViewItem second = new() { Header = "Second", Content = new WpfTextBlock { Text = "Two" } };
                TabView tabView = new()
                {
                    Width = 340,
                    Height = 150,
                    IsAddTabButtonVisible = false,
                };
                _ = tabView.Items.Add(first);
                _ = tabView.Items.Add(second);

                WpfButton afterButton = new() { Content = "After" };
                WpfStackPanel root = new();
                _ = root.Children.Add(tabView);
                _ = root.Children.Add(afterButton);

                Window window = new() { Content = root, Width = 440, Height = 250 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    WpfGrid? rootGrid = tabView.Template.FindName("RootGrid", tabView) as WpfGrid;
                    WpfBorder? contentPanel = tabView.Template.FindName("ContentPanel", tabView) as WpfBorder;
                    Assert.IsNotNull(rootGrid, "TabView template should expose RootGrid for keyboard navigation.");
                    Assert.IsNotNull(contentPanel, "TabView template should expose ContentPanel for keyboard navigation.");
                    Assert.AreEqual(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(rootGrid),
                        "TabView should not trap tab navigation inside the control template.");
                    Assert.AreEqual(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(contentPanel),
                        "TabView content should continue tab navigation out of the control.");

                    _ = Keyboard.Focus(first);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreSame(first, Keyboard.FocusedElement,
                        "Test setup should put keyboard focus on the first TabView header.");

                    KeyEventArgs firstTabArgs = RaiseTabKey(first, window);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.IsTrue(firstTabArgs.Handled,
                        "Tab on the first TabView header should be handled as header navigation.");
                    Assert.AreSame(second, Keyboard.FocusedElement,
                        "Tab should move focus to the next TabView header.");
                    Assert.AreSame(second, tabView.SelectedItem,
                        "Tabbing to the next TabView header should select that tab.");

                    KeyEventArgs secondTabArgs = RaiseTabKey(second, window);
                    Assert.IsFalse(secondTabArgs.Handled,
                        "Tab on the final TabView header should be left for normal focus navigation.");
                    bool movedOut = second.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(movedOut, "Tab should be able to move past the last TabView header.");
                    Assert.AreSame(afterButton, Keyboard.FocusedElement,
                        "Tab should continue out of the TabView after the final header.");
                }
                finally
                {
                    Keyboard.ClearFocus();
                    window.Close();
                }
            });
        }

        private static KeyEventArgs RaiseTabKey(UIElement target, Window window)
        {
            PresentationSource source = PresentationSource.FromVisual(window)
                ?? throw new InvalidOperationException("Window must have a presentation source before raising keyboard input.");
            KeyEventArgs args = new(Keyboard.PrimaryDevice, source, 0, Key.Tab)
            {
                RoutedEvent = UIElement.PreviewKeyDownEvent,
                Source = target,
            };
            target.RaiseEvent(args);
            return args;
        }

        [TestMethod]
        public void FocusVisual_NavigationViewItem_PointerInvokeDoesNotMoveKeyboardFocus()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                NavigationViewItem first = new() { Content = "Home" };
                NavigationViewItem second = new() { Content = "Colors" };
                NavigationView nav = new()
                {
                    Width = 320,
                    Height = 220,
                    PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    SelectionFollowsFocus = false,
                };
                _ = nav.Items.Add(first);
                _ = nav.Items.Add(second);
                Window w = new() { Content = nav, Width = 360, Height = 260 };

                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    _ = Keyboard.Focus(first);
                    DrainDispatcher(w.Dispatcher);
                    Assert.AreSame(first, Keyboard.FocusedElement,
                        "Test setup should put keyboard focus on the first navigation item.");

                    MouseButtonEventArgs mouseArgs = new(
                        Mouse.PrimaryDevice,
                        Environment.TickCount,
                        MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                        Source = second,
                    };
                    second.RaiseEvent(mouseArgs);
                    DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    Assert.AreSame(second, nav.SelectedItem,
                        "Pointer selection should still select the clicked navigation item.");
                    Assert.AreNotSame(second, Keyboard.FocusedElement,
                        "Pointer selection should not leave the keyboard focus visual on the clicked navigation item.");
                }
                finally
                {
                    Keyboard.ClearFocus();
                    w.Close();
                }
            });
        }
    }
}
