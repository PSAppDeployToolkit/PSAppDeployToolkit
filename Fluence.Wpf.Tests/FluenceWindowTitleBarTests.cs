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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Native;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class FluenceWindowTitleBarTests
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

        private static void RunWithWindow(Action<FluenceWindow> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow();
                    testBody(window);
                }
                finally
                {
                    window?.Close();

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        /// <summary>
        /// Shows a FluenceWindow off-screen so template parts (caption buttons) exist for hit-testing.
        /// </summary>
        /// <param name="testBody">The action to run with the shown window.</param>
        private static void RunWithShownWindow(Action<FluenceWindow> testBody)
        {
            RunOnFreshStaThread(() =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
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
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded, default);
                    testBody(window);
                }
                finally
                {
                    window?.Close();

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        private static int? InvokeHitTestTitleBar(FluenceWindow window, IntPtr lParam)
        {
            MethodInfo? method = typeof(FluenceWindow).GetMethod(
                "HitTestTitleBar",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            return (int?)method.Invoke(window, [lParam]);
        }

        private static IntPtr? InvokeWndProc(FluenceWindow window, int msg, IntPtr wParam, IntPtr lParam, out bool handled)
        {
            MethodInfo? method = typeof(FluenceWindow).GetMethod(
                "WndProc",
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);

            object[] args = [IntPtr.Zero, msg, wParam, lParam, false];
            IntPtr? result = (IntPtr?)method.Invoke(window, args);
            handled = (bool)args[4];
            return result;
        }

        private static void AssertNativeConstantValue(string fieldName, object expectedValue)
        {
            FieldInfo? field = typeof(NativeConstants).GetField(fieldName, BindingFlags.Static | BindingFlags.Public);
            Assert.NotNull(field);
            Assert.Equal(expectedValue, field.GetRawConstantValue());
        }

        private static IntPtr MakeLParamScreen(double screenX, double screenY)
        {
            int x = (int)screenX;
            int y = (int)screenY;
            return new IntPtr((y << 16) | (x & 0xffff));
        }

        private static System.Windows.Controls.Button? GetCaptionButtonField(FluenceWindow window, string fieldName)
        {
            FieldInfo? field = typeof(FluenceWindow).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(field);
            return field.GetValue(window) as System.Windows.Controls.Button;
        }

        #region 1. ExtendsContentIntoTitleBar default

        [Fact]
        public void ExtendsContentIntoTitleBar_DefaultIsFalse()
        {
            RunWithWindow(static w =>
            {
                Assert.False(w.ExtendsContentIntoTitleBar,
                    "ExtendsContentIntoTitleBar should default to false.");
            });
        }

        #endregion 1. ExtendsContentIntoTitleBar default

        #region 2. FluenceWindow sizing defaults

        [Fact]
        public void TitleBarHeight_DefaultIs48()
        {
            RunWithWindow(static w => Assert.Equal(48d, w.TitleBarHeight));
        }

        [Fact]
        public void MinWidth_DefaultRemainsUnset()
        {
            RunWithWindow(static w => Assert.Equal(0d, w.MinWidth));
        }

        #endregion 2. FluenceWindow sizing defaults

        #region 3. ShowIcon and ShowTitle defaults

        [Fact]
        public void ShowIcon_DefaultIsTrue()
        {
            RunWithWindow(static w => Assert.True(w.ShowIcon, "ShowIcon should default to true."));
        }

        [Fact]
        public void ShowTitle_DefaultIsTrue()
        {
            RunWithWindow(static w => Assert.True(w.ShowTitle, "ShowTitle should default to true."));
        }

        #endregion 3. ShowIcon and ShowTitle defaults

        #region 4. Caption button visibility defaults

        [Fact]
        public void CaptionButtonVisibility_DefaultsAreVisible()
        {
            RunWithWindow(static w =>
            {
                Assert.Equal(Visibility.Visible, w.IsMinimizeButtonVisible);
                Assert.Equal(Visibility.Visible, w.IsMaximizeButtonVisible);
                Assert.Equal(Visibility.Visible, w.IsCloseButtonVisible);
            });
        }

        [Fact]
        public void CaptionButtonEnabled_DefaultsAreTrue()
        {
            RunWithWindow(static w =>
            {
                Assert.True(w.IsMinimizable);
                Assert.True(w.IsMaximizable);
                Assert.True(w.IsClosable);
            });
        }

        #endregion 4. Caption button visibility defaults

        #region 5. HasShadow and WindowBorder defaults

        [Fact]
        public void HasShadow_DefaultIsTrue()
        {
            RunWithWindow(static w => Assert.True(w.HasShadow, "HasShadow should default to true."));
        }

        [Fact]
        public void BorderThickness_DefaultIsOne()
        {
            RunWithWindow(static w => Assert.Equal(new Thickness(1), w.BorderThickness));
        }

        #endregion 5. HasShadow and WindowBorder defaults

        #region 6. SetTitleBar method

        [Fact]
        public void SetTitleBar_SetsTitleBarProperty()
        {
            RunWithWindow(static w =>
            {
                System.Windows.Controls.TextBlock customElement = new() { Text = "Custom Title" };
                w.SetTitleBar(customElement);
                Assert.Same(customElement, w.TitleBar);
            });
        }

        [Fact]
        public void SetTitleBar_NullReverts()
        {
            RunWithWindow(static w =>
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
        public void CaptionHeight_AlwaysZero_RegardlessOfExtendsContentIntoTitleBar()
        {
            RunWithWindow(static w =>
            {
                WindowChrome chrome = WindowChrome.GetWindowChrome(w);
                Assert.NotNull(chrome);
                Assert.Equal(0d, chrome.CaptionHeight);

                w.ExtendsContentIntoTitleBar = true;

                Assert.Equal(0d, chrome.CaptionHeight);
            });
        }

        [Fact]
        public void HasShadow_False_SetsGlassFrameToNearZero()
        {
            RunWithWindow(static w =>
            {
                WindowChrome chrome = WindowChrome.GetWindowChrome(w);
                Assert.Equal(new Thickness(-1), chrome.GlassFrameThickness);

                w.HasShadow = false;
                w.SystemBackdropType = BackdropType.None;

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
        public void CaptionHeight_IsZero_EvenBeforeExtendsContentIntoTitleBar()
        {
            RunWithWindow(static w =>
            {
                WindowChrome chrome = WindowChrome.GetWindowChrome(w);
                Assert.NotNull(chrome);
                Assert.Equal(0d, chrome.CaptionHeight);
            });
        }

        [Fact]
        public void WindowChrome_AppliedInConstructor()
        {
            RunWithWindow(static w =>
            {
                WindowChrome chrome = WindowChrome.GetWindowChrome(w);
                Assert.NotNull(chrome);
            });
        }

        [Fact]
        public void DefaultBorderThickness_IsOne()
        {
            RunWithWindow(static w => Assert.Equal(new Thickness(1), w.BorderThickness));
        }

        [Fact]
        public void ThemeSwitch_UpdatesWindowBackground()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
                FluenceWindow? window = null;

                try
                {
                    window = new FluenceWindow();
                    Brush lightBg = window.Background;

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    Brush darkBg = window.Background;

                    Assert.NotEqual(lightBg, darkBg);
                }
                finally
                {
                    window?.Close();

                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [Fact]
        public void ThemeChanged_FiresOnApply()
        {
            RunOnFreshStaThread(() =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
                int fireCount = 0;
                void handler(object? s, ThemeChangedEventArgs e)
                {
                    fireCount++;
                }

                try
                {
                    ApplicationThemeManager.Changed += handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    Assert.Equal(1, fireCount);
                }
                finally
                {
                    ApplicationThemeManager.Changed -= handler;
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [Fact]
        public void FluenceWindowXaml_NoStaticResourceForThemeBrushes()
        {
            string xamlPath = System.IO.Path.Combine(
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
                    "CardStrokeColorDefaultSolidBrush",
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
        public void FullThemeCycle_KeyBrushesResolve()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);

                try
                {
                    foreach (ApplicationTheme theme in (ApplicationTheme[])[ApplicationTheme.Dark, ApplicationTheme.Light])
                    {
                        ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                        object? bg = app?.TryFindResource("ApplicationBackgroundBrush");
                        Assert.NotNull(bg);
                        object? fg = app?.TryFindResource("TextFillColorPrimaryBrush");
                        Assert.NotNull(fg);
                    }
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [Fact]
        public void MergedDictionaries_CountStableAfterMultipleSwitches()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);

                try
                {
                    int? initialCount = app?.Resources.MergedDictionaries.Count;

                    for (int i = 0; i < 5; i++)
                    {
                        ApplicationTheme theme = i % 2 is 0 ? ApplicationTheme.Dark : ApplicationTheme.Light;
                        ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    }

                    Assert.Equal(initialCount, app?.Resources.MergedDictionaries.Count);
                }
                finally
                {
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        #endregion Bug Fix Tests - Title Bar Flash and Theme Switching

        #region Caption button hit-test (WM_NCHITTEST vs WPF commands)

        [Fact]
        public void FluenceWindowXaml_CaptionButtonsUseSystemCommands()
        {
            string xamlPath = System.IO.Path.Combine(
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
        public void HitTestTitleBar_MinimizeButton_ReturnsZero_NotHtMinButton()
        {
            RunWithShownWindow(static w =>
            {
                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_minimizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Visible, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(0, hit);
                Assert.NotEqual(NativeConstants.HTMINBUTTON, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_CloseButton_ReturnsZero_NotHtClose()
        {
            RunWithShownWindow(static w =>
            {
                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_closeButton");
                Assert.NotNull(btn);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(0, hit);
                Assert.NotEqual(NativeConstants.HTCLOSE, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_MaximizeButton_ReturnsHtMaxButton()
        {
            RunWithShownWindow(static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);
                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_maximizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Visible, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.Equal(NativeConstants.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_MaximizeButtonHidden_DoesNotReturnHtMaxButton()
        {
            RunWithShownWindow(static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Hidden;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_maximizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Hidden, btn.Visibility);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.NotEqual(NativeConstants.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_MaximizeButtonDisabled_DoesNotReturnHtMaxButton()
        {
            RunWithShownWindow(static w =>
            {
                w.IsMaximizable = false;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_maximizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Visible, btn.Visibility);
                Assert.False(btn.IsEnabled);

                Point center = btn.PointToScreen(new Point(btn.RenderSize.Width / 2, btn.RenderSize.Height / 2));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(center.X, center.Y));
                Assert.NotEqual(NativeConstants.HTMAXBUTTON, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_TitleBarDragArea_ReturnsHtCaption()
        {
            RunWithShownWindow(static w =>
            {
                w.UpdateLayout();
                Point clientMidTitle = new(Math.Max(40, w.ActualWidth / 2), Math.Max(1, w.TitleBarHeight / 2));
                Point screen = w.PointToScreen(clientMidTitle);
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(NativeConstants.HTCAPTION, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_TopResizeBand_ReturnsHtTopBeforeCaption()
        {
            RunWithShownWindow(static w =>
            {
                w.UpdateLayout();
                Point screen = w.PointToScreen(new Point(w.ActualWidth / 2.0, 1.0));
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(NativeConstants.HTTOP, hit);
            });
        }

        [Fact]
        public void HitTestTitleBar_UpperCorners_ReturnResizeCornersBeforeCaption()
        {
            RunWithShownWindow(static w =>
            {
                w.UpdateLayout();

                Point topLeft = w.PointToScreen(new Point(1.0, 1.0));
                int? leftHit = InvokeHitTestTitleBar(w, MakeLParamScreen(topLeft.X, topLeft.Y));
                Assert.Equal(NativeConstants.HTTOPLEFT, leftHit);

                Point topRight = w.PointToScreen(new Point(w.ActualWidth - 1.0, 1.0));
                int? rightHit = InvokeHitTestTitleBar(w, MakeLParamScreen(topRight.X, topRight.Y));
                Assert.Equal(NativeConstants.HTTOPRIGHT, rightHit);
            });
        }

        [Fact]
        public void HitTestTitleBar_IsMoveableFalse_TitleBarDragAreaReturnsZero()
        {
            RunWithShownWindow(static w =>
            {
                w.IsMoveable = false;
                w.UpdateLayout();

                Point clientMidTitle = new(Math.Max(40, w.ActualWidth / 2), Math.Max(1, w.TitleBarHeight / 2));
                Point screen = w.PointToScreen(clientMidTitle);
                int? hit = InvokeHitTestTitleBar(w, MakeLParamScreen(screen.X, screen.Y));
                Assert.Equal(0, hit);
            });
        }

        [Fact]
        public void WndProc_IsMoveableFalse_SuppressesSystemMove()
        {
            RunWithWindow(static w =>
            {
                w.IsMoveable = false;
                _ = InvokeWndProc(w, NativeConstants.WM_SYSCOMMAND, new IntPtr(NativeConstants.SC_MOVE), IntPtr.Zero, out bool handled);
                Assert.True(handled, "IsMoveable=false must handle SC_MOVE.");

                w.IsMoveable = true;
                _ = InvokeWndProc(w, NativeConstants.WM_SYSCOMMAND, new IntPtr(NativeConstants.SC_MOVE), IntPtr.Zero, out handled);
                Assert.False(handled, "IsMoveable=true must leave SC_MOVE available.");
            });
        }

        [Fact]
        public void WndProc_NcLeftButtonUpHtMaxButton_UsesDirectMaximizeAndRefreshesCaptionButtons()
        {
            RunWithShownWindow(static w =>
            {
                System.Windows.Controls.Button? max = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? restore = GetCaptionButtonField(w, "_restoreButton");
                Assert.NotNull(max);
                Assert.NotNull(restore);
                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(Visibility.Collapsed, restore.Visibility);

                _ = InvokeWndProc(
                    w,
                    NativeConstants.WM_NCLBUTTONUP,
                    new IntPtr(NativeConstants.HTMAXBUTTON),
                    IntPtr.Zero,
                    out bool handled);
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.True(handled,
                    "WM_NCLBUTTONUP/HTMAXBUTTON should be handled by FluenceWindow.");
                Assert.Equal(WindowState.Maximized, w.WindowState);
                Assert.Equal(Visibility.Collapsed, max.Visibility);
                Assert.Equal(Visibility.Visible, restore.Visibility);

                _ = InvokeWndProc(
                    w,
                    NativeConstants.WM_NCLBUTTONUP,
                    new IntPtr(NativeConstants.HTMAXBUTTON),
                    IntPtr.Zero,
                    out handled);
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.True(handled,
                    "Second WM_NCLBUTTONUP/HTMAXBUTTON should be handled by FluenceWindow.");
                Assert.Equal(WindowState.Normal, w.WindowState);
                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(Visibility.Collapsed, restore.Visibility);
            });
        }

        [Fact]
        public void SetSnapHover_UsesSubtleFillTokens_MatchingTemplatePointerOver()
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
            RunWithShownWindow(static w =>
            {
                System.Windows.Controls.Button? max = GetCaptionButtonField(w, "_maximizeButton");
                Assert.NotNull(max);
                Assert.True(max.IsEnabled,
                    "Precondition: maximize button must be enabled for SetSnapHover to apply a hover visual.");

                // The resolved brushes the template PointerOver state would show, looked up the same
                // way SetSnapHover's resource references resolve them.
                object? expectedBackground = w.TryFindResource("SubtleFillColorSecondaryBrush");
                object? expectedForeground = w.TryFindResource("TextFillColorPrimaryBrush");
                object? staleBackground = w.TryFindResource("ControlStrongFillColorDefaultBrush");
                _ = Assert.IsAssignableFrom<Brush>(expectedBackground);
                _ = Assert.IsAssignableFrom<Brush>(expectedForeground);

                MethodInfo? setSnapHover = typeof(FluenceWindow).GetMethod(
                    "SetSnapHover",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(setSnapHover);
                _ = setSnapHover.Invoke(w, [max]);
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Same(expectedBackground, max.Background);
                Assert.Same(expectedForeground, max.Foreground);
                Assert.NotSame(staleBackground, max.Background);

                // ClearSnapHover must restore the template/style defaults via ClearValue, so the
                // local Background/Foreground values are cleared back to the unset (style-driven) state.
                MethodInfo? clearSnapHover = typeof(FluenceWindow).GetMethod(
                    "ClearSnapHover",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(clearSnapHover);
                _ = clearSnapHover.Invoke(w, parameters: null);
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(DependencyProperty.UnsetValue, max.ReadLocalValue(System.Windows.Controls.Control.BackgroundProperty));
                Assert.Equal(DependencyProperty.UnsetValue, max.ReadLocalValue(System.Windows.Controls.Control.ForegroundProperty));
            });
        }

        #endregion Caption button hit-test (WM_NCHITTEST vs WPF commands)

        #region Caption button DP overrides (authoritative when explicitly set)

        [Fact]
        public void IsMinimizeButtonVisible_ExplicitVisible_UnderNoResize_ShowsAndEnablesButton()
        {
            RunWithShownWindow(static w =>
            {
                // XAML sets IsMinimizeButtonVisible=Collapsed
                // on the FluentDialog template, then code-behind flips it back to Visible when
                // DialogAllowMinimize is honoured (IsMinimizeButtonVisible=Visibility.Visible).
                w.IsMinimizeButtonVisible = Visibility.Collapsed;
                w.ResizeMode = ResizeMode.NoResize;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_minimizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Collapsed, btn.Visibility);

                w.IsMinimizeButtonVisible = Visibility.Visible;
                w.IsMinimizable = true;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(Visibility.Visible, btn.Visibility);
                Assert.True(btn.IsEnabled,
                    "Explicit Visible under NoResize must also enable the minimize button.");
            });
        }

        [Fact]
        public void IsMinimizeButtonVisible_ExplicitCollapsed_UnderCanResize_HidesButton()
        {
            RunWithShownWindow(static w =>
            {
                w.ResizeMode = ResizeMode.CanResize;
                w.IsMinimizeButtonVisible = Visibility.Collapsed;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? btn = GetCaptionButtonField(w, "_minimizeButton");
                Assert.NotNull(btn);
                Assert.Equal(Visibility.Collapsed, btn?.Visibility);
                Assert.False(btn?.IsEnabled ?? false,
                    "Explicit Collapsed must also disable the button.");
            });
        }

        [Fact]
        public void IsMaximizeButtonVisible_ExplicitVisible_UnderNoResize_ShowsAndEnablesMaximize()
        {
            RunWithShownWindow(static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Collapsed;
                w.ResizeMode = ResizeMode.NoResize;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? max = GetCaptionButtonField(w, "_maximizeButton");
                Assert.NotNull(max);
                Assert.Equal(Visibility.Collapsed, max.Visibility);

                w.IsMaximizeButtonVisible = Visibility.Visible;
                w.IsMaximizable = true;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(Visibility.Visible, max.Visibility);
                Assert.Equal(WindowState.Normal, w.WindowState);
                Assert.True(max.IsEnabled,
                    "Maximize button must be enabled when the window is not already maximized and the DP is explicit.");
            });
        }

        [Fact]
        public void IsMaximizeButtonVisible_Hidden_ReservesOnlyTheActiveButtonSlot()
        {
            RunWithShownWindow(static w =>
            {
                w.IsMaximizeButtonVisible = Visibility.Hidden;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                System.Windows.Controls.Button? max = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? restore = GetCaptionButtonField(w, "_restoreButton");
                Assert.Equal(Visibility.Hidden, max?.Visibility);
                Assert.Equal(Visibility.Collapsed, restore?.Visibility);
                Assert.False(max?.IsEnabled ?? false);
                Assert.False(restore?.IsEnabled ?? false);

                w.WindowState = WindowState.Maximized;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(Visibility.Collapsed, max?.Visibility);
                Assert.Equal(Visibility.Hidden, restore?.Visibility);
                Assert.False(max?.IsEnabled ?? false);
                Assert.False(restore?.IsEnabled ?? false);
            });
        }

        [Fact]
        public void CaptionButtonVisibleProperties_SetTheVisibilityDps()
        {
            RunWithWindow(static w =>
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
        public void CaptionButtonVisibilityProperties_ApplyVisibleHiddenCollapsedToTemplateButtons()
        {
            RunWithShownWindow(static w =>
            {
                System.Windows.Controls.Button? minimize = GetCaptionButtonField(w, "_minimizeButton");
                System.Windows.Controls.Button? maximize = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? restore = GetCaptionButtonField(w, "_restoreButton");
                System.Windows.Controls.Button? close = GetCaptionButtonField(w, "_closeButton");
                Assert.NotNull(minimize);
                Assert.NotNull(maximize);
                Assert.NotNull(restore);
                Assert.NotNull(close);

                foreach (Visibility value in (Visibility[])[Visibility.Visible, Visibility.Hidden, Visibility.Collapsed])
                {
                    w.IsMinimizeButtonVisible = value;
                    w.IsMaximizeButtonVisible = value;
                    w.IsCloseButtonVisible = value;
                    w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

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
            ConstructorInfo? ctor = typeof(CanExecuteRoutedEventArgs).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
binder: null,
                [typeof(ICommand), typeof(object)],
modifiers: null);
            Assert.NotNull(ctor);
            return (CanExecuteRoutedEventArgs)ctor.Invoke([command, null]);
        }

        private static bool InvokeCanHandler(FluenceWindow window, string handlerName, CanExecuteRoutedEventArgs args)
        {
            MethodInfo? handler = typeof(FluenceWindow).GetMethod(
                handlerName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(handler);
            _ = handler.Invoke(window, [window, args]);
            return args.CanExecute;
        }

        private static ExecutedRoutedEventArgs CreateExecutedArgs(ICommand command)
        {
            ConstructorInfo? ctor = typeof(ExecutedRoutedEventArgs).GetConstructor(
                BindingFlags.Instance | BindingFlags.NonPublic,
binder: null,
                [typeof(ICommand), typeof(object)],
modifiers: null);
            Assert.NotNull(ctor);
            return (ExecutedRoutedEventArgs)ctor.Invoke([command, null]);
        }

        private static void InvokeExecutedHandler(FluenceWindow window, string handlerName, ExecutedRoutedEventArgs args)
        {
            MethodInfo? handler = typeof(FluenceWindow).GetMethod(
                handlerName,
                BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(handler);
            _ = handler.Invoke(window, [window, args]);
        }

        [Fact]
        public void CanMinimizeWindow_RespectsExplicitDp_UnderNoResize()
        {
            RunWithWindow(static w =>
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
        public void CanMaximizeWindow_RespectsExplicitDp_UnderNoResize()
        {
            RunWithWindow(static w =>
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
        public void CaptionButtons_DefaultBehaviorUnchanged_WhenDpsNotTouched()
        {
            RunWithShownWindow(static w =>
            {
                Assert.Equal(ResizeMode.CanResize, w.ResizeMode);

                System.Windows.Controls.Button? minBtn = GetCaptionButtonField(w, "_minimizeButton");
                System.Windows.Controls.Button? maxBtn = GetCaptionButtonField(w, "_maximizeButton");
                System.Windows.Controls.Button? closeBtn = GetCaptionButtonField(w, "_closeButton");

                Assert.Equal(Visibility.Visible, minBtn?.Visibility);
                Assert.Equal(Visibility.Visible, maxBtn?.Visibility);
                Assert.Equal(Visibility.Visible, closeBtn?.Visibility);

                w.ResizeMode = ResizeMode.NoResize;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(Visibility.Collapsed, minBtn?.Visibility);
                Assert.Equal(Visibility.Collapsed, maxBtn?.Visibility);
            });
        }

        [Fact]
        public void OnMinimizeWindow_DrivesWindowStateMinimized_EvenAfterSysMenuStripped()
        {
            // RunWithShownWindow triggers OnSourceInitialized → ApplyWindowShell →
            // HideNativeCaptionButtons → NativeMethods.HideAllWindowButtons, which strips
            // WS_SYSMENU on the native HWND. Without WS_SYSMENU (and the implicitly-disabled
            // WS_MINIMIZEBOX) DefWindowProc silently drops WM_SYSCOMMAND/SC_MINIMIZE, so
            // SystemCommands.MinimizeWindow(this) would be a no-op - exactly the production
            // symptom that made the AllowMinimize caption button look clickable but
            // refuse to actually minimize. The Executed handler must bypass the sysmenu gate
            // by assigning WindowState directly so the transition always lands.
            RunWithShownWindow(static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnMinimizeWindow",
                    CreateExecutedArgs(SystemCommands.MinimizeWindowCommand));

                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(WindowState.Minimized, w.WindowState);
            });
        }

        [Fact]
        public void OnMaximizeWindow_DrivesWindowStateMaximized_EvenAfterSysMenuStripped()
        {
            RunWithShownWindow(static w =>
            {
                Assert.Equal(WindowState.Normal, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnMaximizeWindow",
                    CreateExecutedArgs(SystemCommands.MaximizeWindowCommand));

                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(WindowState.Maximized, w.WindowState);
            });
        }

        [Fact]
        public void OnRestoreWindow_DrivesWindowStateNormal_FromMaximized()
        {
            RunWithShownWindow(static w =>
            {
                w.WindowState = WindowState.Maximized;
                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);
                Assert.Equal(WindowState.Maximized, w.WindowState);

                InvokeExecutedHandler(
                    w,
                    "OnRestoreWindow",
                    CreateExecutedArgs(SystemCommands.RestoreWindowCommand));

                w.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);

                Assert.Equal(WindowState.Normal, w.WindowState);
            });
        }

        [Fact]
        public void MinimizeButton_EndToEnd_ClicksActuallyMinimizeUnderPsadtConfig()
        {
            // Reproduces the exact PSADT FluentDialog topology: Topmost=True + ResizeMode=NoResize
            // + ExtendsContentIntoTitleBar=True + IsMinimizeButtonVisible flipped from
            // Collapsed (XAML baseline) to Visible (IsMinimizeButtonVisible=Visibility.Visible). The
            // test drives the Button via its ICommand to mirror the real click path (WPF
            // Button → SystemCommands.MinimizeWindowCommand → FluenceWindow CommandBinding →
            // OnMinimizeWindow) and asserts the caption is clickable AND the state lands on
            // Minimized. If this ever regresses to "visible but inert" we'll catch it here
            // instead of only in manual QA.
            RunOnFreshStaThread(static () =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
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
                    window.Dispatcher.Invoke(static () => { }, DispatcherPriority.Loaded, default);

                    // Flip visibility after Show() to mirror PSADT's IsMinimizeButtonVisible=Visibility.Visible.
                    window.IsMinimizeButtonVisible = Visibility.Visible;
                    window.IsMinimizable = true;
                    window.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);
                    CommandManager.InvalidateRequerySuggested();
                    window.Dispatcher.Invoke(static () => { }, DispatcherPriority.ApplicationIdle, default);

                    System.Windows.Controls.Button? minBtn = GetCaptionButtonField(window, "_minimizeButton");
                    Assert.NotNull(minBtn);
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
                    window.Dispatcher.Invoke(static () => { }, DispatcherPriority.Render, default);
                    window.Dispatcher.Invoke(static () => { }, DispatcherPriority.ApplicationIdle, default);

                    Assert.Equal(WindowState.Minimized, window.WindowState);
                }
                finally
                {
                    window?.Close();

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        [Fact]
        public void MinimizeButton_EndToEnd_WorksUnderShowDialogModalPsadtConfig()
        {
            // Same topology as the Show() variant above, but uses ShowDialog() which is what
            // PSADT's DialogManager actually invokes (see DialogManager.ShowModalDialog -> dialog.ShowDialog()).
            // Modal WPF windows push a nested Dispatcher frame, disable their owner, and in the
            // PSADT case are also Topmost - a combination that can mask bugs a Show() test misses.
            // We schedule the click via Dispatcher.BeginInvoke(ApplicationIdle) from Loaded so
            // the command fires after the modal frame is pumping, then verify WindowState.
            RunOnFreshStaThread(() =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
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
                                    catch (Exception exInner)
                                    {
                                        scenarioExceptionInfo = ExceptionDispatchInfo.Capture(exInner);
                                        capturedWindow.Close();
                                    }
                                }, DispatcherPriority.ApplicationIdle);
                            }
                            catch (Exception exOuter)
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

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        #endregion Caption button DP overrides (authoritative when explicitly set)

        #region 8. PasswordBox.SelectAll

        [Fact]
        public void PasswordBox_SelectAll_DoesNotThrowWithoutTemplate()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);

                try
                {
                    PasswordBox passwordBox = new()
                    {
                        Password = "hidden",
                    };
                    passwordBox.SelectAll();

                    Assert.Equal("hidden", passwordBox.Password, StringComparer.Ordinal);
                }
                finally
                {
                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
                }
            });
        }

        #endregion 8. PasswordBox.SelectAll

        #region WM_GETMINMAXINFO

        [Fact]
        public void MinMaxInfo_StructLayout_HasCorrectSize()
        {
            // MINMAXINFO must be 5 POINTs = 5 * 8 bytes = 40 bytes.
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MINMAXINFO));
            Assert.Equal(40, size);
        }

        [Fact]
        public void MonitorInfo_StructLayout_HasCorrectSize()
        {
            // MONITORINFO = int + 3 RECTs (16 bytes each) + uint = 4 + 16 + 16 + 16 + 4 = 40 bytes.
            // Actually: cbSize(4) + rcMonitor(16) + rcWork(16) + dwFlags(4) = 40 bytes.
            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MONITORINFO));
            Assert.Equal(40, size);
        }

        [Fact]
        public void MinMaxInfo_RoundTrip_PreservesValues()
        {
            MINMAXINFO mmi = new()
            {
                ptMaxPosition = new POINT { X = 10, Y = 20 },
                ptMaxSize = new POINT { X = 1920, Y = 1040 },
                ptMaxTrackSize = new POINT { X = 3840, Y = 2160 },
                ptMinTrackSize = new POINT { X = 200, Y = 150 },
            };

            int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(MINMAXINFO));
            nint ptr = System.Runtime.InteropServices.Marshal.AllocHGlobal(size);
            try
            {
                System.Runtime.InteropServices.Marshal.StructureToPtr(mmi, ptr, fDeleteOld: false);
                MINMAXINFO? result = (MINMAXINFO?)System.Runtime.InteropServices.Marshal.PtrToStructure(ptr, typeof(MINMAXINFO));

                Assert.Equal(10, result?.ptMaxPosition.X);
                Assert.Equal(20, result?.ptMaxPosition.Y);
                Assert.Equal(1920, result?.ptMaxSize.X);
                Assert.Equal(1040, result?.ptMaxSize.Y);
                Assert.Equal(3840, result?.ptMaxTrackSize.X);
                Assert.Equal(2160, result?.ptMaxTrackSize.Y);
                Assert.Equal(200, result?.ptMinTrackSize.X);
                Assert.Equal(150, result?.ptMinTrackSize.Y);
            }
            finally
            {
                System.Runtime.InteropServices.Marshal.FreeHGlobal(ptr);
            }
        }

        [Fact]
        public void NativeConstants_DefineRequiredWin32Values()
        {
            AssertNativeConstantValue("WM_NCLBUTTONUP", 0x00A2);
            AssertNativeConstantValue("WM_GETMINMAXINFO", 0x0024);
            AssertNativeConstantValue("MONITOR_DEFAULTTONEAREST", 2u);
        }

        #endregion WM_GETMINMAXINFO

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
        public void CaptionButtons_AboveContent_WhenExtendsContentIntoTitleBar()
        {
            RunOnFreshStaThread(() =>
            {
                Application? app = EnsureApplication();
                ResourceDictionary? dict = MergeTheme(app);
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
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.Loaded, default);
                    window.UpdateLayout();
                    window.Dispatcher.Invoke(() => { }, DispatcherPriority.Render, default);

                    foreach (string? fieldName in new[] { "_minimizeButton", "_maximizeButton", "_closeButton" })
                    {
                        System.Windows.Controls.Button? btn = GetCaptionButtonField(window, fieldName);
                        Assert.NotNull(btn);
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

                    if (dict is not null)
                    {
                        _ = app?.Resources.MergedDictionaries.Remove(dict);
                    }
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
    }
}
