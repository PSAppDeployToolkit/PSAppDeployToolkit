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
using System.Collections;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.ComboBox"/> control: dropdown placement, hover
    /// brush, popup corner tracking, auto-select and FocusedStates VSM.
    /// Authority: WinUI 3 ComboBox_themeresources.xaml (FocusedStates / EditableFocusedStates groups).
    /// </summary>
    public sealed class ComboBoxTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        private static Task RunWithComboBoxAsync(Action<Controls.ComboBox> testBody)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Controls.ComboBox comboBox = new();
                testBody(comboBox);
            });
        }

        #region Dropdown placement

        [Fact]
        public Task IsDropDownOpenedUpward_DefaultIsFalseAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                Assert.False(cb.IsDropDownOpenedUpward,
                    "IsDropDownOpenedUpward should default to false.");
            });
        }

        [Fact]
        public Task DropdownCornerRadius_DefaultIs8Async()
        {
            return RunWithComboBoxAsync(static cb => Assert.Equal(new CornerRadius(8), cb.DropdownCornerRadius));
        }

        [Fact]
        public Task IsDropDownOpenedUpward_FalseWhenDropDownNotOpenAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                _ = cb.Items.Add("A");
                _ = cb.Items.Add("B");

                Assert.False(cb.IsDropDownOpen,
                    "IsDropDownOpen should be false by default.");
                Assert.False(cb.IsDropDownOpenedUpward,
                    "IsDropDownOpenedUpward must be false when dropdown is not open.");
            });
        }

        #endregion Dropdown placement

        #region Hover state (brush verification)

        [Fact]
        public void ComboBoxXaml_HoverUsesSubtleFillBrush()
        {
            string xamlPath = System.IO.Path.Join(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\ComboBox.xaml");

            if (System.IO.File.Exists(xamlPath))
            {
                string xaml = System.IO.File.ReadAllText(xamlPath);

                Assert.True(
                    xaml.Contains("SubtleFillColorSecondaryBrush", StringComparison.Ordinal),
                    "ComboBoxItem hover trigger must use SubtleFillColorSecondaryBrush.");

                Assert.False(
                    xaml.Contains("IsHighlighted", StringComparison.Ordinal) &&
                    xaml.Contains("ControlFillColorSecondaryBrush", StringComparison.Ordinal) &&
                    !xaml.Contains("SubtleFillColorSecondaryBrush", StringComparison.Ordinal),
                    "ComboBoxItem must not use ControlFillColorSecondaryBrush for hover.");
            }
        }

        #endregion Hover state (brush verification)

        #region Popup corner tracking (bottom-rounded regression guard)

        // Regression: the inner acrylic-noise Border inside the popup was using a
        // fixed TemplateBinding for CornerRadius, so when IsDropDownOpenedUpward=True
        // flipped the OUTER PART_DropdownBorder to "8,8,0,0" (flat bottom) the inner
        // noise Border kept "8,8,8,8", painting a rounded bottom noise that no longer
        // matched the outer shape. Users saw this as "bottom corners not properly
        // rounded". The fix names the inner Border "NoiseOverlay" and extends the
        // IsDropDownOpenedUpward trigger to flip NoiseOverlay.CornerRadius in lockstep.

        [Fact]
        public void ComboBoxXaml_NoiseOverlay_IsNamed()
        {
            string xamlPath = System.IO.Path.Join(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\ComboBox.xaml");

            if (!System.IO.File.Exists(xamlPath))
            {
                return;
            }

            string xaml = System.IO.File.ReadAllText(xamlPath);

            Assert.True(
                xaml.Contains("x:Name=\"NoiseOverlay\"", StringComparison.Ordinal),
                "The acrylic-noise Border inside PART_DropdownBorder must be named " +
                "\"NoiseOverlay\" so the IsDropDownOpenedUpward trigger can retarget " +
                "its CornerRadius to match the outer border's flat-bottom shape.");
        }

        [Fact]
        public void ComboBoxXaml_UpwardTrigger_SetsNoiseOverlayCornerRadius()
        {
            string xamlPath = System.IO.Path.Join(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\ComboBox.xaml");

            if (!System.IO.File.Exists(xamlPath))
            {
                return;
            }

            string xaml = System.IO.File.ReadAllText(xamlPath);

            // The setter must appear inside the IsDropDownOpenedUpward trigger and
            // target NoiseOverlay with the same "8,8,0,0" value used by the outer
            // PART_DropdownBorder - otherwise the noise overlay paints rounded
            // bottom corners while the outer is flat, producing the visual bug.
            const string expectedSetter =
                "<Setter TargetName=\"NoiseOverlay\" Property=\"CornerRadius\" Value=\"8,8,0,0\" />";

            Assert.True(
                xaml.Contains(expectedSetter, StringComparison.Ordinal),
                "IsDropDownOpenedUpward trigger must set NoiseOverlay.CornerRadius=\"8,8,0,0\" " +
                "so the inner noise tracks the outer flat-bottom shape when the popup opens upward.");
        }

        [Fact]
        public Task ComboBox_Template_ExposesDropdownBorderAndNoiseOverlayAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();
                try
                {
                    Controls.ComboBox comboBox = new();
                    _ = comboBox.Items.Add("Alpha");
                    _ = comboBox.Items.Add("Beta");

                    window.Content = comboBox;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    await WpfTestSta.Dispatcher.InvokeAsync(static () => { }, priority: System.Windows.Threading.DispatcherPriority.Background, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    window.UpdateLayout();
                    _ = comboBox.ApplyTemplate();

                    Border dropdownBorder = Assert.IsType<Border>(
                        comboBox.Template.FindName("PART_DropdownBorder", comboBox), exactMatch: false);

                    Border noiseOverlay = Assert.IsType<Border>(
                        comboBox.Template.FindName("NoiseOverlay", comboBox), exactMatch: false);

                    // Default (downward-opening) state: both borders share the same radius,
                    // inherited from DropdownCornerRadius (default CornerRadius(8)).
                    Assert.Equal(new CornerRadius(8), dropdownBorder.CornerRadius);
                    Assert.Equal(new CornerRadius(8), noiseOverlay.CornerRadius);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        #endregion Popup corner tracking (bottom-rounded regression guard)

        #region Auto-select first item

        [Fact]
        public Task FirstItem_AutoSelectedWhenNoSelectionProvidedAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                _ = cb.Items.Add("Alpha");
                _ = cb.Items.Add("Beta");
                _ = cb.Items.Add("Gamma");
                _ = cb.Items.Add("Delta");
                _ = cb.Items.Add("Epsilon");

                Assert.Equal(0, cb.SelectedIndex);
            });
        }

        [Fact]
        public Task ExplicitSelectedIndex_MinusOne_IsRespectedAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                cb.SelectedIndex = -1;

                _ = cb.Items.Add("Alpha");
                _ = cb.Items.Add("Beta");
                _ = cb.Items.Add("Gamma");

                Assert.Equal(-1, cb.SelectedIndex);
            });
        }

        [Fact]
        public Task AutoSelect_WorksAfterDynamicItemAddAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                Assert.Equal(-1, cb.SelectedIndex);

                _ = cb.Items.Add("First");
                _ = cb.Items.Add("Second");
                _ = cb.Items.Add("Third");

                Assert.Equal(0, cb.SelectedIndex);
            });
        }

        [Fact]
        public Task AutoSelect_DoesNotOverrideExplicitSelectionAsync()
        {
            return RunWithComboBoxAsync(static cb =>
            {
                _ = cb.Items.Add("Alpha");
                _ = cb.Items.Add("Beta");
                _ = cb.Items.Add("Gamma");

                cb.SelectedIndex = 2;

                _ = cb.Items.Add("Delta");

                Assert.Equal(2, cb.SelectedIndex);
            });
        }

        #endregion Auto-select first item

        #region WI-3 C18  ComboBox FocusedStates VSM

        [Fact]
        public Task ComboBox_FocusedStates_GroupExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("One");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // VSM groups are attached to the root Grid of the template
                Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(cb), exactMatch: false);
                IList groups = VisualStateManager.GetVisualStateGroups(root);
                bool hasFocusedStates = groups
                    .Cast<VisualStateGroup>()
                    .Any(static g => string.Equals(g.Name, "FocusedStates", StringComparison.Ordinal));
                Assert.True(hasFocusedStates,
                    "ComboBox template root must have a FocusedStates VSM group per WI-3 C18.");
                w.Close();
            });
        }

        [Fact]
        public Task ComboBox_EditableFocusedStates_GroupExistsInTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("One");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(cb), exactMatch: false);
                IList groups = VisualStateManager.GetVisualStateGroups(root);
                bool hasEditableFocusedStates = groups
                    .Cast<VisualStateGroup>()
                    .Any(static g => string.Equals(g.Name, "EditableFocusedStates", StringComparison.Ordinal));
                Assert.True(hasEditableFocusedStates,
                    "ComboBox template root must have an EditableFocusedStates VSM group per WI-3 C18.");
                w.Close();
            });
        }

        [Fact]
        public Task ComboBox_FocusedState_DoesNotShowFocusAccentLineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Alpha");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                bool transitioned = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                Assert.True(transitioned, "GoToState('Focused') must return true.");
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border accentLine = Assert.IsType<Border>(FindVisualChildByName<Border>(cb, "FocusAccentLine"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, accentLine.Visibility);
                Assert.Equal(0.0, accentLine.Opacity, 0.01);
                w.Close();
            });
        }

        [Fact]
        public Task ComboBox_UnfocusedState_FocusAccentLineIsHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Beta");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                // Focused first, then Unfocused
                _ = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                bool transitioned = VisualStateManager.GoToState(cb, "Unfocused", useTransitions: false);
                Assert.True(transitioned, "GoToState('Unfocused') must return true.");
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border accentLine = Assert.IsType<Border>(FindVisualChildByName<Border>(cb, "FocusAccentLine"), exactMatch: false);
                Assert.Equal(0.0, accentLine.Opacity, 0.01);
                Assert.Equal(Visibility.Collapsed, accentLine.Visibility);
                w.Close();
            });
        }

        [Fact]
        public Task ComboBox_InitialTemplate_DoesNotShowFocusAccentLineAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Alpha");
                cb.SelectedIndex = 0;
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border accentLine = Assert.IsType<Border>(FindVisualChildByName<Border>(cb, "FocusAccentLine"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, accentLine.Visibility);
                Assert.Equal(0.0, accentLine.Opacity, 0.01);

                w.Close();
            });
        }

        [Fact]
        public Task ComboBox_ThemeCycle_FocusedStateKeepsAccentLineHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox cb = new();
                _ = cb.Items.Add("Gamma");
                Window w = new() { Content = cb, Width = 300, Height = 100 };
                w.Show();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                ThemeTestHelpers.ApplyStandardThemeCycle();
                WpfTestSta.DrainDispatcher(w.Dispatcher);
                _ = cb.ApplyTemplate();
                w.UpdateLayout();
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                bool transitioned = VisualStateManager.GoToState(cb, "Focused", useTransitions: false);
                Assert.True(transitioned, "GoToState('Focused') must return true after theme cycle.");
                WpfTestSta.DrainDispatcher(w.Dispatcher);

                Border accentLine = Assert.IsType<Border>(FindVisualChildByName<Border>(cb, "FocusAccentLine"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, accentLine.Visibility);
                Assert.Equal(0.0, accentLine.Opacity, 0.01);
                w.Close();
            });
        }

        #endregion WI-3 C18  ComboBox FocusedStates VSM

        [Fact]
        public Task Stage3_ComboBox_PlaceholderText_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox combo = new() { PlaceholderText = "Pick one" };
                Assert.Equal("Pick one", combo.PlaceholderText, StringComparer.Ordinal);
            });
        }

        [Fact]
        public Task ComboBox_SelectionChange_UpdatesDisplayedContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });
                combo.SelectedIndex = 0;

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(combo.Template.FindName("contentPresenter", combo));
                    Assert.Equal("Alpha", presenter.Content as string, StringComparer.Ordinal);

                    combo.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("Beta", presenter.Content as string, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_ItemTemplate_HasHoverOverlayAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(0));
                    _ = item.ApplyTemplate();

                    Assert.NotNull(item.Template.FindName("OuterBorder", item));
                    Assert.NotNull(item.Template.FindName("SelectionIndicator", item));

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_DropDownItem_UsesWinUiMetricsAsync()
        {
            // ComboBox_themeresources_perf2026.xaml: ComboBoxItemThemePadding 11,5,11,7 (line 335),
            // ComboBoxItemCornerRadius 3 (line 345), the item LayoutRoot margin 5,2,5,2 (line 568),
            // and the selection pill at ComboBoxItemPillWidth 3 with
            // ComboBoxItemPillCornerRadius 1.5 (lines 325 and 346).
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
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
                    window.UpdateLayout();

                    Assert.Equal(504.0, combo.MaxDropDownHeight);

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(0));
                    _ = item.ApplyTemplate();
                    Assert.Equal(new Thickness(11, 5, 11, 7), item.Padding);

                    Border outer = Assert.IsType<Border>(item.Template.FindName("OuterBorder", item), exactMatch: false);
                    Assert.Equal(new Thickness(5, 2, 5, 2), outer.Margin);
                    Assert.Equal(new CornerRadius(3), outer.CornerRadius);

                    Border pill = Assert.IsType<Border>(item.Template.FindName("SelectionIndicator", item), exactMatch: false);
                    Assert.Equal(3.0, pill.Width);
                    Assert.Equal(16.0, pill.Height);
                    Assert.Equal(new CornerRadius(1.5), pill.CornerRadius);

                    combo.IsDropDownOpen = false;
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_DropdownReveal_SettlesAtRestAndSurvivesReopenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border border = Assert.IsType<Border>(combo.Template.FindName("PART_DropdownBorder", combo));
                    TranslateTransform translate =
                        Assert.IsType<TranslateTransform>(border.RenderTransform);
                    Panel dropdownRoot = Assert.IsType<Panel>(combo.Template.FindName("PART_DropdownRoot", combo), exactMatch: false);

                    // The fade runs on the dropdown root, not on the surface border alone, so
                    // the opaque elevation caster behind the surface fades with it instead of
                    // painting a blank plate at full strength on the first frame of the open.
                    Assert.Equal(0.0, dropdownRoot.Opacity, 0.001);

                    // The code-driven reveal (moved out of the template MultiTriggers) must
                    // settle at the rest position with its Stop-fill clocks released.
                    for (int open = 0; open < 2; open++)
                    {
                        combo.IsDropDownOpen = true;
                        Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                                () => Math.Abs(translate.Y) < 0.001 && dropdownRoot.Opacity >= 1.0 &&
                                    !translate.HasAnimatedProperties && !dropdownRoot.HasAnimatedProperties).ConfigureAwait(true),
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "Open {0}: the dropdown reveal must settle at Y=0, full opacity, and release its clocks.",
                                open));

                        combo.IsDropDownOpen = false;
                        WpfTestSta.DrainDispatcher(window.Dispatcher);

                        // Closing re-hides the root so the next open cannot composite a frame at
                        // rest before the reveal seeds its start pose.
                        Assert.True(dropdownRoot.Opacity < 0.001,
                            string.Format(
                                System.Globalization.CultureInfo.InvariantCulture,
                                "Open {0}: closing the dropdown must return the root to hidden.",
                                open));
                    }
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_DropdownReveal_RestsAtTheOpenPoseWhileItRunsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 400, Height = 300 };
                Controls.ComboBox combo = new() { Width = 240 };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Border border = Assert.IsType<Border>(combo.Template.FindName("PART_DropdownBorder", combo));
                    TranslateTransform translate = Assert.IsType<TranslateTransform>(border.RenderTransform);
                    Panel dropdownRoot = Assert.IsType<Panel>(combo.Template.FindName("PART_DropdownRoot", combo), exactMatch: false);

                    combo.IsDropDownOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Regression guard, the defect that hid the flyout: the reveal clocks are
                    // FillBehavior.Stop, so the property reverts to its BASE value the instant the
                    // clock ends, before any Completed handler runs. The base has to be the open
                    // pose, or a dropdown whose handler is late composites transparent and
                    // offset. The reveal's own discrete keyframe supplies the start frame.
                    Assert.Equal(1.0, (double)dropdownRoot.ReadLocalValue(UIElement.OpacityProperty), 0.001);

                    // Releasing the clocks is what the end of a Stop-fill clock does, minus the
                    // wait, so this reads the value the dropdown would settle at either way.
                    dropdownRoot.BeginAnimation(UIElement.OpacityProperty, animation: null);
                    translate.BeginAnimation(TranslateTransform.YProperty, animation: null);

                    Assert.Equal(1.0, dropdownRoot.Opacity, 0.001);
                    Assert.Equal(0.0, translate.Y, 0.001);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_NoSelection_ShowsPlaceholderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Choose...",
                    SelectedIndex = -1,
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock placeholder = Assert.IsType<TextBlock>(combo.Template.FindName("PlaceholderTextBlock", combo));
                    Assert.Equal(Visibility.Visible, placeholder.Visibility);

                    combo.SelectedIndex = 0;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, placeholder.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_ToggleButton_OpensDropDownAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = combo.ApplyTemplate();
                    ToggleButton toggle = Assert.IsType<ToggleButton>(combo.Template.FindName("ToggleButton", combo), exactMatch: false);
                    Popup popup = Assert.IsType<Popup>(combo.Template.FindName("PART_Popup", combo));

                    ToggleButtonAutomationPeer peer = new(toggle);
                    IToggleProvider toggleProvider = Assert.IsType<IToggleProvider>(peer.GetPattern(PatternInterface.Toggle), exactMatch: false);

                    toggleProvider.Toggle();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(combo.IsDropDownOpen, "ComboBox toggle should open the drop-down.");
                    Assert.True(popup.IsOpen, "ComboBox popup should open when the toggle is clicked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_ToggleButton_UsesReleaseClickModeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = combo.ApplyTemplate();
                    ToggleButton toggle = Assert.IsType<ToggleButton>(combo.Template.FindName("ToggleButton", combo), exactMatch: false);

                    Assert.Equal(ClickMode.Release, toggle.ClickMode);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBox_DropDownSelection_UpdatesSelectedIndexAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.ComboBox combo = new()
                {
                    Width = 240,
                    PlaceholderText = "Pick one",
                };
                _ = combo.Items.Add(new ComboBoxItem { Content = "Alpha" });
                _ = combo.Items.Add(new ComboBoxItem { Content = "Beta" });

                try
                {
                    window.Content = combo;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    combo.IsDropDownOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(combo.ItemContainerGenerator.ContainerFromIndex(1));

                    item.IsSelected = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, combo.SelectedIndex);
                    Assert.Equal("Beta", combo.SelectedText, StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ComboBoxItem_CornerRadius_ComesFromTheKeyedResourceAsync()
        {
            // WinUI's ComboBoxItemCornerRadius is 3, deliberately distinct from ControlCornerRadius
            // (4). It used to be a literal in the item template, which put it out of reach of an
            // application that retunes corner radii; keying it is what makes it reachable.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 320, Height = 200 };
                Controls.ComboBox comboBox = new();
                _ = comboBox.Items.Add("Alpha");
                _ = comboBox.Items.Add("Beta");
                comboBox.SelectedIndex = 0;

                try
                {
                    window.Content = comboBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(new CornerRadius(3), Assert.IsType<CornerRadius>(Application.Current.TryFindResource("ComboBoxItemCornerRadius")));

                    comboBox.SetCurrentValue(ComboBox.IsDropDownOpenProperty, value: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ComboBoxItem item = Assert.IsType<ComboBoxItem>(
                        comboBox.ItemContainerGenerator.ContainerFromIndex(0), exactMatch: false);
                    Border outer = Assert.IsType<Border>(FindVisualChildByName<Border>(item, "OuterBorder"), exactMatch: false);
                    Assert.Equal(new CornerRadius(3), outer.CornerRadius);

                    // The key is the single source: overriding it reaches the realized item.
                    Application.Current.Resources["ComboBoxItemCornerRadius"] = new CornerRadius(1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(new CornerRadius(1), outer.CornerRadius);
                }
                finally
                {
                    Application.Current.Resources.Remove("ComboBoxItemCornerRadius");
                    comboBox.SetCurrentValue(ComboBox.IsDropDownOpenProperty, value: false);
                    window.Close();
                }
            });
        }
    }
}
