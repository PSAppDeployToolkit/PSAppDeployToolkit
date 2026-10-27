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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="TabView"/> / <see cref="TabViewItem"/>
    /// pair: default property values, container generation, add/close button template parts and
    /// events, and scroll-button visibility when tabs overflow the strip.
    /// </summary>
    public sealed class TabViewTests : IClassFixture<LightThemeFixture>
    {
        [Theory]
        [InlineData(FlowDirection.LeftToRight)]
        [InlineData(FlowDirection.RightToLeft)]
        public Task TabView_OverflowScrollButtons_VisibilityAndClickFollowOffsetAsync(FlowDirection flow)
        {
            return RunOverflowTabsAsync((tabs, _, _, window) =>
            {
                tabs.FlowDirection = flow;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                ScrollViewer scroller = GetTabScroller(tabs);
                System.Windows.Controls.Primitives.RepeatButton back = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(tabs.Template.FindName("PART_ScrollBackButton", tabs), exactMatch: false);
                System.Windows.Controls.Primitives.RepeatButton forward = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(tabs.Template.FindName("PART_ScrollForwardButton", tabs), exactMatch: false);
                _ = back.ApplyTemplate();
                _ = forward.ApplyTemplate();
                System.Windows.Controls.TextBlock backGlyph = Assert.IsType<System.Windows.Controls.TextBlock>(back.Template.FindName("ScrollBackGlyph", back));
                System.Windows.Controls.TextBlock forwardGlyph = Assert.IsType<System.Windows.Controls.TextBlock>(forward.Template.FindName("ScrollForwardGlyph", forward));
                Assert.Equal(flow is FlowDirection.LeftToRight ? "\uE76B" : "\uE76C", backGlyph.Text);
                Assert.Equal(flow is FlowDirection.LeftToRight ? "\uE76C" : "\uE76B", forwardGlyph.Text);
                FrameworkElement viewport = Assert.IsType<ScrollContentPresenter>(FindVisualChild<ScrollContentPresenter>(scroller), exactMatch: false);
                Rect viewportBounds = viewport.TransformToAncestor(window).TransformBounds(new Rect(viewport.RenderSize));
                Rect forwardBounds = forward.TransformToAncestor(window).TransformBounds(new Rect(forward.RenderSize));
                Assert.True(flow is FlowDirection.LeftToRight ? forwardBounds.Left >= viewportBounds.Right : forwardBounds.Right <= viewportBounds.Left);
                TabViewItem first = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(0));
                double firstPosition = first.TransformToAncestor(window).Transform(new Point()).X;
                int firstVisible = GetFirstVisibleHeader(tabs, viewport, window);
                Assert.Equal(Visibility.Collapsed, back.Visibility);
                Assert.Equal(Visibility.Visible, forward.Visibility);
                InputSimulation.RaiseButtonClick(forward);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                double singleClickOffset = scroller.HorizontalOffset;
                Assert.True(singleClickOffset > 0);
                Assert.True(GetFirstVisibleHeader(tabs, viewport, window) > firstVisible);
                double movedPosition = first.TransformToAncestor(window).Transform(new Point()).X;
                Assert.True(flow is FlowDirection.LeftToRight ? movedPosition < firstPosition : movedPosition > firstPosition);
                Assert.Equal(Visibility.Visible, back.Visibility);
                InputSimulation.RaiseButtonClick(back);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(0, scroller.HorizontalOffset, 0.5);
                Assert.Equal(firstVisible, GetFirstVisibleHeader(tabs, viewport, window));
                Assert.Equal(Visibility.Collapsed, back.Visibility);
                tabs.OnApplyTemplate();
                tabs.OnApplyTemplate();
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                InputSimulation.RaiseButtonClick(forward);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(singleClickOffset, scroller.HorizontalOffset, 0.5);
                tabs.SelectedIndex = 29;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                Assert.Equal(Visibility.Visible, back.Visibility);
                InputSimulation.RaiseButtonClick(forward);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(Visibility.Collapsed, forward.Visibility);
                tabs.SelectedIndex = 0;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                InputSimulation.RaiseButtonClick(forward);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.True(scroller.HorizontalOffset > 0);
                tabs.IsAddTabButtonVisible = false;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
            });
        }

        private static int GetFirstVisibleHeader(TabView tabs, FrameworkElement viewport, Window window)
        {
            Rect viewportBounds = viewport.TransformToAncestor(window).TransformBounds(new Rect(viewport.RenderSize));
            for (int index = 0; index < tabs.Items.Count; index++)
            {
                TabViewItem header = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(index));
                Rect bounds = header.TransformToAncestor(window).TransformBounds(new Rect(header.RenderSize));
                bounds.Intersect(viewportBounds);
                if (!bounds.IsEmpty && bounds.Width > 1)
                {
                    return index;
                }
            }
            throw new InvalidOperationException("No tab header intersects the visible strip.");
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight)]
        [InlineData(FlowDirection.RightToLeft)]
        public Task TabView_ProgrammaticSelection_RevealsOffscreenHeaderAsync(FlowDirection flow)
        {
            return RunOverflowTabsAsync((tabs, _, outer, _) =>
            {
                tabs.FlowDirection = flow;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                double horizontal = outer.HorizontalOffset;
                double vertical = outer.VerticalOffset;
                tabs.SelectedIndex = 29;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                Assert.True(GetTabScroller(tabs).HorizontalOffset > 0);
                tabs.SelectedIndex = 0;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                Assert.Equal(horizontal, outer.HorizontalOffset);
                Assert.Equal(vertical, outer.VerticalOffset);
            });
        }

        [Fact]
        public Task TabView_KeyboardSelection_RevealsOffscreenHeaderAsync()
        {
            return RunOverflowTabsAsync(static (tabs, _, _, _) =>
            {
                TabViewItem first = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(0));
                Assert.True(first.Focus());
                InputSimulation.RaiseKeyEvent(tabs, Key.End, UIElement.KeyDownEvent);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(29, tabs.SelectedIndex);
                AssertSelectedHeaderVisible(tabs);
            });
        }

        [Fact]
        public Task TabView_CollectionChanges_KeepSelectedHeaderVisibleAsync()
        {
            return RunOverflowTabsAsync(static (tabs, items, _, _) =>
            {
                tabs.SelectedIndex = 29;
                items.Insert(0, "Inserted");
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                items.Move(30, 0);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                items.Move(0, 30);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                items.RemoveAt(tabs.SelectedIndex);
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                items.Clear();
                items.Add("Replacement first");
                items.Add("Replacement last");
                tabs.SelectedIndex = 1;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
            });
        }

        [Fact]
        public Task TabView_ResizeAndRetemplate_KeepSelectionVisibleAsync()
        {
            return RunOverflowTabsAsync(static (tabs, _, outer, _) =>
            {
                tabs.SelectedIndex = 29;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                tabs.Width = 260;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                tabs.Width = 650;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                ScrollViewer oldScroller = GetTabScroller(tabs);
                double oldOffset = oldScroller.HorizontalOffset;
                tabs.SelectedIndex = 0;
                tabs.Template = null;
                tabs.ClearValue(System.Windows.Controls.Control.TemplateProperty);
                tabs.SelectedIndex = 29;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.NotSame(oldScroller, GetTabScroller(tabs));
                AssertSelectedHeaderVisible(tabs);
                Assert.Equal(oldOffset, oldScroller.HorizontalOffset);
                double stableOffset = GetTabScroller(tabs).HorizontalOffset;
                tabs.OnApplyTemplate();
                tabs.OnApplyTemplate();
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(stableOffset, GetTabScroller(tabs).HorizontalOffset);
                TabViewItem selected = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromItem(tabs.SelectedItem));
                selected.Header = "A wider selected header title";
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs);
                stableOffset = GetTabScroller(tabs).HorizontalOffset;
                tabs.SelectedIndex = 0;
                outer.Content = null;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(stableOffset, GetTabScroller(tabs).HorizontalOffset);
            });
        }

        private static readonly double[] _oversizedViewportWidths = [280.0, 390.0];

        [Theory]
        [InlineData(FlowDirection.LeftToRight)]
        [InlineData(FlowDirection.RightToLeft)]
        public Task TabView_OversizedHeader_AlignsLeadingEdgeWithoutOscillationAsync(FlowDirection flow)
        {
            return RunOverflowTabsAsync((tabs, _, outer, _) =>
            {
                tabs.FlowDirection = flow;
                double horizontal = outer.HorizontalOffset;
                double vertical = outer.VerticalOffset;
                tabs.SelectedIndex = 15;
                TabViewItem selected = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(15));
                selected.MaxWidth = double.PositiveInfinity;
                selected.Width = 500;
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                AssertSelectedHeaderVisible(tabs, oversized: true);
                double offset = GetTabScroller(tabs).HorizontalOffset;
                tabs.OnApplyTemplate();
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.Equal(offset, GetTabScroller(tabs).HorizontalOffset, 0.5);
                AssertSelectedHeaderVisible(tabs, oversized: true);
                tabs.SelectedIndex = 14;
                tabs.SelectedIndex = 15;
                foreach (double width in _oversizedViewportWidths)
                {
                    tabs.Width = width;
                    WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                    AssertSelectedHeaderVisible(tabs, oversized: true);
                    double resizedOffset = GetTabScroller(tabs).HorizontalOffset;
                    WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                    Assert.Equal(resizedOffset, GetTabScroller(tabs).HorizontalOffset, 0.5);
                }
                Assert.Equal(horizontal, outer.HorizontalOffset);
                Assert.Equal(vertical, outer.VerticalOffset);
            });
        }

        private static Task RunOverflowTabsAsync(Action<TabView, ObservableCollection<string>, ScrollViewer, Window> scenario)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ObservableCollection<string> items = new(Enumerable.Range(0, 30).Select(static index => "Tab " + index.ToString("00", CultureInfo.InvariantCulture)));
                TabView tabs = new() { Width = 330, Height = 200, ItemsSource = items, SelectedIndex = 0, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };
                Grid surface = new() { Width = 800, Height = 600 };
                _ = surface.Children.Add(tabs);
                ScrollViewer outer = new() { Content = surface, HorizontalScrollBarVisibility = ScrollBarVisibility.Auto, VerticalScrollBarVisibility = ScrollBarVisibility.Auto };
                Window window = new() { Content = outer, Width = 420, Height = 310 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    scenario(tabs, items, outer, window);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static ScrollViewer GetTabScroller(TabView tabs)
        {
            return Assert.IsType<ScrollViewer>(tabs.Template.FindName("PART_TabContentScroller", tabs), exactMatch: false);
        }

        private static void AssertSelectedHeaderVisible(TabView tabs, bool oversized = false)
        {
            ScrollViewer scroller = GetTabScroller(tabs);
            ScrollContentPresenter viewport = Assert.IsType<ScrollContentPresenter>(FindVisualChild<ScrollContentPresenter>(scroller), exactMatch: false);
            TabViewItem selected = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromItem(tabs.SelectedItem));
            Window window = Window.GetWindow(tabs);
            Assert.Equal(FlowDirection.LeftToRight, window.FlowDirection);
            Rect bounds = selected.TransformToAncestor(window).TransformBounds(new Rect(selected.RenderSize));
            Rect viewportBounds = viewport.TransformToAncestor(window).TransformBounds(new Rect(viewport.RenderSize));
            if (oversized)
            {
                Assert.True(bounds.Width > viewport.ActualWidth);
                double actualEdge = tabs.FlowDirection is FlowDirection.LeftToRight ? bounds.Left : bounds.Right;
                double expectedEdge = tabs.FlowDirection is FlowDirection.LeftToRight ? viewportBounds.Left : viewportBounds.Right;
                Assert.Equal(expectedEdge, actualEdge, 1.0);
            }
            else
            {
                Assert.InRange(bounds.Left, viewportBounds.Left - 1, viewportBounds.Right);
                Assert.InRange(bounds.Right, viewportBounds.Left, viewportBounds.Right + 1);
            }
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight, "empty")]
        [InlineData(FlowDirection.RightToLeft, "empty")]
        [InlineData(FlowDirection.LeftToRight, "removed")]
        [InlineData(FlowDirection.RightToLeft, "removed")]
        [InlineData(FlowDirection.LeftToRight, "disabled")]
        [InlineData(FlowDirection.RightToLeft, "disabled")]
        [InlineData(FlowDirection.LeftToRight, "hidden")]
        [InlineData(FlowDirection.RightToLeft, "hidden")]
        [InlineData(FlowDirection.LeftToRight, "not-focusable")]
        [InlineData(FlowDirection.RightToLeft, "not-focusable")]
        public Task TabView_NoAvailableHeader_TabReachesAddAndEnterRequestsOnceAsync(FlowDirection flow, string state)
        {
            return RunTabEntryAsync((tabs, items, before, after, add) =>
            {
                if (string.Equals(state, "removed", StringComparison.Ordinal))
                {
                    items.Clear();
                }
                else if (items.Count > 0)
                {
                    foreach (TabViewItem header in items)
                    {
                        if (string.Equals(state, "disabled", StringComparison.Ordinal))
                        {
                            header.IsEnabled = false;
                        }
                        else if (string.Equals(state, "hidden", StringComparison.Ordinal))
                        {
                            header.Visibility = Visibility.Collapsed;
                        }
                        else
                        {
                            header.Focusable = false;
                        }
                    }
                }
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.True(before.Focus());
                Assert.True(before.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(add, Keyboard.FocusedElement);
                Assert.True(add.IsTabStop);
                int originalCount = items.Count;
                int requests = 0;
                tabs.AddTabButtonClick += (_, _) => requests++;
                InputSimulation.RaiseKeyEvent(add, Key.Enter, UIElement.KeyDownEvent);
                Assert.Equal(1, requests);
                Assert.Equal(originalCount, items.Count);
                Assert.True(add.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(after, Keyboard.FocusedElement);

                if (items.Count is 0)
                {
                    items.Add(new TabViewItem { Header = "New available tab" });
                }
                else
                {
                    items[0].IsEnabled = true;
                    items[0].Visibility = Visibility.Visible;
                    items[0].Focusable = true;
                }
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.False(add.IsTabStop);
            }, flow, initiallyEmpty: string.Equals(state, "empty", StringComparison.Ordinal));
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight, false)]
        [InlineData(FlowDirection.RightToLeft, false)]
        [InlineData(FlowDirection.LeftToRight, true)]
        [InlineData(FlowDirection.RightToLeft, true)]
        public Task TabView_NoAvailableHeader_HiddenOrDisabledAddIsSkippedAsync(FlowDirection flow, bool hide)
        {
            return RunTabEntryAsync((tabs, _, before, after, add) =>
            {
                if (hide)
                {
                    tabs.IsAddTabButtonVisible = false;
                }
                else
                {
                    add.IsEnabled = false;
                }
                WpfTestSta.DrainDispatcher(tabs.Dispatcher);
                Assert.True(before.Focus());
                Assert.True(before.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(after, Keyboard.FocusedElement);
            }, flow, initiallyEmpty: true);
        }

        private static Task RunTabEntryAsync(Action<TabView, ObservableCollection<TabViewItem>, ButtonBase, ButtonBase, ButtonBase> scenario, FlowDirection flow, bool initiallyEmpty)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                ObservableCollection<TabViewItem> items = initiallyEmpty
                    ? []
                    : [new TabViewItem { Header = "First" }, new TabViewItem { Header = "Last" }];
                TabView tabs = new() { Width = 420, Height = 180, ItemsSource = items, FlowDirection = flow };
                System.Windows.Controls.Button before = new() { Content = "Before tabs" };
                System.Windows.Controls.Button after = new() { Content = "After tabs" };
                System.Windows.Controls.StackPanel layout = new();
                _ = layout.Children.Add(before);
                _ = layout.Children.Add(tabs);
                _ = layout.Children.Add(after);
                Window window = new() { Content = layout, Width = 480, Height = 320 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    ButtonBase add = Assert.IsType<ButtonBase>(tabs.Template.FindName("PART_AddTabButton", tabs), exactMatch: false);
                    scenario(tabs, items, before, after, add);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Theory]
        [InlineData(FlowDirection.LeftToRight, Key.Right, Key.Left)]
        [InlineData(FlowDirection.RightToLeft, Key.Left, Key.Right)]
        public Task TabView_AddButton_ArrowFocusAndEnterInvokesRequestOnceAsync(FlowDirection flow, Key forward, Key back)
        {
            return RunTabKeyboardAsync((tabs, last, add, outside) =>
            {
                int requests = 0;
                tabs.AddTabButtonClick += (_, _) => requests++;
                _ = last.Focus();
                InputSimulation.RaiseKeyEvent(last, forward, UIElement.PreviewKeyDownEvent);
                Assert.Same(add, Keyboard.FocusedElement);
                Assert.Equal(1, tabs.SelectedIndex);
                Assert.True(UIElementAutomationPeer.CreatePeerForElement(add).IsKeyboardFocusable());
                InputSimulation.RaiseKeyEvent(add, Key.Enter, UIElement.KeyDownEvent);
                Assert.Equal(1, requests);
                Assert.Equal(2, tabs.Items.Count);

                InputSimulation.RaiseKeyEvent(add, back, UIElement.PreviewKeyDownEvent);
                Assert.Same(last, Keyboard.FocusedElement);
                _ = add.Focus();
                Assert.True(add.MoveFocus(new TraversalRequest(FocusNavigationDirection.Next)));
                Assert.Same(outside, Keyboard.FocusedElement);
            }, flow);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public Task TabView_AddButton_HiddenOrDisabledIsSkippedAsync(bool hide)
        {
            return RunTabKeyboardAsync((tabs, last, add, _) =>
            {
                if (hide)
                {
                    tabs.IsAddTabButtonVisible = false;
                }
                else
                {
                    add.IsEnabled = false;
                }
                Assert.True(last.Focus());
                InputSimulation.RaiseKeyEvent(last, Key.Right, UIElement.PreviewKeyDownEvent);
                Assert.NotSame(add, Keyboard.FocusedElement);
                Assert.Equal(2, tabs.Items.Count);
            });
        }

        [Fact]
        public Task TabView_CtrlF4_RequestsSelectedClosableTabOnceAsync()
        {
            return RunTabKeyboardAsync(static (tabs, last, _, _) =>
            {
                int childRequests = 0;
                int parentRequests = 0;
                last.CloseRequested += (_, args) =>
                {
                    childRequests++;
                    Assert.Same(last, args.Tab);
                    Assert.Same(tabs.SelectedItem, args.Item);
                };
                tabs.TabCloseRequested += (_, args) =>
                {
                    parentRequests++;
                    Assert.Same(last, args.Tab);
                    Assert.Same(tabs.SelectedItem, args.Item);
                };
                Assert.True(InvokeCloseShortcut(tabs, Key.F4, ModifierKeys.Control));
                Assert.Equal(1, childRequests);
                Assert.Equal(1, parentRequests);
                Assert.Equal(2, tabs.Items.Count);
            });
        }

        [Fact]
        public Task TabView_CtrlF4_GuardsUnclosableAndNoSelectionAsync()
        {
            return RunTabKeyboardAsync(static (tabs, last, _, _) =>
            {
                int requests = 0;
                tabs.TabCloseRequested += (_, _) => requests++;
                Assert.False(InvokeCloseShortcut(tabs, Key.F4, ModifierKeys.None));
                Assert.False(InvokeCloseShortcut(tabs, Key.F4, ModifierKeys.Control | ModifierKeys.Shift));
                Assert.False(InvokeCloseShortcut(tabs, Key.Enter, ModifierKeys.Control));
                last.IsClosable = false;
                Assert.False(InvokeCloseShortcut(tabs, Key.F4, ModifierKeys.Control));
                tabs.SelectedIndex = -1;
                Assert.False(InvokeCloseShortcut(tabs, Key.F4, ModifierKeys.Control));
                Assert.Equal(0, requests);
            });
        }

        private static bool InvokeCloseShortcut(TabView tabs, Key key, ModifierKeys modifiers)
        {
            MethodInfo method = Assert.IsType<MethodInfo>(typeof(TabView).GetMethod("TryRequestClose", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            return Assert.IsType<bool>(method.Invoke(tabs, [key, modifiers]));
        }

        private static Task RunTabKeyboardAsync(Action<TabView, TabViewItem, ButtonBase, ButtonBase> scenario, FlowDirection flow = FlowDirection.LeftToRight)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                TabView tabs = new() { Width = 420, Height = 180, FlowDirection = flow, ItemsSource = (IReadOnlyList<string>)["First", "Last"], SelectedIndex = 1 };
                System.Windows.Controls.Button outside = new() { Content = "After tabs" };
                System.Windows.Controls.StackPanel layout = new();
                _ = layout.Children.Add(tabs);
                _ = layout.Children.Add(outside);
                Window window = new() { Content = layout, Width = 480, Height = 280 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    TabViewItem last = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(1));
                    ButtonBase add = Assert.IsType<ButtonBase>(tabs.Template.FindName("PART_AddTabButton", tabs), exactMatch: false);
                    scenario(tabs, last, add, outside);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        public TabViewTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---- TabViewItem defaults ----

        [Fact]
        public Task TabViewItem_DefaultIsClosable_IsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                Assert.True(tab.IsClosable);
            });
        }

        [Fact]
        public Task TabViewItem_DefaultIcon_IsNullAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                Assert.Null(tab.Icon);
            });
        }

        [Fact]
        public Task TabViewItem_IconProperty_RoundTripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                FontIcon icon = new() { Glyph = "\uE8A5" };
                tab.Icon = icon;

                Assert.Same(icon, tab.Icon);
            });
        }

        // ---- TabViewItem WinUI parity chrome ----

        [Fact]
        public Task TabViewItem_MinHeight_Is32Async()
        {
            // WinUI TabViewItemMinHeight = 32 (TabView_themeresources.xaml).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem item = new() { Header = "Tab" };
                Window window = new() { Content = item, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(32.0, item.MinHeight);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_HeaderMetrics_MatchWinUiAsync()
        {
            // TabView_themeresources.xaml: TabViewItemHeaderFontSize 12 (line 245),
            // TabViewItemHeaderPaddingWithCloseButton 8,3,4,3 (line 253),
            // TabViewItemHeaderPaddingWithoutCloseButton 8,3,8,3 (line 254),
            // TabViewItemHeaderCloseButtonWidth 32 and Height 24 (lines 249 and 248).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem item = new() { Header = "Tab" };
                Window window = new() { Content = item, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    _ = item.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(12.0, item.FontSize);
                    Assert.Equal(new Thickness(8, 3, 4, 3), item.Padding);

                    FrameworkElement closeButton = Assert.IsType<FrameworkElement>(item.Template.FindName("PART_CloseButton", item), exactMatch: false);
                    Assert.Equal(32.0, closeButton.Width);
                    Assert.Equal(24.0, closeButton.Height);

                    item.IsClosable = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(new Thickness(8, 3, 8, 3), item.Padding);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_Template_HasNoSelectionIndicatorAsync()
        {
            // Regression: the accent underline that used to sit under the selected tab has no
            // WinUI counterpart and was removed, so the selected tab now reads through its plate
            // fill and border alone.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem item = new() { Header = "Tab", IsSelected = true };
                Window window = new() { Content = item, Width = 240, Height = 80 };

                try
                {
                    window.Show();
                    _ = item.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FrameworkElement? indicator = FindVisualChildByName<FrameworkElement>(item, "SelectionIndicator");
                    Assert.Null(indicator);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_LeadingSeparator_SitsInTheMiddleOfTheGapAsync()
        {
            // The seam is drawn on each tab's leading edge, and the 6 dip gap between tabs belongs
            // to the previous tab's own margin, so without an offset the line lands against this
            // tab rather than between the two. WinUI has no gap to centre in: it draws the seam on
            // the trailing edge of tabs that sit flush (TabView.xaml:553).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 640, Height = 200 };
                TabView tabs = new() { Width = 600, Height = 200 };
                TabViewItem first = new() { Header = "A" };
                TabViewItem second = new() { Header = "B" };
                TabViewItem third = new() { Header = "C" };
                _ = tabs.Items.Add(first);
                _ = tabs.Items.Add(second);
                _ = tabs.Items.Add(third);
                first.IsSelected = true;

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // Third carries the only visible seam here: first is the first tab, and second
                    // sits next to the selection.
                    Assert.Equal(Visibility.Visible, third.LeadingSeparatorVisibility);
                    System.Windows.Controls.Border seam = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(third, "LeadingSeparator"), exactMatch: false);

                    double secondRight = second.TransformToAncestor(tabs).Transform(new Point(second.ActualWidth, 0)).X;
                    double thirdLeft = third.TransformToAncestor(tabs).Transform(new Point(0, 0)).X;
                    double seamCentre = seam.TransformToAncestor(tabs).Transform(new Point(seam.ActualWidth / 2, 0)).X;

                    // The gap is the previous tab's right margin, and the seam halves it.
                    Assert.Equal(6.0, thirdLeft - secondRight, 0.5);
                    Assert.Equal((secondRight + thirdLeft) / 2, seamCentre, 0.6);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_LeadingSeparator_HiddenAroundSelectionAndFirstItem_ForDirectlyDeclaredItemsAsync()
        {
            // WinUI TabViewItemSeparator: a hairline divider between adjacent tabs, hidden next
            // to the selected plate on both sides and before the first tab in the strip. Driven
            // by TabView.UpdateLeadingSeparators via TabViewItem.LeadingSeparatorVisibility (a
            // code-computed, read-only DP), not by a declarative RelativeSource PreviousData
            // binding: PreviousData only carries adjacency data when items are TabViewItems
            // themselves, which is what this test exercises. The ItemsSource-bound shape, where
            // that assumption does not hold, is covered separately below.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 640, Height = 200 };
                TabView tabs = new() { Width = 600, Height = 200 };
                TabViewItem first = new() { Header = "A" };
                TabViewItem second = new() { Header = "B" };
                TabViewItem third = new() { Header = "C" };
                TabViewItem fourth = new() { Header = "D" };
                _ = tabs.Items.Add(first);
                _ = tabs.Items.Add(second);
                _ = tabs.Items.Add(third);
                _ = tabs.Items.Add(fourth);
                second.IsSelected = true;

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, first.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Collapsed, second.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Collapsed, third.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Visible, fourth.LeadingSeparatorVisibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabView_LeadingSeparator_HiddenAroundSelectionAndFirstItem_ForItemsSourceBoundTabsAsync()
        {
            // Regression: an ItemsSource-bound TabView generates its TabViewItem containers from
            // plain data objects (here, strings), so a RelativeSource PreviousData binding in the
            // template would have no IsSelected to read from the previous data item and would
            // silently never hide the separator. TabView.UpdateLeadingSeparators computes the
            // same adjacency in code from ItemContainerGenerator.ContainerFromIndex, so it holds
            // for this shape too.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 640, Height = 200 };
                TabView tabs = new()
                {
                    Width = 600,
                    Height = 200,
                    ItemsSource = (IReadOnlyList<string>)["Alpha", "Beta", "Gamma", "Delta"],
                    SelectedIndex = 1,
                };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewItem first = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(0), exactMatch: false);
                    TabViewItem second = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(1), exactMatch: false);
                    TabViewItem third = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(2), exactMatch: false);
                    TabViewItem fourth = Assert.IsType<TabViewItem>(tabs.ItemContainerGenerator.ContainerFromIndex(3), exactMatch: false);

                    Assert.Equal(Visibility.Collapsed, first.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Collapsed, second.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Collapsed, third.LeadingSeparatorVisibility);
                    Assert.Equal(Visibility.Visible, fourth.LeadingSeparatorVisibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // ---- TabView defaults ----

        [Fact]
        public Task TabView_DefaultIsAddTabButtonVisible_IsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.True(tabs.IsAddTabButtonVisible);
            });
        }

        [Fact]
        public Task TabView_DefaultTabWidthMode_IsSizeToContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.Equal(TabViewWidthMode.SizeToContent, tabs.TabWidthMode);
            });
        }

        [Fact]
        public Task TabView_DefaultCloseButtonOverlayMode_IsAutoAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.Equal(TabViewCloseButtonOverlayMode.Auto, tabs.CloseButtonOverlayMode);
            });
        }

        [Fact]
        public Task TabView_ContainerGeneration_UsesTabViewItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new()
                {
                    Width = 420,
                    Height = 200,
                    ItemsSource = (IReadOnlyList<string>)["Alpha", "Beta"],
                };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    DependencyObject container = tabs.ItemContainerGenerator.ContainerFromIndex(0);
                    _ = Assert.IsType<TabViewItem>(container, exactMatch: false);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabView_IsItemItsOwnContainerOverride_TrueForTabViewItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                TabViewItem candidate = new();

                MethodInfo method = Assert.IsType<MethodInfo>(typeof(TabView).GetMethod(
                    "IsItemItsOwnContainerOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public), exactMatch: false);
                bool? result = (bool?)method.Invoke(tabs, [candidate]);
                Assert.True(result, "A TabViewItem should be recognized as its own container.");

                bool? nonTab = (bool?)method.Invoke(tabs, ["Alpha"]);
                Assert.False(nonTab, "Plain objects should require container generation.");
            });
        }

        // ---- Template parts & events ----

        [Fact]
        public Task TabView_AddTabButtonClick_RaisesAddTabButtonClickEventAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = true };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ButtonBase addButton = Assert.IsType<ButtonBase>(tabs.Template.FindName("PART_AddTabButton", tabs), exactMatch: false);

                    int raised = 0;
                    tabs.AddTabButtonClick += (s, e) => raised++;
                    ButtonAutomationPeer peer = new(addButton as System.Windows.Controls.Button);
                    IInvokeProvider invoke = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    invoke.Invoke();

                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, raised);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_CloseButton_RaisesCloseRequestedAndBubblesToTabViewAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem first = new() { Header = "Alpha", IsSelected = true };
                TabViewItem second = new() { Header = "Beta" };
                _ = tabs.Items.Add(first);
                _ = tabs.Items.Add(second);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Force template application on the first tab so its PART_CloseButton is realized.
                    _ = first.ApplyTemplate();

                    ButtonBase closeButton = Assert.IsType<ButtonBase>(first.Template.FindName("PART_CloseButton", first), exactMatch: false);

                    TabViewTabCloseRequestedEventArgs? viewArgs = null;
                    int itemRaised = 0;
                    first.CloseRequested += (s, e) => itemRaised++;
                    tabs.TabCloseRequested += (s, e) => viewArgs = e;

                    ButtonAutomationPeer peer = new(closeButton as System.Windows.Controls.Button);
                    IInvokeProvider invoke = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, itemRaised);
                    Assert.NotNull(viewArgs);
                    Assert.Same(first, viewArgs.Tab);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabViewItem_IsClosableFalse_HidesCloseButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem locked = new() { Header = "Pinned", IsClosable = false, IsSelected = true };
                _ = tabs.Items.Add(locked);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = locked.ApplyTemplate();

                    FrameworkElement closeButton = Assert.IsType<FrameworkElement>(locked.Template.FindName("PART_CloseButton", locked), exactMatch: false);
                    Assert.False(closeButton.IsVisible,
                        "IsClosable=false should hide the close button regardless of overlay mode.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabView_AddTabButtonHidden_WhenIsAddTabButtonVisibleFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = false };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement addButton = Assert.IsType<FrameworkElement>(tabs.Template.FindName("PART_AddTabButton", tabs), exactMatch: false);
                    Assert.False(addButton.IsVisible,
                        "IsAddTabButtonVisible=false should collapse the add button.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TabView_Items_AddsAndRemovesTabsOnDemandAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem first = new() { Header = "Alpha", IsSelected = true };
                _ = tabs.Items.Add(first);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewItem added = new() { Header = "Beta" };
                    _ = tabs.Items.Add(added);
                    tabs.SelectedItem = added;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, tabs.Items.Count);
                    Assert.Same(added, tabs.SelectedItem);

                    tabs.Items.Remove(first);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = Assert.Single(tabs.Items);
                    Assert.Same(added, tabs.Items[0]);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        // ---- TabView scroll buttons ----

        [Fact]
        public Task TabView_PART_ScrollBackButton_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Primitives.RepeatButton btn = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollBackButton"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task TabView_PART_ScrollForwardButton_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 2" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Primitives.RepeatButton btn = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollForwardButton"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task TabView_PART_TabContentScroller_ExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "Tab 1" });
                Window w = new() { Content = tv, Width = 600, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    ScrollViewer sv = Assert.IsType<ScrollViewer>(FindVisualChildByName<ScrollViewer>(tv, "PART_TabContentScroller"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task TabView_ScrollButtons_HiddenWhenNoTabOverflowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tv = new();
                _ = tv.Items.Add(new TabViewItem { Header = "A" });
                _ = tv.Items.Add(new TabViewItem { Header = "B" });
                // Wide window: 2 short tabs will not overflow a 700px wide control
                Window w = new() { Content = tv, Width = 700, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Primitives.RepeatButton back = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollBackButton"), exactMatch: false);
                    System.Windows.Controls.Primitives.RepeatButton fwd = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(tv, "PART_ScrollForwardButton"), exactMatch: false);

                    Assert.Equal(
                        Visibility.Collapsed, back.Visibility);
                    Assert.Equal(
                        Visibility.Collapsed, fwd.Visibility);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        /// <summary>
        /// A handler typed on the args class receives it directly, with no cast, from both the
        /// per-item event and the aggregated TabView event.
        /// </summary>
        [Fact]
        public Task CloseRequested_TypedHandler_ReceivesArgsWithoutCastAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 640, Height = 480 };
                TabView tabs = new();
                TabViewItem first = new() { Header = "One", IsClosable = true };
                _ = tabs.Items.Add(first);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewTabCloseRequestedEventArgs? itemArgs = null;
                    TabViewTabCloseRequestedEventArgs? viewArgs = null;
                    first.CloseRequested += (_, e) => itemArgs = e;
                    tabs.TabCloseRequested += (_, e) => viewArgs = e;

                    first.RaiseEvent(new TabViewTabCloseRequestedEventArgs(
                        TabViewItem.CloseRequestedEvent, first, first, first));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(itemArgs);
                    Assert.Same(first, itemArgs.Tab);
                    Assert.NotNull(viewArgs);
                    Assert.Same(first, viewArgs.Tab);
                    Assert.Same(first, viewArgs.Item);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// The aggregated event is still a bubbling RoutedEvent, so a parent element that never
        /// sees the TabView's CLR event can still handle it with AddHandler.
        /// </summary>
        [Fact]
        public Task TabCloseRequested_StillBubblesToAParentHandlerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 640, Height = 480 };
                Grid host = new();
                TabView tabs = new();
                TabViewItem first = new() { Header = "One", IsClosable = true };
                _ = tabs.Items.Add(first);
                _ = host.Children.Add(tabs);

                try
                {
                    window.Content = host;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewTabCloseRequestedEventArgs? bubbled = null;
                    host.AddHandler(
                        TabView.TabCloseRequestedEvent,
                        new EventHandler<TabViewTabCloseRequestedEventArgs>((_, e) => bubbled = e));

                    first.RaiseEvent(new TabViewTabCloseRequestedEventArgs(
                        TabViewItem.CloseRequestedEvent, first, first, first));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.NotNull(bubbled);
                    Assert.Same(first, bubbled.Tab);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
