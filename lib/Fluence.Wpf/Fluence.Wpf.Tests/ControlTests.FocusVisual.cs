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
using System.Windows.Input;
using Fluence.Wpf.Controls;
using Xunit;

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

        [Fact]
        public void FocusVisual_DefaultControlFocusVisualStyle_ResolvesInAllThemes()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Light, ApplicationTheme.Dark, ApplicationTheme.HighContrast })
                {
                    _ = MergeGenericDictionary(app);
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);

                    Style style = Assert.IsType<Style>(app?.TryFindResource("DefaultControlFocusVisualStyle"));
                }
            });
        }

        [Fact]
        public void FocusVisual_PerControlKeys_RemovedFromDictionary()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                // These per-control duplicate keys must no longer exist now that
                // all four controls reference DefaultControlFocusVisualStyle.
                Assert.Null(app?.TryFindResource("ButtonFocusVisual"));
                Assert.Null(app?.TryFindResource("CheckBoxFocusVisual"));
                Assert.Null(app?.TryFindResource("RadioButtonFocusVisual"));
                Assert.Null(app?.TryFindResource("ToggleButtonFocusVisual"));
            });
        }

        [Fact]
        public void FocusVisual_Button_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style sharedStyle = Assert.IsType<Style>(app?.TryFindResource("DefaultControlFocusVisualStyle"));

                Button btn = new();
                Window w = new() { Content = btn, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.Same(sharedStyle, btn.FocusVisualStyle);
                w.Close();
            });
        }

        [Fact]
        public void FocusVisual_CheckBox_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style sharedStyle = Assert.IsType<Style>(app?.TryFindResource("DefaultControlFocusVisualStyle"));

                CheckBox cb = new() { Content = "Test" };
                Window w = new() { Content = cb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.Same(sharedStyle, cb.FocusVisualStyle);
                w.Close();
            });
        }

        [Fact]
        public void FocusVisual_RadioButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style sharedStyle = Assert.IsType<Style>(app?.TryFindResource("DefaultControlFocusVisualStyle"));

                RadioButton rb = new() { Content = "Option A" };
                Window w = new() { Content = rb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.Same(sharedStyle, rb.FocusVisualStyle);
                w.Close();
            });
        }

        [Fact]
        public void FocusVisual_ToggleButton_FocusVisualStyleIsSharedResource()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style sharedStyle = Assert.IsType<Style>(app?.TryFindResource("DefaultControlFocusVisualStyle"));

                ToggleButton tb = new() { Content = "Toggle" };
                Window w = new() { Content = tb, Width = 200, Height = 100 };
                w.Show();
                DrainDispatcher(w.Dispatcher);

                Assert.Same(sharedStyle, tb.FocusVisualStyle);
                w.Close();
            });
        }

        [Fact]
        public void FocusVisual_TabItem_UsesCollectionFocusStyleWithRightBreathingRoom()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style sharedStyle = Assert.IsType<Style>(app?.TryFindResource("DefaultCollectionFocusVisualStyle"));

                System.Windows.Controls.TabControl tabControl = new();
                _ = tabControl.Items.Add(new System.Windows.Controls.TabItem { Header = "Text", Content = new System.Windows.Controls.TextBlock { Text = "A" } });
                _ = tabControl.Items.Add(new System.Windows.Controls.TabItem { Header = "Fill", Content = new System.Windows.Controls.TextBlock { Text = "B" } });
                Window w = new() { Content = tabControl, Width = 360, Height = 180 };

                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    System.Windows.Controls.TabItem first = Assert.IsType<System.Windows.Controls.TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.Same(sharedStyle, first.FocusVisualStyle);
                    Assert.True(first.Margin.Right >= 8.0,
                        "TabItem should reserve enough right margin so the focus rectangle is not clipped at the tab edge.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public void TabControl_TabKeySelectsNextHeaderThenContinuesOut()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                System.Windows.Controls.TabControl tabControl = new()
                {
                    Width = 320,
                    Height = 140,
                };
                _ = tabControl.Items.Add(new System.Windows.Controls.TabItem { Header = "First", Content = new System.Windows.Controls.TextBlock { Text = "One" } });
                _ = tabControl.Items.Add(new System.Windows.Controls.TabItem { Header = "Second", Content = new System.Windows.Controls.TextBlock { Text = "Two" } });

                System.Windows.Controls.Button afterButton = new() { Content = "After" };
                System.Windows.Controls.StackPanel root = new();
                _ = root.Children.Add(tabControl);
                _ = root.Children.Add(afterButton);

                Window window = new() { Content = root, Width = 420, Height = 240 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.TabItem first = Assert.IsType<System.Windows.Controls.TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(0));
                    System.Windows.Controls.TabItem second = Assert.IsType<System.Windows.Controls.TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(1));

                    _ = Keyboard.Focus(first);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Same(first, Keyboard.FocusedElement);

                    KeyEventArgs firstTabArgs = RaiseTabKey(first, window);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(firstTabArgs.Handled,
                        "Tab on the first TabControl header should be handled as header navigation.");
                    Assert.Same(second, Keyboard.FocusedElement);
                    Assert.Same(second, tabControl.SelectedItem);

                    KeyEventArgs secondTabArgs = RaiseTabKey(second, window);
                    Assert.False(secondTabArgs.Handled,
                        "Tab on the final TabControl header should be left for normal focus navigation.");
                    bool movedOut = second.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    DrainDispatcher(window.Dispatcher);

                    Assert.True(movedOut, "Tab should be able to move past the last TabControl header.");
                    Assert.Same(afterButton, Keyboard.FocusedElement);
                }
                finally
                {
                    Keyboard.ClearFocus();
                    window.Close();
                }
            });
        }

        [Fact]
        public void TabView_TabKeySelectsNextHeaderThenContinuesOut()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TabViewItem first = new() { Header = "First", Content = new System.Windows.Controls.TextBlock { Text = "One" } };
                TabViewItem second = new() { Header = "Second", Content = new System.Windows.Controls.TextBlock { Text = "Two" } };
                TabView tabView = new()
                {
                    Width = 340,
                    Height = 150,
                    IsAddTabButtonVisible = false,
                };
                _ = tabView.Items.Add(first);
                _ = tabView.Items.Add(second);

                System.Windows.Controls.Button afterButton = new() { Content = "After" };
                System.Windows.Controls.StackPanel root = new();
                _ = root.Children.Add(tabView);
                _ = root.Children.Add(afterButton);

                Window window = new() { Content = root, Width = 440, Height = 250 };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Grid rootGrid = Assert.IsType<System.Windows.Controls.Grid>(tabView.Template.FindName("RootGrid", tabView));
                    System.Windows.Controls.Border contentPanel = Assert.IsType<System.Windows.Controls.Border>(tabView.Template.FindName("ContentPanel", tabView));
                    Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(rootGrid));
                    Assert.Equal(KeyboardNavigationMode.Continue, KeyboardNavigation.GetTabNavigation(contentPanel));

                    _ = Keyboard.Focus(first);
                    DrainDispatcher(window.Dispatcher);
                    Assert.Same(first, Keyboard.FocusedElement);

                    KeyEventArgs firstTabArgs = RaiseTabKey(first, window);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(firstTabArgs.Handled,
                        "Tab on the first TabView header should be handled as header navigation.");
                    Assert.Same(second, Keyboard.FocusedElement);
                    Assert.Same(second, tabView.SelectedItem);

                    KeyEventArgs secondTabArgs = RaiseTabKey(second, window);
                    Assert.False(secondTabArgs.Handled,
                        "Tab on the final TabView header should be left for normal focus navigation.");
                    bool movedOut = second.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next));
                    DrainDispatcher(window.Dispatcher);

                    Assert.True(movedOut, "Tab should be able to move past the last TabView header.");
                    Assert.Same(afterButton, Keyboard.FocusedElement);
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

        [Fact]
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
                    Assert.Same(first, Keyboard.FocusedElement);

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

                    Assert.Same(second, nav.SelectedItem);
                    Assert.NotSame(second, Keyboard.FocusedElement);
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
