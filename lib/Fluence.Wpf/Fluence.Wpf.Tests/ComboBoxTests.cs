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
using System.Runtime.ExceptionServices;
using System.Windows;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class ComboBoxTests
    {
        private static void RunOnFreshStaThread(Action action)
        {
            Exception? capturedException = null;
            WpfTestSta.Dispatcher?.Invoke(new Action(delegate
            {
                try
                {
                    action();
                }
                catch (Exception exception)
                {
                    capturedException = exception;
                }
            }));

            if (capturedException is not null)
            {
                ExceptionDispatchInfo.Capture(capturedException).Throw();
            }
        }

        private static Application? EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static ResourceDictionary? MergeTheme(Application? application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            return dictionaries?.Count > 0 ? dictionaries[^1] : null;
        }

        private static void RunWithComboBox(Action<ComboBox> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                ComboBox comboBox = new();
                testBody(comboBox);
            });
        }

        #region Dropdown placement

        [Fact]
        public void IsDropDownOpenedUpward_DefaultIsFalse()
        {
            RunWithComboBox(static cb =>
            {
                Assert.False(cb.IsDropDownOpenedUpward,
                    "IsDropDownOpenedUpward should default to false.");
            });
        }

        [Fact]
        public void DropdownCornerRadius_DefaultIs8()
        {
            RunWithComboBox(static cb => Assert.Equal(new CornerRadius(8), cb.DropdownCornerRadius));
        }

        [Fact]
        public void IsDropDownOpenedUpward_FalseWhenDropDownNotOpen()
        {
            RunWithComboBox(static cb =>
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
            string xamlPath = System.IO.Path.Combine(
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
            string xamlPath = System.IO.Path.Combine(
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
            string xamlPath = System.IO.Path.Combine(
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
        public void ComboBox_Template_ExposesDropdownBorderAndNoiseOverlay()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? application = EnsureApplication();
                _ = MergeTheme(application);

                Window window = new();
                try
                {
                    ComboBox comboBox = new();
                    _ = comboBox.Items.Add("Alpha");
                    _ = comboBox.Items.Add("Beta");

                    window.Content = comboBox;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.Dispatcher?.Invoke(static () => { }, System.Windows.Threading.DispatcherPriority.Background, default);
                    window.UpdateLayout();
                    _ = comboBox.ApplyTemplate();

                    System.Windows.Controls.Border? dropdownBorder = comboBox.Template.FindName("PART_DropdownBorder", comboBox)
                        as System.Windows.Controls.Border;
                    Assert.NotNull(dropdownBorder);

                    System.Windows.Controls.Border? noiseOverlay = comboBox.Template.FindName("NoiseOverlay", comboBox)
                        as System.Windows.Controls.Border;
                    Assert.NotNull(noiseOverlay);

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
        public void FirstItem_AutoSelectedWhenNoSelectionProvided()
        {
            RunWithComboBox(static cb =>
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
        public void ExplicitSelectedIndex_MinusOne_IsRespected()
        {
            RunWithComboBox(static cb =>
            {
                cb.SelectedIndex = -1;

                _ = cb.Items.Add("Alpha");
                _ = cb.Items.Add("Beta");
                _ = cb.Items.Add("Gamma");

                Assert.Equal(-1, cb.SelectedIndex);
            });
        }

        [Fact]
        public void AutoSelect_WorksAfterDynamicItemAdd()
        {
            RunWithComboBox(static cb =>
            {
                Assert.Equal(-1, cb.SelectedIndex);

                _ = cb.Items.Add("First");
                _ = cb.Items.Add("Second");
                _ = cb.Items.Add("Third");

                Assert.Equal(0, cb.SelectedIndex);
            });
        }

        [Fact]
        public void AutoSelect_DoesNotOverrideExplicitSelection()
        {
            RunWithComboBox(static cb =>
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
    }
}
