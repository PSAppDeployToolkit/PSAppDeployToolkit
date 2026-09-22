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
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Windows.Win32;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Windowing
{
    public sealed class TitleBarTests
    {
        private static Task RunWithWindowAsync(Action<FluenceWindow> testBody)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow();
                    testBody(window);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        /// <summary>
        /// Shows a FluenceWindow off-screen so template parts (caption buttons) exist for hit-testing.
        /// </summary>
        /// <param name="testBody">The action to run with the shown window.</param>
        private static Task RunWithShownWindowAsync(Action<FluenceWindow> testBody)
        {
            return RunWithShownWindowAsync(window =>
            {
                testBody(window);
                return Task.CompletedTask;
            });
        }

        private static Task RunWithShownWindowAsync(Func<FluenceWindow, Task> testBody)
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow
                    {
                        Width = 520,
                        Height = 360,
                        Left = -20000,
                        Top = -20000,
                        ExtendsContentIntoTitleBar = true,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                    };
                    window.Show();
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    await testBody(window).ConfigureAwait(true);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        private static uint? InvokeHitTestTitleBar(FluenceWindow window, IntPtr lParam)
        {
            MethodInfo method = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod("HitTestTitleBar", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            return (uint?)method.Invoke(window, [lParam]);
        }

        private static IntPtr? InvokeWndProc(FluenceWindow window, uint msg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            MethodInfo method = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod("WndProc", BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);

            object[] args = [IntPtr.Zero, (int)msg, wParam, lParam, false];
            IntPtr? result = (IntPtr?)method.Invoke(window, args);
            handled = (bool)args[4];
            return result;
        }

        private static IntPtr MakeLParamScreen(double screenX, double screenY)
        {
            int x = (int)screenX;
            int y = (int)screenY;
            return new IntPtr((y << 16) | (x & 0xffff));
        }

        private static System.Windows.Controls.Button? GetCaptionButtonField(FluenceWindow window, string fieldName)
        {
            FieldInfo field = Assert.IsType<FieldInfo>(typeof(FluenceWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            return field.GetValue(window) as System.Windows.Controls.Button;
        }

        #region 1. ExtendsContentIntoTitleBar default

        [Fact]
        public Task ExtendsContentIntoTitleBar_DefaultIsFalseAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                Assert.False(w.ExtendsContentIntoTitleBar,
                    "ExtendsContentIntoTitleBar should default to false.");
            });
        }

        #endregion 1. ExtendsContentIntoTitleBar default

        #region 2. FluenceWindow sizing defaults

        [Fact]
        public Task TitleBarHeight_DefaultIs48Async()
        {
            return RunWithWindowAsync(static w => Assert.Equal(48d, w.TitleBarHeight));
        }

        [Fact]
        public Task MinWidth_DefaultRemainsUnsetAsync()
        {
            return RunWithWindowAsync(static w => Assert.Equal(0d, w.MinWidth));
        }

        #endregion 2. FluenceWindow sizing defaults

        #region 3. ShowIcon and ShowTitle defaults

        [Fact]
        public Task ShowIcon_DefaultIsTrueAsync()
        {
            return RunWithWindowAsync(static w => Assert.True(w.ShowIcon, "ShowIcon should default to true."));
        }

        [Fact]
        public Task ShowTitle_DefaultIsTrueAsync()
        {
            return RunWithWindowAsync(static w => Assert.True(w.ShowTitle, "ShowTitle should default to true."));
        }

        #endregion 3. ShowIcon and ShowTitle defaults

        #region 4. Caption button visibility defaults

        [Fact]
        public Task CaptionButtonVisibility_DefaultsAreVisibleAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                Assert.Equal(Visibility.Visible, w.IsMinimizeButtonVisible);
                Assert.Equal(Visibility.Visible, w.IsMaximizeButtonVisible);
                Assert.Equal(Visibility.Visible, w.IsCloseButtonVisible);
            });
        }

        [Fact]
        public Task CaptionButtonEnabled_DefaultsAreTrueAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                Assert.True(w.IsMinimizable);
                Assert.True(w.IsMaximizable);
                Assert.True(w.IsClosable);
            });
        }

        #endregion 4. Caption button visibility defaults

        #region 5. HasShadow and WindowBorder defaults

        [Fact]
        public Task HasShadow_DefaultIsTrueAsync()
        {
            return RunWithWindowAsync(static w => Assert.True(w.HasShadow, "HasShadow should default to true."));
        }

        [Fact]
        public Task BorderThickness_DefaultIsOneAsync()
        {
            // The implicit FluenceWindow style in Themes/Controls/FluenceWindow.xaml sets 1 (the
            // Windows 10 value, and the value shown before ApplyFrame's first pass, which never runs
            // here because the window is never shown).
            return RunWithWindowAsync(static w => Assert.Equal(new Thickness(1), w.BorderThickness));
        }

        #endregion 5. HasShadow and WindowBorder defaults

        #region 6. SetTitleBar method

        [Fact]
        public Task SetTitleBar_SetsTitleBarPropertyAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                System.Windows.Controls.TextBlock customElement = new() { Text = "Custom Title" };
                w.SetTitleBar(customElement);
                Assert.Same(customElement, w.TitleBar);
            });
        }

        [Fact]
        public Task SetTitleBar_NullRevertsAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                System.Windows.Controls.TextBlock customElement = new() { Text = "Custom Title" };
                w.SetTitleBar(customElement);
                Assert.NotNull(w.TitleBar);
                w.SetTitleBar(titleBar: null);
                Assert.Null(w.TitleBar);
            });
        }

        #endregion 6. SetTitleBar method

        #region 7. WindowChrome updates

        [Fact]
        public Task CaptionHeight_AlwaysZero_RegardlessOfExtendsContentIntoTitleBarAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                WindowChrome chrome = Assert.IsType<WindowChrome>(WindowChrome.GetWindowChrome(w), exactMatch: false);
                Assert.Equal(0d, chrome.CaptionHeight);

                w.ExtendsContentIntoTitleBar = true;

                Assert.Equal(0d, chrome.CaptionHeight);
            });
        }

        [Fact]
        public Task HasShadow_False_SetsGlassFrameToNearZeroAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                WindowChrome chrome = WindowChrome.GetWindowChrome(w);
                Assert.Equal(new Thickness(-1), chrome.GlassFrameThickness);

                w.HasShadow = false;
                w.SystemBackdropType = WindowBackdropType.None;

                // The dual-path GlassFrameThickness uses 0.00001 (not 0) when both backdrop
                // is None AND HasShadow is false, so the WindowChrome resize border still
                // hit-tests but no visible glass-frame artifact is painted on Windows 11.
                // See WindowPolicy.GetGlassFrameThickness for the rationale.
                Assert.Equal(new Thickness(0.00001), chrome.GlassFrameThickness);
            });
        }

        #endregion 7. WindowChrome updates

        #region Bug Fix Tests - Title Bar Flash and Theme Switching

        [Fact]
        public Task CaptionHeight_IsZero_EvenBeforeExtendsContentIntoTitleBarAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                WindowChrome chrome = Assert.IsType<WindowChrome>(WindowChrome.GetWindowChrome(w), exactMatch: false);
                Assert.Equal(0d, chrome.CaptionHeight);
            });
        }

        [Fact]
        public Task WindowChrome_AppliedInConstructorAsync()
        {
            return RunWithWindowAsync(static w => _ = Assert.IsType<WindowChrome>(WindowChrome.GetWindowChrome(w), exactMatch: false));
        }

        [Fact]
        public Task DefaultBorderThickness_IsOneAsync()
        {
            // The implicit FluenceWindow style in Themes/Controls/FluenceWindow.xaml sets 1 (the
            // Windows 10 value, and the value shown before ApplyFrame's first pass, which never runs
            // here because the window is never shown).
            return RunWithWindowAsync(static w => Assert.Equal(new Thickness(1), w.BorderThickness));
        }

        [Fact]
        public Task ThemeSwitch_UpdatesWindowBackgroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow();
                    Brush lightBg = window.Background;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    Brush darkBg = window.Background;

                    Assert.NotEqual(lightBg, darkBg);
                }
                finally
                {
                    window?.Close();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                }
            });
        }

        [Fact]
        public Task ThemeChanged_FiresOnApplyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                int fireCount = 0;
                void handler(object? s, ThemeChangedEventArgs e)
                {
                    fireCount++;
                }

                try
                {
                    ApplicationThemeManager.Changed += handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, WindowBackdropType.None);
                    Assert.Equal(1, fireCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                }
            });
        }

        [Fact]
        public void FluenceWindowXaml_NoStaticResourceForThemeBrushes()
        {
            string xamlPath = System.IO.Path.Join(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\FluenceWindow.xaml");

            if (System.IO.File.Exists(xamlPath))
            {
                string xaml = System.IO.File.ReadAllText(xamlPath);
                string[] themeBrushKeys =
                [
                    "ApplicationBackgroundBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorSecondaryBrush",
                    "TextFillColorDisabledBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "SurfaceStrokeColorDefaultBrush",
                ];

                foreach (string key in themeBrushKeys)
                {
                    string staticPattern = "StaticResource " + key;
                    Assert.False(xaml.Contains(staticPattern, StringComparison.Ordinal),
                        "FluenceWindow.xaml must not use StaticResource for theme brush: " + key);
                }
            }
        }

        [Fact]
        public Task FullThemeCycle_KeyBrushesResolveAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = TestApp.EnsureLibraryTheme();

                try
                {
                    foreach (ApplicationTheme theme in (ApplicationTheme[])[ApplicationTheme.Dark, ApplicationTheme.Light])
                    {
                        ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                        object bg = Assert.IsType<object>(app.TryFindResource("ApplicationBackgroundBrush"), exactMatch: false);
                        object fg = Assert.IsType<object>(app.TryFindResource("TextFillColorPrimaryBrush"), exactMatch: false);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
                }
            });
        }

        #endregion Bug Fix Tests - Title Bar Flash and Theme Switching

        #region Caption button hit-test (WM_NCHITTEST vs WPF commands)

        [Fact]
        public void FluenceWindowXaml_CaptionButtonsUseSystemCommands()
        {
            string xamlPath = System.IO.Path.Join(
                AppDomain.CurrentDomain.BaseDirectory,
                @"..\..\..\..\Fluence.Wpf\Themes\Controls\FluenceWindow.xaml");
            xamlPath = System.IO.Path.GetFullPath(xamlPath);

            Assert.True(System.IO.File.Exists(xamlPath),
                "FluenceWindow.xaml should be readable at: " + xamlPath);

            string xaml = System.IO.File.ReadAllText(xamlPath);
            Assert.True(
                xaml.Contains("MinimizeWindowCommand", StringComparison.Ordinal),
                "Minimize button should bind MinimizeWindowCommand.");
            Assert.True(
                xaml.Contains("MaximizeWindowCommand", StringComparison.Ordinal),
                "Maximize button should bind MaximizeWindowCommand.");
            Assert.True(
                xaml.Contains("CloseWindowCommand", StringComparison.Ordinal),
                "Close button should bind CloseWindowCommand.");
        }

        [Fact]
        public Task HitTestTitleBar_MinimizeButton_ReturnsZero_NotHtMinButtonAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_minimizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Visible, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(0u, hit);
                Assert.NotEqual(PInvoke.HTMINBUTTON, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_CloseButton_ReturnsZero_NotHtCloseAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_closeButton"), exactMatch: false);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(0u, hit);
                Assert.NotEqual(PInvoke.HTCLOSE, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_MaximizeButton_ReturnsHtMaxButtonAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);
                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Visible, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(PInvoke.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_MaximizeButtonHidden_DoesNotReturnHtMaxButtonAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Hidden;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Hidden, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.NotEqual(PInvoke.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_MaximizeButtonDisabled_DoesNotReturnHtMaxButtonAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.IsMaximizable = false;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Visible, btn.Visibility);
                Assert.False(btn.IsEnabled);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.NotEqual(PInvoke.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_TitleBarDragArea_ReturnsHtCaptionAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                w.UpdateLayout();
                Point clientMidTitle = new(Math.Max(40, w.ActualWidth / 2), Math.Max(1, w.TitleBarHeight / 2));
                Point screen = w.PointToScreen(clientMidTitle);
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(PInvoke.HTCAPTION, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_TopResizeBand_ReturnsHtTopBeforeCaptionAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                w.UpdateLayout();
                Point screen = w.PointToScreen(new Point(w.ActualWidth / 2.0, 1.0));
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(PInvoke.HTTOP, hit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_UpperCorners_ReturnResizeCornersBeforeCaptionAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                w.UpdateLayout();

                Point topLeft = w.PointToScreen(new Point(1.0, 1.0));
                uint? leftHit = InvokeHitTestTitleBar(w, MakeLParamScreen(topLeft.X, topLeft.Y));
                Assert.Equal(PInvoke.HTTOPLEFT, leftHit);

                Point topRight = w.PointToScreen(new Point(w.ActualWidth - 1.0, 1.0));
                uint? rightHit = InvokeHitTestTitleBar(w, MakeLParamScreen(topRight.X, topRight.Y));
                Assert.Equal(PInvoke.HTTOPRIGHT, rightHit);
            });
        }

        [Fact]
        public Task HitTestTitleBar_IsMoveableFalse_TitleBarDragAreaReturnsZeroAsync()
        {
            return RunWithShownWindowAsync(static w =>
            {
                w.IsMoveable = false;
                w.UpdateLayout();

                Point clientMidTitle = new(Math.Max(40, w.ActualWidth / 2), Math.Max(1, w.TitleBarHeight / 2));
                Point screen = w.PointToScreen(clientMidTitle);
                uint? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(0u, hit);
            });
        }

        [Fact]
        public Task WndProc_IsMoveableFalse_SuppressesSystemMoveAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                w.IsMoveable = false;
                _ = InvokeWndProc(w, PInvoke.WM_SYSCOMMAND, new IntPtr(PInvoke.SC_MOVE), IntPtr.Zero, out bool handled);
                Assert.True(handled, "IsMoveable=false must handle SC_MOVE.");

                w.IsMoveable = true;
                _ = InvokeWndProc(w, PInvoke.WM_SYSCOMMAND, new IntPtr(PInvoke.SC_MOVE), IntPtr.Zero, out handled);
                Assert.False(handled, "IsMoveable=true must leave SC_MOVE available.");
            });
        }

        [Fact]
        public Task WndProc_NcLeftButtonUpHtMaxButton_UsesDirectMaximizeAndRefreshesCaptionButtonsAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                System.Windows.Controls.Button max = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                System.Windows.Controls.Button restore = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_restoreButton"), exactMatch: false);
                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(Visibility.Collapsed, restore.Visibility);

                _ = InvokeWndProc(
                    w,
                    PInvoke.WM_NCLBUTTONUP,
                    new IntPtr(PInvoke.HTMAXBUTTON),
                    IntPtr.Zero,
                    out bool handled);
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.True(handled,
                    "WM_NCLBUTTONUP/HTMAXBUTTON should be handled by FluenceWindow.");
                Assert.Equal(WindowState.Maximized, w.WindowState);
                Assert.Equal(Visibility.Collapsed, max.Visibility);
                Assert.Equal(Visibility.Visible, restore.Visibility);

                _ = InvokeWndProc(
                    w,
                    PInvoke.WM_NCLBUTTONUP,
                    new IntPtr(PInvoke.HTMAXBUTTON),
                    IntPtr.Zero,
                    out handled);
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.True(handled,
                    "Second WM_NCLBUTTONUP/HTMAXBUTTON should be handled by FluenceWindow.");
                Assert.Equal(WindowState.Normal, w.WindowState);
                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(Visibility.Collapsed, restore.Visibility);
            });
        }

        [Fact]
        public Task SetSnapHover_UsesSubtleFillTokens_MatchingTemplatePointerOverAsync()
        {
            // The Windows 11 snap-layout flyout hover over the maximize/restore button is driven by
            // SetSnapHover, because the WM_NCHITTEST/HTMAXBUTTON path bypasses the XAML IsMouseOver
            // trigger. WindowButtonStyle's PointerOver state was migrated to the WinUI subtle fills
            // (SubtleFillColorSecondaryBrush background / TextFillColorPrimaryBrush glyph), so the
            // synthetic snap hover must reference the same tokens or it shows a stale strong-inverted
            // fill while normal mouse hover shows the subtle fill. This pins the keys so the two
            // paths cannot silently drift apart again. SetSnapHover is invoked directly (rather than
            // through WndProc) so the assertion does not depend on the machine's snap-layout setting,
            // OS build, or IsMaximizable gate that WM_NCHITTEST applies before reaching it.
            return RunWithShownWindowAsync(async static w =>
            {
                System.Windows.Controls.Button max = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                Assert.True(max.IsEnabled,
                    "Precondition: maximize button must be enabled for SetSnapHover to apply a hover visual.");

                // The resolved brushes the template PointerOver state would show, looked up the same
                // way SetSnapHover's resource references resolve them.
                object? expectedBackground = w.TryFindResource("SubtleFillColorSecondaryBrush");
                object? expectedForeground = w.TryFindResource("TextFillColorPrimaryBrush");
                object? staleBackground = w.TryFindResource("ControlStrongFillColorDefaultBrush");
                _ = Assert.IsType<Brush>(expectedBackground, exactMatch: false);
                _ = Assert.IsType<Brush>(expectedForeground, exactMatch: false);

                MethodInfo setSnapHover = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod(
                    "SetSnapHover",
                    BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
                _ = setSnapHover.Invoke(w, [max]);
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Same(expectedBackground, max.Background);
                Assert.Same(expectedForeground, max.Foreground);
                Assert.NotSame(staleBackground, max.Background);

                // ClearSnapHover must restore the template/style defaults via ClearValue, so the
                // local Background/Foreground values are cleared back to the unset (style-driven) state.
                MethodInfo clearSnapHover = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod(
                    "ClearSnapHover",
                    BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
                _ = clearSnapHover.Invoke(w, parameters: null);
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(DependencyProperty.UnsetValue, max.ReadLocalValue(System.Windows.Controls.Control.BackgroundProperty));
                Assert.Equal(DependencyProperty.UnsetValue, max.ReadLocalValue(System.Windows.Controls.Control.ForegroundProperty));
            });
        }

        #endregion Caption button hit-test (WM_NCHITTEST vs WPF commands)

        #region Caption button DP overrides (authoritative when explicitly set)

        [Fact]
        public Task IsMinimizeButtonVisible_ExplicitVisible_UnderNoResize_ShowsAndEnablesButtonAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                // XAML sets IsMinimizeButtonVisible=Collapsed
                // on the FluentDialog template, then code-behind flips it back to Visible when
                // DialogAllowMinimize is honoured (IsMinimizeButtonVisible=Visibility.Visible).
                w.IsMinimizeButtonVisible = Visibility.Collapsed;
                w.ResizeMode = ResizeMode.NoResize;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_minimizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, btn.Visibility);

                w.IsMinimizeButtonVisible = Visibility.Visible;
                w.IsMinimizable = true;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(Visibility.Visible, btn.Visibility);
                Assert.True(btn.IsEnabled,
                    "Explicit Visible under NoResize must also enable the minimize button.");
            });
        }

        [Fact]
        public Task IsMinimizeButtonVisible_ExplicitCollapsed_UnderCanResize_HidesButtonAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.ResizeMode = ResizeMode.CanResize;
                w.IsMinimizeButtonVisible = Visibility.Collapsed;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_minimizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, btn?.Visibility);
                Assert.False(btn?.IsEnabled ?? false,
                    "Explicit Collapsed must also disable the button.");
            });
        }

        [Fact]
        public Task IsMaximizeButtonVisible_ExplicitVisible_UnderNoResize_ShowsAndEnablesMaximizeAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Collapsed;
                w.ResizeMode = ResizeMode.NoResize;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button max = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                Assert.Equal(Visibility.Collapsed, max.Visibility);

                w.IsMaximizeButtonVisible = Visibility.Visible;
                w.IsMaximizable = true;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(WindowState.Normal, w.WindowState);
                Assert.True(max.IsEnabled,
                    "Maximize button must be enabled when the window is not already maximized and the DP is explicit.");
            });
        }

        [Fact]
        public Task IsMaximizeButtonVisible_Hidden_ReservesOnlyTheActiveButtonSlotAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Hidden;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                System.Windows.Controls.Button? max = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? restore = GetCaptionButtonField(w, "_restoreButton");
                Assert.Equal(Visibility.Hidden, max?.Visibility);
                Assert.Equal(Visibility.Collapsed, restore?.Visibility);
                Assert.False(max?.IsEnabled ?? false);
                Assert.False(restore?.IsEnabled ?? false);

                w.WindowState = WindowState.Maximized;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(Visibility.Collapsed, max?.Visibility);
                Assert.Equal(Visibility.Hidden, restore?.Visibility);
                Assert.False(max?.IsEnabled ?? false);
                Assert.False(restore?.IsEnabled ?? false);
            });
        }

        [Fact]
        public Task CaptionButtonVisibleProperties_SetTheVisibilityDpsAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                foreach (Visibility value in (Visibility[])[Visibility.Visible, Visibility.Hidden, Visibility.Collapsed])
                {
                    w.IsMinimizeButtonVisible = value;
                    w.IsMaximizeButtonVisible = value;
                    w.IsCloseButtonVisible = value;

                    Assert.Equal(value, w.IsMinimizeButtonVisible);
                    Assert.Equal(value, w.IsMaximizeButtonVisible);
                    Assert.Equal(value, w.IsCloseButtonVisible);
                }
            });
        }

        [Fact]
        public Task CaptionButtonVisibilityProperties_ApplyVisibleHiddenCollapsedToTemplateButtonsAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                System.Windows.Controls.Button minimize = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_minimizeButton"), exactMatch: false);
                System.Windows.Controls.Button maximize = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_maximizeButton"), exactMatch: false);
                System.Windows.Controls.Button restore = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_restoreButton"), exactMatch: false);
                System.Windows.Controls.Button close = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(w, "_closeButton"), exactMatch: false);

                foreach (Visibility value in (Visibility[])[Visibility.Visible, Visibility.Hidden, Visibility.Collapsed])
                {
                    w.IsMinimizeButtonVisible = value;
                    w.IsMaximizeButtonVisible = value;
                    w.IsCloseButtonVisible = value;
                    await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    Assert.Equal(value, minimize.Visibility);
                    Assert.Equal(value, close.Visibility);
                    Assert.Equal(value, maximize.Visibility);
                    Assert.Equal(Visibility.Collapsed, restore.Visibility);

                    bool enabled = value is Visibility.Visible;
                    Assert.Equal(enabled, minimize.IsEnabled);
                    Assert.Equal(enabled, maximize.IsEnabled);
                    Assert.Equal(enabled, close.IsEnabled);
                    Assert.False(restore.IsEnabled);
                }
            });
        }

        private static CanExecuteRoutedEventArgs CreateCanExecuteArgs(ICommand command)
        {
            ConstructorInfo ctor = Assert.IsType<ConstructorInfo>(typeof(CanExecuteRoutedEventArgs).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(ICommand), typeof(object)],
                modifiers: null), exactMatch: false);
            return (CanExecuteRoutedEventArgs)ctor.Invoke([command, null]);
        }

        private static bool InvokeCanHandler(FluenceWindow window, string handlerName, CanExecuteRoutedEventArgs args)
        {
            MethodInfo handler = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod(
                handlerName,
                BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            _ = handler.Invoke(window, [window, args]);
            return args.CanExecute;
        }

        private static ExecutedRoutedEventArgs CreateExecutedArgs(ICommand command)
        {
            ConstructorInfo ctor = Assert.IsType<ConstructorInfo>(typeof(ExecutedRoutedEventArgs).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
                binder: null,
                [typeof(ICommand), typeof(object)],
                modifiers: null), exactMatch: false);
            return (ExecutedRoutedEventArgs)ctor.Invoke([command, null]);
        }

        private static void InvokeExecutedHandler(FluenceWindow window, string handlerName, ExecutedRoutedEventArgs args)
        {
            MethodInfo handler = Assert.IsType<MethodInfo>(typeof(FluenceWindow).GetMethod(
                handlerName,
                BindingFlags.Instance | BindingFlags.NonPublic), exactMatch: false);
            _ = handler.Invoke(window, [window, args]);
        }

        [Fact]
        public Task CanMinimizeWindow_RespectsExplicitDp_UnderNoResizeAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                w.ResizeMode = ResizeMode.NoResize;

                Assert.False(InvokeCanHandler(w, "OnCanMinimizeWindow", CreateCanExecuteArgs(SystemCommands.MinimizeWindowCommand)),
                    "Default: ResizeMode=NoResize must block MinimizeWindowCommand when the DP is at its declared default.");

                w.IsMinimizeButtonVisible = Visibility.Visible;
                w.IsMinimizable = true;
                Assert.True(InvokeCanHandler(w, "OnCanMinimizeWindow", CreateCanExecuteArgs(SystemCommands.MinimizeWindowCommand)),
                    "Explicit IsMinimizeButtonVisible=Visible + IsMinimizable=true must allow MinimizeWindowCommand to execute even under NoResize.");

                w.IsMinimizable = false;
                Assert.False(InvokeCanHandler(w, "OnCanMinimizeWindow", CreateCanExecuteArgs(SystemCommands.MinimizeWindowCommand)),
                    "IsMinimizable=false must gate the command regardless of DP visibility override.");
            });
        }

        [Fact]
        public Task CanMaximizeWindow_RespectsExplicitDp_UnderNoResizeAsync()
        {
            return RunWithWindowAsync(static w =>
            {
                w.ResizeMode = ResizeMode.NoResize;

                Assert.False(InvokeCanHandler(w, "OnCanResizeWindow", CreateCanExecuteArgs(SystemCommands.MaximizeWindowCommand)),
                    "Default: ResizeMode=NoResize must block MaximizeWindowCommand when the DP is at its declared default.");

                w.IsMaximizeButtonVisible = Visibility.Visible;
                w.IsMaximizable = true;
                Assert.True(InvokeCanHandler(w, "OnCanResizeWindow", CreateCanExecuteArgs(SystemCommands.MaximizeWindowCommand)),
                    "Explicit IsMaximizeButtonVisible=Visible + IsMaximizable=true must allow MaximizeWindowCommand even under NoResize.");
            });
        }

        [Fact]
        public Task CaptionButtons_DefaultBehaviorUnchanged_WhenDpsNotTouchedAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                Assert.Equal(ResizeMode.CanResize, w.ResizeMode);

                System.Windows.Controls.Button? minBtn = GetCaptionButtonField(w, "_minimizeButton");
                System.Windows.Controls.Button? maxBtn = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? closeBtn = GetCaptionButtonField(w, "_closeButton");

                Assert.Equal(Visibility.Visible, minBtn?.Visibility);
                Assert.Equal(Visibility.Visible, maxBtn?.Visibility);
                Assert.Equal(Visibility.Visible, closeBtn?.Visibility);

                w.ResizeMode = ResizeMode.NoResize;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(Visibility.Collapsed, minBtn?.Visibility);
                Assert.Equal(Visibility.Collapsed, maxBtn?.Visibility);
            });
        }

        [Fact]
        public Task OnMinimizeWindow_DrivesWindowStateMinimized_EvenAfterSysMenuStrippedAsync()
        {
            // RunWithShownWindow triggers OnSourceInitialized → ApplyWindowShell →
            // HideNativeCaptionButtons → NativeMethods.HideAllWindowButtons, which strips
            // WS_SYSMENU on the native HWND. Without WS_SYSMENU (and the implicitly-disabled
            // WS_MINIMIZEBOX) DefWindowProc silently drops WM_SYSCOMMAND/SC_MINIMIZE, so
            // SystemCommands.MinimizeWindow(this) would be a no-op - exactly the production
            // symptom that made the AllowMinimize caption button look clickable but
            // refuse to actually minimize. The Executed handler must bypass the sysmenu gate
            // by assigning WindowState directly so the transition always lands.
            return RunWithShownWindowAsync(async static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnMinimizeWindow",
                    CreateExecutedArgs(SystemCommands.MinimizeWindowCommand));

                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(WindowState.Minimized, w.WindowState);
            });
        }

        [Fact]
        public Task OnMaximizeWindow_DrivesWindowStateMaximized_EvenAfterSysMenuStrippedAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnMaximizeWindow",
                    CreateExecutedArgs(SystemCommands.MaximizeWindowCommand));

                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(WindowState.Maximized, w.WindowState);
            });
        }

        [Fact]
        public Task OnRestoreWindow_DrivesWindowStateNormal_FromMaximizedAsync()
        {
            return RunWithShownWindowAsync(async static w =>
            {
                w.WindowState = WindowState.Maximized;
                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                Assert.Equal(WindowState.Maximized, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnRestoreWindow",
                    CreateExecutedArgs(SystemCommands.RestoreWindowCommand));

                await w.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                Assert.Equal(WindowState.Normal, w.WindowState);
            });
        }

        [Fact]
        public Task MinimizeButton_EndToEnd_ClicksActuallyMinimizeUnderPsadtConfigAsync()
        {
            // Reproduces the exact PSADT FluentDialog topology: Topmost=True + ResizeMode=NoResize
            // + ExtendsContentIntoTitleBar=True + IsMinimizeButtonVisible flipped from
            // Collapsed (XAML baseline) to Visible (IsMinimizeButtonVisible=Visibility.Visible). The
            // test drives the Button via its ICommand to mirror the real click path (WPF
            // Button → SystemCommands.MinimizeWindowCommand → FluenceWindow CommandBinding →
            // OnMinimizeWindow) and asserts the caption is clickable AND the state lands on
            // Minimized. If this ever regresses to "visible but inert" we'll catch it here
            // instead of only in manual QA.
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow
                    {
                        Width = 520,
                        Height = 360,
                        Left = -20000,
                        Top = -20000,
                        ExtendsContentIntoTitleBar = true,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = true,
                        Topmost = true,
                        ResizeMode = ResizeMode.NoResize,
                        IsMinimizeButtonVisible = Visibility.Collapsed,
                        IsMaximizeButtonVisible = Visibility.Collapsed,
                        IsCloseButtonVisible = Visibility.Collapsed,
                    };

                    window.Show();
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    // Flip visibility after Show() to mirror PSADT's IsMinimizeButtonVisible=Visibility.Visible.
                    window.IsMinimizeButtonVisible = Visibility.Visible;
                    window.IsMinimizable = true;
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    CommandManager.InvalidateRequerySuggested();
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    System.Windows.Controls.Button minBtn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(window, "_minimizeButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, minBtn.Visibility);
                    Assert.True(minBtn.IsEnabled,
                        "PSADT flow: Button.IsEnabled must be true so clicks dispatch the command.");

                    Assert.True(
                        SystemCommands.MinimizeWindowCommand.CanExecute(parameter: null, minBtn),
                        "PSADT flow: MinimizeWindowCommand.CanExecute must be true once DPs are flipped and IsMinimizable=true.");

                    Assert.Equal(WindowState.Normal, window.WindowState);

                    // Drive the same code path a real click would drive: the button's Command
                    // on its own DataContext (the button is the command target, the window is
                    // the CommandBinding host via routed-command bubbling).
                    SystemCommands.MinimizeWindowCommand.Execute(parameter: null, minBtn);
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    Assert.Equal(WindowState.Minimized, window.WindowState);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task MinimizeButton_EndToEnd_WorksUnderShowDialogModalPsadtConfigAsync()
        {
            // Same topology as the Show() variant above, but uses ShowDialog() which is what
            // PSADT's DialogManager actually invokes (see DialogManager.ShowModalDialog -> dialog.ShowDialog()).
            // Modal WPF windows push a nested Dispatcher frame, disable their owner, and in the
            // PSADT case are also Topmost - a combination that can mask bugs a Show() test misses.
            // We schedule the click via Dispatcher.BeginInvoke(ApplicationIdle) from Loaded so
            // the command fires after the modal frame is pumping, then verify WindowState.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;
                WindowState observedStateAfterMinimize = WindowState.Normal;
                bool minimizeCommandCanExecute = false;
                bool minimizeButtonIsEnabled = false;
                Visibility minimizeButtonVisibility = Visibility.Collapsed;
                ExceptionDispatchInfo? scenarioExceptionInfo = null;

                try
                {
                    window = new FluenceWindow
                    {
                        Width = 520,
                        Height = 360,
                        Left = -20000,
                        Top = -20000,
                        ExtendsContentIntoTitleBar = true,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = true,
                        Topmost = true,
                        ResizeMode = ResizeMode.NoResize,
                        IsMinimizeButtonVisible = Visibility.Collapsed,
                        IsMaximizeButtonVisible = Visibility.Collapsed,
                        IsCloseButtonVisible = Visibility.Collapsed,
                    };

                    FluenceWindow capturedWindow = window;
                    capturedWindow.Loaded += (loadedSender, loadedArgs) =>
                    {
                        _ = capturedWindow.Dispatcher.BeginInvoke(() =>
                        {
                            try
                            {
                                capturedWindow.IsMinimizeButtonVisible = Visibility.Visible;
                                capturedWindow.IsMinimizable = true;
                                CommandManager.InvalidateRequerySuggested();

                                _ = capturedWindow.Dispatcher.BeginInvoke(() =>
                                {
                                    try
                                    {
                                        System.Windows.Controls.Button minBtn = GetCaptionButtonField(capturedWindow, "_minimizeButton") ?? throw new InvalidOperationException("Minimize template part was not materialised inside ShowDialog modal frame.");
                                        minimizeButtonVisibility = minBtn.Visibility;
                                        minimizeButtonIsEnabled = minBtn.IsEnabled;
                                        minimizeCommandCanExecute = SystemCommands.MinimizeWindowCommand.CanExecute(parameter: null, minBtn);

                                        SystemCommands.MinimizeWindowCommand.Execute(parameter: null, minBtn);

                                        _ = capturedWindow.Dispatcher.BeginInvoke(() =>
                                        {
                                            observedStateAfterMinimize = capturedWindow.WindowState;
                                            capturedWindow.Close();
                                        }, DispatcherPriority.ApplicationIdle);
                                    }
                                    catch (Exception exInner) when (exInner.Message is not null)
                                    {
                                        scenarioExceptionInfo = ExceptionDispatchInfo.Capture(exInner);
                                        capturedWindow.Close();
                                    }
                                }, DispatcherPriority.ApplicationIdle);
                            }
                            catch (Exception exOuter) when (exOuter.Message is not null)
                            {
                                scenarioExceptionInfo = ExceptionDispatchInfo.Capture(exOuter);
                                capturedWindow.Close();
                            }
                        }, DispatcherPriority.ApplicationIdle);
                    };

                    _ = window.ShowDialog();

                    scenarioExceptionInfo?.Throw();

                    Assert.Equal(Visibility.Visible, minimizeButtonVisibility);
                    Assert.True(minimizeButtonIsEnabled,
                        "PSADT ShowDialog flow: Button.IsEnabled must be true inside the modal dispatcher frame.");
                    Assert.True(minimizeCommandCanExecute,
                        "PSADT ShowDialog flow: MinimizeWindowCommand.CanExecute must be true inside the modal dispatcher frame.");
                    Assert.Equal(WindowState.Minimized, observedStateAfterMinimize);
                }
                finally
                {
                    if ((window?.IsVisible) is true)
                    {
                        window.Close();
                    }
                }
            });
        }

        #endregion Caption button DP overrides (authoritative when explicitly set)

        #region WI-1 F4 - Caption buttons must remain hit-testable when ExtendsContentIntoTitleBar=true

        // When ExtendsContentIntoTitleBar=true, the content area moves into Grid.Row=0 (same row
        // as the title bar). Because WPF paints siblings in document order, whichever sibling is
        // declared last wins the top of the z-stack. The title bar grid (and its caption button
        // panel) must therefore win - otherwise opaque client content covers min/max/close and
        // clicks are swallowed by the content, not the button.
        //
        // This test plants an opaque full-size Border as the window Content. If the title bar
        // grid is correctly on top, a hit-test at a caption-button center hits the button or one
        // of its Path children - not the Border.
        [Fact]
        public Task CaptionButtons_AboveContent_WhenExtendsContentIntoTitleBarAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                _ = TestApp.EnsureLibraryTheme();
                FluenceWindow? window = null;

                try
                {
                    System.Windows.Controls.Border occluder = new()
                    {
                        Background = Brushes.Magenta,
                        Name = "OccluderBorder",
                    };

                    window = new FluenceWindow
                    {
                        Width = 640,
                        Height = 420,
                        Left = -20000,
                        Top = -20000,
                        ExtendsContentIntoTitleBar = true,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = occluder,
                    };

                    window.Show();
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
                    window.UpdateLayout();
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    foreach (string? fieldName in (IReadOnlyList<string>)["_minimizeButton", "_maximizeButton", "_closeButton"])
                    {
                        System.Windows.Controls.Button btn = Assert.IsType<System.Windows.Controls.Button>(GetCaptionButtonField(window, fieldName), exactMatch: false);
                        Assert.True(btn.IsVisible, fieldName + " must be visible.");

                        Point center = new(btn.ActualWidth / 2, btn.ActualHeight / 2);
                        Point clientPoint = btn.TranslatePoint(center, window);

                        Visual? hitVisual = null;
                        VisualTreeHelper.HitTest(
                            window,
                            filterCallback: null,
                            new HitTestResultCallback(r =>
                            {
                                hitVisual = r.VisualHit as Visual;
                                return HitTestResultBehavior.Stop;
                            }),
                            new PointHitTestParameters(clientPoint));

                        Assert.NotNull(hitVisual);
                        DependencyObject? hitHost = FindLogicalHost(hitVisual);
                        Assert.NotSame(occluder, hitHost);
                        Assert.True(
                            IsDescendantOfButton(hitVisual, btn),
                            fieldName + " center must hit a descendant of the caption button, not the content underneath.");
                    }
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        private static DependencyObject? FindLogicalHost(DependencyObject node)
        {
            while (node is not null)
            {
                if (node is System.Windows.Controls.Border or System.Windows.Controls.Button)
                {
                    return node;
                }

                node = VisualTreeHelper.GetParent(node);
            }

            return null;
        }

        private static bool IsDescendantOfButton(DependencyObject node, System.Windows.Controls.Button target)
        {
            while (node is not null)
            {
                if (ReferenceEquals(node, target))
                {
                    return true;
                }

                node = VisualTreeHelper.GetParent(node);
            }

            return false;
        }

        #endregion WI-1 F4 - Caption buttons must remain hit-testable when ExtendsContentIntoTitleBar=true

        #region TitleBar control - template parts, back/pane-toggle commands, unload cleanup

        [Fact]
        public Task TitleBar_Template_ExposesNavigationButtonsAsync()
        {
            return RunWithTitleBarAsync(
                static delegate
                {
                    return new TitleBar
                    {
                        Title = "Fluence",
                        IsBackButtonVisible = true,
                        IsPaneToggleButtonVisible = true,
                    };
                },
                static titleBar =>
                {
                    System.Windows.Controls.Button backButton = GetTemplateButton(titleBar, "PART_BackButton");
                    System.Windows.Controls.Button paneToggleButton = GetTemplateButton(titleBar, "PART_PaneToggleButton");

                    Assert.Equal(Visibility.Visible, backButton.Visibility);
                    Assert.Equal(Visibility.Visible, paneToggleButton.Visibility);
                    Assert.True(WindowChrome.GetIsHitTestVisibleInChrome(backButton),
                        "PART_BackButton must opt into WindowChrome hit testing.");
                    Assert.True(WindowChrome.GetIsHitTestVisibleInChrome(paneToggleButton),
                        "PART_PaneToggleButton must opt into WindowChrome hit testing.");
                });
        }

        [Fact]
        public Task TitleBar_BackButton_UsesCompactSlotAsync()
        {
            return RunWithTitleBarAsync(
                static delegate
                {
                    return new TitleBar
                    {
                        Title = "Fluence",
                        IsBackButtonVisible = true,
                        IsPaneToggleButtonVisible = true,
                    };
                },
                static titleBar =>
                {
                    System.Windows.Controls.Button backButton = GetTemplateButton(titleBar, "PART_BackButton");
                    System.Windows.Controls.Button paneToggleButton = GetTemplateButton(titleBar, "PART_PaneToggleButton");

                    Assert.Equal(36.0, backButton.ActualWidth, 0.5);
                    Assert.Equal(32.0, backButton.ActualHeight, 0.5);
                    Assert.Equal(40.0, paneToggleButton.ActualWidth, 0.5);
                    Assert.Equal(36.0, paneToggleButton.ActualHeight, 0.5);

                    System.Windows.Controls.TextBlock backGlyph = Assert.IsType<System.Windows.Controls.TextBlock>(FindVisualChild<System.Windows.Controls.TextBlock>(backButton), exactMatch: false);
                    Assert.Equal(16.0, backGlyph.ActualWidth, 0.5);
                    Assert.Equal(16.0, backGlyph.ActualHeight, 0.5);
                });
        }

        [Fact]
        public async Task TitleBar_PaneToggleClick_ExecutesCommandThenRaisesRequestedAsync()
        {
            object parameter = new();
            RecordingCommand command = new(canExecute: true);
            int eventCount = 0;
            int commandCountObservedByEvent = -1;

            await RunWithTitleBarAsync(
                delegate
                {
                    return new TitleBar
                    {
                        IsPaneToggleButtonVisible = true,
                        PaneToggleCommand = command,
                        PaneToggleCommandParameter = parameter,
                    };
                },
                titleBar =>
                {
                    titleBar.PaneToggleRequested += delegate
                    {
                        eventCount++;
                        commandCountObservedByEvent = command.ExecuteCount;
                    };

                    InvokeButton(GetTemplateButton(titleBar, "PART_PaneToggleButton"));

                    Assert.Equal(1, command.ExecuteCount);
                    Assert.Same(parameter, command.LastParameter);
                    Assert.Equal(1, eventCount);
                    Assert.Equal(1, commandCountObservedByEvent);
                }).ConfigureAwait(true);
        }

        [Fact]
        public async Task TitleBar_BackButtonVisibilityAndCommand_WorkAsync()
        {
            object parameter = new();
            RecordingCommand command = new(canExecute: true);
            int eventCount = 0;

            await RunWithTitleBarAsync(
                delegate
                {
                    return new TitleBar
                    {
                        BackCommand = command,
                        BackCommandParameter = parameter,
                    };
                },
                titleBar =>
                {
                    System.Windows.Controls.Button backButton = GetTemplateButton(titleBar, "PART_BackButton");
                    Assert.Equal(Visibility.Collapsed, backButton.Visibility);

                    titleBar.BackRequested += delegate { eventCount++; };
                    titleBar.IsBackButtonVisible = true;
                    titleBar.UpdateLayout();
                    WpfTestSta.DrainDispatcher(titleBar.Dispatcher);

                    Assert.Equal(Visibility.Visible, backButton.Visibility);

                    InvokeButton(backButton);

                    Assert.Equal(1, command.ExecuteCount);
                    Assert.Same(parameter, command.LastParameter);
                    Assert.Equal(1, eventCount);
                }).ConfigureAwait(true);
        }

        [Fact]
        public async Task TitleBar_Unloaded_UnsubscribesCommandCanExecuteHandlersAsync()
        {
            RecordingCommand backCommand = new(canExecute: true);
            RecordingCommand paneToggleCommand = new(canExecute: true);

            await RunWithTitleBarAsync(
                delegate
                {
                    return new TitleBar
                    {
                        IsBackButtonVisible = true,
                        IsPaneToggleButtonVisible = true,
                        BackCommand = backCommand,
                        PaneToggleCommand = paneToggleCommand,
                    };
                },
                titleBar =>
                {
                    Assert.Equal(1, backCommand.CanExecuteSubscriptionCount);
                    Assert.Equal(1, paneToggleCommand.CanExecuteSubscriptionCount);

                    titleBar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, titleBar));
                    WpfTestSta.DrainDispatcher(titleBar.Dispatcher);

                    Assert.Equal(1, backCommand.CanExecuteUnsubscriptionCount);
                    Assert.Equal(1, paneToggleCommand.CanExecuteUnsubscriptionCount);
                }).ConfigureAwait(true);
        }

        private static Task RunWithTitleBarAsync(Func<TitleBar> titleBarFactory, Action<TitleBar> testBody)
        {
            return WpfTestSta.RunOnStaAsync(delegate
            {
                _ = TestApp.EnsureLibraryTheme();
                Window? window = null;
                TitleBar? titleBar = null;

                try
                {
                    titleBar = titleBarFactory();
                    window = new Window
                    {
                        Width = 720,
                        Height = 120,
                        Left = -20000,
                        Top = -20000,
                        WindowStartupLocation = WindowStartupLocation.Manual,
                        ShowInTaskbar = false,
                        Content = titleBar,
                    };

                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = titleBar.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    testBody(titleBar);
                }
                finally
                {
                    if (window is not null)
                    {
                        window.Content = null;
                        window.Close();
                    }
                }
            });
        }

        private static System.Windows.Controls.Button GetTemplateButton(TitleBar titleBar, string partName)
        {
            return Assert.IsType<System.Windows.Controls.Button>(titleBar.Template.FindName(partName, titleBar));
        }

        private static void InvokeButton(System.Windows.Controls.Button button)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
            IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
            invoke.Invoke();
            WpfTestSta.DrainDispatcher(button.Dispatcher);
        }

        private sealed class RecordingCommand : ICommand
        {
            private readonly bool _canExecute;

            internal RecordingCommand(bool canExecute)
            {
                _canExecute = canExecute;
            }

            public event EventHandler? CanExecuteChanged
            {
                add => CanExecuteSubscriptionCount += value is null ? 0 : 1;
                remove => CanExecuteUnsubscriptionCount += value is null ? 0 : 1;
            }

            internal int ExecuteCount { get; private set; }

            internal object? LastParameter { get; private set; }

            internal int CanExecuteSubscriptionCount { get; private set; }

            internal int CanExecuteUnsubscriptionCount { get; private set; }

            public bool CanExecute(object? parameter)
            {
                return _canExecute;
            }

            public void Execute(object? parameter)
            {
                ExecuteCount++;
                LastParameter = parameter;
            }
        }

        #endregion TitleBar control - template parts, back/pane-toggle commands, unload cleanup
    }
}
