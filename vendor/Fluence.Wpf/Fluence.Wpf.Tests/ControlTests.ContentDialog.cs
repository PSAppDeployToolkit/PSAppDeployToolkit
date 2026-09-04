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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.ContentDialog"/> modal dialog:
    /// default style and template parts, adorner-hosted smoke overlay, ShowAsync task
    /// completion, Escape/close handling, click cancellation, and smoke brush theming.
    /// </summary>
    public partial class ControlTests
    {
        private static Window CreateShownContentDialogOwner()
        {
            Window window = new() { Width = 640, Height = 480, Content = new Grid() };
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            window.UpdateLayout();
            return window;
        }

        private static Adorner[]? GetContentDialogOverlayAdorners(Window owner)
        {
            if (owner.Content is not UIElement root)
            {
                return null;
            }

            AdornerLayer? layer = AdornerLayer.GetAdornerLayer(root);
            return layer?.GetAdorners(root);
        }

        private static void RaiseKeyEvent(UIElement target, Key key, RoutedEvent routedEvent)
        {
            PresentationSource source = Assert.IsType<PresentationSource>(PresentationSource.FromVisual(target), exactMatch: false);
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = routedEvent,
            });
        }

        [Fact]
        public Task ContentDialog_DefaultStyle_AppliesAndTemplatePartsFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog defaults = new();
                Assert.Equal(string.Empty, defaults.PrimaryButtonText, StringComparer.Ordinal);
                Assert.Equal(string.Empty, defaults.SecondaryButtonText, StringComparer.Ordinal);
                Assert.Equal(string.Empty, defaults.CloseButtonText, StringComparer.Ordinal);
                Assert.Equal(ContentDialogButton.None, defaults.DefaultButton);
                Assert.True(defaults.IsPrimaryButtonEnabled, "IsPrimaryButtonEnabled must default to true.");
                Assert.True(defaults.IsSecondaryButtonEnabled, "IsSecondaryButtonEnabled must default to true.");

                Controls.ContentDialog dialog = new()
                {
                    Title = "Title",
                    Content = "Body",
                    PrimaryButtonText = "Save",
                    SecondaryButtonText = "Maybe",
                    CloseButtonText = "Cancel",
                };
                Window window = new() { Width = 640, Height = 480, Content = dialog };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, dialog.Visibility);

                    // The collapsed at-rest dialog is skipped by layout, so inflate the
                    // template explicitly to assert the template contract.
                    _ = dialog.ApplyTemplate();

                    Assert.Equal(548.0, dialog.MaxWidth, 0.01);
                    Assert.Equal(320.0, dialog.MinWidth, 0.01);

                    Border surface = Assert.IsType<Border>(FindVisualChildByName<Border>(dialog, "DialogSurface"), exactMatch: false);
                    CornerRadius? overlayRadius = (CornerRadius?)app.FindResource("OverlayCornerRadius");
                    Assert.Equal(overlayRadius, surface.CornerRadius);

                    ButtonBase primary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton"), exactMatch: false);
                    ButtonBase secondary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_SecondaryButton"), exactMatch: false);
                    ButtonBase close = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, primary.Visibility);
                    Assert.Equal(Visibility.Visible, secondary.Visibility);
                    Assert.Equal(Visibility.Visible, close.Visibility);

                    dialog.SecondaryButtonText = string.Empty;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Visibility.Collapsed, secondary.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_DeclaredAsWindowContentChild_CollapsedAtRestAndShowsViaShowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Grid host = new();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Declared",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };
                _ = host.Children.Add(dialog);
                Window window = new() { Width = 640, Height = 480, Content = host };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Visibility.Collapsed, dialog.Visibility);
                    Assert.Equal(0.0, dialog.ActualHeight, 0.001);

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true),
                        "ShowAsync on a declared dialog must succeed and apply the template once overlay-hosted.");
                    Assert.Equal(Visibility.Visible, dialog.Visibility);
                    Assert.False(host.Children.Contains(dialog),
                        "ShowAsync must detach the declared dialog from its XAML parent.");

                    dialog.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true),
                        "Hide must complete the pending ShowAsync task for a declared dialog.");
                    Assert.Equal(Visibility.Collapsed, dialog.Visibility);
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_EnterInAcceptsReturnTextBox_DoesNotInvokeDefaultButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                TextBox body = new() { AcceptsReturn = true, MinLines = 3 };
                Controls.ContentDialog dialog = new()
                {
                    Title = "Notes",
                    Content = body,
                    PrimaryButtonText = "Save",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                };

                try
                {
                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before Enter is simulated.");

                    _ = body.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Real key input tunnels the preview event first and then bubbles the key
                    // down event. The multiline TextBox consumes the bubbling Enter, so the
                    // dialog must leave it alone.
                    RaiseKeyEvent(body, Key.Enter, UIElement.PreviewKeyDownEvent);
                    RaiseKeyEvent(body, Key.Enter, UIElement.KeyDownEvent);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(task.IsCompleted,
                        "Enter inside an AcceptsReturn TextBox must not invoke the default button while DefaultButton=Primary.");
                    Assert.True(GetContentDialogOverlayAdorners(window) is { Length: > 0 },
                        "The dialog must stay open after Enter is consumed by the multiline TextBox.");

                    dialog.Hide();
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_EnterWithDefaultButton_InvokesDefaultViaBubblingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                    DefaultButton = ContentDialogButton.Primary,
                };

                try
                {
                    bool clickRaised = false;
                    dialog.PrimaryButtonClick += (_, _) => clickRaised = true;

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before Enter is simulated.");

                    // Move focus off the command buttons so the default-button shortcut path
                    // (not the native button click) handles Enter.
                    _ = dialog.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(dialog, Key.Enter, UIElement.PreviewKeyDownEvent);
                    RaiseKeyEvent(dialog, Key.Enter, UIElement.KeyDownEvent);

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true),
                        "Enter must invoke the default button through the bubbling key event.");
                    Assert.True(clickRaised, "Enter must raise PrimaryButtonClick while DefaultButton=Primary.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task ContentDialog_OwnerWindowClose_CompletesPendingTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            await WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    dialogTask = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before the owner window closes.");

                    window.Close();

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => dialogTask.IsCompleted).ConfigureAwait(true),
                        "Closing the owner window must complete the pending ShowAsync task.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            }).ConfigureAwait(true);

            Assert.NotNull(dialogTask);
            ContentDialogResult result = await dialogTask.ConfigureAwait(true);
            Assert.Equal(ContentDialogResult.None, result);
        }

        [Fact]
        public Task ContentDialog_Hide_PlaysDialogHiddenExitThenCompletesTaskAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Exiting",
                    Content = "Body",
                    CloseButtonText = "Close",
                };

                try
                {
                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before Hide is called.");

                    dialog.Hide();

                    // The DialogHidden exit runs asynchronously: input dies instantly
                    // (WinUI's discrete IsHitTestVisible keyframe at time zero) while the
                    // surface animates out, so on this same dispatcher frame the task must
                    // still be pending.
                    Assert.False(dialog.IsHitTestVisible,
                        "The closing dialog must stop hit testing the moment the close starts.");
                    Assert.False(task.IsCompleted,
                        "The ShowAsync task must stay pending until the DialogHidden exit completes.");

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true),
                        "The ShowAsync task must complete once the 167 ms DialogHidden exit settles.");
                    Assert.True(dialog.IsHitTestVisible,
                        "The teardown must restore hit testing so a reshown dialog is interactive.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => GetContentDialogOverlayAdorners(window) is not { Length: > 0 }).ConfigureAwait(true),
                        "The teardown must remove the modal overlay after the exit.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task ContentDialog_DoubleHide_CompletesExactlyOnceAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            int closedCount = 0;
            await WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Mashed",
                    Content = "Body",
                    CloseButtonText = "Close",
                };
                dialog.Closed += (_, _) => closedCount++;

                try
                {
                    dialogTask = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true),
                        "The dialog template must apply before the double close.");

                    // The second Hide lands while the DialogHidden exit is playing and must
                    // be ignored by the closing guard.
                    dialog.Hide();
                    dialog.Hide();

                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => dialogTask.IsCompleted).ConfigureAwait(true),
                        "The double close must still complete the ShowAsync task.");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            }).ConfigureAwait(true);

            Assert.NotNull(dialogTask);
            ContentDialogResult result = await dialogTask.ConfigureAwait(true);
            Assert.Equal(ContentDialogResult.None, result);
            Assert.Equal(1, closedCount);
        }

        [Fact]
        public Task ContentDialog_ShowAsync_AddsOverlayAdornerAndReturnsPendingTaskAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    bool openedRaised = false;
                    dialog.Opened += (_, _) => openedRaised = true;

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.False(task.IsCompleted, "ShowAsync must return a task that stays pending until the dialog closes.");
                    Assert.True(openedRaised, "ShowAsync must raise Opened once the overlay has been added.");

                    bool overlayAdded = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is { Length: > 0 }).ConfigureAwait(true);
                    Assert.True(overlayAdded, "ShowAsync must add the modal overlay adorner to the owner window content.");

                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The adorner-hosted dialog must apply its template once layout has run.");
                    Assert.False(task.IsCompleted, "The ShowAsync task must still be pending while the dialog is open.");

                    dialog.Hide();
                    bool completed = await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true);
                    Assert.True(completed, "Hide must complete the pending ShowAsync task.");
                    bool overlayRemoved = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 }).ConfigureAwait(true);
                    Assert.True(overlayRemoved, "Hide must remove the modal overlay adorner from the owner window content.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public async Task ContentDialog_PrimaryButtonClick_CompletesTaskWithPrimaryAndRemovesOverlayAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            await WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    bool clickRaised = false;
                    bool closedRaised = false;
                    dialog.PrimaryButtonClick += (_, _) => clickRaised = true;
                    dialog.Closed += (_, _) => closedRaised = true;

                    dialogTask = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before the primary button can be clicked.");

                    ButtonBase primary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton"), exactMatch: false);
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    bool completed = await WaitUntilAsync(window.Dispatcher, 2000, () => dialogTask.IsCompleted).ConfigureAwait(true);
                    Assert.True(completed, "Clicking the primary button must complete the ShowAsync task.");
                    Assert.True(clickRaised, "Clicking the primary button must raise PrimaryButtonClick.");
                    Assert.True(closedRaised, "Closing via the primary button must raise Closed.");

                    bool overlayRemoved = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 }).ConfigureAwait(true);
                    Assert.True(overlayRemoved, "Closing via the primary button must remove the modal overlay adorner.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            }).ConfigureAwait(true);

            Assert.NotNull(dialogTask);
            ContentDialogResult result = await dialogTask.ConfigureAwait(true);
            Assert.Equal(ContentDialogResult.Primary, result);
        }

        [Fact]
        public async Task ContentDialog_CloseButtonClick_CompletesTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            await WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    bool clickRaised = false;
                    dialog.CloseButtonClick += (_, _) => clickRaised = true;

                    dialogTask = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before the close button can be clicked.");

                    ButtonBase close = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton"), exactMatch: false);
                    close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    bool completed = await WaitUntilAsync(window.Dispatcher, 2000, () => dialogTask.IsCompleted).ConfigureAwait(true);
                    Assert.True(completed, "Clicking the close button must complete the ShowAsync task.");
                    Assert.True(clickRaised, "Clicking the close button must raise CloseButtonClick.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            }).ConfigureAwait(true);

            Assert.NotNull(dialogTask);
            ContentDialogResult result = await dialogTask.ConfigureAwait(true);
            Assert.Equal(ContentDialogResult.None, result);
        }

        [Fact]
        public async Task ContentDialog_EscapeKey_CompletesTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            await WpfTestSta.RunOnStaAsync(async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    dialogTask = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before Escape is simulated.");

                    dialog.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Escape)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });

                    bool completed = await WaitUntilAsync(window.Dispatcher, 2000, () => dialogTask.IsCompleted).ConfigureAwait(true);
                    Assert.True(completed, "Pressing Escape must complete the ShowAsync task.");
                    bool overlayRemoved = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 }).ConfigureAwait(true);
                    Assert.True(overlayRemoved, "Pressing Escape must remove the modal overlay adorner.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            }).ConfigureAwait(true);

            Assert.NotNull(dialogTask);
            ContentDialogResult result = await dialogTask.ConfigureAwait(true);
            Assert.Equal(ContentDialogResult.None, result);
        }

        [Fact]
        public Task ContentDialog_CancelingPrimaryButtonClick_KeepsDialogOpenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Confirm",
                    Content = "Body",
                    PrimaryButtonText = "OK",
                    CloseButtonText = "Cancel",
                };

                try
                {
                    dialog.PrimaryButtonClick += static (_, args) => args.Cancel = true;

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before the primary button can be clicked.");

                    ButtonBase primary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton"), exactMatch: false);
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(task.IsCompleted, "A canceled PrimaryButtonClick must keep the ShowAsync task pending.");
                    Assert.True(GetContentDialogOverlayAdorners(window) is { Length: > 0 },
                        "A canceled PrimaryButtonClick must keep the modal overlay adorner in place.");

                    dialog.Hide();
                    bool completed = await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true);
                    Assert.True(completed, "Hide must still complete the task after a canceled button click.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_SmokeFillBrush_ResolvesAcrossThemeCycleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                SolidColorBrush smoke = Assert.IsType<SolidColorBrush>(app.TryFindResource("SmokeFillColorDefaultBrush"));
                Assert.Equal(Color.FromArgb(0x4D, 0x00, 0x00, 0x00), smoke.Color);

                Color smokeColor = Assert.IsType<Color>(app.TryFindResource("SmokeFillColorDefault") as Color?, exactMatch: false);
            });
        }

        [Fact]
        public Task ContentDialog_WhileOpen_BlocksPointerInputOutsideDialogAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Button behind = new() { Content = "Behind" };
                Window window = new() { Width = 640, Height = 480, Content = behind };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel",
                    };

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before input is simulated.");

                    // A press on a control outside the dialog (standing in for a title-bar
                    // search box) must be swallowed while the dialog is modal.
                    MouseButtonEventArgs outside = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    behind.RaiseEvent(outside);
                    Assert.True(outside.Handled, "Pointer input outside the open dialog must be blocked.");

                    // A press on the dialog's own button must pass through.
                    ButtonBase primary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton"), exactMatch: false);
                    MouseButtonEventArgs inside = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    primary.RaiseEvent(inside);
                    Assert.False(inside.Handled, "Pointer input on the dialog itself must not be blocked.");

                    // The owner stays modal while the DialogHidden exit plays, so wait for
                    // the close to complete before asserting input flows again.
                    dialog.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true),
                        "Hide must complete the ShowAsync task once the exit settles.");

                    // After the dialog closes, input outside it flows normally again.
                    MouseButtonEventArgs afterClose = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    behind.RaiseEvent(afterClose);
                    Assert.False(afterClose.Handled, "Once the dialog closes, owner input must no longer be blocked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_WhileOpen_BlocksKeyInputOutsideDialogAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                TextBox behind = new() { Text = "Behind" };
                Window window = new() { Width = 640, Height = 480, Content = behind };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel",
                    };

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = await WaitUntilAsync(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null).ConfigureAwait(true);
                    Assert.True(templated, "The dialog template must apply before key input is simulated.");

                    PresentationSource source = Assert.IsType<PresentationSource>(PresentationSource.FromVisual(window), exactMatch: false);

                    // A key press sourced outside the dialog (standing in for a title-bar
                    // search box that still holds keyboard focus) must be swallowed.
                    KeyEventArgs outside = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    behind.RaiseEvent(outside);
                    Assert.True(outside.Handled, "Key input outside the open dialog must be blocked.");

                    // A key press sourced inside the dialog must pass through so the dialog's
                    // own Tab cycle and key handling keep working.
                    ButtonBase primary = Assert.IsType<ButtonBase>(FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton"), exactMatch: false);
                    KeyEventArgs inside = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    primary.RaiseEvent(inside);
                    Assert.False(inside.Handled, "Key input inside the dialog must not be blocked.");

                    // The owner stays modal while the DialogHidden exit plays, so wait for
                    // the close to complete before asserting key input flows again.
                    dialog.Hide();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000, () => task.IsCompleted).ConfigureAwait(true),
                        "Hide must complete the ShowAsync task once the exit settles.");

                    // After the dialog closes, key input outside it flows normally again.
                    KeyEventArgs afterClose = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    behind.RaiseEvent(afterClose);
                    Assert.False(afterClose.Handled, "Once the dialog closes, owner key input must no longer be blocked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_Open_UsesSurfaceStrokeAndPlaysEntranceAnimationAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = CreateShownContentDialogOwner();
                Controls.ContentDialog dialog = new()
                {
                    Title = "Animated",
                    Content = "Body",
                    CloseButtonText = "Close",
                };

                try
                {
                    _ = dialog.ShowAsync();
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => FindVisualChildByName<Border>(dialog, "DialogSurface") is not null).ConfigureAwait(true),
                        "The dialog template must apply once overlay-hosted.");

                    // C1: the outer dialog stroke is the WinUI ContentDialogBorderBrush.
                    Border surface = Assert.IsType<Border>(FindVisualChildByName<Border>(dialog, "DialogSurface"), exactMatch: false);
                    Assert.Same(app.TryFindResource("SurfaceStrokeColorDefaultBrush"), surface.BorderBrush);

                    // C2: the entrance animates opacity 0->1 and scale 1.05->1.0 around the center.
                    Assert.Equal(new Point(0.5, 0.5), dialog.RenderTransformOrigin);
                    ScaleTransform scale = Assert.IsType<ScaleTransform>(dialog.RenderTransform);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                            () => dialog.Opacity >= 1.0 && scale.ScaleX <= 1.0 && scale.ScaleY <= 1.0).ConfigureAwait(true),
                        "The entrance animation must settle at full opacity and 1.0 scale.");

                    dialog.Hide();
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_OverFluenceWindow_HostsOverlayAboveTheWholeWindowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.FluenceWindow window = new()
                {
                    Width = 640,
                    Height = 480,
                    Content = new Grid(),
                    TitleBar = new TextBox(),
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Panel host =
                        Assert.IsType<Panel>(window.Template?.FindName("PART_DialogOverlayHost", window), exactMatch: false);

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        CloseButtonText = "Cancel",
                    };

                    _ = dialog.ShowAsync();
                    bool hosted = await WaitUntilAsync(window.Dispatcher, 2000, () => host.Children.Count > 0).ConfigureAwait(true);
                    Assert.True(hosted,
                        "Over a FluenceWindow the dialog overlay must be hosted in PART_DialogOverlayHost so the smoke covers the title bar.");

                    dialog.Hide();
                    bool removed = await WaitUntilAsync(window.Dispatcher, 2000, () => host.Children.Count is 0).ConfigureAwait(true);
                    Assert.True(removed, "Closing the dialog must remove the overlay from PART_DialogOverlayHost.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ContentDialog_DeclaresAssertiveLiveSettingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog dialog = new() { Title = "Confirm" };

                // A modal dialog is not a real HWND, so nothing prompts Narrator to read it on
                // open. The net472 target has no AutomationProperties.IsDialog, so the dialog
                // instead declares an assertive live region and announces it via
                // LiveRegionChanged as it appears (see ContentDialog.AnnounceLiveRegion).
                Assert.Equal(AutomationLiveSetting.Assertive, AutomationProperties.GetLiveSetting(dialog));
            });
        }

        [Fact]
        public Task ContentDialog_AutomationPeer_ReportsWindowRoleAndTitleNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog dialog = new() { Title = "Delete file?" };
                Window window = new() { Width = 320, Height = 240, Content = dialog };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(dialog);
                    _ = Assert.IsType<Automation.ContentDialogAutomationPeer>(peer, exactMatch: false);
                    Assert.Equal(AutomationControlType.Window, peer.GetAutomationControlType());
                    Assert.Equal("Delete file?", peer.GetName(), StringComparer.Ordinal);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
