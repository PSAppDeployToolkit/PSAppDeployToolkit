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
using Xunit;

namespace Fluence.Wpf.Tests
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
    public partial class ControlTests
    {
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
            return Assert.IsAssignableFrom<System.Windows.Controls.Button>(FindVisualChildByName<System.Windows.Controls.Button>(window, name));
        }

        [Fact]
        public Task FluenceWindow_CaptionButtons_AllFourBindToCanonicalSystemCommandsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async delegate
            {
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
            return WpfTestSta.RunOnStaAsync(async delegate
            {
                _ = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

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
    }
}
