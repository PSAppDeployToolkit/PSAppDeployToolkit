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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Shapes;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.TeachingTip"/> control: default style and
    /// template parts, popup hosting, placement and beak resolution, light dismiss mapping,
    /// footer button behavior, and surface brush theming.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task TeachingTip_DefaultStyle_AppliesAndTemplatePartsFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.TeachingTip defaults = new();
                Assert.Equal(string.Empty, defaults.Title, StringComparer.Ordinal);
                Assert.Equal(string.Empty, defaults.Subtitle, StringComparer.Ordinal);
                Assert.False(defaults.IsOpen, "IsOpen must default to false.");
                Assert.False(defaults.IsLightDismissEnabled, "IsLightDismissEnabled must default to false.");
                Assert.Null(defaults.Target);
                Assert.Equal(TeachingTipPlacementMode.Auto, defaults.PreferredPlacement);

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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, tip.Visibility);

                    // The collapsed at-rest tip is skipped by layout, so inflate the template
                    // explicitly to assert the template contract.
                    _ = tip.ApplyTemplate();

                    Assert.Equal(320.0, tip.MinWidth, 0.01);
                    Assert.Equal(336.0, tip.MaxWidth, 0.01);
                    Assert.Equal(new Thickness(16, 15, 16, 17), tip.Padding);

                    Border surface = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(tip, "TipSurface"));
                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Assert.Equal(overlayRadius, surface.CornerRadius);
                    Assert.Equal(new Thickness(1), surface.BorderThickness);

                    ButtonBase action = Assert.IsAssignableFrom<ButtonBase>(FindVisualChildByName<ButtonBase>(tip, "PART_ActionButton"));
                    ButtonBase close = Assert.IsAssignableFrom<ButtonBase>(FindVisualChildByName<ButtonBase>(tip, "PART_CloseButton"));
                    ButtonBase alternateClose = Assert.IsAssignableFrom<ButtonBase>(FindVisualChildByName<ButtonBase>(tip, "PART_AlternateCloseButton"));
                    Assert.Equal(Visibility.Collapsed, action.Visibility);
                    Assert.Equal(Visibility.Collapsed, close.Visibility);
                    Assert.Equal(Visibility.Visible, alternateClose.Visibility);
                    Controls.FontIcon alternateGlyph = Assert.IsType<Controls.FontIcon>(alternateClose.Content);
                    Assert.Equal("\uE711", alternateGlyph.Glyph, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_ClosedInPanel_RendersNothingBeforeFirstOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, tip.Visibility);
                    Assert.Equal(0.0, tip.ActualHeight, 0.001);
                    Assert.Equal(0.0, tip.ActualWidth, 0.001);

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "Opening the declared tip must re-host it in its popup.");
                    Assert.Equal(Visibility.Visible, tip.Visibility);
                    Assert.False(host.Children.Contains(tip),
                        "Opening must detach the tip from its declared panel.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.ActualHeight > 0).ConfigureAwait(true),
                        "The popup-hosted tip must render its surface once open.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_DeclaredAsBorderChild_OpensWithoutThrowingAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "A tip declared as Border.Child must open without throwing.");
                    Assert.Null(host.Child);
                    Assert.Same(tip, tip.HostPopup?.Child);
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_IsOpenTrue_OpensPopupAndRendersContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "IsOpen=true must open the host popup.");

                    Popup popup = Assert.IsAssignableFrom<Popup>(tip.HostPopup);
                    Assert.True(popup.AllowsTransparency, "TeachingTip popups must allow transparency for the rounded surface.");
                    Assert.Equal(PopupAnimation.None, popup.PopupAnimation);
                    Assert.Same(tip, popup.Child);
                    Assert.Same(target, popup.PlacementTarget);
                    Assert.Equal(PlacementMode.Custom, popup.Placement);
                    Assert.NotNull(popup.CustomPopupPlacementCallback);
                    CustomPopupPlacement[] placements = popup.CustomPopupPlacementCallback(new Size(100, 40), new Size(60, 20), default);
                    Assert.Equal(new Point(-20, 20), placements[0].Point);
                    Assert.True(popup.StaysOpen, "Light dismiss is disabled by default, so the popup must stay open.");

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Update ready", StringComparison.Ordinal))).ConfigureAwait(true),
                        "The title must render inside the open tip.");
                    Assert.True(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Restart to apply the update", StringComparison.Ordinal)),
                        "The subtitle must render inside the open tip.");
                    Assert.True(FindVisualChildren<TextBlock>(tip)
                            .Any(t => string.Equals(t.Text, "Body text", StringComparison.Ordinal)),
                        "The body content must render inside the open tip.");

                    Assert.Equal(TeachingTipPlacementMode.Bottom, tip.ActualPlacement);
                    Path topBeak = Assert.IsType<Path>(tip.Template.FindName("TopBeak", tip));
                    Assert.Equal(Visibility.Visible, topBeak.Visibility);

                    // The open reveal (fade plus placement-aware slide, played from
                    // TeachingTip.OnLoaded) must settle at rest once the 167ms slide completes.
                    System.Windows.Media.TranslateTransform translate =
                        Assert.IsType<System.Windows.Media.TranslateTransform>(tip.Template.FindName("TipTranslate", tip));
                    Grid tipRoot = Assert.IsType<Grid>(tip.Template.FindName("TipRoot", tip));
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => Math.Abs(translate.Y) < 0.001 && tipRoot.Opacity >= 1.0).ConfigureAwait(true),
                        "The open reveal must settle at Y=0 and full opacity.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_IsOpenFalse_ClosesPopupAndRaisesClosedAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "IsOpen=true must open the host popup before the close scenario.");

                    bool closedRaised = false;
                    tip.Closed += (_, _) => closedRaised = true;

                    tip.IsOpen = false;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true),
                        "IsOpen=false must close the host popup.");

                    // Popup.Closed is raised asynchronously once the fade-out completes, so
                    // sample the flag instead of asserting immediately.
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => closedRaised).ConfigureAwait(true),
                        "Closing the tip must raise Closed after the popup closes.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_IsLightDismissEnabled_MapsToPopupStaysOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "IsOpen=true must open the host popup before light dismiss is verified.");

                    Popup popup = Assert.IsAssignableFrom<Popup>(tip.HostPopup);
                    Assert.False(popup.StaysOpen,
                        "IsLightDismissEnabled=true must map to a light-dismiss popup (StaysOpen=false).");

                    tip.IsLightDismissEnabled = false;
                    Assert.True(popup.StaysOpen,
                        "Disabling light dismiss while open must restore StaysOpen=true.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_CloseButton_RaisesCloseButtonClickAndClosesAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_CloseButton", tip) is ButtonBase).ConfigureAwait(true),
                        "The tip must open and apply its template before the close button is clicked.");

                    bool closeClickRaised = false;
                    bool closedRaised = false;
                    tip.CloseButtonClick += (_, _) => closeClickRaised = true;
                    tip.Closed += (_, _) => closedRaised = true;

                    ButtonBase closeButton = Assert.IsAssignableFrom<ButtonBase>(tip.Template.FindName("PART_CloseButton", tip));
                    closeButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.True(closeClickRaised, "Clicking the close button must raise CloseButtonClick.");
                    Assert.False(tip.IsOpen, "Clicking the close button must set IsOpen=false.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true),
                        "Clicking the close button must close the host popup.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => closedRaised).ConfigureAwait(true),
                        "Clicking the close button must raise Closed once the popup has closed.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_ActionButton_RaisesActionButtonClickAndInvokesCommandAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_ActionButton", tip) is ButtonBase).ConfigureAwait(true),
                        "The tip must open and apply its template before the action button is clicked.");

                    bool actionClickRaised = false;
                    tip.ActionButtonClick += (_, _) => actionClickRaised = true;

                    ButtonBase actionButton = Assert.IsAssignableFrom<ButtonBase>(tip.Template.FindName("PART_ActionButton", tip));
                    Assert.Equal(Visibility.Visible, actionButton.Visibility);
                    actionButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.True(actionClickRaised, "Clicking the action button must raise ActionButtonClick.");
                    Assert.Equal(1, command.ExecuteCount);
                    Assert.Equal("payload", command.LastParameter);
                    Assert.True(tip.IsOpen, "Invoking the action button must not close the tip.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_NoTarget_DocksBottomRightAndHidesBeakAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "An untargeted tip must still open its host popup.");

                    Popup popup = Assert.IsAssignableFrom<Popup>(tip.HostPopup);
                    Assert.Equal(PlacementMode.Custom, popup.Placement);
                    Assert.NotNull(popup.CustomPopupPlacementCallback);
                    CustomPopupPlacement[] placements = popup.CustomPopupPlacementCallback(new Size(100, 40), new Size(600, 400), default);
                    Assert.Equal(new Point(500, 360), placements[0].Point);
                    Assert.Same(window.Content, popup.PlacementTarget);
                    Assert.Equal(TeachingTipPlacementMode.Center, tip.ActualPlacement);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.Template?.FindName("TopBeak", tip) is Path).ConfigureAwait(true),
                        "The tip template must apply inside the popup.");
                    foreach (string beakName in new[] { "TopBeak", "BottomBeak", "LeftBeak", "RightBeak" })
                    {
                        Path beak = Assert.IsType<Path>(tip.Template.FindName(beakName, tip));
                        Assert.Equal(Visibility.Collapsed, beak.Visibility);
                    }

                    // An explicit Center preference keeps the centered popup for untargeted tips.
                    tip.PreferredPlacement = TeachingTipPlacementMode.Center;
                    Assert.Equal(PlacementMode.Center, popup.Placement);
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_PreferredPlacement_MapsToPopupPlacementAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
                        "IsOpen=true must open the host popup before placement mapping is verified.");

                    Popup popup = Assert.IsAssignableFrom<Popup>(tip.HostPopup);
                    Assert.Equal(PlacementMode.Custom, popup.Placement);

                    // The popup side mapping that feeds the shared edge-centering callback.
                    Assert.Equal(PlacementMode.Top, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Top));
                    Assert.Equal(PlacementMode.Left, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Left));
                    Assert.Equal(PlacementMode.Right, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Right));
                    Assert.Equal(PlacementMode.Bottom, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Bottom));
                    Assert.Equal(PlacementMode.Bottom, Controls.TeachingTip.MapPlacementSide(TeachingTipPlacementMode.Auto));

                    Size popupSize = new(100, 40);
                    Size targetSize = new(60, 20);
                    tip.PreferredPlacement = TeachingTipPlacementMode.Top;
                    Assert.Equal(TeachingTipPlacementMode.Top, tip.ActualPlacement);
                    Assert.NotNull(popup.CustomPopupPlacementCallback);
                    Assert.Equal(new Point(-20, -40), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Path bottomBeak = Assert.IsType<Path>(tip.Template.FindName("BottomBeak", tip));
                    Path topBeak = Assert.IsType<Path>(tip.Template.FindName("TopBeak", tip));
                    Assert.Equal(Visibility.Visible, bottomBeak.Visibility);
                    Assert.Equal(Visibility.Collapsed, topBeak.Visibility);

                    tip.PreferredPlacement = TeachingTipPlacementMode.Left;
                    Assert.Equal(new Point(-100, -10), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point);

                    tip.PreferredPlacement = TeachingTipPlacementMode.Right;
                    Assert.Equal(new Point(60, -10), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point);

                    tip.PreferredPlacement = TeachingTipPlacementMode.Bottom;
                    Assert.Equal(new Point(-20, 20), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point);

                    tip.PreferredPlacement = TeachingTipPlacementMode.Center;
                    Assert.Equal(PlacementMode.Center, popup.Placement);

                    tip.PreferredPlacement = TeachingTipPlacementMode.Auto;
                    Assert.Equal(PlacementMode.Custom, popup.Placement);
                    Assert.Equal(new Point(-20, 20), popup.CustomPopupPlacementCallback(popupSize, targetSize, default)[0].Point);
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_CloseAffordance_FollowsCloseButtonContentAndLightDismissAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_CloseButton", tip) is ButtonBase).ConfigureAwait(true),
                        "The tip must open and apply its template before the affordance matrix is verified.");

                    ButtonBase footerClose = Assert.IsAssignableFrom<ButtonBase>(tip.Template.FindName("PART_CloseButton", tip));
                    ButtonBase alternateClose = Assert.IsAssignableFrom<ButtonBase>(tip.Template.FindName("PART_AlternateCloseButton", tip));
                    FrameworkElement footerArea = Assert.IsAssignableFrom<FrameworkElement>(tip.Template.FindName("FooterArea", tip));

                    // Null content, no light dismiss: alternate top-right X only.
                    Assert.Equal(Visibility.Collapsed, footerClose.Visibility);
                    Assert.Equal(Visibility.Visible, alternateClose.Visibility);
                    Assert.Equal(Visibility.Collapsed, footerArea.Visibility);

                    // Null content, light dismiss: no close affordance at all (WinUI).
                    tip.IsLightDismissEnabled = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, footerClose.Visibility);
                    Assert.Equal(Visibility.Collapsed, alternateClose.Visibility);

                    // Explicit content: footer close button only, regardless of light dismiss.
                    tip.CloseButtonContent = "Got it";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, footerClose.Visibility);
                    Assert.Equal(Visibility.Collapsed, alternateClose.Visibility);
                    Assert.Equal(Visibility.Visible, footerArea.Visibility);

                    tip.IsLightDismissEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Visible, footerClose.Visibility);
                    Assert.Equal(Visibility.Collapsed, alternateClose.Visibility);
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_AlternateCloseButton_RunsCloseButtonPipelineAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tip.HostPopup is { IsOpen: true } && tip.Template?.FindName("PART_AlternateCloseButton", tip) is ButtonBase).ConfigureAwait(true),
                        "The tip must open and apply its template before the alternate close button is clicked.");

                    bool closeClickRaised = false;
                    bool closedRaised = false;
                    tip.CloseButtonClick += (_, _) => closeClickRaised = true;
                    tip.Closed += (_, _) => closedRaised = true;

                    ButtonBase alternateClose = Assert.IsAssignableFrom<ButtonBase>(tip.Template.FindName("PART_AlternateCloseButton", tip));
                    Assert.Equal(Visibility.Visible, alternateClose.Visibility);
                    alternateClose.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    Assert.True(closeClickRaised, "Clicking the alternate X must raise CloseButtonClick.");
                    Assert.False(tip.IsOpen, "Clicking the alternate X must set IsOpen=false.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true),
                        "Clicking the alternate X must close the host popup.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => closedRaised).ConfigureAwait(true),
                        "Clicking the alternate X must raise Closed once the popup has closed.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup?.PlacementTarget is null).ConfigureAwait(true),
                        "Closing must release the popup's placement target so the tip does not pin the anchor.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_Escape_ClosesTipAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true }).ConfigureAwait(true),
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

                    Assert.False(tip.IsOpen, "Escape inside the tip must set IsOpen=false.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true),
                        "Escape inside the tip must close the host popup.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => closedRaised).ConfigureAwait(true),
                        "The Escape dismissal must raise Closed.");
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_OpenReveal_SettlesAtRestForEachPlacementAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 640, Height = 480 };
                Button target = new() { Content = "Anchor" };

                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
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

                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded).ConfigureAwait(true),
                            string.Format("The {0} tip must open and load inside its popup.", placement));
                        Assert.Equal(placement, tip.ActualPlacement);

                        System.Windows.Media.TranslateTransform translate =
                            Assert.IsType<System.Windows.Media.TranslateTransform>(tip.Template.FindName("TipTranslate", tip));
                        Grid tipRoot = Assert.IsType<Grid>(tip.Template.FindName("TipRoot", tip));

                        // The placement-aware reveal must settle at the (0,0) rest position and
                        // full opacity, with the Stop-fill clocks released by the completed
                        // handlers so nothing stays animated.
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                                () => Math.Abs(translate.X) < 0.001 && Math.Abs(translate.Y) < 0.001 &&
                                    tipRoot.Opacity >= 1.0 &&
                                    !translate.HasAnimatedProperties && !tipRoot.HasAnimatedProperties).ConfigureAwait(true),
                            string.Format("The {0} reveal must settle at translate (0,0), full opacity, and release its clocks.", placement));

                        tip.IsOpen = false;
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true),
                            string.Format("The {0} tip must close before the next placement opens.", placement));
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_OpenReveal_CenterTipFadesWithoutSlideAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded).ConfigureAwait(true),
                        "The untargeted tip must open and load inside its popup.");
                    Assert.Equal(TeachingTipPlacementMode.Center, tip.ActualPlacement);

                    System.Windows.Media.TranslateTransform translate =
                        Assert.IsType<System.Windows.Media.TranslateTransform>(tip.Template.FindName("TipTranslate", tip));
                    Grid tipRoot = Assert.IsType<Grid>(tip.Template.FindName("TipRoot", tip));

                    // Center tips fade only: the translate must never receive a nonzero seed or
                    // a slide clock (sampled right after Loaded, while the fade may still run).
                    Assert.Equal(0.0, translate.X, 0.001);
                    Assert.Equal(0.0, translate.Y, 0.001);
                    Assert.False(translate.HasAnimatedProperties,
                        "A Center tip must not carry a reveal slide clock.");

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => tipRoot.Opacity >= 1.0 && !tipRoot.HasAnimatedProperties).ConfigureAwait(true),
                        "The Center fade must settle at full opacity and release its clock.");
                    Assert.Equal(0.0, translate.X, 0.001);
                    Assert.Equal(0.0, translate.Y, 0.001);
                }
                finally
                {
                    tip.IsOpen = false;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task TeachingTip_ThemeCycle_SurfaceBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
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
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Task 9 -- a11y: TeachingTip live-region metadata
        // ---------------------------------------------------------------------------

        [Fact]
        public Task TeachingTip_HasPolite_LiveSettingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.TeachingTip tip = new();
                AutomationLiveSetting liveSetting = AutomationProperties.GetLiveSetting(tip);
                Assert.Equal(
                    AutomationLiveSetting.Polite,
                    liveSetting);
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
