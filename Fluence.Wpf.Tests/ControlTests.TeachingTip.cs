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
using System.Linq;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.TeachingTip"/> control: default style and
    /// template parts, popup hosting, placement and beak resolution, light dismiss mapping,
    /// footer button behavior, and surface brush theming.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void TeachingTip_DefaultStyle_AppliesAndTemplatePartsFound()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TeachingTip defaults = new();
                Assert.AreEqual(string.Empty, defaults.Title, "Title must default to an empty string.");
                Assert.AreEqual(string.Empty, defaults.Subtitle, "Subtitle must default to an empty string.");
                Assert.IsFalse(defaults.IsOpen, "IsOpen must default to false.");
                Assert.IsFalse(defaults.IsLightDismissEnabled, "IsLightDismissEnabled must default to false.");
                Assert.IsNull(defaults.Target, "Target must default to null.");
                Assert.AreEqual(TeachingTipPlacementMode.Auto, defaults.PreferredPlacement,
                    "PreferredPlacement must default to Auto per the WinUI TeachingTip contract.");

                Window window = new() { Width = 640, Height = 480 };
                Grid host = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Title",
                    Subtitle = "Subtitle",
                    Content = "Body",
                };
                _ = host.Children.Add(tip);
                window.Content = host;

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, tip.Visibility,
                        "A closed tip declared in a panel must stay collapsed until it is popup-hosted.");

                    // The collapsed at-rest tip is skipped by layout, so inflate the template
                    // explicitly to assert the template contract.
                    _ = tip.ApplyTemplate();

                    Assert.AreEqual(320.0, tip.MinWidth, 0.01, "TeachingTip.MinWidth must be the WinUI TeachingTipMinWidth (320).");
                    Assert.AreEqual(336.0, tip.MaxWidth, 0.01, "TeachingTip.MaxWidth must be the WinUI TeachingTipMaxWidth (336).");
                    Assert.AreEqual(new Thickness(16, 15, 16, 17), tip.Padding,
                        "TeachingTip.Padding must reuse the WinUI FlyoutContentThemePadding like FlyoutPresenter.");

                    Border? surface = FindVisualChildByName<Border>(tip, "TipSurface");
                    Assert.IsNotNull(surface, "TipSurface must exist in the TeachingTip template (Fluence style applied).");
                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Assert.AreEqual(overlayRadius, surface.CornerRadius,
                        "TeachingTip surface must use OverlayCornerRadius like the other flyout popups.");
                    Assert.AreEqual(new Thickness(1), surface.BorderThickness,
                        "TeachingTip surface must use the 1px flyout stroke.");

                    ButtonBase? action = FindVisualChildByName<ButtonBase>(tip, "PART_ActionButton");
                    ButtonBase? close = FindVisualChildByName<ButtonBase>(tip, "PART_CloseButton");
                    ButtonBase? alternateClose = FindVisualChildByName<ButtonBase>(tip, "PART_AlternateCloseButton");
                    Assert.IsNotNull(action, "PART_ActionButton must exist in the TeachingTip template.");
                    Assert.IsNotNull(close, "PART_CloseButton must exist in the TeachingTip template.");
                    Assert.IsNotNull(alternateClose, "PART_AlternateCloseButton must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Collapsed, action.Visibility,
                        "The action button must collapse while ActionButtonContent is null.");
                    Assert.AreEqual(Visibility.Collapsed, close.Visibility,
                        "The footer close button must collapse while CloseButtonContent is null, per WinUI.");
                    Assert.AreEqual(Visibility.Visible, alternateClose.Visibility,
                        "The alternate top-right X must show while CloseButtonContent is null and light dismiss is off.");
                    Controls.FontIcon? alternateGlyph = alternateClose.Content as Controls.FontIcon;
                    Assert.IsNotNull(alternateGlyph, "The alternate close button must host a FontIcon.");
                    Assert.AreEqual("", alternateGlyph.Glyph,
                        "The alternate close button must show the Fluent close glyph (E711).");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_ClosedInPanel_RendersNothingBeforeFirstOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Grid host = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Inline",
                    Subtitle = "Must not paint in the page",
                    Content = "Body",
                };
                _ = host.Children.Add(tip);
                window.Content = host;

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, tip.Visibility,
                        "A closed tip declared in a panel must be collapsed before its first open.");
                    Assert.AreEqual(0.0, tip.ActualHeight, 0.001,
                        "A closed tip declared in a panel must occupy no layout height before its first open.");
                    Assert.AreEqual(0.0, tip.ActualWidth, 0.001,
                        "A closed tip declared in a panel must occupy no layout width before its first open.");

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "Opening the declared tip must re-host it in its popup.");
                    Assert.AreEqual(Visibility.Visible, tip.Visibility,
                        "Opening must restore visibility once the tip is popup-hosted.");
                    Assert.IsFalse(host.Children.Contains(tip),
                        "Opening must detach the tip from its declared panel.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.ActualHeight > 0),
                        "The popup-hosted tip must render its surface once open.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_DeclaredAsBorderChild_OpensWithoutThrowing()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Border host = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Bordered",
                    Content = "Body",
                };
                host.Child = tip;
                window.Content = host;

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "A tip declared as Border.Child must open without throwing.");
                    Assert.IsNull(host.Child,
                        "Opening must clear the Border.Child slot so the tip can become the popup child.");
                    Assert.AreSame(tip, tip.HostPopup?.Child,
                        "The popup child must be the tip detached from the Border.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsOpenTrue_OpensPopupAndRendersContent()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Update ready",
                    Subtitle = "Restart to apply the update",
                    Content = "Body text",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.IsTrue(popup.AllowsTransparency, "TeachingTip popups must allow transparency for the rounded surface.");
                    Assert.AreEqual(PopupAnimation.None, popup.PopupAnimation,
                        "TeachingTip popups must disable the popup fade; the placement-aware code reveal owns the motion.");
                    Assert.AreSame(tip, popup.Child, "The popup child must be the templated TeachingTip itself.");
                    Assert.AreSame(target, popup.PlacementTarget, "The popup must anchor to TeachingTip.Target.");
                    Assert.AreEqual(PlacementMode.Custom, popup.Placement,
                        "Auto must map to Custom placement so the tip centers on the target's bottom edge.");
                    Assert.IsNotNull(popup.CustomPopupPlacementCallback,
                        "The targeted tip popup must carry the edge-centering placement callback.");
                    CustomPopupPlacement[] placements = popup.CustomPopupPlacementCallback(new Size(100, 40), new Size(60, 20), default);
                    Assert.AreEqual(new Point(-20, 20), placements[0].Point,
                        "Auto (Bottom) must center the tip horizontally on the target's bottom edge.");
                    Assert.IsTrue(popup.StaysOpen, "Light dismiss is disabled by default, so the popup must stay open.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Update ready", StringComparison.Ordinal))),
                        "The title must render inside the open tip.");
                    Assert.IsTrue(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Restart to apply the update", StringComparison.Ordinal)),
                        "The subtitle must render inside the open tip.");
                    Assert.IsTrue(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Body text", StringComparison.Ordinal)),
                        "The body content must render inside the open tip.");

                    Assert.AreEqual(TeachingTipPlacementMode.Bottom, tip.ActualPlacement,
                        "The tip must record the resolved placement when it opens.");
                    Path? topBeak = tip.Template.FindName("TopBeak", tip) as Path;
                    Assert.IsNotNull(topBeak, "TopBeak must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, topBeak.Visibility,
                        "A tip popped below its target must show the beak on its top edge.");

                    // The open reveal (fade plus placement-aware slide, played from
                    // TeachingTip.OnLoaded) must settle at rest once the 167ms slide completes.
                    System.Windows.Media.TranslateTransform? translate =
                        tip.Template.FindName("TipTranslate", tip) as System.Windows.Media.TranslateTransform;
                    Assert.IsNotNull(translate, "The TeachingTip template must expose the TipTranslate reveal transform.");
                    Grid? tipRoot = tip.Template.FindName("TipRoot", tip) as Grid;
                    Assert.IsNotNull(tipRoot, "The TeachingTip template must expose the TipRoot layout root.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && tipRoot.Opacity >= 1.0),
                        "The open reveal must settle at Y=0 and full opacity.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsOpenFalse_ClosesPopupAndRaisesClosed()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Closable",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before the close scenario.");

                    bool closedRaised = false;
                    tip.Closed += (_, _) => closedRaised = true;

                    tip.IsOpen = false;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "IsOpen=false must close the host popup.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Closing the tip must raise Closed after the popup closes.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_IsLightDismissEnabled_MapsToPopupStaysOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Dismissable",
                    Target = target,
                    IsLightDismissEnabled = true,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before light dismiss is verified.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.IsFalse(popup.StaysOpen,
                        "IsLightDismissEnabled=true must map to a light-dismiss popup (StaysOpen=false).");

                    tip.IsLightDismissEnabled = false;
                    Assert.IsTrue(popup.StaysOpen,
                        "Disabling light dismiss while open must restore StaysOpen=true.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_CloseButton_RaisesCloseButtonClickAndCloses()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Close me",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_CloseButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the close button is clicked.");

                    bool closeClickRaised = false;
                    bool closedRaised = false;
                    tip.CloseButtonClick += (_, _) => closeClickRaised = true;
                    tip.Closed += (_, _) => closedRaised = true;

                    ButtonBase? closeButton = tip.Template.FindName("PART_CloseButton", tip) as ButtonBase;
                    Assert.IsNotNull(closeButton, "PART_CloseButton must exist in the TeachingTip template.");
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.IsTrue(closeClickRaised, "Clicking the close button must raise CloseButtonClick.");
                    Assert.IsFalse(tip.IsOpen, "Clicking the close button must set IsOpen=false.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "Clicking the close button must close the host popup.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Clicking the close button must raise Closed once the popup has closed.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_ActionButton_RaisesActionButtonClickAndInvokesCommand()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                TeachingTipRecordingCommand command = new();
                Controls.TeachingTip tip = new()
                {
                    Title = "Act on me",
                    Target = target,
                    ActionButtonContent = "Update now",
                    ActionButtonCommand = command,
                    ActionButtonCommandParameter = "payload",
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_ActionButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the action button is clicked.");

                    bool actionClickRaised = false;
                    tip.ActionButtonClick += (_, _) => actionClickRaised = true;

                    ButtonBase? actionButton = tip.Template.FindName("PART_ActionButton", tip) as ButtonBase;
                    Assert.IsNotNull(actionButton, "PART_ActionButton must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, actionButton.Visibility,
                        "The action button must be visible when ActionButtonContent is set.");
                    actionButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.IsTrue(actionClickRaised, "Clicking the action button must raise ActionButtonClick.");
                    Assert.AreEqual(1, command.ExecuteCount, "Clicking the action button must execute ActionButtonCommand once.");
                    Assert.AreEqual("payload", command.LastParameter,
                        "ActionButtonCommand must receive ActionButtonCommandParameter.");
                    Assert.IsTrue(tip.IsOpen, "Invoking the action button must not close the tip.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_NoTarget_DocksBottomRightAndHidesBeak()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480, Content = new Grid() };
                Controls.TeachingTip tip = new()
                {
                    Title = "Untargeted",
                    Subtitle = "Docked to the bottom-right of the window content",
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "An untargeted tip must still open its host popup.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Custom, popup.Placement,
                        "An untargeted tip must use Custom placement to dock to the bottom-right per WinUI.");
                    Assert.IsNotNull(popup.CustomPopupPlacementCallback,
                        "An untargeted tip must carry the bottom-right placement callback.");
                    CustomPopupPlacement[] placements = popup.CustomPopupPlacementCallback(new Size(100, 40), new Size(600, 400), default);
                    Assert.AreEqual(new Point(500, 360), placements[0].Point,
                        "The untargeted tip must dock to the bottom-right corner of the window content.");
                    Assert.AreSame(window.Content, popup.PlacementTarget,
                        "An untargeted tip must anchor to the active window's content.");
                    Assert.AreEqual(TeachingTipPlacementMode.Center, tip.ActualPlacement,
                        "An untargeted tip must resolve ActualPlacement to Center so no beak is shown.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.Template?.FindName("TopBeak", tip) is Path),
                        "The tip template must apply inside the popup.");
                    foreach (string beakName in new[] { "TopBeak", "BottomBeak", "LeftBeak", "RightBeak" })
                    {
                        Path? beak = tip.Template.FindName(beakName, tip) as Path;
                        Assert.IsNotNull(beak, string.Format("{0} must exist in the TeachingTip template.", beakName));
                        Assert.AreEqual(Visibility.Collapsed, beak.Visibility,
                            string.Format("{0} must be collapsed for an untargeted tip.", beakName));
                    }

                    // An explicit Center preference keeps the centered popup for untargeted tips.
                    tip.PreferredPlacement = TeachingTipPlacementMode.Center;
                    Assert.AreEqual(PlacementMode.Center, popup.Placement,
                        "An untargeted tip with an explicit Center preference must center its popup.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_PreferredPlacement_MapsToPopupPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Placed",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "IsOpen=true must open the host popup before placement mapping is verified.");

                    Popup? popup = tip.HostPopup;
                    Assert.IsNotNull(popup, "Opening the tip must lazily create the host popup.");
                    Assert.AreEqual(PlacementMode.Custom, popup.Placement,
                        "Edge placements must map to Custom placement so the tip centers on the target edge.");

                    // The popup side mapping that feeds the shared edge-centering callback.
                    Assert.AreEqual(PlacementMode.Top, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Top),
                        "Top must map to the top side.");
                    Assert.AreEqual(PlacementMode.Left, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Left),
                        "Left must map to the left side.");
                    Assert.AreEqual(PlacementMode.Right, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Right),
                        "Right must map to the right side.");
                    Assert.AreEqual(PlacementMode.Bottom, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Bottom),
                        "Bottom must map to the bottom side.");
                    Assert.AreEqual(PlacementMode.Bottom, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Auto),
                        "Auto currently maps to the bottom side.");

                    Size popupSize = new(100, 40);
                    Size targetSize = new(60, 20);
                    tip.PreferredPlacement = TeachingTipPlacementMode.Top;
                    Assert.AreEqual(TeachingTipPlacementMode.Top, tip.ActualPlacement, "Top must resolve ActualPlacement to Top.");
                    Assert.IsNotNull(popup.CustomPopupPlacementCallback, "Edge placements must carry the centering callback.");
                    Assert.AreEqual(new Point(-20, -40), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point,
                        "Top placement must center the tip horizontally on the target's top edge.");
                    DrainDispatcher(window.Dispatcher);
                    Path? bottomBeak = tip.Template.FindName("BottomBeak", tip) as Path;
                    Path? topBeak = tip.Template.FindName("TopBeak", tip) as Path;
                    Assert.IsNotNull(bottomBeak, "BottomBeak must exist in the TeachingTip template.");
                    Assert.IsNotNull(topBeak, "TopBeak must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, bottomBeak.Visibility,
                        "A tip popped above its target must show the beak on its bottom edge.");
                    Assert.AreEqual(Visibility.Collapsed, topBeak.Visibility,
                        "The top beak must hide when the tip moves above its target.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Left;
                    Assert.AreEqual(new Point(-100, -10), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point,
                        "Left placement must center the tip vertically on the target's left edge.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Right;
                    Assert.AreEqual(new Point(60, -10), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point,
                        "Right placement must center the tip vertically on the target's right edge.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Bottom;
                    Assert.AreEqual(new Point(-20, 20), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point,
                        "Bottom placement must center the tip horizontally on the target's bottom edge.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Center;
                    Assert.AreEqual(PlacementMode.Center, popup.Placement,
                        "An explicit Center placement with a target must keep the native centered popup.");

                    tip.PreferredPlacement = TeachingTipPlacementMode.Auto;
                    Assert.AreEqual(PlacementMode.Custom, popup.Placement,
                        "Auto must return to the edge-centering Custom placement.");
                    Assert.AreEqual(new Point(-20, 20), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point,
                        "Auto currently centers the tip on the target's bottom edge.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_CloseAffordance_FollowsCloseButtonContentAndLightDismiss()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Affordances",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_CloseButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the affordance matrix is verified.");

                    ButtonBase? footerClose = tip.Template.FindName("PART_CloseButton", tip) as ButtonBase;
                    ButtonBase? alternateClose = tip.Template.FindName("PART_AlternateCloseButton", tip) as ButtonBase;
                    FrameworkElement? footerArea = tip.Template.FindName("FooterArea", tip) as FrameworkElement;
                    Assert.IsNotNull(footerClose, "PART_CloseButton must exist in the TeachingTip template.");
                    Assert.IsNotNull(alternateClose, "PART_AlternateCloseButton must exist in the TeachingTip template.");
                    Assert.IsNotNull(footerArea, "FooterArea must exist in the TeachingTip template.");

                    // Null content, no light dismiss: alternate top-right X only.
                    Assert.AreEqual(Visibility.Collapsed, footerClose.Visibility,
                        "The footer close button must hide while CloseButtonContent is null.");
                    Assert.AreEqual(Visibility.Visible, alternateClose.Visibility,
                        "The alternate X must show for a null CloseButtonContent without light dismiss.");
                    Assert.AreEqual(Visibility.Collapsed, footerArea.Visibility,
                        "The footer row must collapse entirely while it has no visible buttons.");

                    // Null content, light dismiss: no close affordance at all (WinUI).
                    tip.IsLightDismissEnabled = true;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Collapsed, footerClose.Visibility,
                        "The footer close button must stay hidden for a light-dismiss tip without content.");
                    Assert.AreEqual(Visibility.Collapsed, alternateClose.Visibility,
                        "A light-dismiss tip without CloseButtonContent must show no close affordance.");

                    // Explicit content: footer close button only, regardless of light dismiss.
                    tip.CloseButtonContent = "Got it";
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, footerClose.Visibility,
                        "The footer close button must show while CloseButtonContent is set.");
                    Assert.AreEqual(Visibility.Collapsed, alternateClose.Visibility,
                        "The alternate X must hide while CloseButtonContent is set.");
                    Assert.AreEqual(Visibility.Visible, footerArea.Visibility,
                        "The footer row must show while it has a visible button.");

                    tip.IsLightDismissEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Visible, footerClose.Visibility,
                        "The footer close button must keep showing once content is set and light dismiss is off.");
                    Assert.AreEqual(Visibility.Collapsed, alternateClose.Visibility,
                        "The alternate X must stay hidden while CloseButtonContent is set.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_AlternateCloseButton_RunsCloseButtonPipeline()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Corner close",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_AlternateCloseButton", tip) is ButtonBase),
                        "The tip must open and apply its template before the alternate close button is clicked.");

                    bool closeClickRaised = false;
                    bool closedRaised = false;
                    tip.CloseButtonClick += (_, _) => closeClickRaised = true;
                    tip.Closed += (_, _) => closedRaised = true;

                    ButtonBase? alternateClose = tip.Template.FindName("PART_AlternateCloseButton", tip) as ButtonBase;
                    Assert.IsNotNull(alternateClose, "PART_AlternateCloseButton must exist in the TeachingTip template.");
                    Assert.AreEqual(Visibility.Visible, alternateClose.Visibility,
                        "The alternate X must be the visible close affordance for this tip.");
                    alternateClose.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.IsTrue(closeClickRaised, "Clicking the alternate X must raise CloseButtonClick.");
                    Assert.IsFalse(tip.IsOpen, "Clicking the alternate X must set IsOpen=false.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "Clicking the alternate X must close the host popup.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "Clicking the alternate X must raise Closed once the popup has closed.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup?.PlacementTarget is null),
                        "Closing must release the popup's placement target so the tip does not pin the anchor.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_Escape_ClosesTip()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Escapable",
                    Target = target,
                };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }),
                        "The tip must open before Escape is simulated.");

                    bool closedRaised = false;
                    tip.Closed += (_, _) => closedRaised = true;

                    tip.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Escape)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });

                    Assert.IsFalse(tip.IsOpen, "Escape inside the tip must set IsOpen=false.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                        "Escape inside the tip must close the host popup.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => closedRaised),
                        "The Escape dismissal must raise Closed.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_OpenReveal_SettlesAtRestForEachPlacement()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    foreach (TeachingTipPlacementMode placement in new[]
                    {
                        TeachingTipPlacementMode.Top,
                        TeachingTipPlacementMode.Bottom,
                        TeachingTipPlacementMode.Left,
                        TeachingTipPlacementMode.Right,
                    })
                    {
                        // IsOpen last in the initializer: Target and PreferredPlacement must be
                        // set before the open resolves the placement.
                        Controls.TeachingTip tip = new()
                        {
                            Title = "Revealed",
                            Target = target,
                            PreferredPlacement = placement,
                            IsOpen = true,
                        };

                        Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded),
                            string.Format("The {0} tip must open and load inside its popup.", placement));
                        Assert.AreEqual(placement, tip.ActualPlacement,
                            string.Format("The {0} tip must resolve ActualPlacement to the forced placement.", placement));

                        System.Windows.Media.TranslateTransform? translate =
                            tip.Template.FindName("TipTranslate", tip) as System.Windows.Media.TranslateTransform;
                        Assert.IsNotNull(translate,
                            string.Format("The {0} tip template must expose the TipTranslate reveal transform.", placement));
                        Grid? tipRoot = tip.Template.FindName("TipRoot", tip) as Grid;
                        Assert.IsNotNull(tipRoot,
                            string.Format("The {0} tip template must expose the TipRoot layout root.", placement));

                        // The placement-aware reveal must settle at the (0,0) rest position and
                        // full opacity, with the Stop-fill clocks released by the completed
                        // handlers so nothing stays animated.
                        Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                                () => Math.Abs(translate.X) < 0.001 && Math.Abs(translate.Y) < 0.001 &&
                                    tipRoot.Opacity >= 1.0 &&
                                    !translate.HasAnimatedProperties && !tipRoot.HasAnimatedProperties),
                            string.Format("The {0} reveal must settle at translate (0,0), full opacity, and release its clocks.", placement));

                        tip.IsOpen = false;
                        Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }),
                            string.Format("The {0} tip must close before the next placement opens.", placement));
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_OpenReveal_CenterTipFadesWithoutSlide()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480, Content = new Grid() };
                Controls.TeachingTip tip = new()
                {
                    Title = "Centered",
                    Subtitle = "Modal exemption: no directional motion",
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded),
                        "The untargeted tip must open and load inside its popup.");
                    Assert.AreEqual(TeachingTipPlacementMode.Center, tip.ActualPlacement,
                        "An untargeted tip must resolve ActualPlacement to Center.");

                    System.Windows.Media.TranslateTransform? translate =
                        tip.Template.FindName("TipTranslate", tip) as System.Windows.Media.TranslateTransform;
                    Assert.IsNotNull(translate, "The tip template must expose the TipTranslate reveal transform.");
                    Grid? tipRoot = tip.Template.FindName("TipRoot", tip) as Grid;
                    Assert.IsNotNull(tipRoot, "The tip template must expose the TipRoot layout root.");

                    // Center tips fade only: the translate must never receive a nonzero seed or
                    // a slide clock (sampled right after Loaded, while the fade may still run).
                    Assert.AreEqual(0.0, translate.X, 0.001,
                        "A Center tip must never get a nonzero X reveal seed.");
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "A Center tip must never get a nonzero Y reveal seed.");
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "A Center tip must not carry a reveal slide clock.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => tipRoot.Opacity >= 1.0 && !tipRoot.HasAnimatedProperties),
                        "The Center fade must settle at full opacity and release its clock.");
                    Assert.AreEqual(0.0, translate.X, 0.001,
                        "A Center tip must still rest at X=0 after the fade settles.");
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "A Center tip must still rest at Y=0 after the fade settles.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void TeachingTip_ThemeCycle_SurfaceBrushesResolve()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in TeachingTip theme cycle step: {1}", key, theme));
                    }
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Task 9 -- a11y: TeachingTip live-region metadata
        // ---------------------------------------------------------------------------

        [TestMethod]
        public void TeachingTip_HasPolite_LiveSetting()
        {
            RunOnStaThread(static () =>
            {
                Controls.TeachingTip tip = new();
                AutomationLiveSetting liveSetting = AutomationProperties.GetLiveSetting(tip);
                Assert.AreEqual(
                    AutomationLiveSetting.Polite,
                    liveSetting,
                    "TeachingTip must expose AutomationLiveSetting.Polite so Narrator announces tip content.");
            });
        }

        private sealed class TeachingTipRecordingCommand : ICommand
        {
            public object? LastParameter { get; private set; }

            public int ExecuteCount { get; private set; }

            public bool CanExecute(object? parameter) { return true; }

            public void Execute(object? parameter)
            {
                LastParameter = parameter;
                ExecuteCount++;
            }

            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "This is just test code.")]
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
