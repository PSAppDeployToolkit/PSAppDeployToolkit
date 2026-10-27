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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Windowing
{
    // WI-1 F4 regression guard. The four caption buttons on FluenceWindow
    // (PART_MinimizeButton / PART_MaximizeButton / PART_RestoreButton / PART_CloseButton, see
    // Themes/Controls/FluenceWindow.xaml:203-251) bind to SystemCommands via XAML
    // and are routed through CommandBindings registered in FluenceWindow's
    // constructor (FluenceWindow.cs:394-397) to private handlers that drive
    // WindowState directly (belt-and-braces-paired with NativeMethods.*WindowNative
    // P/Invoke so SC_MINIMIZE/SC_MAXIMIZE gating by DefWindowProc cannot silently
    // drop caption clicks). These tests pin both slots: the XAML binding
    // (Button.Command reference-equals the expected SystemCommand) and the
    // runtime effect (WindowState transition / Closing event).
    public sealed class CaptionButtonTests : IClassFixture<LightThemeFixture>
    {
        public CaptionButtonTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        private static async Task<FluenceWindow> CreateAndShowOffScreenFluenceWindowAsync()
        {
            FluenceWindow window = new()
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
            return window;
        }

        private static System.Windows.Controls.Button GetCaptionButton(FluenceWindow window, string name)
        {
            return Assert.IsType<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(window, name), exactMatch: false);
        }

        [Fact]
        public Task FluenceWindow_CaptionButtons_AllFourBindToCanonicalSystemCommandsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);

                    System.Windows.Controls.Button minimize = GetCaptionButton(window, "PART_MinimizeButton");
                    System.Windows.Controls.Button maximize = GetCaptionButton(window, "PART_MaximizeButton");
                    System.Windows.Controls.Button restore = GetCaptionButton(window, "PART_RestoreButton");
                    System.Windows.Controls.Button close = GetCaptionButton(window, "PART_CloseButton");

                    Assert.Same(SystemCommands.MinimizeWindowCommand, minimize.Command);
                    Assert.Same(SystemCommands.MaximizeWindowCommand, maximize.Command);
                    Assert.Same(SystemCommands.RestoreWindowCommand, restore.Command);
                    Assert.Same(SystemCommands.CloseWindowCommand, close.Command);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_CaptionButtons_ReflowIntoRightAlignedSlotsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);

                    System.Windows.Controls.Button minimize = GetCaptionButton(window, "PART_MinimizeButton");
                    System.Windows.Controls.Button maximize = GetCaptionButton(window, "PART_MaximizeButton");
                    System.Windows.Controls.Button restore = GetCaptionButton(window, "PART_RestoreButton");
                    System.Windows.Controls.Button close = GetCaptionButton(window, "PART_CloseButton");

                    Assert.Equal(0, System.Windows.Controls.Grid.GetColumn(minimize));
                    Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(maximize));
                    Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(restore));
                    Assert.Equal(2, System.Windows.Controls.Grid.GetColumn(close));

                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(minimize));
                    Assert.Equal(2, System.Windows.Controls.Grid.GetColumn(maximize));
                    Assert.Equal(2, System.Windows.Controls.Grid.GetColumn(restore));

                    window.IsCloseButtonVisible = Visibility.Visible;
                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(maximize));
                    Assert.Equal(1, System.Windows.Controls.Grid.GetColumn(restore));
                    Assert.Equal(2, System.Windows.Controls.Grid.GetColumn(close));
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_MinimizeCommand_TransitionsToMinimizedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);
                    Assert.Equal(WindowState.Normal, window.WindowState);

                    SystemCommands.MinimizeWindowCommand.Execute(parameter: null, window);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(WindowState.Minimized, window.WindowState);
                }
                finally
                {
                    if (window is not null)
                    {
                        // Restore before close so the dispatcher does not leak a minimized window.
                        window.WindowState = WindowState.Normal;
                        window.Close();
                    }
                }
            });
        }

        [Fact]
        public Task FluenceWindow_MaximizeCommand_TransitionsToMaximizedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);
                    Assert.Equal(WindowState.Normal, window.WindowState);

                    SystemCommands.MaximizeWindowCommand.Execute(parameter: null, window);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(WindowState.Maximized, window.WindowState);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_RestoreCommand_TransitionsMaximizedToNormalAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);
                    window.WindowState = WindowState.Maximized;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(WindowState.Maximized, window.WindowState);

                    SystemCommands.RestoreWindowCommand.Execute(parameter: null, window);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(WindowState.Normal, window.WindowState);
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [Fact]
        public Task FluenceWindow_CloseCommand_FiresClosingEventAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                FluenceWindow? window = null;
                try
                {
                    window = await CreateAndShowOffScreenFluenceWindowAsync().ConfigureAwait(true);
                    bool closingFired = false;
                    window.Closing += delegate { closingFired = true; };

                    SystemCommands.CloseWindowCommand.Execute(parameter: null, window);
                    // OnCloseWindow calls SystemCommands.CloseWindow(this) which posts
                    // WM_SYSCOMMAND/SC_CLOSE via PostMessage. Block at Background priority
                    // so the Win32 message pump processes the queued message and close
                    // flow runs before we assert.
                    await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Background, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                    Assert.True(closingFired,
                        "CloseWindowCommand must raise Window.Closing via SystemCommands.CloseWindow -> WM_SYSCOMMAND/SC_CLOSE.");

                    // Window is now closed; defeat the finally Close() so we do not double-dispose.
                    window = null;
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        #region CaptionButtonChrome - visibility and enablement matrix

        [Fact]
        public void Minimize_NoResize_Hides()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.NoResize,
                visibilityOverride: null,
                isMinimizable: true,
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Collapsed, vis);
            Assert.False(en);
        }

        [Fact]
        public void Minimize_CanResize_ShowsEnabled()
        {
            CaptionButtonChrome.GetMinimizeChrome(
                ResizeMode.CanResize,
                visibilityOverride: null,
                isMinimizable: true,
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Visible, vis);
            Assert.True(en);
        }

        [Fact]
        public void MaximizeRestore_CanResize_Normal_ShowsMaximizeOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Normal,
                visibilityOverride: null,
                isMaximizable: true,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Visible, maxVis);
            Assert.Equal(Visibility.Collapsed, restVis);
            Assert.True(maxEn);
            Assert.False(restEn);
        }

        [Fact]
        public void MaximizeRestore_CanResize_Maximized_ShowsRestoreOnly()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanResize,
                WindowState.Maximized,
                visibilityOverride: null,
                isMaximizable: true,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Collapsed, maxVis);
            Assert.Equal(Visibility.Visible, restVis);
            Assert.False(maxEn);
            Assert.True(restEn);
        }

        [Fact]
        public void MaximizeRestore_CanMinimize_DisablesBoth()
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode.CanMinimize,
                WindowState.Normal,
                visibilityOverride: null,
                isMaximizable: true,
                out Visibility maxVis,
                out _,
                out bool maxEn,
                out bool restEn);

            Assert.Equal(Visibility.Visible, maxVis);
            Assert.False(maxEn);
            Assert.False(restEn);
        }

        [Fact]
        public void Close_VisibleAndEnabled()
        {
            CaptionButtonChrome.GetCloseChrome(
                visibilityOverride: null,
                isClosable: true,
                out Visibility vis,
                out bool en);

            Assert.Equal(Visibility.Visible, vis);
            Assert.True(en);
        }

        [Theory]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, true, Visibility.Visible, true)]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, false, Visibility.Visible, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Hidden, true, Visibility.Hidden, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Collapsed, true, Visibility.Collapsed, false)]
        [InlineData(ResizeMode.CanResize, Visibility.Hidden, true, Visibility.Hidden, false)]
        [InlineData(ResizeMode.CanResize, Visibility.Collapsed, true, Visibility.Collapsed, false)]
        [InlineData(ResizeMode.CanMinimize, null, true, Visibility.Visible, true)]
        public void MinimizeChrome_OverrideAndCapability_HaveExpectedVisibilityAndEnablement(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMinimizable,
            Visibility expectedVisibility,
            bool expectedEnabled)
        {
            CaptionButtonChrome.GetMinimizeChrome(
                resizeMode,
                visibilityOverride,
                isMinimizable,
                out Visibility visibility,
                out bool enabled);

            Assert.Equal(expectedVisibility, visibility);
            Assert.Equal(expectedEnabled, enabled);
        }

        [Theory]
        [InlineData(ResizeMode.NoResize, WindowState.Normal, Visibility.Visible, true, Visibility.Visible, Visibility.Collapsed, true, false)]
        [InlineData(ResizeMode.NoResize, WindowState.Normal, Visibility.Visible, false, Visibility.Visible, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.NoResize, WindowState.Maximized, Visibility.Visible, true, Visibility.Collapsed, Visibility.Visible, false, true)]
        [InlineData(ResizeMode.CanMinimize, WindowState.Normal, null, true, Visibility.Visible, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Normal, Visibility.Hidden, true, Visibility.Hidden, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Maximized, Visibility.Hidden, true, Visibility.Collapsed, Visibility.Hidden, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Normal, Visibility.Collapsed, true, Visibility.Collapsed, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Maximized, Visibility.Collapsed, true, Visibility.Collapsed, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Normal, (Visibility)99, true, Visibility.Collapsed, Visibility.Collapsed, false, false)]
        [InlineData(ResizeMode.CanResize, WindowState.Maximized, (Visibility)99, true, Visibility.Collapsed, Visibility.Collapsed, false, false)]
        public void MaximizeRestoreChrome_OverrideAndCapability_HaveExpectedActivePair(
            ResizeMode resizeMode,
            WindowState windowState,
            Visibility? visibilityOverride,
            bool isMaximizable,
            Visibility expectedMaximizeVisibility,
            Visibility expectedRestoreVisibility,
            bool expectedMaximizeEnabled,
            bool expectedRestoreEnabled)
        {
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                resizeMode,
                windowState,
                visibilityOverride,
                isMaximizable,
                out Visibility maximizeVisibility,
                out Visibility restoreVisibility,
                out bool maximizeEnabled,
                out bool restoreEnabled);

            Assert.Equal(expectedMaximizeVisibility, maximizeVisibility);
            Assert.Equal(expectedRestoreVisibility, restoreVisibility);
            Assert.Equal(expectedMaximizeEnabled, maximizeEnabled);
            Assert.Equal(expectedRestoreEnabled, restoreEnabled);
        }

        [Theory]
        [InlineData(null, false, Visibility.Visible, false)]
        [InlineData(Visibility.Visible, false, Visibility.Visible, false)]
        [InlineData(Visibility.Hidden, false, Visibility.Hidden, false)]
        [InlineData(Visibility.Collapsed, false, Visibility.Collapsed, false)]
        [InlineData(Visibility.Hidden, true, Visibility.Hidden, false)]
        public void CloseChrome_VisibilityAndCapability_HaveExpectedResult(
            Visibility? visibilityOverride,
            bool isClosable,
            Visibility expectedVisibility,
            bool expectedEnabled)
        {
            CaptionButtonChrome.GetCloseChrome(
                visibilityOverride,
                isClosable,
                out Visibility visibility,
                out bool enabled);

            Assert.Equal(expectedVisibility, visibility);
            Assert.Equal(expectedEnabled, enabled);
        }

        [Theory]
        [InlineData(ResizeMode.NoResize, null, true, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, true, true)]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, false, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Hidden, true, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Collapsed, true, false)]
        [InlineData(ResizeMode.CanResize, Visibility.Hidden, true, true)]
        [InlineData(ResizeMode.CanResize, Visibility.Collapsed, true, true)]
        public void CanMinimize_CommandPolicy_UsesResizeModeAndExplicitVisible(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMinimizable,
            bool expectedCanExecute)
        {
            Assert.Equal(expectedCanExecute,
                CaptionButtonChrome.CanMinimize(resizeMode, visibilityOverride, isMinimizable));
        }

        [Theory]
        [InlineData(ResizeMode.NoResize, null, true, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, true, true)]
        [InlineData(ResizeMode.NoResize, Visibility.Visible, false, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Hidden, true, false)]
        [InlineData(ResizeMode.NoResize, Visibility.Collapsed, true, false)]
        [InlineData(ResizeMode.CanResize, Visibility.Hidden, true, true)]
        [InlineData(ResizeMode.CanResize, Visibility.Collapsed, true, true)]
        [InlineData(ResizeMode.CanMinimize, null, true, false)]
        public void CanMaximize_CommandPolicy_UsesResizeModeAndExplicitVisible(
            ResizeMode resizeMode,
            Visibility? visibilityOverride,
            bool isMaximizable,
            bool expectedCanExecute)
        {
            Assert.Equal(expectedCanExecute,
                CaptionButtonChrome.CanMaximize(resizeMode, visibilityOverride, isMaximizable));
        }

        #endregion CaptionButtonChrome - visibility and enablement matrix
    }
}
