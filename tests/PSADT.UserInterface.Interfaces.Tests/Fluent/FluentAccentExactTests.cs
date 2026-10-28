using System;
using System.Collections;
using System.Windows.Media;
using Fluence.Wpf;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Checks published accent colors in real dialogs without showing a window.
    /// </summary>
    public sealed class FluentAccentExactTests
    {
        /// <summary>
        /// Verifies paired calculated colors by default and exact colors when either setting requests them.
        /// </summary>
        /// <param name="lightExact">The Light preference.</param>
        /// <param name="darkExact">The Dark preference.</param>
        /// <param name="expectedExact">Whether the visible primary colors must match the configured colors.</param>
        [Theory]
        [InlineData(false, false, false)]
        [InlineData(true, false, true)]
        [InlineData(false, true, true)]
        [InlineData(true, true, true)]
        public void Constructor_EitherExactPreferenceAppliesToBothThemes(bool lightExact, bool darkExact, bool expectedExact)
        {
            WithTheme(() =>
            {
                Color light = Color.FromRgb(0xFF, 0xEE, 0xCC);
                Color dark = Color.FromRgb(0x11, 0x22, 0x44);
                Hashtable table = SampleOptions.CustomDialog();
                table["FluentAccentColor"] = new Hashtable { ["Color"] = unchecked((int)0xFFFFEECC), ["Exact"] = lightExact };
                table["FluentAccentColorDark"] = new Hashtable { ["Color"] = unchecked((int)0xFF112244), ["Exact"] = darkExact };
                using CustomDialog dialog = new(new CustomDialogOptions(table));
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                AssertPrimary(dialog, expectedExact ? light : ApplicationAccentColorManager.SystemAccentColorDark1);
                if (!expectedExact)
                {
                    Assert.NotEqual(light, Assert.IsType<Color>(dialog.TryFindResource("AccentFillColorDefault")));
                }
                ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                AssertPrimary(dialog, expectedExact ? dark : ApplicationAccentColorManager.SystemAccentColorLight2);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                AssertPrimary(dialog, expectedExact ? light : ApplicationAccentColorManager.SystemAccentColorDark1);
            });
        }

        /// <summary>
        /// Verifies that the paired call fills a missing theme color from the current cached accent.
        /// </summary>
        /// <param name="darkOnly">Whether the configured color belongs to Dark only.</param>
        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public void Constructor_ExactPairFallsBackToTheConfiguredColor(bool darkOnly)
        {
            WithTheme(() =>
            {
                Color color = Color.FromRgb(0x87, 0xAB, 0xC8);
                Hashtable table = SampleOptions.CustomDialog();
                table[darkOnly ? "FluentAccentColorDark" : "FluentAccentColor"] = new Hashtable
                {
                    ["Color"] = unchecked((int)0xFF87ABC8),
                    ["Exact"] = true,
                };
                using CustomDialog dialog = new(new CustomDialogOptions(table));
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);

                if (darkOnly)
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                }
                AssertPrimary(dialog, color);
                ApplicationThemeManager.Apply(darkOnly ? ApplicationTheme.Light : ApplicationTheme.Dark, WindowBackdropType.None);
                AssertPrimary(dialog, color);
            });
        }

        /// <summary>
        /// Runs on the existing dialog apartment and restores the theme after each scenario.
        /// </summary>
        /// <param name="body">The hidden-dialog scenario.</param>
        private static void WithTheme(Action body)
        {
            DialogHost.Run(() =>
            {
                ApplicationTheme previousTheme = ApplicationThemeManager.CurrentTheme;
                WindowBackdropType previousBackdrop = ApplicationThemeManager.CurrentBackdrop;
                try
                {
                    ApplicationAccentColorManager.ApplySystemAccent();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    body();
                }
                finally
                {
                    ApplicationAccentColorManager.ApplySystemAccent();
                    ApplicationThemeManager.Apply(previousTheme, previousBackdrop);
                }
            });
        }

        /// <summary>
        /// Checks both the visible primary color and the brush using it.
        /// </summary>
        /// <param name="dialog">The hidden dialog.</param>
        /// <param name="expected">The expected visible primary.</param>
        private static void AssertPrimary(CustomDialog dialog, Color expected)
        {
            Assert.Equal(expected, Assert.IsType<Color>(dialog.TryFindResource("AccentFillColorDefault")));
            Assert.Equal(expected, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("AccentFillColorDefaultBrush")).Color);
        }
    }
}
