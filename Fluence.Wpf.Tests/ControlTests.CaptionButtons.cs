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

using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Windows;
using System.Windows.Threading;
using WpfButton = System.Windows.Controls.Button;

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
        private static FluenceWindow CreateAndShowOffScreenFluenceWindow()
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
            _ = window.Dispatcher.Invoke(DispatcherPriority.Loaded, new Action(static delegate { }));
            return window;
        }

        private static WpfButton GetCaptionButton(FluenceWindow window, string name)
        {
            WpfButton? button = FindVisualChildByName<WpfButton>(window, name);
            Assert.IsNotNull(button,
                string.Format("Caption template part '{0}' must exist on FluenceWindow.", name));
            return button;
        }

        [TestMethod]
        public void FluenceWindow_CaptionButtons_AllFourBindToCanonicalSystemCommands()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();

                    WpfButton minimize = GetCaptionButton(window, "PART_MinimizeButton");
                    WpfButton maximize = GetCaptionButton(window, "PART_MaximizeButton");
                    WpfButton restore = GetCaptionButton(window, "PART_RestoreButton");
                    WpfButton close = GetCaptionButton(window, "PART_CloseButton");

                    Assert.AreSame(SystemCommands.MinimizeWindowCommand, minimize.Command,
                        "PART_MinimizeButton must bind to SystemCommands.MinimizeWindowCommand.");
                    Assert.AreSame(SystemCommands.MaximizeWindowCommand, maximize.Command,
                        "PART_MaximizeButton must bind to SystemCommands.MaximizeWindowCommand.");
                    Assert.AreSame(SystemCommands.RestoreWindowCommand, restore.Command,
                        "PART_RestoreButton must bind to SystemCommands.RestoreWindowCommand.");
                    Assert.AreSame(SystemCommands.CloseWindowCommand, close.Command,
                        "PART_CloseButton must bind to SystemCommands.CloseWindowCommand.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_CaptionButtons_ReflowIntoRightAlignedSlots()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();

                    WpfButton minimize = GetCaptionButton(window, "PART_MinimizeButton");
                    WpfButton maximize = GetCaptionButton(window, "PART_MaximizeButton");
                    WpfButton restore = GetCaptionButton(window, "PART_RestoreButton");
                    WpfButton close = GetCaptionButton(window, "PART_CloseButton");

                    Assert.AreEqual(0, System.Windows.Controls.Grid.GetColumn(minimize));
                    Assert.AreEqual(1, System.Windows.Controls.Grid.GetColumn(maximize));
                    Assert.AreEqual(1, System.Windows.Controls.Grid.GetColumn(restore));
                    Assert.AreEqual(2, System.Windows.Controls.Grid.GetColumn(close));

                    window.IsCloseButtonVisible = Visibility.Collapsed;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, System.Windows.Controls.Grid.GetColumn(minimize),
                        "When close is collapsed, minimize should shift right to keep the visible group right-aligned.");
                    Assert.AreEqual(2, System.Windows.Controls.Grid.GetColumn(maximize),
                        "When close is collapsed, maximize should shift into the rightmost caption slot.");
                    Assert.AreEqual(2, System.Windows.Controls.Grid.GetColumn(restore),
                        "Restore shares maximize's right-aligned slot.");

                    window.IsCloseButtonVisible = Visibility.Visible;
                    window.IsMinimizeButtonVisible = Visibility.Collapsed;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(1, System.Windows.Controls.Grid.GetColumn(maximize),
                        "When minimize is collapsed, maximize should keep its normal slot.");
                    Assert.AreEqual(1, System.Windows.Controls.Grid.GetColumn(restore),
                        "Restore should keep maximize's normal slot when minimize is collapsed.");
                    Assert.AreEqual(2, System.Windows.Controls.Grid.GetColumn(close),
                        "Close should remain in the rightmost caption slot when minimize is collapsed.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_MinimizeCommand_TransitionsToMinimized()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();
                    Assert.AreEqual(WindowState.Normal, window.WindowState,
                        "Baseline: FluenceWindow should start in WindowState.Normal.");

                    SystemCommands.MinimizeWindowCommand.Execute(parameter: null, window);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(WindowState.Minimized, window.WindowState,
                        "MinimizeWindowCommand must drive WindowState to Minimized via OnMinimizeWindow.");
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

        [TestMethod]
        public void FluenceWindow_MaximizeCommand_TransitionsToMaximized()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();
                    Assert.AreEqual(WindowState.Normal, window.WindowState,
                        "Baseline: FluenceWindow should start in WindowState.Normal.");

                    SystemCommands.MaximizeWindowCommand.Execute(parameter: null, window);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(WindowState.Maximized, window.WindowState,
                        "MaximizeWindowCommand must drive WindowState to Maximized via OnMaximizeWindow.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_RestoreCommand_TransitionsMaximizedToNormal()
        {
            RunOnStaThread(static delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();
                    window.WindowState = WindowState.Maximized;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(WindowState.Maximized, window.WindowState,
                        "Baseline: window should be Maximized before Restore is exercised.");

                    SystemCommands.RestoreWindowCommand.Execute(parameter: null, window);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(WindowState.Normal, window.WindowState,
                        "RestoreWindowCommand must drive WindowState back to Normal via OnRestoreWindow.");
                }
                finally
                {
                    window?.Close();
                }
            });
        }

        [TestMethod]
        public void FluenceWindow_CloseCommand_FiresClosingEvent()
        {
            RunOnStaThread(delegate
            {
                _ = EnsureApplication();
                _ = MergeGenericDictionary(Application.Current);

                FluenceWindow? window = null;
                try
                {
                    window = CreateAndShowOffScreenFluenceWindow();
                    bool closingFired = false;
                    window.Closing += delegate { closingFired = true; };

                    SystemCommands.CloseWindowCommand.Execute(parameter: null, window);
                    // OnCloseWindow calls SystemCommands.CloseWindow(this) which posts
                    // WM_SYSCOMMAND/SC_CLOSE via PostMessage. Block at Background priority
                    // so the Win32 message pump processes the queued message and close
                    // flow runs before we assert.
                    _ = window.Dispatcher.Invoke(DispatcherPriority.Background, new Action(delegate { }));

                    Assert.IsTrue(closingFired,
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
