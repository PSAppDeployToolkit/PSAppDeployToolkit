using System;
using System.Collections;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.Interfaces.Fluent;
using PSADT.UserInterface.Interfaces.Tests.TestHelpers;
using Xunit;

namespace PSADT.UserInterface.Interfaces.Tests.Fluent
{
    /// <summary>
    /// Tests the dialog's calculated and exact accent dispatch across theme changes.
    /// </summary>
    public sealed class FluentAccentColorTests
    {
        private static readonly Color LightAccent = Color.FromRgb(0x87, 0xAB, 0xC8);
        private static readonly Color DarkAccent = Color.FromRgb(0x42, 0x36, 0xA4);

        /// <summary>
        /// Verifies that two exact colors stay visible through repeated theme switches.
        /// </summary>
        [Fact]
        public void ExactPair_PinsBothVisibleFillsAcrossThemeSwitches()
        {
            WithAccentDialog(Options(light: LightAccent, dark: DarkAccent, exact: true), static dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(LightAccent, VisibleFill(dialog));
                Assert.Equal(LightAccent, ApplicationAccentColorManager.SystemAccentColorPrimary);

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(DarkAccent, VisibleFill(dialog));
                Assert.Equal(DarkAccent, ApplicationAccentColorManager.SystemAccentColorPrimary);

                ApplyTheme(ApplicationTheme.HighContrast);
                Assert.Equal(SystemColors.WindowColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("AccentFillColorDefaultBrush")).Color);
                Assert.Equal(SystemColors.HighlightColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("SystemAccentColorPrimaryBrush")).Color);

                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(LightAccent, VisibleFill(dialog));
            });
        }

        /// <summary>
        /// Verifies that a single exact Light color derives the Dark primary from its ramp.
        /// </summary>
        [Fact]
        public void ExactLightOnly_DerivesDarkVisibleFillFromResolvedRamp()
        {
            WithAccentDialog(Options(light: LightAccent, dark: null, exact: true), static dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(LightAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, VisibleFill(dialog));
                Assert.NotEqual(LightAccent, VisibleFill(dialog));
            });
        }

        /// <summary>
        /// Verifies that a calculated Light seed remains pinned while Dark uses its derived shade.
        /// </summary>
        [Fact]
        public void CalculatedLightOnly_DerivesDarkVisibleFillAcrossThemeSwitches()
        {
            WithAccentDialog(Options(light: LightAccent, dark: null, exact: null), static dialog =>
            {
                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorDark1, VisibleFill(dialog));
                Assert.NotEqual(LightAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, VisibleFill(dialog));
            });
        }

        /// <summary>
        /// Verifies that a Dark-only override leaves Light on the Windows accent.
        /// </summary>
        [Fact]
        public void ExactDarkOnly_UsesSystemLightAndExplicitDark()
        {
            Color systemLight = SystemLightFill();
            WithAccentDialog(Options(light: null, dark: DarkAccent, exact: true), dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(DarkAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.HighContrast);
                Assert.Equal(SystemColors.WindowColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("AccentFillColorDefaultBrush")).Color);
                Assert.Equal(SystemColors.HighlightColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("SystemAccentColorPrimaryBrush")).Color);

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(DarkAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));
            });
        }

        /// <summary>
        /// Verifies that a calculated Dark-only seed leaves Light on Windows and uses its Dark shade.
        /// </summary>
        [Fact]
        public void CalculatedDarkOnly_UsesSystemLightAndDerivedDark()
        {
            Color systemLight = SystemLightFill();
            WithAccentDialog(Options(light: null, dark: DarkAccent, exact: null), dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, VisibleFill(dialog));
                Assert.NotEqual(DarkAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.HighContrast);
                Assert.Equal(SystemColors.WindowColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("AccentFillColorDefaultBrush")).Color);
                Assert.Equal(SystemColors.HighlightColor, Assert.IsType<SolidColorBrush>(dialog.TryFindResource("SystemAccentColorPrimaryBrush")).Color);

                ApplyTheme(ApplicationTheme.Dark);
                Assert.Equal(ApplicationAccentColorManager.SystemAccentColorLight2, VisibleFill(dialog));
                Assert.NotEqual(DarkAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));
            });
        }

        /// <summary>
        /// Verifies that the default keeps generated, theme-selected shades and no override resets to Windows.
        /// </summary>
        [Fact]
        public void CalculatedDefaultAndNoColors_PreserveExistingSelectionAndSystemReset()
        {
            WithAccentDialog(Options(light: LightAccent, dark: DarkAccent, exact: null), static dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.NotEqual(LightAccent, VisibleFill(dialog));

                ApplyTheme(ApplicationTheme.Dark);
                Assert.NotEqual(DarkAccent, VisibleFill(dialog));
            });

            Color systemLight = SystemLightFill();
            SeedStickyCustomAccent();
            WithAccentDialog(Options(light: null, dark: null, exact: null), dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));
            });
            SeedStickyCustomAccent();
            WithAccentDialog(Options(light: null, dark: null, exact: true), dialog =>
            {
                ApplyTheme(ApplicationTheme.Light);
                Assert.Equal(systemLight, VisibleFill(dialog));
            });
        }

        private static Hashtable Options(Color? light, Color? dark, bool? exact)
        {
            Hashtable options = SampleOptions.CustomDialog();
            if (light is Color lightColor)
            {
                options["FluentAccentColor"] = exact is bool isExact
                    ? new Hashtable { ["Color"] = PackedColor(lightColor), ["Exact"] = isExact }
                    : PackedColor(lightColor);
            }
            if (dark is Color darkColor)
            {
                options["FluentAccentColorDark"] = PackedColor(darkColor);
            }
            if (exact is bool useExactColors && light is null)
            {
                options["FluentAccentColor"] = new Hashtable { ["Color"] = null, ["Exact"] = useExactColors };
            }
            return options;
        }

        private static int PackedColor(Color color)
        {
            return unchecked((int)(((uint)color.A << 24) | ((uint)color.R << 16) | ((uint)color.G << 8) | color.B));
        }

        private static void ApplyTheme(ApplicationTheme theme)
        {
            ApplicationThemeManager.Apply(theme);
        }

        private static Color VisibleFill(CustomDialog dialog)
        {
            return Assert.IsType<Color>(dialog.TryFindResource("AccentFillColorDefault"));
        }

        private static Color SystemLightFill()
        {
            return DialogHost.Run(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ApplicationAccentColorManager.ApplySystemAccent();
                return ApplicationAccentColorManager.SystemAccentColorPrimary;
            });
        }

        private static void SeedStickyCustomAccent()
        {
            DialogHost.Run(static () =>
            {
                ApplicationThemeManager.Apply(ApplicationTheme.Light);
                ApplicationAccentColorManager.ApplyCustomAccentExact(LightAccent);
            });
        }

        private static void WithAccentDialog(Hashtable options, Action<CustomDialog> body)
        {
            DialogHost.Run(() =>
            {
                try
                {
                    using CustomDialog dialog = new(new CustomDialogOptions(options));
                    body(dialog);
                }
                finally
                {
                    ApplicationAccentColorManager.ApplySystemAccent();
                    ApplicationThemeManager.Apply(ApplicationTheme.Auto);
                }
            });
        }
    }
}
