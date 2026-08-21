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
using System.Threading.Tasks;
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
        public Task BreadcrumbBar_DefaultStyle_GeneratesBreadcrumbBarItemContainersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.BreadcrumbBar)));

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(crumbs.Length, bar.Items.Count);
                    for (int index = 0; index < crumbs.Length; index++)
                    {
                        Controls.BreadcrumbBarItem container =
                            Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(index));
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
        public Task BreadcrumbBar_LastItem_HidesChevronAndUsesPrimaryTypographyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents", "Design"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush primaryBrush = Assert.IsType<SolidColorBrush>(app.TryFindResource("TextFillColorPrimaryBrush"));

                    for (int index = 0; index < crumbs.Length - 1; index++)
                    {
                        Controls.BreadcrumbBarItem ancestor =
                            Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(index));
                        Assert.False(ancestor.IsLastItem,
                            string.Format(CultureInfo.InvariantCulture, "The ancestor crumb at index {0} must not report IsLastItem.", index));

                        Controls.FontIcon chevron = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(ancestor, "ChevronIcon"));
                        Assert.Equal(Visibility.Visible, chevron.Visibility);

                        // WinUI BreadcrumbBarChevronLeftToRight is E974 painted in
                        // BreadcrumbBarNormalForegroundBrush (TextFillColorPrimaryBrush).
                        Assert.Equal("\uE974", chevron.Glyph, StringComparer.Ordinal);
                        SolidColorBrush chevronForeground = Assert.IsType<SolidColorBrush>(chevron.Foreground);
                        Assert.Equal(primaryBrush.Color, chevronForeground.Color);

                        SolidColorBrush ancestorForeground = Assert.IsType<SolidColorBrush>(ancestor.Foreground);
                        Assert.Equal(primaryBrush.Color, ancestorForeground.Color);
                    }

                    Controls.BreadcrumbBarItem last =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(crumbs.Length - 1));
                    Assert.True(last.IsLastItem, "The last crumb must report IsLastItem=true.");
                    Assert.Equal(FontWeights.SemiBold, last.FontWeight);

                    Controls.FontIcon lastChevron = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(last, "ChevronIcon"));
                    Assert.Equal(Visibility.Collapsed, lastChevron.Visibility);

                    SolidColorBrush lastForeground = Assert.IsType<SolidColorBrush>(last.Foreground);
                    Assert.Equal(primaryBrush.Color, lastForeground.Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_CrumbClick_RaisesItemClickedWithItemAndIndexAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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

                    Controls.BreadcrumbBarItem ancestor =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));

                    ancestor.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, ancestor));
                    Assert.Equal(1, raiseCount);
                    Assert.Equal("Documents", clickedItem);
                    Assert.Equal(1, clickedIndex);

                    Controls.BreadcrumbBarItem last =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(2));

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
        public Task BreadcrumbBarItem_MouseAndKeyboard_ActivateCrumbAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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
                    PresentationSource source = Assert.IsAssignableFrom<PresentationSource>(PresentationSource.FromVisual(second));

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
        public Task BreadcrumbBar_ItemsChanges_UpdateLastItemStateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                ObservableCollection<string> crumbs = ["Home", "Documents"];
                bar.ItemsSource = crumbs;

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.BreadcrumbBarItem documents =
                        Assert.IsType<Controls.BreadcrumbBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));
                    Assert.True(documents.IsLastItem, "The final crumb must start with IsLastItem=true.");

                    crumbs.Add("Design");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => bar.ItemContainerGenerator.ContainerFromIndex(2) is Controls.BreadcrumbBarItem { IsLastItem: true }).ConfigureAwait(true),
                        "Adding a crumb must realize a new last container with IsLastItem=true.");
                    Assert.False(documents.IsLastItem,
                        "The previously last crumb must lose IsLastItem after an append.");

                    Controls.FontIcon documentsChevron = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildByName<Controls.FontIcon>(documents, "ChevronIcon"));
                    Assert.Equal(Visibility.Visible, documentsChevron.Visibility);

                    crumbs.RemoveAt(2);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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
        public Task BreadcrumbBar_ThemeCycle_CrumbBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public Task BreadcrumbBar_AutomationPeer_ReportsGroupClassNameAndNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer peer = Assert.IsAssignableFrom<AutomationPeer>(UIElementAutomationPeer.CreatePeerForElement(bar));
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
        public Task BreadcrumbBarItem_Pressed_AnimatesContentPlatePressScaleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(first.IsLastItem, "The pressed crumb must be an ancestor (non-last) item.");

                    ScaleTransform pressScale = Assert.IsType<ScaleTransform>(first.Template.FindName("PressScale", first));
                    Assert.Equal(1.0, pressScale.ScaleX, 0.001);

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    first.SimulateMouseDown();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98).ConfigureAwait(true),
                        "Pressing a crumb must animate its content plate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    first.SimulateMouseUp();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => pressScale.ScaleX >= 1.0 && pressScale.ScaleY >= 1.0).ConfigureAwait(true),
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
