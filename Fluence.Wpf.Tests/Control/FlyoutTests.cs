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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using Fluence.Wpf.Tests.Infrastructure;
using Windows.Win32;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.Flyout"/> / <see cref="Controls.FlyoutBase"/> /
    /// <see cref="Controls.FlyoutPresenter"/> family.
    /// </summary>
    public sealed class FlyoutTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task FlyoutPresenter_DefaultStyle_AppliesFluentSurfaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Style style = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.FlyoutPresenter)));

                Window window = new() { Width = 400, Height = 300 };
                Controls.FlyoutPresenter presenter = new() { Content = "Surface" };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    CornerRadius? overlayRadius = (CornerRadius?)app.FindResource("OverlayCornerRadius");

                    // By name, not "the first Border in the tree": the elevation caster is painted
                    // ahead of the surface and would otherwise be the one under assertion.
                    Border surface = Assert.IsType<Border>(FindVisualChildByName<Border>(presenter, "PresenterSurface"), exactMatch: false);

                    Assert.Equal(overlayRadius, surface.CornerRadius);
                    Assert.Equal(new Thickness(1), surface.BorderThickness);
                    Assert.Equal(new Thickness(16, 15, 16, 17), presenter.Padding);

                    // FlyoutThemeMinWidth 96 / MaxWidth 456 (Flyout_themeresources.xaml) sit on the
                    // plate (PresenterSurface), not on the gutter-inclusive presenter control: a
                    // limit on the control would constrain the 16px gutter too and shrink the plate.
                    Assert.Equal(96.0, surface.MinWidth, 0.01);
                    Assert.Equal(456.0, surface.MaxWidth, 0.01);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_ShowAt_OpensLightDismissPopupAndPresentsContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Flyout body" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bool openingRaised = false;
                    bool openedRaised = false;
                    flyout.Opening += (_, _) => openingRaised = true;
                    flyout.Opened += (_, _) => openedRaised = true;

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup.");
                    Assert.True(openingRaised, "ShowAt should raise Opening before the popup opens.");
                    Assert.True(openedRaised, "ShowAt should raise Opened after the popup opens.");

                    Popup popup = Assert.IsType<Popup>(flyout.HostPopup, exactMatch: false);
                    // Light dismiss is run from the owning window rather than from the popup's own
                    // mouse capture, because a capture taken inside a button's Click handler is
                    // handed straight back when the button releases and the flyout closes as it
                    // appears. The popup therefore stays pinned and FlyoutBase watches for a press
                    // outside it; FlyoutBase_LightDismiss_ClosesOnAPressOutside covers the
                    // behaviour that replaced the flag.
                    Assert.True(popup.StaysOpen, "The popup is pinned; FlyoutBase owns the dismissal.");
                    Assert.True(popup.AllowsTransparency, "Flyout popups must allow transparency for the rounded surface.");
                    Assert.Equal(PopupAnimation.None, popup.PopupAnimation);
                    Assert.Same(target, popup.PlacementTarget);

                    Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(popup.Child);
                    Assert.Equal("Flyout body", presenter.Content);

                    // The open reveal (a placement-aware slide with a fade, run by
                    // FlyoutPresenter.OnLoaded) must target the named template parts and
                    // settle at rest once the 167ms reveal completes.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => presenter.IsLoaded).ConfigureAwait(true),
                        "The presenter must load inside the open popup.");
                    System.Windows.Media.TranslateTransform translate =
                        Assert.IsType<System.Windows.Media.TranslateTransform>(presenter.Template.FindName("PresenterTranslate", presenter));
                    Border surface = Assert.IsType<Border>(presenter.Template.FindName("PresenterSurface", presenter));
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && surface.Opacity >= 1.0).ConfigureAwait(true),
                        "The open reveal must settle at Y=0 and full opacity.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_Hide_ClosesPopupAndRaisesClosingThenClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Closable" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before Hide is exercised.");

                    bool closingRaised = false;
                    bool closedRaised = false;
                    flyout.Closing += (_, _) => closingRaised = true;
                    flyout.Closed += (_, _) => closedRaised = true;

                    flyout.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Hide should close the flyout popup.");
                    Assert.True(closingRaised, "Hide should raise Closing before the popup closes.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately after Hide returns.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => closedRaised).ConfigureAwait(true),
                        "Hide should raise Closed after the popup closes.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_ClosingCancel_KeepsFlyoutOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Sticky" };
                bool cancelClose = true;
                flyout.Closing += (_, args) => args.Cancel = cancelClose;

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before the cancel scenario.");

                    flyout.Hide();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(flyout.IsOpen, "Canceling Closing must keep the flyout open.");

                    cancelClose = false;
                    flyout.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Hide should close the flyout once Closing is no longer canceled.");
                }
                finally
                {
                    cancelClose = false;
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_ContentChange_FlowsToPresenterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "First" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before content is swapped.");

                    Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(flyout.HostPopup?.Child);
                    Assert.Equal("First", presenter.Content);

                    flyout.Content = "Second";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal("Second", presenter.Content);
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutBase_LightDismiss_ClosesOnAPressOutsideAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner", VerticalAlignment = VerticalAlignment.Top };
                Border elsewhere = new() { Height = 100, VerticalAlignment = VerticalAlignment.Bottom };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    Grid root = new();
                    _ = root.Children.Add(owner);
                    _ = root.Children.Add(elsewhere);
                    window.Content = root;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(owner);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "The flyout must open.");

                    // A press anywhere in the owning window closes the flyout. The flyout's own
                    // content lives in the popup's separate window, so a press inside it never
                    // reaches this handler and never closes it. Away from the anchor the press is
                    // left alone, so whatever was pressed still receives its click.
                    MouseButtonEventArgs press = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    elsewhere.RaiseEvent(press);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "A press outside the flyout must dismiss it.");
                    Assert.False(press.Handled, "A press away from the anchor must still reach what was pressed.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutBase_LightDismiss_PressOnTheAnchorClosesWithoutReopeningAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(owner);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "The flyout must open.");

                    // The anchor is usually the button whose Click opened the flyout. Its press has
                    // to close the flyout and stop there: left to continue, the click that follows
                    // would call ShowAt again and reopen what the press just closed, so the anchor
                    // could never toggle its own flyout shut. WinUI's light dismiss swallows the
                    // press the same way.
                    MouseButtonEventArgs press = new(Mouse.PrimaryDevice, Environment.TickCount, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    owner.RaiseEvent(press);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "A press on the anchor must dismiss the flyout.");
                    Assert.True(press.Handled, "A press on the anchor must be swallowed so the following click cannot reopen the flyout.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutBase_LightDismiss_ClosesWhenAnotherWindowOfTheSameApplicationIsActivatedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300, WindowStartupLocation = WindowStartupLocation.Manual, Left = 100, Top = 100 };
                Window other = new() { Width = 300, Height = 200, WindowStartupLocation = WindowStartupLocation.Manual, Left = 560, Top = 100 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    _ = window.Activate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(owner);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "The flyout must open.");

                    // Focusing the presenter hands activation to the popup, which must not read as a
                    // dismissal, or the flyout would close as it opens.
                    Assert.True(flyout.IsOpen, "The flyout's own activation handoff must not dismiss it.");

                    // WM_ACTIVATEAPP is raised only when activation leaves the application, so a
                    // second window of the same application never produces one. The popup this
                    // replaced closed here through capture loss, so the flyout has to as well.
                    other.Show();
                    _ = other.Activate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Activating another window of the same application must dismiss the flyout.");
                }
                finally
                {
                    flyout.Hide();
                    other.Close();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutBase_LightDismiss_ClosesWhenTheOwningWindowMovesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300, WindowStartupLocation = WindowStartupLocation.Manual, Left = 100, Top = 100 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(owner);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "The flyout must open.");

                    // The popup is pinned, so it does not follow the window it is anchored in. A move
                    // has to close the flyout, as it does for a WPF light-dismiss popup and in WinUI,
                    // rather than leave it floating where the anchor used to be.
                    window.Left += 40;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Moving the owning window must dismiss the flyout.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Theory]
        [InlineData((int)PInvoke.WM_NCLBUTTONDOWN, 0, true)]
        [InlineData((int)PInvoke.WM_NCRBUTTONDOWN, 0, true)]
        [InlineData((int)PInvoke.WM_NCMBUTTONDOWN, 0, true)]
        [InlineData((int)PInvoke.WM_ACTIVATEAPP, 0, true)]
        [InlineData((int)PInvoke.WM_ACTIVATEAPP, 1, false)]
        [InlineData((int)PInvoke.WM_NCHITTEST, 0, false)]
        [InlineData((int)PInvoke.WM_NCLBUTTONUP, 0, false)]
        public void FlyoutBase_DismissMessages_AreNonClientPressesAndLosingTheForeground(int msg, int wParam, bool expected)
        {
            // These are the light-dismiss signals that never surface as routed input: a press on
            // the caption or a resize border arrives as a non-client button message, and a click in
            // another application, on the desktop, or an Alt+Tab arrives as WM_ACTIVATEAPP with a
            // false wParam. Gaining the foreground back (a true wParam) must not close anything.
            Assert.Equal(expected, Controls.FlyoutBase.IsDismissMessage(msg, new IntPtr(wParam)));
        }

        [Theory]
        [InlineData((int)PInvoke.WM_ACTIVATE, 0, 0x2222, true)]
        [InlineData((int)PInvoke.WM_ACTIVATE, 0, 0, true)]
        [InlineData((int)PInvoke.WM_ACTIVATE, 0, 0x1111, false)]
        [InlineData((int)PInvoke.WM_ACTIVATE, 1, 0x2222, false)]
        [InlineData((int)PInvoke.WM_ACTIVATE, 0x00010000, 0x2222, true)]
        [InlineData((int)PInvoke.WM_ACTIVATEAPP, 0, 0x2222, false)]
        public void FlyoutBase_ForeignActivation_IsDeactivationToAWindowThatIsNotThePopup(int msg, int wParam, int lParam, bool expected)
        {
            // WA_INACTIVE (the low word of wParam) with any window other than the popup is the
            // handoff WM_ACTIVATEAPP cannot report: a second window of the same application. The
            // popup's own handle is exempt, because focusing the presenter activates it and the
            // flyout would otherwise close as it opens. A null handle means activation left for a
            // window this thread does not own, which dismisses. WA_ACTIVE never dismisses, and the
            // minimised flag rides the high word, so it must not change the decode.
            IntPtr popupHandle = new(0x1111);

            Assert.Equal(expected, Controls.FlyoutBase.IsForeignActivationMessage(msg, new IntPtr(wParam), new IntPtr(lParam), popupHandle));
        }

        [Fact]
        public Task FlyoutBase_ShownFromAClick_SurvivesTheButtonReleasingCaptureAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.FlyoutBase.SetAttachedFlyout(owner, flyout);

                    // A Button raises Click from its mouse-up handler while it still holds the
                    // mouse capture and releases that capture once the handler returns. Opening a
                    // light-dismiss popup in that window used to hand it a capture it lost again
                    // immediately, so the flyout vanished as it appeared.
                    Assert.True(Mouse.Capture(owner), "The owner must take the capture the gesture would give it.");
                    Controls.FlyoutBase.ShowAttachedFlyout(owner);

                    // The popup opens before the call returns, capture or no capture.
                    Assert.True(flyout.IsOpen, "ShowAt must open the flyout synchronously.");
                    _ = Mouse.Capture(element: null);

                    // And stay open: a dismissal arriving late would close it a frame later.
                    _ = await WaitUntilAsync(window.Dispatcher, 300, () => !flyout.IsOpen).ConfigureAwait(true);
                    Assert.True(flyout.IsOpen, "The flyout must survive the button releasing its capture.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutBase_ShowAttachedFlyout_OpensAttachedFlyoutAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.FlyoutBase.SetAttachedFlyout(owner, flyout);
                    Assert.Same(flyout, Controls.FlyoutBase.GetAttachedFlyout(owner));

                    Controls.FlyoutBase.ShowAttachedFlyout(owner);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAttachedFlyout should open the attached flyout.");
                    Assert.Same(owner, flyout.HostPopup?.PlacementTarget);
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_PlacementModes_MapToPopupPlacementAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Placed" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(FlyoutPlacementMode.Top, flyout.Placement);
                    Assert.True(flyout.ShouldConstrainToRootBounds,
                        "ShouldConstrainToRootBounds must default to true.");

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before placement mapping is verified.");

                    Popup popup = Assert.IsType<Popup>(flyout.HostPopup, exactMatch: false);
                    CustomPopupPlacementCallback? callback = Assert.IsType<CustomPopupPlacementCallback>(popup.CustomPopupPlacementCallback);
                    Assert.Equal(PlacementMode.Custom, popup.Placement);

                    // The popup side mapping that feeds the callback.
                    Assert.Equal(PlacementMode.Top, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Top));
                    Assert.Equal(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Bottom));
                    Assert.Equal(PlacementMode.Left, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Left));
                    Assert.Equal(PlacementMode.Right, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Right));
                    Assert.Equal(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Full));
                    Assert.Equal(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Auto));

                    // The live popup callback must follow the flyout's current Placement: the
                    // default Top placement centers the popup horizontally above the target.
                    Size popupSize = new(100, 40);
                    Size targetSize = new(60, 20);
                    // Points now include the 16px shadow gutter subtraction (FlyoutBase.ShadowGutter):
                    // the popup grew by the gutter on every side, so placement pulls each candidate
                    // back by the same amount to keep the plate where it was before the gutter.
                    CustomPopupPlacement[] topPlacements = callback(popupSize, targetSize, default);
                    Assert.Equal(new Point(-20, -24), topPlacements[0].Point);

                    flyout.Placement = FlyoutPlacementMode.Bottom;
                    CustomPopupPlacement[] bottomPlacements = callback(popupSize, targetSize, default);
                    Assert.Equal(new Point(-20, 4), bottomPlacements[0].Point);
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_ShowAt_StampsRevealPlacementWithMappedSideAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Directional" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.FlyoutPresenter unstamped = new();
                    Assert.Equal(PlacementMode.Bottom, unstamped.RevealPlacement);

                    foreach (FlyoutPlacementMode mode in new[]
                    {
                        FlyoutPlacementMode.Top,
                        FlyoutPlacementMode.Bottom,
                        FlyoutPlacementMode.Left,
                        FlyoutPlacementMode.Right,
                    })
                    {
                        flyout.Placement = mode;
                        flyout.ShowAt(target);
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                            string.Format("ShowAt should open the flyout popup for placement {0}.", mode));

                        Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(flyout.HostPopup?.Child);
                        PlacementMode expectedSide = Controls.FlyoutBase.MapPlacementSide(mode);
                        Assert.Equal(expectedSide, presenter.RevealPlacement);

                        flyout.Hide();
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                            string.Format("Hide should close the flyout popup for placement {0}.", mode));
                    }
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void FlyoutBase_GetEdgeCenteredPlacements_CentersOnFacingEdge()
        {
            // Pure placement math: a 100x40 popup against a 60x20 target. Points are relative
            // to the target's top-left corner. popupSize includes the 16px shadow gutter
            // (FlyoutBase.ShadowGutter) on every side, so each candidate is pulled back by the
            // same amount to keep the plate centered on the target edge rather than the popup.
            Size popupSize = new(100, 40);
            Size targetSize = new(60, 20);

            CustomPopupPlacement[] top = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Top, popupSize, targetSize, default);
            Assert.Equal(new Point(-20, -24), top[0].Point);
            Assert.Equal(new Point(-20, 4), top[1].Point);

            CustomPopupPlacement[] bottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, default);
            Assert.Equal(new Point(-20, 4), bottom[0].Point);
            Assert.Equal(new Point(-20, -24), bottom[1].Point);

            CustomPopupPlacement[] left = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Left, popupSize, targetSize, default);
            Assert.Equal(new Point(-84, -10), left[0].Point);

            CustomPopupPlacement[] right = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Right, popupSize, targetSize, default);
            Assert.Equal(new Point(44, -10), right[0].Point);

            CustomPopupPlacement[] offsetBottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, new Point(5, 7));
            Assert.Equal(new Point(-15, 11), offsetBottom[0].Point);
        }

        [Fact]
        public Task Flyout_Escape_HidesFlyoutThroughClosingPipelineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Dismiss me" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before Escape is simulated.");

                    bool closingRaised = false;
                    flyout.Closing += (_, _) => closingRaised = true;

                    Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(flyout.HostPopup?.Child);
                    presenter.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Escape)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Escape inside the flyout must dismiss it.");
                    Assert.True(closingRaised, "The Escape dismissal must run through the cancelable Closing event.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Flyout_ShowAt_FlowsTargetDataContextIntoPresenterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                object viewModel = new();
                Button target = new() { Content = "Anchor", DataContext = viewModel };
                Controls.Flyout flyout = new() { Content = "Bound" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup before the DataContext is verified.");

                    Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(flyout.HostPopup?.Child);
                    Assert.Same(viewModel, presenter.DataContext);

                    flyout.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true),
                        "Hide should close the flyout popup before the cleanup is verified.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => presenter.DataContext is null).ConfigureAwait(true),
                        "Closing must clear the DataContext flowed onto the presenter.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.HostPopup?.PlacementTarget is null).ConfigureAwait(true),
                        "Closing must release the popup's placement target so the flyout does not pin the anchor.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FlyoutPresenter_ThemeCycle_SurfaceBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app.TryFindResource(key));
                    }
                }
            });
        }
    }
}
