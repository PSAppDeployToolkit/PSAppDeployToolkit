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
using System.Collections.ObjectModel;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Input;
using System.Windows.Media;

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
                    Source = this
                };
                OnMouseLeftButtonDown(args);
            }

            public void SimulateMouseUp()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonUpEvent,
                    Source = this
                };
                OnMouseLeftButtonUp(args);
            }
        }

        [TestMethod]
        public void BreadcrumbBar_DefaultStyle_GeneratesBreadcrumbBarItemContainers()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.BreadcrumbBar)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.BreadcrumbBar.");

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

                    Assert.AreEqual(crumbs.Length, bar.Items.Count, "Binding ItemsSource must surface every crumb item.");
                    for (int index = 0; index < crumbs.Length; index++)
                    {
                        Controls.BreadcrumbBarItem? container =
                            bar.ItemContainerGenerator.ContainerFromIndex(index) as Controls.BreadcrumbBarItem;
                        Assert.IsNotNull(container,
                            string.Format("The container at index {0} must be a BreadcrumbBarItem.", index));
                        Assert.AreEqual(crumbs[index], container.Content,
                            string.Format("The container at index {0} must carry its bound item as content.", index));
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void BreadcrumbBar_LastItem_HidesChevronAndUsesPrimaryTypography()
        {
            RunOnStaThread(() =>
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
                    Assert.IsNotNull(primaryBrush, "TextFillColorPrimaryBrush must resolve.");

                    for (int index = 0; index < crumbs.Length - 1; index++)
                    {
                        Controls.BreadcrumbBarItem? ancestor =
                            bar.ItemContainerGenerator.ContainerFromIndex(index) as Controls.BreadcrumbBarItem;
                        Assert.IsNotNull(ancestor,
                            string.Format("The ancestor container at index {0} must be a BreadcrumbBarItem.", index));
                        Assert.IsFalse(ancestor.IsLastItem,
                            string.Format("The ancestor crumb at index {0} must not report IsLastItem.", index));

                        Controls.FontIcon? chevron = FindVisualChildByName<Controls.FontIcon>(ancestor, "ChevronIcon");
                        Assert.IsNotNull(chevron,
                            string.Format("The ancestor crumb at index {0} must render its chevron separator.", index));
                        Assert.AreEqual(Visibility.Visible, chevron.Visibility,
                            string.Format("The chevron of the ancestor crumb at index {0} must be visible.", index));

                        // WinUI BreadcrumbBarChevronLeftToRight is E974 painted in
                        // BreadcrumbBarNormalForegroundBrush (TextFillColorPrimaryBrush).
                        Assert.AreEqual("", chevron.Glyph,
                            string.Format("The chevron of the ancestor crumb at index {0} must use the WinUI E974 glyph.", index));
                        SolidColorBrush? chevronForeground = chevron.Foreground as SolidColorBrush;
                        Assert.IsNotNull(chevronForeground,
                            string.Format("The chevron of the ancestor crumb at index {0} must use a solid foreground brush.", index));
                        Assert.AreEqual(primaryBrush.Color, chevronForeground.Color,
                            string.Format("The chevron of the ancestor crumb at index {0} must use the primary text fill.", index));

                        SolidColorBrush? ancestorForeground = ancestor.Foreground as SolidColorBrush;
                        Assert.IsNotNull(ancestorForeground,
                            string.Format("The ancestor crumb at index {0} must use a solid foreground brush.", index));
                        Assert.AreEqual(primaryBrush.Color, ancestorForeground.Color,
                            string.Format("The ancestor crumb at index {0} must use the primary text fill at rest, matching WinUI.", index));
                    }

                    Controls.BreadcrumbBarItem? last =
                        bar.ItemContainerGenerator.ContainerFromIndex(crumbs.Length - 1) as Controls.BreadcrumbBarItem;
                    Assert.IsNotNull(last, "The last container must be a BreadcrumbBarItem.");
                    Assert.IsTrue(last.IsLastItem, "The last crumb must report IsLastItem=true.");
                    Assert.AreEqual(FontWeights.SemiBold, last.FontWeight,
                        "The last crumb must switch to SemiBold typography.");

                    Controls.FontIcon? lastChevron = FindVisualChildByName<Controls.FontIcon>(last, "ChevronIcon");
                    Assert.IsNotNull(lastChevron, "The last crumb template must still contain the chevron element.");
                    Assert.AreEqual(Visibility.Collapsed, lastChevron.Visibility,
                        "The last crumb must collapse its trailing chevron.");

                    SolidColorBrush? lastForeground = last.Foreground as SolidColorBrush;
                    Assert.IsNotNull(lastForeground, "The last crumb must use a solid foreground brush.");
                    Assert.AreEqual(primaryBrush.Color, lastForeground.Color,
                        "The last crumb must use the primary text fill.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void BreadcrumbBar_CrumbClick_RaisesItemClickedWithItemAndIndex()
        {
            RunOnStaThread(() =>
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
                    Assert.IsNotNull(ancestor, "The container at index 1 must be a BreadcrumbBarItem.");

                    ancestor.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, ancestor));
                    Assert.AreEqual(1, raiseCount, "Clicking an ancestor crumb must raise ItemClicked once.");
                    Assert.AreEqual("Documents", clickedItem, "ItemClicked must carry the clicked data item.");
                    Assert.AreEqual(1, clickedIndex, "ItemClicked must carry the clicked crumb's index.");

                    Controls.BreadcrumbBarItem? last =
                        bar.ItemContainerGenerator.ContainerFromIndex(2) as Controls.BreadcrumbBarItem;
                    Assert.IsNotNull(last, "The container at index 2 must be a BreadcrumbBarItem.");

                    last.RaiseEvent(new RoutedEventArgs(Controls.BreadcrumbBarItem.ClickEvent, last));
                    Assert.AreEqual(2, raiseCount,
                        "Clicking the last crumb must also raise ItemClicked, matching WinUI.");
                    Assert.AreEqual("Design", clickedItem, "ItemClicked must carry the last crumb's data item.");
                    Assert.AreEqual(2, clickedIndex, "ItemClicked must carry the last crumb's index.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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

                    Assert.IsFalse(first.IsLastItem, "A directly added ancestor crumb must not report IsLastItem.");
                    Assert.IsTrue(second.IsLastItem, "The directly added final crumb must report IsLastItem.");

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
                    Assert.IsTrue(first.IsPressed, "IsPressed must flip true after a left-button press.");

                    first.SimulateMouseUp();
                    Assert.IsFalse(first.IsPressed, "IsPressed must reset after the left-button release.");
                    Assert.AreEqual(1, raiseCount, "A press-release pair on a crumb must raise ItemClicked once.");
                    Assert.AreSame(first, clickedItem,
                        "ItemClicked must carry the crumb itself when it was added directly to Items.");
                    Assert.AreEqual(0, clickedIndex, "ItemClicked must carry the mouse-clicked crumb's index.");

                    _ = second.Focus();
                    PresentationSource? source = PresentationSource.FromVisual(second);
                    Assert.IsNotNull(source, "The crumb must have a presentation source once the window is shown.");

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyDownEvent,
                    });
                    Assert.IsTrue(second.IsPressed, "Enter key-down on a focused crumb must press it.");

                    second.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, Key.Enter)
                    {
                        RoutedEvent = Keyboard.KeyUpEvent,
                    });
                    Assert.IsFalse(second.IsPressed, "Enter key-up must release the crumb.");
                    Assert.AreEqual(2, raiseCount, "Enter on a focused crumb must raise ItemClicked.");
                    Assert.AreSame(second, clickedItem, "ItemClicked must carry the keyboard-activated crumb.");
                    Assert.AreEqual(1, clickedIndex, "ItemClicked must carry the keyboard-activated crumb's index.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(documents, "The container at index 1 must be a BreadcrumbBarItem.");
                    Assert.IsTrue(documents.IsLastItem, "The final crumb must start with IsLastItem=true.");

                    crumbs.Add("Design");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                        () => bar.ItemContainerGenerator.ContainerFromIndex(2) is Controls.BreadcrumbBarItem { IsLastItem: true }),
                        "Adding a crumb must realize a new last container with IsLastItem=true.");
                    Assert.IsFalse(documents.IsLastItem,
                        "The previously last crumb must lose IsLastItem after an append.");

                    Controls.FontIcon? documentsChevron = FindVisualChildByName<Controls.FontIcon>(documents, "ChevronIcon");
                    Assert.IsNotNull(documentsChevron, "The demoted crumb must render its chevron element.");
                    Assert.AreEqual(Visibility.Visible, documentsChevron.Visibility,
                        "The demoted crumb must show its chevron again once a crumb follows it.");

                    crumbs.RemoveAt(2);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.IsTrue(documents.IsLastItem,
                        "Removing the trailing crumb must promote the previous crumb back to IsLastItem=true.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void BreadcrumbBar_ThemeCycle_CrumbBrushesResolve()
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
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in BreadcrumbBar theme cycle step: {1}", key, theme));
                    }
                }
            });
        }

        [TestMethod]
        public void BreadcrumbBar_AutomationPeer_ReportsGroupClassNameAndName()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 500, Height = 200 };
                Controls.BreadcrumbBar bar = new();
                string[] crumbs = ["Home", "Documents"];
                bar.ItemsSource = crumbs;
                AutomationProperties.SetName(bar, "Navigation breadcrumb");

                try
                {
                    window.Content = bar;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AutomationPeer? peer = UIElementAutomationPeer.CreatePeerForElement(bar);
                    Assert.IsNotNull(peer, "BreadcrumbBar must create an automation peer.");
                    _ = Assert.IsInstanceOfType<Automation.BreadcrumbBarAutomationPeer>(peer,
                        "BreadcrumbBar must expose the BreadcrumbBarAutomationPeer.");
                    Assert.AreEqual("BreadcrumbBar", peer.GetClassName(),
                        "The peer must report the BreadcrumbBar class name.");
                    Assert.AreEqual(AutomationControlType.Group, peer.GetAutomationControlType(),
                        "The peer must report the Group control type.");
                    Assert.AreEqual("Navigation breadcrumb", peer.GetName(),
                        "The peer name must come from AutomationProperties.Name.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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

                    Assert.IsFalse(first.IsLastItem, "The pressed crumb must be an ancestor (non-last) item.");

                    ScaleTransform? pressScale = first.Template.FindName("PressScale", first) as ScaleTransform;
                    Assert.IsNotNull(pressScale, "The BreadcrumbBarItem template must expose the PressScale transform.");
                    Assert.AreEqual(1.0, pressScale.ScaleX, 0.001, "The content plate must rest at 1.0 scale.");

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    first.SimulateMouseDown();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98),
                        "Pressing a crumb must animate its content plate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    first.SimulateMouseUp();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
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
