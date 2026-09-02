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
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shell;
using System.Windows.Threading;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public sealed class TitleBarTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(ResetSharedWpfStateAsync));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(ResetSharedWpfStateAsync));
        }

        [Fact]
        public Task TitleBar_Template_ExposesNavigationButtonsAsync()
        {
            return RunWithTitleBarAsync(
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
                    return new Controls.TitleBar
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
                    Assert.Equal(1, backCommand.CanExecuteSubscriptionCount);
                    Assert.Equal(1, paneToggleCommand.CanExecuteSubscriptionCount);

                    titleBar.RaiseEvent(new RoutedEventArgs(FrameworkElement.UnloadedEvent, titleBar));
                    WpfTestSta.DrainDispatcher(titleBar.Dispatcher);

                    Assert.Equal(1, backCommand.CanExecuteUnsubscriptionCount);
                    Assert.Equal(1, paneToggleCommand.CanExecuteUnsubscriptionCount);
                }).ConfigureAwait(true);
        }

        private static Task RunWithTitleBarAsync(Func<Controls.TitleBar> titleBarFactory, Action<Controls.TitleBar> testBody)
        {
            return WpfTestSta.RunOnStaAsync(delegate
            {
                Application application = WpfTestSta.EnsureApplication();
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

                    if (genericDictionary is not null)
                    {
                        _ = application.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        private static System.Windows.Controls.Button GetTemplateButton(Controls.TitleBar titleBar, string partName)
        {
            return Assert.IsType<System.Windows.Controls.Button>(titleBar.Template.FindName(partName, titleBar));
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
            WpfTestSta.DrainDispatcher(button.Dispatcher);
        }

        private static ResourceDictionary? MergeGenericDictionary(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application.Resources.MergedDictionaries;
            return dictionaries?.Count > 0 ? dictionaries[^1] : null;
        }

        private static async Task ResetSharedWpfStateAsync()
        {
            Application application = WpfTestSta.EnsureApplication();
            Keyboard.ClearFocus();

            foreach (Window? window in application.Windows.Cast<Window>() ?? [])
            {
                window.Content = null;
                window.Close();
            }

            Dispatcher dispatcher = Dispatcher.CurrentDispatcher;
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Loaded, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ContextIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
            await dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.ApplicationIdle, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application.Resources.MergedDictionaries.Clear();
            application.Resources.Clear();
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
