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
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.Flyout"/> / <see cref="Controls.FlyoutBase"/> /
    /// <see cref="Controls.FlyoutPresenter"/> family.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void FlyoutPresenter_DefaultStyle_AppliesFluentSurface()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.FlyoutPresenter)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.FlyoutPresenter.");

                Window window = new() { Width = 400, Height = 300 };
                Controls.FlyoutPresenter presenter = new() { Content = "Surface" };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Border? surface = FindVisualChild<Border>(presenter);

                    Assert.IsNotNull(surface, "FlyoutPresenter template should render its surface Border.");
                    Assert.AreEqual(overlayRadius, surface.CornerRadius,
                        "FlyoutPresenter surface must use OverlayCornerRadius like the other flyout popups.");
                    Assert.AreEqual(new Thickness(1), surface.BorderThickness,
                        "FlyoutPresenter surface must use the 1px flyout stroke.");
                    Assert.AreEqual(new Thickness(16, 15, 16, 17), presenter.Padding,
                        "FlyoutPresenter.Padding must be the WinUI FlyoutContentThemePadding.");
                    Assert.AreEqual(96.0, presenter.MinWidth, 0.01, "FlyoutPresenter.MinWidth must be 96 per WinUI metrics.");
                    Assert.AreEqual(456.0, presenter.MaxWidth, 0.01, "FlyoutPresenter.MaxWidth must be 456 per WinUI metrics.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ShowAt_OpensLightDismissPopupAndPresentsContent()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Flyout body" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bool openingRaised = false;
                    bool openedRaised = false;
                    flyout.Opening += (_, _) => openingRaised = true;
                    flyout.Opened += (_, _) => openedRaised = true;

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup.");
                    Assert.IsTrue(openingRaised, "ShowAt should raise Opening before the popup opens.");
                    Assert.IsTrue(openedRaised, "ShowAt should raise Opened after the popup opens.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Assert.IsFalse(popup.StaysOpen, "Flyout popups must be light-dismiss (StaysOpen=false).");
                    Assert.IsTrue(popup.AllowsTransparency, "Flyout popups must allow transparency for the rounded surface.");
                    Assert.AreEqual(PopupAnimation.None, popup.PopupAnimation,
                        "Flyout popups must disable the popup fade; the presenter's Loaded storyboard owns the reveal.");
                    Assert.AreSame(target, popup.PlacementTarget, "ShowAt must anchor the popup to the placement target.");

                    Controls.FlyoutPresenter? presenter = popup.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.AreEqual("Flyout body", presenter.Content, "Flyout.Content must flow to the presenter.");

                    // The open reveal (slide from Y=-8 with a fade) must exist in the template
                    // and settle at rest once the 167ms storyboard completes.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => presenter.IsLoaded),
                        "The presenter must load inside the open popup.");
                    System.Windows.Media.TranslateTransform? translate =
                        presenter.Template.FindName("PresenterTranslate", presenter) as System.Windows.Media.TranslateTransform;
                    Assert.IsNotNull(translate, "The presenter template must expose the PresenterTranslate reveal transform.");
                    Border? surface = presenter.Template.FindName("PresenterSurface", presenter) as Border;
                    Assert.IsNotNull(surface, "The presenter template must expose the PresenterSurface root.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && surface.Opacity >= 1.0),
                        "The open reveal must settle at Y=0 and full opacity.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_Hide_ClosesPopupAndRaisesClosingThenClosed()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Closable" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before Hide is exercised.");

                    bool closingRaised = false;
                    bool closedRaised = false;
                    flyout.Closing += (_, _) => closingRaised = true;
                    flyout.Closed += (_, _) => closedRaised = true;

                    flyout.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Hide should close the flyout popup.");
                    Assert.IsTrue(closingRaised, "Hide should raise Closing before the popup closes.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately after Hide returns.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Hide should raise Closed after the popup closes.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ClosingCancel_KeepsFlyoutOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before the cancel scenario.");

                    flyout.Hide();
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(flyout.IsOpen, "Canceling Closing must keep the flyout open.");

                    cancelClose = false;
                    flyout.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
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

        [TestMethod]
        public void Flyout_ContentChange_FlowsToPresenter()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "First" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before content is swapped.");

                    Controls.FlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.AreEqual("First", presenter.Content, "The initial Flyout.Content must reach the presenter.");

                    flyout.Content = "Second";
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual("Second", presenter.Content, "Flyout.Content changes must flow to the presenter binding.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FlyoutBase_ShowAttachedFlyout_OpensAttachedFlyout()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button owner = new() { Content = "Owner" };
                Controls.Flyout flyout = new() { Content = "Attached" };

                try
                {
                    window.Content = owner;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.FlyoutBase.SetAttachedFlyout(owner, flyout);
                    Assert.AreSame(flyout, Controls.FlyoutBase.GetAttachedFlyout(owner),
                        "GetAttachedFlyout must return the flyout set via SetAttachedFlyout.");

                    Controls.FlyoutBase.ShowAttachedFlyout(owner);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAttachedFlyout should open the attached flyout.");
                    Assert.AreSame(owner, flyout.HostPopup?.PlacementTarget,
                        "ShowAttachedFlyout must anchor the flyout to the owner element.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_PlacementModes_MapToPopupPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Placed" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(FlyoutPlacementMode.Top, flyout.Placement,
                        "Flyout placement must default to Top per the WinUI FlyoutBase contract.");
                    Assert.IsTrue(flyout.ShouldConstrainToRootBounds,
                        "ShouldConstrainToRootBounds must default to true.");

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before placement mapping is verified.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Custom, popup.Placement,
                        "The flyout popup must use Custom placement so it can center on the target edge like WinUI.");
                    Assert.IsNotNull(popup.CustomPopupPlacementCallback,
                        "The flyout popup must carry the edge-centering placement callback.");

                    // The popup side mapping that feeds the callback.
                    Assert.AreEqual(PlacementMode.Top, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Top),
                        "Top must map to the top side.");
                    Assert.AreEqual(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Bottom),
                        "Bottom must map to the bottom side.");
                    Assert.AreEqual(PlacementMode.Left, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Left),
                        "Left must map to the left side.");
                    Assert.AreEqual(PlacementMode.Right, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Right),
                        "Right must map to the right side.");
                    Assert.AreEqual(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Full),
                        "Full currently maps to the bottom side.");
                    Assert.AreEqual(PlacementMode.Bottom, Controls.FlyoutBase.MapPlacementSide(FlyoutPlacementMode.Auto),
                        "Auto currently maps to the bottom side.");

                    // The live popup callback must follow the flyout's current Placement: the
                    // default Top placement centers the popup horizontally above the target.
                    Size popupSize = new(100, 40);
                    Size targetSize = new(60, 20);
                    CustomPopupPlacement[] topPlacements = popup.CustomPopupPlacementCallback(popupSize, targetSize, default);
                    Assert.AreEqual(new Point(-20, -40), topPlacements[0].Point,
                        "Top placement must center the popup horizontally on the target's top edge.");

                    flyout.Placement = FlyoutPlacementMode.Bottom;
                    CustomPopupPlacement[] bottomPlacements = popup.CustomPopupPlacementCallback(popupSize, targetSize, default);
                    Assert.AreEqual(new Point(-20, 20), bottomPlacements[0].Point,
                        "Bottom placement must center the popup horizontally on the target's bottom edge.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FlyoutBase_GetEdgeCenteredPlacements_CentersOnFacingEdge()
        {
            // Pure placement math: a 100x40 popup against a 60x20 target. Points are relative
            // to the target's top-left corner.
            Size popupSize = new(100, 40);
            Size targetSize = new(60, 20);

            CustomPopupPlacement[] top = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Top, popupSize, targetSize, default);
            Assert.AreEqual(new Point(-20, -40), top[0].Point,
                "Top must center horizontally ((60-100)/2 = -20) with the popup bottom on the target top.");
            Assert.AreEqual(new Point(-20, 20), top[1].Point,
                "Top must offer the bottom edge as the screen-edge flip fallback.");

            CustomPopupPlacement[] bottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, default);
            Assert.AreEqual(new Point(-20, 20), bottom[0].Point,
                "Bottom must center horizontally with the popup top on the target bottom.");
            Assert.AreEqual(new Point(-20, -40), bottom[1].Point,
                "Bottom must offer the top edge as the screen-edge flip fallback.");

            CustomPopupPlacement[] left = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Left, popupSize, targetSize, default);
            Assert.AreEqual(new Point(-100, -10), left[0].Point,
                "Left must center vertically ((20-40)/2 = -10) with the popup right on the target left.");

            CustomPopupPlacement[] right = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Right, popupSize, targetSize, default);
            Assert.AreEqual(new Point(60, -10), right[0].Point,
                "Right must center vertically with the popup left on the target right.");

            CustomPopupPlacement[] offsetBottom = Controls.FlyoutBase.GetEdgeCenteredPlacements(
                PlacementMode.Bottom, popupSize, targetSize, new Point(5, 7));
            Assert.AreEqual(new Point(-15, 27), offsetBottom[0].Point,
                "HorizontalOffset/VerticalOffset must shift the centered placement.");
        }

        [TestMethod]
        public void Flyout_Escape_HidesFlyoutThroughClosingPipeline()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Dismiss me" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before Escape is simulated.");

                    bool closingRaised = false;
                    flyout.Closing += (_, _) => closingRaised = true;

                    Controls.FlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    presenter.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Escape)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Escape inside the flyout must dismiss it.");
                    Assert.IsTrue(closingRaised, "The Escape dismissal must run through the cancelable Closing event.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void Flyout_ShowAt_FlowsTargetDataContextIntoPresenter()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                object viewModel = new();
                Button target = new() { Content = "Anchor", DataContext = viewModel };
                Controls.Flyout flyout = new() { Content = "Bound" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup before the DataContext is verified.");

                    Controls.FlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.AreSame(viewModel, presenter.DataContext,
                        "ShowAt must flow the placement target's DataContext onto the presenter.");

                    flyout.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Hide should close the flyout popup before the cleanup is verified.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => presenter.DataContext is null),
                        "Closing must clear the DataContext flowed onto the presenter.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.HostPopup?.PlacementTarget is null),
                        "Closing must release the popup's placement target so the flyout does not pin the anchor.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void FlyoutPresenter_ThemeCycle_SurfaceBrushesResolve()
        {
            WpfTestSta.Invoke(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys = ["SolidBackgroundFillColorTertiaryBrush", "SurfaceStrokeColorFlyoutBrush", "TextFillColorPrimaryBrush"];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in FlyoutPresenter theme cycle step: {1}", key, theme));
                    }
                }
            });
        }
    }
}
