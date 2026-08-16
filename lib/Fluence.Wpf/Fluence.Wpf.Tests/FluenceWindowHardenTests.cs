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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Helpers;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// WI-2 hardening tests for FluenceWindow: backdrop swap, full HC theme cycle,
    /// close-button DynamicResource fix (Finding B).
    /// </summary>
    public class FluenceWindowHardenTests
    {

        private static void ResetAndApply(ApplicationTheme theme, Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();

            ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
        }

        // ---------------------------------------------------------------------------
        // 1. SystemBackdropType DP defaults and round-trip
        // ---------------------------------------------------------------------------

        [Fact]
        public Task SystemBackdropType_Default_IsAutoAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                FluenceWindow w = new();
                try
                {
                    Assert.Equal(BackdropType.Auto, w.SystemBackdropType);
                }
                finally { w.Close(); }
            });
        }

        [Fact]
        public Task SystemBackdropType_CanSetAllValuesAsync()
        {
            // Verifies that the DP accepts all four BackdropType values without throwing.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                FluenceWindow w = new();
                try
                {
                    foreach (BackdropType bd in new[] { BackdropType.None, BackdropType.Mica, BackdropType.Acrylic, BackdropType.Tabbed, BackdropType.Auto })
                    {
                        w.SystemBackdropType = bd;
                        Assert.Equal(bd, w.SystemBackdropType);
                    }
                }
                finally { w.Close(); }
            });
        }

        // ---------------------------------------------------------------------------
        // 2. Full theme cycle Light → Dark → HighContrast → Light; key brushes resolve
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ThemeCycle_LightDarkHcLight_KeyBrushesResolveAfterEachStepAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                string[] keys =
                [
                    "ApplicationBackgroundBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "ControlFillColorDefaultBrush",
                    "SystemFillColorCriticalBrush",
                    "WindowCloseButtonBackgroundPointerOverBrush",
                    "WindowCloseButtonBackgroundPressedBrush",
                    "WindowCloseButtonForegroundPointerOverBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in keys)
                    {
                        object resource = Assert.IsAssignableFrom<object>(app.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public Task ThemeCycle_HighContrast_SystemFillColorCriticalBrush_ResolvesAsync()
        {
            // HC theme maps SystemFillColorCriticalBrush to WindowTextColorKey (white on black).
            // Caption close-button chrome uses its own DynamicResource tokens; this guard keeps the
            // general critical brush available for controls that intentionally consume it.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, BackdropType.None, updateAccent: true);
                object brush = Assert.IsAssignableFrom<object>(app.TryFindResource("SystemFillColorCriticalBrush"));
            });
        }

        // ---------------------------------------------------------------------------
        // 4. Close button resource-token and template-part regression guards.
        // ---------------------------------------------------------------------------

        [Fact]
        public async Task FluenceWindowXaml_CloseButtonHover_UsesCanonicalCloseButtonBrushTokensAsync()
        {
            string xaml = await DemoTestHost.ReadRepositoryFileAsync("Fluence.Wpf", "Themes", "Controls", "FluenceWindow.xaml").ConfigureAwait(true);

            Assert.Contains("WindowCloseButtonBackgroundPointerOverBrush", xaml, StringComparison.Ordinal);
            Assert.Contains("WindowCloseButtonBackgroundPressedBrush", xaml, StringComparison.Ordinal);
            Assert.Contains("WindowCloseButtonForegroundPointerOverBrush", xaml, StringComparison.Ordinal);

            Assert.False(xaml.Contains("WindowCloseFillColorHoverBrush", StringComparison.Ordinal),
                "FluenceWindow.xaml should consume the canonical close-button background token.");
            Assert.False(xaml.Contains("WindowCloseFillColorPressedBrush", StringComparison.Ordinal),
                "FluenceWindow.xaml should consume the canonical close-button pressed token.");
            Assert.False(xaml.Contains("WindowCloseForegroundHoverBrush", StringComparison.Ordinal),
                "FluenceWindow.xaml should consume the canonical close-button foreground token.");
            Assert.False(xaml.Contains("SystemFillColorCriticalBrush", StringComparison.Ordinal),
                "Caption close-button hover must not use the general critical brush.");
            Assert.False(xaml.Contains("#C42B1C", StringComparison.Ordinal) || xaml.Contains("#B4271C", StringComparison.Ordinal) || xaml.Contains("#FFFFFF", StringComparison.Ordinal),
                "Production control templates must not inline close-button hex colors.");
        }

        [Fact]
        public Task FluenceWindowCloseButtonThemeTokens_AreThemeIndependentAndResolveAsync()
        {
            // The three Windows close-button Color tokens are theme-independent - the Windows shell
            // uses the same red across Light, Dark, and HighContrast - so they are seeded in code by
            // BaseColorTables, not duplicated across per-theme XAML. BrushFactory emits the *Brush twins.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                AssertCloseButtonBrush(app, "WindowCloseButtonBackgroundPointerOverBrush", Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C));
                AssertCloseButtonBrush(app, "WindowCloseButtonBackgroundPressedBrush", Color.FromArgb(0xFF, 0xB4, 0x27, 0x1C));
                AssertCloseButtonBrush(app, "WindowCloseButtonForegroundPointerOverBrush", Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF));
            });
        }

        private static void AssertCloseButtonBrush(Application app, string key, Color expected)
        {
            object? resource = app.TryFindResource(key);
            SolidColorBrush brush = Assert.IsAssignableFrom<SolidColorBrush>(resource);
            Assert.Equal(expected, brush.Color);
        }

        [Fact]
        public void FluenceWindow_DeclaresCaptionButtonTemplateParts()
        {
            object[] attributes = typeof(FluenceWindow).GetCustomAttributes(typeof(TemplatePartAttribute), inherit: false);

            AssertTemplatePart(attributes, "PART_MinimizeButton");
            AssertTemplatePart(attributes, "PART_MaximizeButton");
            AssertTemplatePart(attributes, "PART_RestoreButton");
            AssertTemplatePart(attributes, "PART_CloseButton");
        }

        private static void AssertTemplatePart(object[] attributes, string name)
        {
            if (!attributes.OfType<TemplatePartAttribute>().Any(attribute => string.Equals(attribute.Name, name, StringComparison.Ordinal) && attribute.Type == typeof(System.Windows.Controls.Button)))
            {
                Assert.Fail("FluenceWindow must declare TemplatePart '" + name + "' with type System.Windows.Controls.Button.");
            }
        }

        // ---------------------------------------------------------------------------
        // 5. WindowPolicy.BuildBackdropPlan - None backdrop returns non-transparent bg
        // ---------------------------------------------------------------------------

        [Fact]
        public void BuildBackdropPlan_None_ReturnsOpaqueBackground()
        {
            // Capability with no backdrop support at all.
            WindowCapabilities caps = new(
                supportsSystemBackdropType: false,
                supportsMicaEffect: false,
                supportsRoundedCorners: false,
                supportsCaptionColor: false,
                supportsBorderColor: false);

            Color light = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.None, ApplicationTheme.Light, caps, light);

            Assert.False(plan.UseTransparentBackground,
                "BackdropType.None must NOT use transparent background.");
            Assert.NotEqual(Colors.Transparent, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Mica_SupportedOs_ReturnsTransparent()
        {
            WindowCapabilities caps = new(
                supportsSystemBackdropType: true,
                supportsMicaEffect: true,
                supportsRoundedCorners: true,
                supportsCaptionColor: true,
                supportsBorderColor: true);

            Color fallback = Color.FromRgb(0xFA, 0xFA, 0xFA);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.Mica, ApplicationTheme.Light, caps, fallback);

            Assert.True(plan.UseTransparentBackground,
                "Mica backdrop on a capable OS must use transparent background.");
            Assert.Equal(Colors.Transparent, plan.BackgroundColor);
        }

        [Fact]
        public void BuildBackdropPlan_Acrylic_FallsBackToMica_WhenMicaEffectButNoSystemBackdrop()
        {
            // Windows 10 21H2: supports DwmSetWindowAttribute(DWMWA_MICA_EFFECT) but NOT
            // DWMWA_SYSTEMBACKDROP_TYPE. Acrylic request must downgrade to Mica.
            WindowCapabilities caps = new(
                supportsSystemBackdropType: false,
                supportsMicaEffect: true,
                supportsRoundedCorners: false,
                supportsCaptionColor: false);

            Color fallback = Color.FromRgb(0x20, 0x20, 0x20);
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(BackdropType.Acrylic, ApplicationTheme.Dark, caps, fallback);

            // Should fall back to Mica (legacy) and use transparent background.
            Assert.True(plan.UseTransparentBackground,
                "Acrylic→Mica fallback must still use transparent background.");
            Assert.Equal(BackdropType.Mica, plan.EffectiveBackdrop);
        }

        // ---------------------------------------------------------------------------
        // 6. C3: manager subscription leak guard.
        //
        // The static managers hold strong references to every subscribed FluenceWindow.
        // Subscribing in the constructor leaked windows that were constructed but never
        // shown (and therefore never reach OnClosed to unsubscribe). The fix moves the
        // subscriptions to OnSourceInitialized (HWND realisation) so only shown windows
        // subscribe, and they always reach OnClosed.
        //
        // A GC + WeakReference test cannot prove this here: Application.AddWindow roots
        // every constructed Window for the lifetime of the Application. Instead we count
        // subscribers directly via the compiler-emitted private static delegate backing
        // fields for the two field-like events.
        // ---------------------------------------------------------------------------

        private static int GetEventSubscriberCount(Type declaringType, string eventName)
        {
            FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(declaringType.GetField(eventName, BindingFlags.NonPublic | BindingFlags.Static));
            Delegate? handler = field.GetValue(null) as Delegate;
            return handler?.GetInvocationList().Length ?? 0;
        }

        private static (int Theme, int Accent) SnapshotManagerSubscriberCounts()
        {
            int theme = GetEventSubscriberCount(typeof(ApplicationThemeManager), "Changed");
            int accent = GetEventSubscriberCount(typeof(ApplicationAccentColorManager), "AccentColorChanged");
            return (theme, accent);
        }

        [Fact]
        public Task Constructor_DoesNotSubscribeToManagersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                (int beforeTheme, int beforeAccent) = SnapshotManagerSubscriberCounts();
                FluenceWindow w = new();
                try
                {
                    (int afterTheme, int afterAccent) = SnapshotManagerSubscriberCounts();
                    Assert.Equal(beforeTheme, afterTheme);
                    Assert.Equal(beforeAccent, afterAccent);
                }
                finally { w.Close(); }
            });
        }

        [Fact]
        public Task ShowThenClose_LeavesNoNetManagerSubscriptionsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                (int baselineTheme, int baselineAccent) = SnapshotManagerSubscriberCounts();

                FluenceWindow w = new()
                {
                    Width = 200,
                    Height = 150,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                w.Show();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                w.Close();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                (int afterTheme, int afterAccent) = SnapshotManagerSubscriberCounts();
                Assert.Equal(baselineTheme, afterTheme);
                Assert.Equal(baselineAccent, afterAccent);
            });
        }

        // ---------------------------------------------------------------------------
        // 7. First-paint redirection-surface guard.
        //
        // A top-level WPF window paints two background layers: the WPF content background
        // (Window.Background) and the HWND redirection surface (HwndTarget.BackgroundColor),
        // which WPF clears to opaque black by default. With an active DWM backdrop the content
        // background is transparent, so a default-black redirection surface flashes before the
        // system backdrop composites (the first-paint "black flash"). FluenceWindow clears the
        // redirection surface to match the content background, which is why it never needs to
        // DWM-cloak the window. These tests pin both invariants: the redirection surface tracks
        // the content background across a backdrop swap, and the window is never left cloaked
        // (a cloaked window is permanently invisible - the failure mode of the abandoned cloak).
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ShowThenDrain_NeverCloaksWindowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = BackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    nint handle = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                    Assert.Equal(0, Native.NativeMethods.GetWindowCloakedState(handle));
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task RedirectionSurface_MatchesContentBackground_AcrossBackdropSwapAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = BackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    nint handle = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                    System.Windows.Interop.HwndTarget sourceCompositionTarget = Assert.IsAssignableFrom<System.Windows.Interop.HwndTarget>(System.Windows.Interop.HwndSource.FromHwnd(handle)?.CompositionTarget);

                    // The fix: the HWND redirection surface (HwndTarget.BackgroundColor) must be
                    // cleared to the same color WPF paints its content background, so no opaque
                    // black surface is exposed before the DWM backdrop composites.
                    Color content = ((SolidColorBrush)w.Background).Color;
                    Assert.Equal(content, sourceCompositionTarget.BackgroundColor);

                    // Swapping to None re-runs ApplyBackdrop; both layers must move together to the
                    // opaque theme fallback so the invariant holds across runtime backdrop changes.
                    w.SystemBackdropType = BackdropType.None;
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                    Color contentNone = ((SolidColorBrush)w.Background).Color;
                    Assert.Equal(contentNone, sourceCompositionTarget.BackgroundColor);
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        private static int GetWatchedWindowCount()
        {
            FieldInfo field = Assert.IsAssignableFrom<FieldInfo>(typeof(SystemThemeWatcher).GetField("_watchedWindows", BindingFlags.NonPublic | BindingFlags.Static));
            return field.GetValue(null) is System.Collections.IList list ? list.Count : 0;
        }

        [Fact]
        public Task ShowThenClose_ReleasesHwndSourceHookAndThemeWatcherRegistrationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                int baselineWatched = GetWatchedWindowCount();

                FluenceWindow w = new()
                {
                    Width = 200,
                    Height = 150,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                w.Show();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                w.Close();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                // The HWND itself is owned and destroyed by WPF on close; the library must release
                // its managed references to that HWND's source so nothing is pinned past teardown.
                Assert.Equal(baselineWatched, GetWatchedWindowCount());

                FieldInfo sourceField = Assert.IsAssignableFrom<FieldInfo>(typeof(FluenceWindow).GetField("_hwndSource", BindingFlags.NonPublic | BindingFlags.Instance));
                Assert.Null(sourceField.GetValue(w));
            });
        }

        [Fact]
        public Task SystemThemeWatcher_AutoReleasesWatchedWindow_OnClose_WithoutExplicitUnWatchAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                int baselineWatched = GetWatchedWindowCount();

                Window w = new()
                {
                    Width = 200,
                    Height = 150,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                SystemThemeWatcher.Watch(w);
                Assert.Equal(baselineWatched + 1, GetWatchedWindowCount());

                w.Show();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                // Deliberately do NOT call UnWatch: closing the window must auto-release it.
                w.Close();
                WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                Assert.Equal(baselineWatched, GetWatchedWindowCount());
            });
        }

    }
}
