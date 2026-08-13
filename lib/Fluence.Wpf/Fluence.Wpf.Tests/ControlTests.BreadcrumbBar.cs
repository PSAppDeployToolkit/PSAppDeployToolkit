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
using System.Collections.ObjectModel;
using System.Globalization;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.BreadcrumbBar"/> /
    /// <see cref="Controls.BreadcrumbBarItem"/> family.
    /// </summary>
    public partial class ControlTests
    {
        // Lightweight subclass that exposes the protected mouse button overrides so we
        // can assert crumb click semantics without relying on a real input device.
        private sealed class BreadcrumbBarItemProbe : Controls.BreadcrumbBarItem
        {
            public void SimulateMouseDown()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent,
                    Source = this,
                };
                OnMouseLeftButtonDown(args);
            }

            public void SimulateMouseUp()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonUpEvent,
                    Source = this,
                };
                OnMouseLeftButtonUp(args);
            }
        }

        [Fact]
        public void BreadcrumbBar_DefaultStyle_GeneratesBreadcrumbBarItemContainers()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.BreadcrumbBar)) as Style;
                Assert.NotNull(style);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(crumbs.Length, bar.Items.Count);
                    for (int index = 0; index < crumbs.Length; index++)
                    {
                        Controls.BreadcrumbBarItem? container =
                            bar.ItemContainerGenerator.ContainerFromIndex(index) as Controls.BreadcrumbBarItem;
                        Assert.NotNull(container);
                        Assert.Equal(crumbs[index], container.Content);
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBar_LastItem_HidesChevronAndUsesPrimaryTypography()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush? primaryBrush = app?.TryFindResource("TextFillColorPrimaryBrush") as SolidColorBrush;
                    Assert.NotNull(primaryBrush);

                    for (int index = 0; index < crumbs.Length - 1; index++)
                    {
                        Controls.BreadcrumbBarItem? ancestor =
                            bar.ItemContainerGenerator.ContainerFromIndex(index) as Controls.BreadcrumbBarItem;
                        Assert.NotNull(ancestor);
                        Assert.False(ancestor.IsLastItem,
                            string.Format(CultureInfo.InvariantCulture, "The ancestor crumb at index {0} must not report IsLastItem.", index));

                        Controls.FontIcon? chevron = FindVisualChildByName<Controls.FontIcon>(ancestor, "ChevronIcon");
                        Assert.NotNull(chevron);
                        Assert.Equal(Visibility.Visible, chevron.Visibility);

                        // WinUI BreadcrumbBarChevronLeftToRight is E974 painted in
                        // BreadcrumbBarNormalForegroundBrush (TextFillColorPrimaryBrush).
                        Assert.Equal("", chevron.Glyph, StringComparer.Ordinal);
                        SolidColorBrush? chevronForeground = chevron.Foreground as SolidColorBrush;
                        Assert.NotNull(chevronForeground);
                        Assert.Equal(primaryBrush.Color, chevronForeground.Color);

                        SolidColorBrush? ancestorForeground = ancestor.Foreground as SolidColorBrush;
                        Assert.NotNull(ancestorForeground);
                        Assert.Equal(primaryBrush.Color, ancestorForeground.Color);
                    }

                    Controls.BreadcrumbBarItem? last =
                        bar.ItemContainerGenerator.ContainerFromIndex(crumbs.Length - 1) as Controls.BreadcrumbBarItem;
                    Assert.NotNull(last);
                    Assert.True(last.IsLastItem, "The last crumb must report IsLastItem=true.");
                    Assert.Equal(FontWeights.SemiBold, last.FontWeight);

                    Controls.FontIcon? lastChevron = FindVisualChildByName<Controls.FontIcon>(last, "ChevronIcon");
                    Assert.NotNull(lastChevron);
                    Assert.Equal(Visibility.Collapsed, lastChevron.Visibility);

                    SolidColorBrush? lastForeground = last.Foreground as SolidColorBrush;
                    Assert.NotNull(lastForeground);
                    Assert.Equal(primaryBrush.Color, lastForeground.Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBar_CrumbClick_RaisesItemClickedWithItemAndIndex()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    ItemsSource = (string[])["Home", "Documents", "Design"],
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    int raiseCount = 0;
                    bar.ItemClicked += (_, args) =>
                    {
                        clickedItem = args.Item;
                        clickedIndex = args.Index;
                        raiseCount++;
                    };

                    Controls.BreadcrumbBarItem? ancestor =
                        bar.ItemContainerGenerator.ContainerFromIndex(1) as Controls.BreadcrumbBarItem;
                    Assert.NotNull(ancestor);

                    ancestor.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, ancestor));
                    Assert.Equal(1, raiseCount);
                    Assert.Equal("Documents", clickedItem);
                    Assert.Equal(1, clickedIndex);

                    Controls.BreadcrumbBarItem? last =
                        bar.ItemContainerGenerator.ContainerFromIndex(2) as Controls.BreadcrumbBarItem;
                    Assert.NotNull(last);

                    last.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, last));
                    Assert.Equal(2, raiseCount);
                    Assert.Equal("Design", clickedItem);
                    Assert.Equal(2, clickedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBarItem_MouseAndKeyboard_ActivateCrumb()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                BreadcrumbBarItemProbe first = new() { Content = "Home" };
                BreadcrumbBarItemProbe second = new() { Content = "Documents" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(first.IsLastItem, "A directly added ancestor crumb must not report IsLastItem.");
                    Assert.True(second.IsLastItem, "The directly added final crumb must report IsLastItem.");

                    object? clickedItem = null;
                    int clickedIndex = -1;
                    int raiseCount = 0;
                    bar.ItemClicked += (_, args) =>
                    {
                        clickedItem = args.Item;
                        clickedIndex = args.Index;
                        raiseCount++;
                    };

                    first.SimulateMouseDown();
                    Assert.True(first.IsPressed, "IsPressed must flip true after a left-button press.");

                    first.SimulateMouseUp();
                    Assert.False(first.IsPressed, "IsPressed must reset after the left-button release.");
                    Assert.Equal(1, raiseCount);
                    Assert.Same(first, clickedItem);
                    Assert.Equal(0, clickedIndex);

                    _ = second.Focus();
                    PresentationSource? source = PresentationSource.FromVisual(second);
                    Assert.NotNull(source);

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.True(second.IsPressed, "Enter key-down on a focused crumb must press it.");

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyUpEvent,
                    });
                    Assert.False(second.IsPressed, "Enter key-up must release the crumb.");
                    Assert.Equal(2, raiseCount);
                    Assert.Same(second, clickedItem);
                    Assert.Equal(1, clickedIndex);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBar_ItemsChanges_UpdateLastItemState()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                ObservableCollection<string> crumbs = ["Home", "Documents"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.BreadcrumbBarItem? documents =
                        bar.ItemContainerGenerator.ContainerFromIndex(1) as Controls.BreadcrumbBarItem;
                    Assert.NotNull(documents);
                    Assert.True(documents.IsLastItem, "The final crumb must start with IsLastItem=true.");

                    crumbs.Add("Design");
                    Assert.True(WaitUntil(window.Dispatcher, 2000,
                        () => bar.ItemContainerGenerator.ContainerFromIndex(2) is Controls.BreadcrumbBarItem { IsLastItem: true }),
                        "Adding a crumb must realize a new last container with IsLastItem=true.");
                    Assert.False(documents.IsLastItem,
                        "The previously last crumb must lose IsLastItem after an append.");

                    Controls.FontIcon? documentsChevron = FindVisualChildByName<Controls.FontIcon>(documents, "ChevronIcon");
                    Assert.NotNull(documentsChevron);
                    Assert.Equal(Visibility.Visible, documentsChevron.Visibility);

                    crumbs.RemoveAt(2);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.True(documents.IsLastItem,
                        "Removing the trailing crumb must promote the previous crumb back to IsLastItem=true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBar_ThemeCycle_CrumbBrushesResolve()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public void BreadcrumbBar_AutomationPeer_ReportsGroupClassNameAndName()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new()
                {
                    ItemsSource = (string[])["Home", "Documents"],
                };
                AutomationProperties.SetName(bar, "Navigation breadcrumb");

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(bar);
                    Assert.NotNull(peer);
                    _ = Assert.IsAssignableFrom<Automation.BreadcrumbBarAutomationPeer>(peer);
                    Assert.Equal("BreadcrumbBar", peer.GetClassName(), StringComparer.Ordinal);
                    Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
                    Assert.Equal("Navigation breadcrumb", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void BreadcrumbBarItem_Pressed_AnimatesContentPlatePressScale()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                BreadcrumbBarItemProbe first = new() { Content = "Home" };
                BreadcrumbBarItemProbe second = new() { Content = "Documents" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(first.IsLastItem, "The pressed crumb must be an ancestor (non-last) item.");

                    ScaleTransform? pressScale = first.Template.FindName("PressScale", first) as ScaleTransform;
                    Assert.NotNull(pressScale);
                    Assert.Equal(1.0, pressScale.ScaleX, 0.001);

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    first.SimulateMouseDown();
                    Assert.True(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98),
                        "Pressing a crumb must animate its content plate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    first.SimulateMouseUp();
                    Assert.True(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX >= 1.0 && pressScale.ScaleY >= 1.0),
                        "Releasing a crumb must animate its content plate back to 1.0 scale.");
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
