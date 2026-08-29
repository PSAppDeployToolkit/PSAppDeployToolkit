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
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Reduced-motion tests: when <see cref="MotionHelper.IsMotionEnabled"/> is false (the
    /// Windows "Show animations in Windows" accessibility setting is off), code-driven
    /// animations must not start and controls must jump straight to their final visual state.
    /// Every test forces the gate through <see cref="MotionHelper.OverrideIsMotionEnabled"/>
    /// and resets it to null in a finally block.
    /// <para>
    /// The one deliberate exception is the indeterminate <see cref="Controls.ProgressBar"/>: its
    /// motion is the only thing that communicates "work is still happening", so it is treated as
    /// essential status feedback rather than decoration and keeps animating with the gate off.
    /// See <see cref="ReducedMotion_ProgressBar_Indeterminate_KeepsAnimatingAsync"/>.
    /// </para>
    /// </summary>
    public partial class ControlTests
    {
        /// <summary>
        /// An indeterminate ProgressBar must keep animating even with motion disabled.
        /// <para>
        /// A determinate bar still reads correctly when frozen, because its fill width carries the
        /// information. An indeterminate bar carries no value at all - the movement *is* the
        /// message - so parking it produces a control that looks like a stalled determinate bar
        /// and tells the user the operation has hung. That is what happened in PSAppDeployToolkit:
        /// <c language="powershell">Show-ADTInstallationProgress</c> returned normally but its dialog sat motionless on
        /// any machine with animation effects off, and the install looked frozen. Screen-reader
        /// users, the exact audience the reduced-motion work was for, very often have animation
        /// effects off.
        /// </para>
        /// <para>
        /// So indeterminate progress is classed as essential status feedback, not decoration, and
        /// is exempt from the gate. Everything else in this file stays gated. Do not "fix" this
        /// test by re-adding a <see cref="MotionHelper.IsMotionEnabled"/> check to
        /// <c language="csharp">ProgressBar.StartIndeterminate</c>.
        /// </para>
        /// </summary>
        [Fact]
        public Task ReducedMotion_ProgressBar_Indeterminate_KeepsAnimatingAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.ProgressBar bar = new()
                {
                    IsIndeterminate = true,
                    Width = 240,
                    Height = 6,
                };
                Window w = new() { Content = bar, Width = 300, Height = 120 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(bar.Template.FindName("PART_IndeterminateTranslate", bar));

                    Assert.True(translate.HasAnimatedProperties,
                        "An indeterminate ProgressBar must animate with motion disabled: the movement is the only status signal it has.");

                    double first = translate.X;
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => Math.Abs(translate.X - first) > 0.5).ConfigureAwait(true),
                        "The indeterminate bar must actually travel, not merely hold an animation clock.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_ProgressRing_Indeterminate_RendersStaticFrameWithoutClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.ProgressRing ring = new()
                {
                    IsActive = true,
                    IsIndeterminate = true,
                    Width = 64,
                    Height = 64,
                };
                Window w = new() { Content = ring, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Path indeterminateArc = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(ring, "PART_IndeterminateArc"));
                    Assert.Equal(Visibility.Visible, indeterminateArc.Visibility);
                    Assert.NotNull(indeterminateArc.Data);

                    RotateTransform rotate = Assert.IsAssignableFrom<RotateTransform>(GetIndeterminateRotateTransform(ring));
                    Assert.False(rotate.HasAnimatedProperties,
                        "With motion disabled the indeterminate rotation must not run.");
                    Assert.Equal(90.0, rotate.Angle, 0.01);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_FontIcon_IsSpinning_DoesNotAnimateRotationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.FontIcon icon = new()
                {
                    Glyph = "\uE72C",
                    IsSpinning = true,
                };
                Window w = new() { Content = icon, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    RotateTransform rotate = Assert.IsType<RotateTransform>(icon.Template.FindName("PART_Rotate", icon));
                    Assert.False(rotate.HasAnimatedProperties,
                        "With motion disabled the spin animation must not run even while IsSpinning is true.");
                    Assert.Equal(icon.Rotation, rotate.Angle, 0.01);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_ToggleSwitch_Toggle_SnapsKnobToFinalOffsetSynchronouslyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    ts.IsChecked = true;

                    // No drain: the knob must land at its final offset synchronously.
                    Assert.Equal(20.0, tx.X, 0.01);
                    Assert.False(tx.HasAnimatedProperties,
                        "With motion disabled the knob translate must carry no animation clock.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_Expander_Expand_OpensContentAtRestWithoutSlideClockAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.Expander expander = new()
                {
                    Header = "Header",
                    Content = new Border { Height = 80 },
                    IsExpanded = false,
                };
                Window w = new() { Content = expander, Width = 300, Height = 300 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    expander.IsExpanded = true;

                    // No drain: the steady state must apply synchronously (no deferred slide).
                    ContentPresenter expandSite = Assert.IsType<ContentPresenter>(expander.Template.FindName("ExpandSite", expander));
                    TranslateTransform translate = Assert.IsType<TranslateTransform>(expandSite.RenderTransform);
                    Assert.False(translate.HasAnimatedProperties,
                        "With motion disabled the expand slide must not run.");
                    Assert.Equal(0.0, translate.Y, 0.001);

                    RowDefinition contentRow = Assert.IsType<RowDefinition>(expander.Template.FindName("Row1Def", expander));
                    Assert.True(contentRow.Height.IsStar,
                        "With motion disabled the content row must open to its star height immediately.");

                    WpfTestSta.DrainDispatcher(w.Dispatcher);
                    Assert.Equal(0.0, translate.Y, 0.001);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_Flyout_ShowAt_PresentsSurfaceAtRestWithoutClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Flyout body" };
                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => flyout.IsOpen).ConfigureAwait(true),
                        "ShowAt should open the flyout popup.");

                    Popup popup = Assert.IsAssignableFrom<Popup>(flyout.HostPopup);
                    Controls.FlyoutPresenter presenter = Assert.IsType<Controls.FlyoutPresenter>(popup.Child);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => presenter.IsLoaded).ConfigureAwait(true),
                        "The presenter must load inside the open popup.");

                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(presenter.Template.FindName("PresenterTranslate", presenter));
                    Border surface = Assert.IsType<Border>(presenter.Template.FindName("PresenterSurface", presenter));

                    Assert.Equal(0.0, translate.X, 0.001);
                    Assert.Equal(0.0, translate.Y, 0.001);
                    Assert.Equal(1.0, surface.Opacity, 0.001);
                    Assert.False(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.False(surface.HasAnimatedProperties,
                        "With motion disabled the reveal fade must not leave an animation clock.");

                    flyout.Hide();
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => !flyout.IsOpen).ConfigureAwait(true);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_TeachingTip_Open_PresentsTipAtRestWithoutClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.TeachingTip tip = new()
                {
                    Title = "Reduced motion",
                    Target = target,
                };
                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded).ConfigureAwait(true),
                        "IsOpen=true should open the host popup and load the tip.");

                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(tip.Template.FindName("TipTranslate", tip));
                    Grid tipRoot = Assert.IsType<Grid>(tip.Template.FindName("TipRoot", tip));

                    Assert.Equal(0.0, translate.X, 0.001);
                    Assert.Equal(0.0, translate.Y, 0.001);
                    Assert.Equal(1.0, tipRoot.Opacity, 0.001);
                    Assert.False(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.False(tipRoot.HasAnimatedProperties,
                        "With motion disabled the reveal fade must not leave an animation clock.");

                    tip.IsOpen = false;
                    _ = await WaitUntilAsync(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false }).ConfigureAwait(true);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_ContentDialog_Hide_TearsDownSynchronouslyWithoutClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Reduced motion",
                    Content = "Body",
                    CloseButtonText = "Close",
                };

                try
                {
                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before Hide is called.");

                    dialog.Hide();

                    Assert.True(task.IsCompleted,
                        "With motion disabled Hide must tear the dialog down synchronously.");
                    Assert.False(dialog.HasAnimatedProperties,
                        "With motion disabled the close must not leave an opacity animation clock.");
                    if (dialog.RenderTransform is ScaleTransform scale)
                    {
                        Assert.False(scale.HasAnimatedProperties,
                            "With motion disabled the close must not leave scale animation clocks.");
                    }

                    Assert.True(dialog.IsHitTestVisible,
                        "The synchronous teardown must restore hit testing for the next show.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_ComboBox_DropdownOpen_PresentsDropdownAtRestWithoutClocksAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Border border =
                        Assert.IsType<Border>(combo.Template.FindName("PART_DropdownBorder", combo));
                    TranslateTransform translate = Assert.IsType<TranslateTransform>(border.RenderTransform);

                    Assert.Equal(0.0, translate.Y, 0.001);
                    Assert.Equal(1.0, border.Opacity, 0.001);
                    Assert.False(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.False(border.HasAnimatedProperties,
                        "With motion disabled the reveal fade must not leave an animation clock.");

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ReducedMotion_OverrideTrue_ToggleSwitch_StillAnimatesKnobAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = true;

                Controls.ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    ts.IsChecked = true;

                    Assert.True(tx.HasAnimatedProperties,
                        "With motion enabled the knob slide must animate, proving the gate is the only change.");
                    Assert.True(await WaitUntilAsync(w.Dispatcher, 2000, () => Math.Abs(tx.X - 20.0) < 0.01).ConfigureAwait(true),
                        "The animated knob must settle at the on offset.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        /// <summary>
        /// The test seam must decide the gate on its own. <see cref="MotionHelper.IsMotionEnabled"/>
        /// now also requires hardware rendering, so the environment can supply either answer for the
        /// unforced branch; a non-null <see cref="MotionHelper.OverrideIsMotionEnabled"/> has to win
        /// in both directions or every other test in this file becomes machine dependent. The live
        /// branch is deliberately not asserted: the render tier of the test host is not a contract.
        /// </summary>
        [Fact]
        public Task ReducedMotion_Override_WinsOverEnvironmentInBothDirectionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                try
                {
                    MotionHelper.OverrideIsMotionEnabled = true;
                    Assert.True(MotionHelper.IsMotionEnabled,
                        "An override of true must enable motion whatever the OS animation setting and render tier report.");

                    MotionHelper.OverrideIsMotionEnabled = false;
                    Assert.False(MotionHelper.IsMotionEnabled,
                        "An override of false must disable motion whatever the OS animation setting and render tier report.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                }
            });
        }
    }
}
