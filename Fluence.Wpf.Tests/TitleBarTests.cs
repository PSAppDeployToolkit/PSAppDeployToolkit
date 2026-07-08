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

using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class TitleBarTests
    {
        [TestInitialize]
        public void Initialize()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        [TestCleanup]
        public void Cleanup()
        {
            WpfTestSta.Invoke(ResetSharedWpfState);
        }

        [TestMethod]
        public void TitleBar_Template_ExposesNavigationButtons()
        {
            RunWithTitleBar(
                static delegate
                {
                    return new Controls.TitleBar
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

                    Assert.AreEqual(Visibility.Visible, backButton.Visibility,
                        "PART_BackButton must be visible when IsBackButtonVisible is true.");
                    Assert.AreEqual(Visibility.Visible, paneToggleButton.Visibility,
                        "PART_PaneToggleButton must be visible when IsPaneToggleButtonVisible is true.");
                    Assert.IsTrue(WindowChrome.GetIsHitTestVisibleInChrome(backButton),
                        "PART_BackButton must opt into WindowChrome hit testing.");
                    Assert.IsTrue(WindowChrome.GetIsHitTestVisibleInChrome(paneToggleButton),
                        "PART_PaneToggleButton must opt into WindowChrome hit testing.");
                });
        }

        [TestMethod]
        public void TitleBar_BackButton_UsesCompactSlot()
        {
            RunWithTitleBar(
                static delegate
                {
                    return new Controls.TitleBar
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

                    Assert.AreEqual(36.0, backButton.ActualWidth, 0.5,
                        "The title-bar back button should use a smaller slot than the pane toggle.");
                    Assert.AreEqual(32.0, backButton.ActualHeight, 0.5,
                        "The title-bar back button should use a smaller height than the pane toggle.");
                    Assert.AreEqual(40.0, paneToggleButton.ActualWidth, 0.5,
                        "The title-bar pane toggle should use the WinUI-canonical 40 px glyph button width.");
                    Assert.AreEqual(36.0, paneToggleButton.ActualHeight, 0.5,
                        "The title-bar pane toggle should keep the compact title-bar glyph height.");

                    System.Windows.Controls.TextBlock? backGlyph = FindVisualChild<System.Windows.Controls.TextBlock>(backButton);
                    Assert.IsNotNull(backGlyph, "Back button should render a glyph text block.");
                    Assert.AreEqual(16.0, backGlyph.ActualWidth, 0.5,
                        "Back glyph should occupy a 16px visual box.");
                    Assert.AreEqual(16.0, backGlyph.ActualHeight, 0.5,
                        "Back glyph should occupy a 16px visual box.");
                });
        }

        [TestMethod]
        public void TitleBar_PaneToggleClick_ExecutesCommandThenRaisesRequested()
        {
            object parameter = new();
            RecordingCommand command = new(canExecute: true);
            int eventCount = 0;
            int commandCountObservedByEvent = -1;

            RunWithTitleBar(
                delegate
                {
                    return new Controls.TitleBar
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

                    Assert.AreEqual(1, command.ExecuteCount,
                        "PaneToggleCommand must execute once when PART_PaneToggleButton is invoked.");
                    Assert.AreSame(parameter, command.LastParameter,
                        "PaneToggleCommandParameter must be passed to PaneToggleCommand.");
                    Assert.AreEqual(1, eventCount,
                        "PaneToggleRequested must be raised once when PART_PaneToggleButton is invoked.");
                    Assert.AreEqual(1, commandCountObservedByEvent,
                        "PaneToggleRequested must be raised after PaneToggleCommand executes.");
                });
        }

        [TestMethod]
        public void TitleBar_BackButtonVisibilityAndCommand_Work()
        {
            object parameter = new();
            RecordingCommand command = new(canExecute: true);
            int eventCount = 0;

            RunWithTitleBar(
                delegate
                {
                    return new Controls.TitleBar
                    {
                        BackCommand = command,
                        BackCommandParameter = parameter,
                    };
                },
                titleBar =>
                {
                    System.Windows.Controls.Button backButton = GetTemplateButton(titleBar, "PART_BackButton");
                    Assert.AreEqual(Visibility.Collapsed, backButton.Visibility,
                        "PART_BackButton must default to collapsed.");

                    titleBar.BackRequested += delegate { eventCount++; };
                    titleBar.IsBackButtonVisible = true;
                    titleBar.UpdateLayout();
                    DrainDispatcher(titleBar.Dispatcher);

                    Assert.AreEqual(Visibility.Visible, backButton.Visibility,
                        "PART_BackButton must become visible when IsBackButtonVisible is true.");

                    InvokeButton(backButton);

                    Assert.AreEqual(1, command.ExecuteCount,
                        "BackCommand must execute once when PART_BackButton is invoked.");
                    Assert.AreSame(parameter, command.LastParameter,
                        "BackCommandParameter must be passed to BackCommand.");
                    Assert.AreEqual(1, eventCount,
                        "BackRequested must be raised once when PART_BackButton is invoked.");
                });
        }

        [TestMethod]
        public void TitleBar_Unloaded_UnsubscribesCommandCanExecuteHandlers()
        {
            RecordingCommand backCommand = new(canExecute: true);
            RecordingCommand paneToggleCommand = new(canExecute: true);

            RunWithTitleBar(
                delegate
                {
                    return new Controls.TitleBar
                    {
                        IsBackButtonVisible = true,
                        IsPaneToggleButtonVisible = true,
                        BackCommand = backCommand,
                        PaneToggleCommand = paneToggleCommand,
                    };
                },
                titleBar =>
                {
                    Assert.AreEqual(1, backCommand.CanExecuteSubscriptionCount,
                        "TitleBar should subscribe to BackCommand.CanExecuteChanged once.");
                    Assert.AreEqual(1, paneToggleCommand.CanExecuteSubscriptionCount,
                        "TitleBar should subscribe to PaneToggleCommand.CanExecuteChanged once.");

                    titleBar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, titleBar));
                    DrainDispatcher(titleBar.Dispatcher);

                    Assert.AreEqual(1, backCommand.CanExecuteUnsubscriptionCount,
                        "TitleBar must unsubscribe from BackCommand.CanExecuteChanged when unloaded.");
                    Assert.AreEqual(1, paneToggleCommand.CanExecuteUnsubscriptionCount,
                        "TitleBar must unsubscribe from PaneToggleCommand.CanExecuteChanged when unloaded.");
                });
        }

        private static void RunWithTitleBar(Func<Controls.TitleBar> titleBarFactory, Action<Controls.TitleBar> testBody)
        {
            RunOnFreshStaThread(delegate
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window? window = null;
                Controls.TitleBar? titleBar = null;

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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = titleBar.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    testBody(titleBar);
                }
                finally
                {
                    if (window is not null)
                    {
                        window.Content = null;
                        window.Close();
                    }

                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        private static System.Windows.Controls.Button GetTemplateButton(Controls.TitleBar titleBar, string partName)
        {
            System.Windows.Controls.Button? button = titleBar.Template.FindName(partName, titleBar) as System.Windows.Controls.Button;
            Assert.IsNotNull(button, partName + " must exist in the TitleBar template.");
            return button;
        }

        private static T? FindVisualChild<T>(DependencyObject parent)
            where T : DependencyObject
        {
            int childCount = VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                {
                    return typedChild;
                }

                T? descendant = FindVisualChild<T>(child);
                if (descendant is not null)
                {
                    return descendant;
                }
            }

            return null;
        }

        private static void InvokeButton(System.Windows.Controls.Button button)
        {
            AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
            IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
            invoke.Invoke();
            DrainDispatcher(button.Dispatcher);
        }

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

        private static ResourceDictionary? MergeGenericDictionary(Application? application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            application?.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            return dictionaries?.Count > 0 ? dictionaries[^1] : null;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        private static void ResetSharedWpfState()
        {
            Application? application = WpfTestSta.EnsureApplication();
            Keyboard.ClearFocus();

            foreach (Window? window in application?.Windows.Cast<Window>() ?? [])
            {
                window.Content = null;
                window.Close();
            }

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            _ = dispatcher.Invoke(DispatcherPriority.Loaded, new Action(static delegate { }));
            _ = dispatcher.Invoke(DispatcherPriority.ContextIdle, new Action(static delegate { }));
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            application?.Resources.Clear();
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
    }
}
