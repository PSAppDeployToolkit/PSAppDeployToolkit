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
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.Flyout"/> / <see cref="Controls.FlyoutBase"/> /
    /// <see cref="Controls.FlyoutPresenter"/> family.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task FlyoutPresenter_DefaultStyle_AppliesFluentSurfaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
                    Border surface = Assert.IsAssignableFrom<Border>(FindVisualChild<Border>(presenter));

                    Assert.Equal(overlayRadius, surface.CornerRadius);
                    Assert.Equal(new Thickness(1), surface.BorderThickness);
                    Assert.Equal(new Thickness(16, 15, 16, 17), presenter.Padding);
                    Assert.Equal(96.0, presenter.MinWidth, 0.01);
                    Assert.Equal(456.0, presenter.MaxWidth, 0.01);
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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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

                    Popup popup = Assert.IsAssignableFrom<Popup>(flyout.HostPopup);
                    Assert.False(popup.StaysOpen, "Flyout popups must be light-dismiss (StaysOpen=false).");
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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
        public Task FlyoutBase_ShowAttachedFlyout_OpensAttachedFlyoutAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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

                    Popup popup = Assert.IsAssignableFrom<Popup>(flyout.HostPopup);
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
                    CustomPopupPlacement[] topPlacements = callback(popupSize, targetSize, default);
                    Assert.Equal(new Point(-20, -40), topPlacements[0].Point);

                    flyout.Placement = FlyoutPlacementMode.Bottom;
                    CustomPopupPlacement[] bottomPlacements = callback(popupSize, targetSize, default);
                    Assert.Equal(new Point(-20, 20), bottomPlacements[0].Point);
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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
            // to the target's top-left corner.
            Size popupSize = new(100, 40);
            Size targetSize = new(60, 20);

            CustomPopupPlacement[] top = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Top, popupSize, targetSize, default);
            Assert.Equal(new Point(-20, -40), top[0].Point);
            Assert.Equal(new Point(-20, 20), top[1].Point);

            CustomPopupPlacement[] bottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, default);
            Assert.Equal(new Point(-20, 20), bottom[0].Point);
            Assert.Equal(new Point(-20, -40), bottom[1].Point);

            CustomPopupPlacement[] left = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Left, popupSize, targetSize, default);
            Assert.Equal(new Point(-100, -10), left[0].Point);

            CustomPopupPlacement[] right = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Right, popupSize, targetSize, default);
            Assert.Equal(new Point(60, -10), right[0].Point);

            CustomPopupPlacement[] offsetBottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, new Point(5, 7));
            Assert.Equal(new Point(-15, 27), offsetBottom[0].Point);
        }

        [Fact]
        public Task Flyout_Escape_HidesFlyoutThroughClosingPipelineAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

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
                _ = MergeGenericDictionary(app);

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

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
    }
}
