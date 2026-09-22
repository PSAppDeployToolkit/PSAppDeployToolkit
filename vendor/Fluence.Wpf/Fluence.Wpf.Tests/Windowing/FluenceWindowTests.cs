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
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Windowing
{
    /// <summary>
    /// WI-2 hardening tests for FluenceWindow: backdrop swap, full HC theme cycle,
    /// close-button DynamicResource fix (Finding B).
    /// </summary>
    public sealed class FluenceWindowTests
    {
        private static void ResetAndApply(ApplicationTheme theme, Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();

            ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
        }

        private static void ResetAndApply(Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
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
                    Assert.Equal(WindowBackdropType.Auto, w.SystemBackdropType);
                }
                finally { w.Close(); }
            });
        }

        [Fact]
        public Task SystemBackdropType_CanSetAllValuesAsync()
        {
            // Verifies that the DP accepts all four WindowBackdropType values without throwing.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                FluenceWindow w = new();
                try
                {
                    foreach (WindowBackdropType bd in new[] { WindowBackdropType.None, WindowBackdropType.Mica, WindowBackdropType.Acrylic, WindowBackdropType.Tabbed, WindowBackdropType.Auto })
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
                    ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                    foreach (string? key in keys)
                    {
                        object resource = Assert.IsType<object>(app.TryFindResource(key), exactMatch: false);
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

                ApplicationThemeManager.Apply(ApplicationTheme.HighContrast, WindowBackdropType.None);
                object brush = Assert.IsType<object>(app.TryFindResource("SystemFillColorCriticalBrush"), exactMatch: false);
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

                Assert.Equal(Color.FromArgb(0xFF, 0xC4, 0x2B, 0x1C), BrushAssert.ResolvedColor(app, "WindowCloseButtonBackgroundPointerOverBrush"));
                Assert.Equal(Color.FromArgb(0xFF, 0xB4, 0x27, 0x1C), BrushAssert.ResolvedColor(app, "WindowCloseButtonBackgroundPressedBrush"));
                Assert.Equal(Color.FromArgb(0xFF, 0xFF, 0xFF, 0xFF), BrushAssert.ResolvedColor(app, "WindowCloseButtonForegroundPointerOverBrush"));
            });
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
            FieldInfo field = Assert.IsType<FieldInfo>(declaringType.GetField(eventName, BindingFlags.NonPublic | BindingFlags.Static), exactMatch: false);
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
                    SystemBackdropType = WindowBackdropType.Mica,
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
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    nint handle = new System.Windows.Interop.WindowInteropHelper(w).Handle;
                    System.Windows.Interop.HwndTarget sourceCompositionTarget = Assert.IsType<System.Windows.Interop.HwndTarget>(System.Windows.Interop.HwndSource.FromHwnd(handle)?.CompositionTarget, exactMatch: false);

                    // The fix: the HWND redirection surface (HwndTarget.BackgroundColor) must be
                    // cleared to the same color WPF paints its content background, so no opaque
                    // black surface is exposed before the DWM backdrop composites.
                    Color content = ((SolidColorBrush)w.Background).Color;
                    Assert.Equal(content, sourceCompositionTarget.BackgroundColor);

                    // Swapping to None re-runs ApplyBackdrop; both layers must move together to the
                    // opaque theme fallback so the invariant holds across runtime backdrop changes.
                    w.SystemBackdropType = WindowBackdropType.None;
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

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(2)]
        public Task SystemThemeWatcher_AttachesBeforeDuringAndAfterSourceInitializationAsync(int watchMode)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);
                Window window = watchMode is 2 ? new FluenceWindow() : new Window();
                window.Width = 200;
                window.Height = 150;
                window.ShowInTaskbar = false;
                try
                {
                    if (watchMode is 1)
                    {
                        _ = new System.Windows.Interop.WindowInteropHelper(window).EnsureHandle();
                    }
                    if (watchMode is not 2)
                    {
                        SystemThemeWatcher.Watch(window);
                    }
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    FieldInfo field = Assert.IsType<FieldInfo>(typeof(SystemThemeWatcher).GetField("_watchedWindows", BindingFlags.NonPublic | BindingFlags.Static), exactMatch: false);
                    System.Collections.IEnumerable registrations = Assert.IsType<System.Collections.IEnumerable>(field.GetValue(null), exactMatch: false);
                    object registration = Assert.Single(registrations.Cast<object>(), candidate =>
                        ReferenceEquals(candidate.GetType().GetProperty("Window", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(candidate), window));
                    object? hooked = registration.GetType().GetProperty("IsHooked", BindingFlags.NonPublic | BindingFlags.Instance)?.GetValue(registration);
                    Assert.True(Assert.IsType<bool>(hooked));
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                }
            });
        }

        private static int GetWatchedWindowCount()
        {
            FieldInfo field = Assert.IsType<FieldInfo>(typeof(SystemThemeWatcher).GetField("_watchedWindows", BindingFlags.NonPublic | BindingFlags.Static), exactMatch: false);
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

                FieldInfo sourceField = Assert.IsType<FieldInfo>(typeof(FluenceWindow).GetField("_hwndSource", BindingFlags.NonPublic | BindingFlags.Instance), exactMatch: false);
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

        // ---------------------------------------------------------------------------
        // 8. Defect: disagreeing Light "window base" values. Under WindowBackdropType.None the
        // realised window Background must agree with the published ApplicationBackgroundBrush
        // in every theme (High Contrast pins the live SystemColors.WindowColor override), and
        // the Light token must equal the WinUI ApplicationPageBackgroundThemeBrush source.
        // ---------------------------------------------------------------------------

        [Theory]
        [InlineData(ApplicationTheme.Light)]
        [InlineData(ApplicationTheme.Dark)]
        [InlineData(ApplicationTheme.HighContrast)]
        public Task FluenceWindow_BackdropNone_BackgroundMatchesApplicationBackgroundBrushAsync(ApplicationTheme theme)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(theme, app);

                Color expected = Assert.IsType<SolidColorBrush>(app.TryFindResource("ApplicationBackgroundBrush"), exactMatch: false).Color;

                FluenceWindow w = new()
                {
                    Width = 200,
                    Height = 150,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.None,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Color actual = Assert.IsType<SolidColorBrush>(w.Background, exactMatch: false).Color;
                    Assert.Equal(expected, actual);
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // 9. Content-layer pre-blend (KNOWN_ISSUES.md: "Translucent layers over a DWM backdrop
        // lose alpha precision on a 10 bpc display"). DisplayDepthProbe.Override stands in for a
        // live 10 bpc, advanced-color-off display path, which cannot be forced on the machine
        // actually running the test. SystemBackdropType = Mica is asserted against the real,
        // capability-resolved effective backdrop, so every test below skips (rather than fails)
        // on a host whose WindowCapabilities cannot resolve Mica at all.
        // ---------------------------------------------------------------------------

        private static bool HostCanResolveMica()
        {
            WindowCapabilities capabilities = WindowCapabilities.Current;
            return capabilities.SupportsSystemBackdropType || capabilities.SupportsMicaEffect;
        }

        [Fact]
        public Task ContentLayerPreBlend_Bpc10AdvancedColorOff_Mica_SetsWindowResourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                if (!HostCanResolveMica())
                {
                    Assert.Skip("host cannot resolve Mica");
                }

                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(10, advancedColorEnabled: false);
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    SolidColorBrush brush = Assert.IsType<SolidColorBrush>(w.Resources["NavigationViewContentBackgroundBrush"], exactMatch: false);
                    Assert.Equal(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), brush.Color);
                }
                finally
                {
                    DisplayDepthProbe.Override = null;
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ContentLayerPreBlend_Bpc8_Mica_LeavesWindowResourceAbsentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                if (!HostCanResolveMica())
                {
                    Assert.Skip("host cannot resolve Mica");
                }

                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(8, advancedColorEnabled: false);
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.False(w.Resources.Contains("NavigationViewContentBackgroundBrush"));
                }
                finally
                {
                    DisplayDepthProbe.Override = null;
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ContentLayerPreBlend_OverrideDroppedTo8ThenReapplied_RemovesWindowResourceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                if (!HostCanResolveMica())
                {
                    Assert.Skip("host cannot resolve Mica");
                }

                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(10, advancedColorEnabled: false);
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                    Assert.True(w.Resources.Contains("NavigationViewContentBackgroundBrush"));

                    // Simulate the display path dropping to 8 bpc and the window re-applying its
                    // backdrop (the same path WM_DISPLAYCHANGE and OnDpiChanged drive): toggle away
                    // from Mica and back so the SystemBackdropType change callback re-runs
                    // ApplyBackdrop.
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(8, advancedColorEnabled: false);
                    w.SystemBackdropType = WindowBackdropType.None;
                    w.SystemBackdropType = WindowBackdropType.Mica;
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.False(w.Resources.Contains("NavigationViewContentBackgroundBrush"));
                }
                finally
                {
                    DisplayDepthProbe.Override = null;
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ContentLayerPreBlend_ConsumerOwnedResource_NeverOverwrittenOrRemovedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                if (!HostCanResolveMica())
                {
                    Assert.Skip("host cannot resolve Mica");
                }

                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                SolidColorBrush consumerBrush = new(Colors.HotPink);
                consumerBrush.Freeze();

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                // The consumer sets its own override before the window is ever shown, so
                // ApplyBackdrop's first run must find the key already present and never claim
                // ownership of it.
                w.Resources["NavigationViewContentBackgroundBrush"] = consumerBrush;
                try
                {
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(10, advancedColorEnabled: false);
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.Same(consumerBrush, w.Resources["NavigationViewContentBackgroundBrush"]);

                    // Even a swing to 8 bpc, which would otherwise remove Fluence's own substitute,
                    // must leave a consumer-owned value alone.
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(8, advancedColorEnabled: false);
                    w.SystemBackdropType = WindowBackdropType.None;
                    w.SystemBackdropType = WindowBackdropType.Mica;
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.Same(consumerBrush, w.Resources["NavigationViewContentBackgroundBrush"]);
                }
                finally
                {
                    DisplayDepthProbe.Override = null;
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ContentLayerPreBlend_BoundBorder_ResolvesPreBlendThenCanonicalTokenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                if (!HostCanResolveMica())
                {
                    Assert.Skip("host cannot resolve Mica");
                }

                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                Color slotZeroToken = Assert.IsType<Color>(app.Resources["NavigationViewContentBackground"]);

                System.Windows.Controls.Border border = new();
                border.SetResourceReference(System.Windows.Controls.Border.BackgroundProperty, "NavigationViewContentBackgroundBrush");

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = border,
                };
                try
                {
                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(10, advancedColorEnabled: false);
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Color resolved = Assert.IsType<SolidColorBrush>(border.Background, exactMatch: false).Color;
                    Assert.Equal(Color.FromArgb(0xFF, 0xF9, 0xF9, 0xF9), resolved);

                    DisplayDepthProbe.Override = static _ => new DisplayColorDepth(8, advancedColorEnabled: false);
                    w.SystemBackdropType = WindowBackdropType.None;
                    w.SystemBackdropType = WindowBackdropType.Mica;
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Color afterRemoval = Assert.IsType<SolidColorBrush>(border.Background, exactMatch: false).Color;
                    Assert.Equal(slotZeroToken, afterRemoval);
                }
                finally
                {
                    DisplayDepthProbe.Override = null;
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ApplicationBackgroundColor_Light_EqualsCanonicalSolidBackgroundFillColorBaseAsync()
        {
            // Pins the WinUI relationship this library mirrors: ApplicationBackgroundColor is the
            // WPF analogue of ApplicationPageBackgroundThemeBrush, which WinUI resolves to
            // SolidBackgroundFillColorBase. A drift here silently reintroduces a second
            // disagreeing "window base" value alongside the one FluenceWindow reads.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                Color applicationBackground = Assert.IsType<Color>(app.Resources["ApplicationBackgroundColor"]);
                Color solidBackgroundBase = Assert.IsType<Color>(app.Resources["SolidBackgroundFillColorBase"]);
                Assert.Equal(solidBackgroundBase, applicationBackground);
            });
        }

        // ---------------------------------------------------------------------------
        // 10. DWM owns the outer border on Windows 11 (CHANGELOG.md 0.8.17-Preview
        // pale-line defect): a realised window carries the capability-correct template
        // border thickness and never the Card brush key, and ApplyFrame's SetCurrentValue
        // calls must not clobber a consumer's own style-level BorderBrush setter with a
        // promoted local value.
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ApplyFrame_RealisedWindow_TemplateBorderMatchesCapability_AndBrushIsNeverCardStrokeAsync()
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
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    // Exercises the Windows 10 1 dp path on a host (for example CI on Server 2022)
                    // that does not support DWMWA_BORDER_COLOR, and the Windows 11 0 dp path
                    // everywhere else, instead of skipping one branch outright.
                    Thickness expectedThickness = WindowCapabilities.Current.SupportsBorderColor
                        ? new Thickness(0)
                        : new Thickness(1);
                    Assert.Equal(expectedThickness, w.BorderThickness);

                    object? accentBrush = app.TryFindResource("SystemAccentColorBrush");
                    object? surfaceBrush = app.TryFindResource("SurfaceStrokeColorDefaultBrush");
                    Assert.True(ReferenceEquals(w.BorderBrush, accentBrush) || ReferenceEquals(w.BorderBrush, surfaceBrush),
                        "FluenceWindow.BorderBrush must resolve to the accent brush (active) or the surface stroke brush (inactive) by identity.");

                    object? cardStrokeBrush = app.TryFindResource("CardStrokeColorDefaultSolidBrush");
                    Assert.False(ReferenceEquals(w.BorderBrush, cardStrokeBrush),
                        "FluenceWindow.BorderBrush must never resolve to CardStrokeColorDefaultSolidBrush; that key is reserved for Card surfaces.");
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public Task ApplyFrame_ConsumerStyleSetterOnBorderBrush_BaseValueSourceStaysStyleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.Light, app);

                // BasedOn the implicit style so the template and every other setter survive; only
                // BorderBrush is overridden, the way a consumer app would retheme one property.
                Style baseStyle = Assert.IsType<Style>(app.TryFindResource(typeof(FluenceWindow)), exactMatch: false);
                Style consumerStyle = new(typeof(FluenceWindow), baseStyle);
                consumerStyle.Setters.Add(new Setter(System.Windows.Controls.Control.BorderBrushProperty, Brushes.HotPink));

                FluenceWindow w = new()
                {
                    Style = consumerStyle,
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    // ApplyFrame ran (ApplyWindowShell calls it from OnSourceInitialized) and wrote
                    // through SetCurrentValue. A plain assignment would have promoted BorderBrush to
                    // a Local value, permanently shadowing the consumer's style setter for the
                    // lifetime of the window; SetCurrentValue instead leaves the Style setter as the
                    // reported base value source and only marks the effective value current.
                    ValueSource source = DependencyPropertyHelper.GetValueSource(w, System.Windows.Controls.Control.BorderBrushProperty);
                    Assert.Equal(BaseValueSource.Style, source.BaseValueSource);
                    Assert.True(source.IsCurrent);
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // 11. Defect: HighContrast must suppress every DWM backdrop, not only the Windows 10
        // legacy acrylic path (Microsoft Learn "Materials in Windows apps": "High contrast mode:
        // all materials are suppressed"). A window that requests Mica realises an opaque
        // Background painted from the published ApplicationBackgroundBrush (the HC override of
        // SystemColors.WindowColor) while HighContrast is the resolved theme, and returns to a
        // transparent Background once the theme moves back to Light on a host whose
        // WindowCapabilities can resolve a system backdrop at all.
        // ---------------------------------------------------------------------------

        [Fact]
        public Task HighContrast_SuppressesRequestedMicaBackdrop_ThenLightRestoresTransparentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(ApplicationTheme.HighContrast, app);

                Color expectedHighContrastBackground =
                    Assert.IsType<SolidColorBrush>(app.TryFindResource("ApplicationBackgroundBrush"), exactMatch: false).Color;

                FluenceWindow w = new()
                {
                    Width = 320,
                    Height = 240,
                    ShowInTaskbar = false,
                    SystemBackdropType = WindowBackdropType.Mica,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Color highContrastBackground = Assert.IsType<SolidColorBrush>(w.Background, exactMatch: false).Color;
                    Assert.NotEqual(Colors.Transparent, highContrastBackground);
                    Assert.Equal(expectedHighContrastBackground, highContrastBackground);

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Color afterLightBackground = Assert.IsType<SolidColorBrush>(w.Background, exactMatch: false).Color;
                    if (HostCanResolveMica())
                    {
                        Assert.Equal(Colors.Transparent, afterLightBackground);
                    }
                    else
                    {
                        // A host with no Mica-capable DWM attribute at all keeps the opaque fallback
                        // regardless of theme; the branch still exercises the same re-Apply path.
                        Assert.NotEqual(Colors.Transparent, expectedHighContrastBackground);
                        Assert.NotEqual(Colors.Transparent, afterLightBackground);
                    }
                }
                finally
                {
                    w.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        // ---------------------------------------------------------------------------
        // 12. Regression tests for the SizeToContent double-border fix. A Window with an
        // active Window.SizeToContent sizes its HWND to the latest content-desired size, but
        // the template root Border (the accent-bordered window chrome) was left arranged one
        // layout pass behind the realised client area, so it floated inside the DWM accent
        // border on every edge. The fix re-arranges the root visual to the full client area
        // whenever SizeToContent is active, while keeping SizeToContent's auto-grow behavior.
        // ---------------------------------------------------------------------------

        /// <summary>
        /// Tolerance (in DIPs) between the window's client size and the template root border's
        /// arranged size. Layout rounding can introduce a sub-pixel difference; anything larger is the
        /// multi-DIP inset that produced the double border.
        /// </summary>
        private const double FillTolerance = 1.0;

        private static System.Windows.Controls.Border FindWindowBorder(FluenceWindow window)
        {
            System.Windows.Controls.Border? border = WpfTestSta
                .FindVisualDescendants<System.Windows.Controls.Border>(window)
                .FirstOrDefault(static b => string.Equals(b.Name, "WindowBorder", StringComparison.Ordinal));
            return border ?? throw new InvalidOperationException(
                "Expected the template root Border named 'WindowBorder' to be present after Show().");
        }

        private static System.Windows.Controls.StackPanel BuildContent()
        {
            System.Windows.Controls.StackPanel panel = new() { Margin = new Thickness(24) };
            foreach (string label in new[] { "Full name", "Age", "Country", "Start date" })
            {
                _ = panel.Children.Add(new System.Windows.Controls.TextBlock { Text = label, Margin = new Thickness(0, 0, 0, 4) });
                _ = panel.Children.Add(new System.Windows.Controls.TextBox { Margin = new Thickness(0, 0, 0, 12), MinWidth = 240 });
            }
            return panel;
        }

        /// <summary>
        /// A SizeToContent window must arrange its template root border to fill the realised client
        /// area (the window's ActualWidth/ActualHeight), exactly as a fixed-size window already does.
        /// Before the fix the border was inset several DIPs on every edge, which read as a second
        /// accent border floating inside the DWM accent border (the double-border bug).
        /// </summary>
        [Fact]
        public Task SizeToContentWindow_TemplateBorder_FillsClientAreaAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                FluenceWindow window = new()
                {
                    Title = "SizeToContent fill",
                    SystemBackdropType = WindowBackdropType.None,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = BuildContent(),
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    System.Windows.Controls.Border border = FindWindowBorder(window);

                    Assert.True(window.ActualWidth > 0 && window.ActualHeight > 0,
                        "The window must have a realised non-zero size after Show() with SizeToContent.");

                    // The root border must coincide with the client area (window ActualWidth/Height
                    // equal the client area in DIPs). A larger gap is the inset that floated the
                    // template accent border inside the DWM accent border (the double-border bug).
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        /// <summary>
        /// The fix must not freeze SizeToContent: when the content grows at runtime (the scenario the
        /// PowerShell dialogs rely on when their validation InfoBar opens), the window must still grow
        /// AND the template root border must still fill the new, larger client area (stay
        /// single-bordered after growing).
        /// </summary>
        [Fact]
        public Task SizeToContentWindow_StillGrowsAndStaysFilled_WhenContentGrowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                System.Windows.Controls.StackPanel panel = BuildContent();
                FluenceWindow window = new()
                {
                    Title = "SizeToContent grow",
                    SystemBackdropType = WindowBackdropType.None,
                    ShowInTaskbar = false,
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = panel,
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    double heightBeforeGrow = window.ActualHeight;

                    // Simulate the validation InfoBar opening: add a tall row so the window auto-grows.
                    // Settle via a dispatcher drain (not a synchronous UpdateLayout): UpdateLayout would
                    // itself force the fill, masking whether the fix is what keeps the border flush
                    // after a SizeToContent-driven grow.
                    _ = panel.Children.Add(new System.Windows.Controls.Border { Height = 120, Margin = new Thickness(0, 12, 0, 0) });
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    Assert.True(window.ActualHeight > heightBeforeGrow,
                        "SizeToContent must remain active so the window grows when its content grows.");

                    System.Windows.Controls.Border border = FindWindowBorder(window);
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        /// <summary>
        /// A fixed-size window already renders with the borders coincident; the fill correction must
        /// be a no-op for it (its template root border fills the client area before and after the
        /// fix). This pins that the fix does not regress fixed-size windows.
        /// </summary>
        [Fact]
        public Task FixedSizeWindow_TemplateBorder_FillsClientAreaAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                ResetAndApply(app);

                FluenceWindow window = new()
                {
                    Title = "Fixed size",
                    SystemBackdropType = WindowBackdropType.None,
                    ShowInTaskbar = false,
                    Width = 420,
                    Height = 320,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    Left = -10000,
                    Top = -10000,
                    Content = BuildContent(),
                };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);

                    System.Windows.Controls.Border border = FindWindowBorder(window);
                    Assert.Equal(window.ActualWidth, border.ActualWidth, FillTolerance);
                    Assert.Equal(window.ActualHeight, border.ActualHeight, FillTolerance);
                }
                finally
                {
                    window.Close();
                    WpfTestSta.DrainDispatcher(WpfTestSta.Dispatcher);
                }
            });
        }

        [Fact]
        public void FluenceWindow_MaxButtonRelease_DecodesAnyWParamWithoutThrowing()
        {
            // Any process on the desktop can post WM_NCLBUTTONUP with an arbitrary 64-bit wParam to
            // a FluenceWindow. The decode used IntPtr.ToInt32, which throws OverflowException past
            // 32 bits, and an exception escaping an HwndSource hook tears the process down.
            Assert.True(FluenceWindow.IsMaxButtonRelease(new IntPtr(9)));
            Assert.False(FluenceWindow.IsMaxButtonRelease(new IntPtr(2)));
            Assert.False(FluenceWindow.IsMaxButtonRelease(IntPtr.Zero));

            if (IntPtr.Size is not 8)
            {
                Assert.Skip("A 64-bit wParam needs a 64-bit process.");
            }

            // HTMAXBUTTON in the low dword with a set bit above it: exactly what ToInt32 threw on.
            Assert.False(FluenceWindow.IsMaxButtonRelease(new IntPtr((1L << 32) | 9L)));
            Assert.False(FluenceWindow.IsMaxButtonRelease(new IntPtr(long.MinValue)));
        }
    }
}
