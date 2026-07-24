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

using Fluence.Wpf.Helpers;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Shapes;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Reduced-motion tests: when <see cref="MotionHelper.IsMotionEnabled"/> is false (the
    /// Windows "Show animations in Windows" accessibility setting is off), code-driven
    /// animations must not start and controls must jump straight to their final visual state.
    /// Every test forces the gate through <see cref="MotionHelper.OverrideIsMotionEnabled"/>
    /// and resets it to null in a finally block.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void ReducedMotion_ProgressRing_Indeterminate_RendersStaticFrameWithoutClocks()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(w.Dispatcher);

                    Path? indeterminateArc = FindVisualChildByName<Path>(ring, "PART_IndeterminateArc");
                    Assert.IsNotNull(indeterminateArc, "PART_IndeterminateArc must exist.");
                    Assert.AreEqual(Visibility.Visible, indeterminateArc.Visibility,
                        "An active indeterminate ring must stay visible with motion disabled.");
                    Assert.IsNotNull(indeterminateArc.Data,
                        "With motion disabled the ring must render its static resting arc, not nothing.");

                    RotateTransform? rotate = GetIndeterminateRotateTransform(ring);
                    Assert.IsNotNull(rotate, "ProgressRing template must contain PART_IndeterminateRotate.");
                    Assert.IsFalse(rotate.HasAnimatedProperties,
                        "With motion disabled the indeterminate rotation must not run.");
                    Assert.AreEqual(90.0, rotate.Angle, 0.01,
                        "With motion disabled the rotate transform must park at its 90 degree reset angle.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_FontIcon_IsSpinning_DoesNotAnimateRotation()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(w.Dispatcher);

                    RotateTransform? rotate = icon.Template.FindName("PART_Rotate", icon) as RotateTransform;
                    Assert.IsNotNull(rotate, "FontIcon template must contain PART_Rotate.");
                    Assert.IsFalse(rotate.HasAnimatedProperties,
                        "With motion disabled the spin animation must not run even while IsSpinning is true.");
                    Assert.AreEqual(icon.Rotation, rotate.Angle, 0.01,
                        "With motion disabled the angle must rest at the static Rotation value.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_ToggleSwitch_Toggle_SnapsKnobToFinalOffsetSynchronously()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Controls.ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    ts.IsChecked = true;

                    // No drain: the knob must land at its final offset synchronously.
                    Assert.AreEqual(20.0, tx.X, 0.01,
                        "With motion disabled the knob must snap to the on offset (20) synchronously.");
                    Assert.IsFalse(tx.HasAnimatedProperties,
                        "With motion disabled the knob translate must carry no animation clock.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_Expander_Expand_OpensContentAtRestWithoutSlideClock()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(w.Dispatcher);

                    expander.IsExpanded = true;

                    // No drain: the steady state must apply synchronously (no deferred slide).
                    ContentPresenter? expandSite = expander.Template.FindName("ExpandSite", expander) as ContentPresenter;
                    Assert.IsNotNull(expandSite, "Expander template must contain the ExpandSite presenter.");
                    TranslateTransform? translate = expandSite.RenderTransform as TranslateTransform;
                    Assert.IsNotNull(translate, "ExpandSite must carry the mutable slide TranslateTransform.");
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "With motion disabled the expand slide must not run.");
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "With motion disabled the content must rest at offset 0 immediately.");

                    RowDefinition? contentRow = expander.Template.FindName("Row1Def", expander) as RowDefinition;
                    Assert.IsNotNull(contentRow, "Expander template must contain Row1Def (the content row when expanding down).");
                    Assert.IsTrue(contentRow.Height.IsStar,
                        "With motion disabled the content row must open to its star height immediately.");

                    DrainDispatcher(w.Dispatcher);
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "The content must stay at rest after layout with motion disabled.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_Flyout_ShowAt_PresentsSurfaceAtRestWithoutClocks()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.Flyout flyout = new() { Content = "Flyout body" };
                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the flyout popup.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Controls.FlyoutPresenter? presenter = popup.Child as Controls.FlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a FlyoutPresenter.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => presenter.IsLoaded),
                        "The presenter must load inside the open popup.");

                    TranslateTransform? translate =
                        presenter.Template.FindName("PresenterTranslate", presenter) as TranslateTransform;
                    Assert.IsNotNull(translate, "The presenter template must expose the PresenterTranslate transform.");
                    Border? surface = presenter.Template.FindName("PresenterSurface", presenter) as Border;
                    Assert.IsNotNull(surface, "The presenter template must expose the PresenterSurface root.");

                    Assert.AreEqual(0.0, translate.X, 0.001,
                        "With motion disabled the presenter must rest at X=0 with no reveal slide.");
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "With motion disabled the presenter must rest at Y=0 with no reveal slide.");
                    Assert.AreEqual(1.0, surface.Opacity, 0.001,
                        "With motion disabled the surface must show at full opacity immediately.");
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.IsFalse(surface.HasAnimatedProperties,
                        "With motion disabled the reveal fade must not leave an animation clock.");

                    flyout.Hide();
                    _ = WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen);
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_TeachingTip_Open_PresentsTipAtRestWithoutClocks()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: true } && tip.IsLoaded),
                        "IsOpen=true should open the host popup and load the tip.");

                    TranslateTransform? translate =
                        tip.Template.FindName("TipTranslate", tip) as TranslateTransform;
                    Assert.IsNotNull(translate, "The tip template must expose the TipTranslate transform.");
                    Grid? tipRoot = tip.Template.FindName("TipRoot", tip) as Grid;
                    Assert.IsNotNull(tipRoot, "The tip template must expose the TipRoot layout root.");

                    Assert.AreEqual(0.0, translate.X, 0.001,
                        "With motion disabled the tip must rest at X=0 with no reveal slide.");
                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "With motion disabled the tip must rest at Y=0 with no reveal slide.");
                    Assert.AreEqual(1.0, tipRoot.Opacity, 0.001,
                        "With motion disabled the tip must show at full opacity immediately.");
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.IsFalse(tipRoot.HasAnimatedProperties,
                        "With motion disabled the reveal fade must not leave an animation clock.");

                    tip.IsOpen = false;
                    _ = WaitUntil(window.Dispatcher, 2000, () => tip.HostPopup is { IsOpen: false });
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ReducedMotion_ContentDialog_Hide_TearsDownSynchronouslyWithoutClocks()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    System.Threading.Tasks.Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<System.Windows.Controls.Primitives.ButtonBase>(dialog, "PART_CloseButton") is not null),
                        "The dialog template must apply before Hide is called.");

                    dialog.Hide();

                    Assert.IsTrue(task.IsCompleted,
                        "With motion disabled Hide must tear the dialog down synchronously.");
                    Assert.IsFalse(dialog.HasAnimatedProperties,
                        "With motion disabled the close must not leave an opacity animation clock.");
                    if (dialog.RenderTransform is ScaleTransform scale)
                    {
                        Assert.IsFalse(scale.HasAnimatedProperties,
                            "With motion disabled the close must not leave scale animation clocks.");
                    }

                    Assert.IsTrue(dialog.IsHitTestVisible,
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

        [TestMethod]
        public void ReducedMotion_ComboBox_DropdownOpen_PresentsDropdownAtRestWithoutClocks()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new System.Windows.Controls.ComboBoxItem { Content = "Alpha" });
                try
                {
                    window.Content = combo;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    DrainDispatcher(window.Dispatcher);

                    System.Windows.Controls.Border? border =
                        combo.Template.FindName("PART_DropdownBorder", combo) as System.Windows.Controls.Border;
                    Assert.IsNotNull(border, "PART_DropdownBorder must exist in the template.");
                    TranslateTransform? translate = border.RenderTransform as TranslateTransform;
                    Assert.IsNotNull(translate, "PART_DropdownBorder must carry the DropdownTranslate transform.");

                    Assert.AreEqual(0.0, translate.Y, 0.001,
                        "With motion disabled the dropdown must rest at Y=0 with no reveal slide.");
                    Assert.AreEqual(1.0, border.Opacity, 0.001,
                        "With motion disabled the dropdown must show at full opacity immediately.");
                    Assert.IsFalse(translate.HasAnimatedProperties,
                        "With motion disabled the reveal slide must not leave an animation clock.");
                    Assert.IsFalse(border.HasAnimatedProperties,
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

        [TestMethod]
        public void ReducedMotion_OverrideTrue_ToggleSwitch_StillAnimatesKnob()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);
                MotionHelper.OverrideIsMotionEnabled = true;

                Controls.ToggleSwitch ts = new();
                Window w = new() { Content = ts, Width = 200, Height = 200 };
                try
                {
                    w.Show();
                    DrainDispatcher(w.Dispatcher);

                    TranslateTransform tx = GetToggleSwitchKnobTranslate(ts);
                    ts.IsChecked = true;

                    Assert.IsTrue(tx.HasAnimatedProperties,
                        "With motion enabled the knob slide must animate, proving the gate is the only change.");
                    Assert.IsTrue(WaitUntil(w.Dispatcher, 2000, () => Math.Abs(tx.X - 20.0) < 0.01),
                        "The animated knob must settle at the on offset.");
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = null;
                    w.Close();
                }
            });
        }
    }
}
