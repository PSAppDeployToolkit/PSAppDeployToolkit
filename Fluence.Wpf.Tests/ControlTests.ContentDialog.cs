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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;

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
            DrainDispatcher(window.Dispatcher);
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
            PresentationSource? source = PresentationSource.FromVisual(target);
            Assert.IsNotNull(source, "The key event target must be connected to a presentation source.");
            target.RaiseEvent(new KeyEventArgs(Keyboard.PrimaryDevice, source, 0, key)
            {
                RoutedEvent = routedEvent,
            });
        }

        [TestMethod]
        public void ContentDialog_DefaultStyle_AppliesAndTemplatePartsFound()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog defaults = new();
                Assert.AreEqual(string.Empty, defaults.PrimaryButtonText, "PrimaryButtonText must default to an empty string.");
                Assert.AreEqual(string.Empty, defaults.SecondaryButtonText, "SecondaryButtonText must default to an empty string.");
                Assert.AreEqual(string.Empty, defaults.CloseButtonText, "CloseButtonText must default to an empty string.");
                Assert.AreEqual(ContentDialogButton.None, defaults.DefaultButton, "DefaultButton must default to None.");
                Assert.IsTrue(defaults.IsPrimaryButtonEnabled, "IsPrimaryButtonEnabled must default to true.");
                Assert.IsTrue(defaults.IsSecondaryButtonEnabled, "IsSecondaryButtonEnabled must default to true.");

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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, dialog.Visibility,
                        "A dialog declared in window XAML must stay collapsed until ShowAsync hosts it in its modal overlay.");

                    // The collapsed at-rest dialog is skipped by layout, so inflate the
                    // template explicitly to assert the template contract.
                    _ = dialog.ApplyTemplate();

                    Assert.AreEqual(548.0, dialog.MaxWidth, 0.01, "ContentDialog.MaxWidth must be the WinUI ContentDialogMaxWidth (548).");
                    Assert.AreEqual(320.0, dialog.MinWidth, 0.01, "ContentDialog.MinWidth must be the WinUI ContentDialogMinWidth (320).");

                    Border? surface = FindVisualChildByName<Border>(dialog, "DialogSurface");
                    Assert.IsNotNull(surface, "DialogSurface must exist in the ContentDialog template (Fluence style applied).");
                    CornerRadius? overlayRadius = (CornerRadius?)app?.FindResource("OverlayCornerRadius");
                    Assert.AreEqual(overlayRadius, surface.CornerRadius, "ContentDialog surface must use OverlayCornerRadius like the other Fluent overlays.");

                    ButtonBase? primary = FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton");
                    ButtonBase? secondary = FindVisualChildByName<ButtonBase>(dialog, "PART_SecondaryButton");
                    ButtonBase? close = FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton");
                    Assert.IsNotNull(primary, "PART_PrimaryButton must exist in the ContentDialog template.");
                    Assert.IsNotNull(secondary, "PART_SecondaryButton must exist in the ContentDialog template.");
                    Assert.IsNotNull(close, "PART_CloseButton must exist in the ContentDialog template.");
                    Assert.AreEqual(Visibility.Visible, primary.Visibility, "Primary button must be visible when PrimaryButtonText is set.");
                    Assert.AreEqual(Visibility.Visible, secondary.Visibility, "Secondary button must be visible when SecondaryButtonText is set.");
                    Assert.AreEqual(Visibility.Visible, close.Visibility, "Close button must be visible when CloseButtonText is set.");

                    dialog.SecondaryButtonText = string.Empty;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Visibility.Collapsed, secondary.Visibility, "A command button must collapse when its text is empty.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_DeclaredAsWindowContentChild_CollapsedAtRestAndShowsViaShow()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Visibility.Collapsed, dialog.Visibility,
                        "A dialog declared in window XAML must be collapsed at rest.");
                    Assert.AreEqual(0.0, dialog.ActualHeight, 0.001,
                        "A dialog declared in window XAML must occupy no layout height at rest.");

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null),
                        "ShowAsync on a declared dialog must succeed and apply the template once overlay-hosted.");
                    Assert.AreEqual(Visibility.Visible, dialog.Visibility,
                        "The dialog must be visible while it is hosted in its modal overlay.");
                    Assert.IsFalse(host.Children.Contains(dialog),
                        "ShowAsync must detach the declared dialog from its XAML parent.");

                    dialog.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted),
                        "Hide must complete the pending ShowAsync task for a declared dialog.");
                    Assert.AreEqual(Visibility.Collapsed, dialog.Visibility,
                        "Closing must collapse the dialog again so it renders nothing at rest.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_EnterInAcceptsReturnTextBox_DoesNotInvokeDefaultButton()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null),
                        "The dialog template must apply before Enter is simulated.");

                    _ = body.Focus();
                    DrainDispatcher(window.Dispatcher);

                    // Real key input tunnels the preview event first and then bubbles the key
                    // down event. The multiline TextBox consumes the bubbling Enter, so the
                    // dialog must leave it alone.
                    RaiseKeyEvent(body, Key.Enter, UIElement.PreviewKeyDownEvent);
                    RaiseKeyEvent(body, Key.Enter, UIElement.KeyDownEvent);
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(task.IsCompleted,
                        "Enter inside an AcceptsReturn TextBox must not invoke the default button while DefaultButton=Primary.");
                    Assert.IsTrue(GetContentDialogOverlayAdorners(window) is { Length: > 0 },
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

        [TestMethod]
        public void ContentDialog_EnterWithDefaultButton_InvokesDefaultViaBubbling()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null),
                        "The dialog template must apply before Enter is simulated.");

                    // Move focus off the command buttons so the default-button shortcut path
                    // (not the native button click) handles Enter.
                    _ = dialog.Focus();
                    DrainDispatcher(window.Dispatcher);

                    RaiseKeyEvent(dialog, Key.Enter, UIElement.PreviewKeyDownEvent);
                    RaiseKeyEvent(dialog, Key.Enter, UIElement.KeyDownEvent);

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted),
                        "Enter must invoke the default button through the bubbling key event.");
                    Assert.IsTrue(clickRaised, "Enter must raise PrimaryButtonClick while DefaultButton=Primary.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public async Task ContentDialog_OwnerWindowClose_CompletesPendingTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null),
                        "The dialog template must apply before the owner window closes.");

                    window.Close();

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => dialogTask.IsCompleted),
                        "Closing the owner window must complete the pending ShowAsync task.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });

            Assert.IsNotNull(dialogTask, "ShowAsync must have produced a dialog task.");
            ContentDialogResult result = await dialogTask.ConfigureAwait(false);
            Assert.AreEqual(ContentDialogResult.None, result,
                "An owner window close must complete the task with ContentDialogResult.None.");
        }

        [TestMethod]
        public void ContentDialog_Hide_PlaysDialogHiddenExitThenCompletesTask()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null),
                        "The dialog template must apply before Hide is called.");

                    dialog.Hide();

                    // The DialogHidden exit runs asynchronously: input dies instantly
                    // (WinUI's discrete IsHitTestVisible keyframe at time zero) while the
                    // surface animates out, so on this same dispatcher frame the task must
                    // still be pending.
                    Assert.IsFalse(dialog.IsHitTestVisible,
                        "The closing dialog must stop hit testing the moment the close starts.");
                    Assert.IsFalse(task.IsCompleted,
                        "The ShowAsync task must stay pending until the DialogHidden exit completes.");

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted),
                        "The ShowAsync task must complete once the 167 ms DialogHidden exit settles.");
                    Assert.IsTrue(dialog.IsHitTestVisible,
                        "The teardown must restore hit testing so a reshown dialog is interactive.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => GetContentDialogOverlayAdorners(window) is not { Length: > 0 }),
                        "The teardown must remove the modal overlay after the exit.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public async Task ContentDialog_DoubleHide_CompletesExactlyOnceAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            int closedCount = 0;
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null),
                        "The dialog template must apply before the double close.");

                    // The second Hide lands while the DialogHidden exit is playing and must
                    // be ignored by the closing guard.
                    dialog.Hide();
                    dialog.Hide();

                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => dialogTask.IsCompleted),
                        "The double close must still complete the ShowAsync task.");
                    DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });

            Assert.IsNotNull(dialogTask, "ShowAsync must have produced a dialog task.");
            ContentDialogResult result = await dialogTask.ConfigureAwait(false);
            Assert.AreEqual(ContentDialogResult.None, result,
                "Hide must complete the task with ContentDialogResult.None.");
            Assert.AreEqual(1, closedCount, "A double Hide must raise Closed exactly once.");
        }

        [TestMethod]
        public void ContentDialog_ShowAsync_AddsOverlayAdornerAndReturnsPendingTask()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsFalse(task.IsCompleted, "ShowAsync must return a task that stays pending until the dialog closes.");
                    Assert.IsTrue(openedRaised, "ShowAsync must raise Opened once the overlay has been added.");

                    bool overlayAdded = WaitUntil(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is { Length: > 0 });
                    Assert.IsTrue(overlayAdded, "ShowAsync must add the modal overlay adorner to the owner window content.");

                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null);
                    Assert.IsTrue(templated, "The adorner-hosted dialog must apply its template once layout has run.");
                    Assert.IsFalse(task.IsCompleted, "The ShowAsync task must still be pending while the dialog is open.");

                    dialog.Hide();
                    bool completed = WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted);
                    Assert.IsTrue(completed, "Hide must complete the pending ShowAsync task.");
                    bool overlayRemoved = WaitUntil(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 });
                    Assert.IsTrue(overlayRemoved, "Hide must remove the modal overlay adorner from the owner window content.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public async Task ContentDialog_PrimaryButtonClick_CompletesTaskWithPrimaryAndRemovesOverlayAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before the primary button can be clicked.");

                    ButtonBase? primary = FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton");
                    Assert.IsNotNull(primary, "PART_PrimaryButton must be present in the open dialog.");
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    bool completed = WaitUntil(window.Dispatcher, 2000, () => dialogTask.IsCompleted);
                    Assert.IsTrue(completed, "Clicking the primary button must complete the ShowAsync task.");
                    Assert.IsTrue(clickRaised, "Clicking the primary button must raise PrimaryButtonClick.");
                    Assert.IsTrue(closedRaised, "Closing via the primary button must raise Closed.");

                    bool overlayRemoved = WaitUntil(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 });
                    Assert.IsTrue(overlayRemoved, "Closing via the primary button must remove the modal overlay adorner.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });

            Assert.IsNotNull(dialogTask, "ShowAsync must have produced a dialog task.");
            ContentDialogResult result = await dialogTask.ConfigureAwait(false);
            Assert.AreEqual(ContentDialogResult.Primary, result, "Clicking the primary button must complete the task with ContentDialogResult.Primary.");
        }

        [TestMethod]
        public async Task ContentDialog_CloseButtonClick_CompletesTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before the close button can be clicked.");

                    ButtonBase? close = FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton");
                    Assert.IsNotNull(close, "PART_CloseButton must be present in the open dialog.");
                    close.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));

                    bool completed = WaitUntil(window.Dispatcher, 2000, () => dialogTask.IsCompleted);
                    Assert.IsTrue(completed, "Clicking the close button must complete the ShowAsync task.");
                    Assert.IsTrue(clickRaised, "Clicking the close button must raise CloseButtonClick.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });

            Assert.IsNotNull(dialogTask, "ShowAsync must have produced a dialog task.");
            ContentDialogResult result = await dialogTask.ConfigureAwait(false);
            Assert.AreEqual(ContentDialogResult.None, result, "Clicking the close button must complete the task with ContentDialogResult.None.");
        }

        [TestMethod]
        public async Task ContentDialog_EscapeKey_CompletesTaskWithNoneAsync()
        {
            Task<ContentDialogResult>? dialogTask = null;
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_CloseButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before Escape is simulated.");

                    dialog.RaiseEvent(new KeyEventArgs(
                        Keyboard.PrimaryDevice,
                        PresentationSource.FromVisual(window),
                        0,
                        Key.Escape)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    });

                    bool completed = WaitUntil(window.Dispatcher, 2000, () => dialogTask.IsCompleted);
                    Assert.IsTrue(completed, "Pressing Escape must complete the ShowAsync task.");
                    bool overlayRemoved = WaitUntil(window.Dispatcher, 2000,
                        () => GetContentDialogOverlayAdorners(window) is null or { Length: 0 });
                    Assert.IsTrue(overlayRemoved, "Pressing Escape must remove the modal overlay adorner.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });

            Assert.IsNotNull(dialogTask, "ShowAsync must have produced a dialog task.");
            ContentDialogResult result = await dialogTask.ConfigureAwait(false);
            Assert.AreEqual(ContentDialogResult.None, result, "Pressing Escape must complete the task with ContentDialogResult.None.");
        }

        [TestMethod]
        public void ContentDialog_CancelingPrimaryButtonClick_KeepsDialogOpen()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    dialog.PrimaryButtonClick += (_, args) => args.Cancel = true;

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before the primary button can be clicked.");

                    ButtonBase? primary = FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton");
                    Assert.IsNotNull(primary, "PART_PrimaryButton must be present in the open dialog.");
                    primary.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(task.IsCompleted, "A canceled PrimaryButtonClick must keep the ShowAsync task pending.");
                    Assert.IsTrue(GetContentDialogOverlayAdorners(window) is { Length: > 0 },
                        "A canceled PrimaryButtonClick must keep the modal overlay adorner in place.");

                    dialog.Hide();
                    bool completed = WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted);
                    Assert.IsTrue(completed, "Hide must still complete the task after a canceled button click.");
                }
                finally
                {
                    dialog.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_SmokeFillBrush_ResolvesAcrossThemeCycle()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                ThemeTestHelpers.ApplyStandardThemeCycle();

                SolidColorBrush? smoke = app?.TryFindResource("SmokeFillColorDefaultBrush") as SolidColorBrush;
                Assert.IsNotNull(smoke, "SmokeFillColorDefaultBrush must resolve after a full Light/Dark/HighContrast/Light theme cycle.");
                Assert.AreEqual(Color.FromArgb(0x4D, 0x00, 0x00, 0x00), smoke.Color,
                    "SmokeFillColorDefaultBrush must rebuild to the Light-theme smoke color (#4D000000) at the end of the cycle.");

                Color? smokeColor = app?.TryFindResource("SmokeFillColorDefault") as Color?;
                Assert.IsNotNull(smokeColor, "SmokeFillColorDefault color token must resolve so BrushFactory can auto-twin it.");
            });
        }

        [TestMethod]
        public void ContentDialog_WhileOpen_BlocksPointerInputOutsideDialog()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Button behind = new() { Content = "Behind" };
                Window window = new() { Width = 640, Height = 480, Content = behind };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel",
                    };

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before input is simulated.");

                    // A press on a control outside the dialog (standing in for a title-bar
                    // search box) must be swallowed while the dialog is modal.
                    MouseButtonEventArgs outside = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    behind.RaiseEvent(outside);
                    Assert.IsTrue(outside.Handled, "Pointer input outside the open dialog must be blocked.");

                    // A press on the dialog's own button must pass through.
                    ButtonBase? primary = FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton");
                    Assert.IsNotNull(primary, "PART_PrimaryButton must be present in the open dialog.");
                    MouseButtonEventArgs inside = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    primary.RaiseEvent(inside);
                    Assert.IsFalse(inside.Handled, "Pointer input on the dialog itself must not be blocked.");

                    // The owner stays modal while the DialogHidden exit plays, so wait for
                    // the close to complete before asserting input flows again.
                    dialog.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted),
                        "Hide must complete the ShowAsync task once the exit settles.");

                    // After the dialog closes, input outside it flows normally again.
                    MouseButtonEventArgs afterClose = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseDownEvent,
                    };
                    behind.RaiseEvent(afterClose);
                    Assert.IsFalse(afterClose.Handled, "Once the dialog closes, owner input must no longer be blocked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_WhileOpen_BlocksKeyInputOutsideDialog()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                TextBox behind = new() { Text = "Behind" };
                Window window = new() { Width = 640, Height = 480, Content = behind };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        PrimaryButtonText = "OK",
                        CloseButtonText = "Cancel",
                    };

                    Task<ContentDialogResult> task = dialog.ShowAsync();
                    bool templated = WaitUntil(window.Dispatcher, 2000,
                        () => FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton") is not null);
                    Assert.IsTrue(templated, "The dialog template must apply before key input is simulated.");

                    PresentationSource? source = PresentationSource.FromVisual(window);
                    Assert.IsNotNull(source, "The owner window must have a presentation source once shown.");

                    // A key press sourced outside the dialog (standing in for a title-bar
                    // search box that still holds keyboard focus) must be swallowed.
                    KeyEventArgs outside = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    behind.RaiseEvent(outside);
                    Assert.IsTrue(outside.Handled, "Key input outside the open dialog must be blocked.");

                    // A key press sourced inside the dialog must pass through so the dialog's
                    // own Tab cycle and key handling keep working.
                    ButtonBase? primary = FindVisualChildByName<ButtonBase>(dialog, "PART_PrimaryButton");
                    Assert.IsNotNull(primary, "PART_PrimaryButton must be present in the open dialog.");
                    KeyEventArgs inside = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    primary.RaiseEvent(inside);
                    Assert.IsFalse(inside.Handled, "Key input inside the dialog must not be blocked.");

                    // The owner stays modal while the DialogHidden exit plays, so wait for
                    // the close to complete before asserting key input flows again.
                    dialog.Hide();
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => task.IsCompleted),
                        "Hide must complete the ShowAsync task once the exit settles.");

                    // After the dialog closes, key input outside it flows normally again.
                    KeyEventArgs afterClose = new(Keyboard.PrimaryDevice, source, 0, Key.A)
                    {
                        RoutedEvent = UIElement.PreviewKeyDownEvent,
                    };
                    behind.RaiseEvent(afterClose);
                    Assert.IsFalse(afterClose.Handled, "Once the dialog closes, owner key input must no longer be blocked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_Open_UsesSurfaceStrokeAndPlaysEntranceAnimation()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => FindVisualChildByName<Border>(dialog, "DialogSurface") is not null),
                        "The dialog template must apply once overlay-hosted.");

                    // C1: the outer dialog stroke is the WinUI ContentDialogBorderBrush.
                    Border? surface = FindVisualChildByName<Border>(dialog, "DialogSurface");
                    Assert.IsNotNull(surface, "DialogSurface must exist in the ContentDialog template.");
                    Assert.AreSame(app?.TryFindResource("SurfaceStrokeColorDefaultBrush"), surface.BorderBrush,
                        "The dialog's outer BorderBrush must resolve to SurfaceStrokeColorDefaultBrush (WinUI ContentDialogBorderBrush).");

                    // C2: the entrance animates opacity 0->1 and scale 1.05->1.0 around the center.
                    Assert.AreEqual(new Point(0.5, 0.5), dialog.RenderTransformOrigin,
                        "The entrance animation must scale around the dialog's center.");
                    ScaleTransform? scale = dialog.RenderTransform as ScaleTransform;
                    Assert.IsNotNull(scale, "Opening must install a ScaleTransform for the entrance animation.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => dialog.Opacity >= 1.0 && scale.ScaleX <= 1.0 && scale.ScaleY <= 1.0),
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

        [TestMethod]
        public void ContentDialog_OverFluenceWindow_HostsOverlayAboveTheWholeWindow()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Panel? host =
                        window.Template?.FindName("PART_DialogOverlayHost", window) as Panel;
                    Assert.IsNotNull(host, "FluenceWindow template must expose the full-window PART_DialogOverlayHost.");

                    Controls.ContentDialog dialog = new()
                    {
                        Title = "Confirm",
                        Content = "Body",
                        CloseButtonText = "Cancel",
                    };

                    _ = dialog.ShowAsync();
                    bool hosted = WaitUntil(window.Dispatcher, 2000, () => host.Children.Count > 0);
                    Assert.IsTrue(hosted,
                        "Over a FluenceWindow the dialog overlay must be hosted in PART_DialogOverlayHost so the smoke covers the title bar.");

                    dialog.Hide();
                    bool removed = WaitUntil(window.Dispatcher, 2000, () => host.Children.Count is 0);
                    Assert.IsTrue(removed, "Closing the dialog must remove the overlay from PART_DialogOverlayHost.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ContentDialog_DeclaresAssertiveLiveSetting()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog dialog = new() { Title = "Confirm" };

                // A modal dialog is not a real HWND, so nothing prompts Narrator to read it on
                // open. The net472 target has no AutomationProperties.IsDialog, so the dialog
                // instead declares an assertive live region and announces it via
                // LiveRegionChanged as it appears (see ContentDialog.AnnounceLiveRegion).
                Assert.AreEqual(AutomationLiveSetting.Assertive, AutomationProperties.GetLiveSetting(dialog),
                    "ContentDialog must declare an assertive live region so Narrator announces the dialog when it opens (net472-safe substitute for AutomationProperties.IsDialog).");
            });
        }

        [TestMethod]
        public void ContentDialog_AutomationPeer_ReportsWindowRoleAndTitleName()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Controls.ContentDialog dialog = new() { Title = "Delete file?" };
                Window window = new() { Width = 320, Height = 240, Content = dialog };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(dialog);
                    _ = Assert.IsInstanceOfType<Automation.ContentDialogAutomationPeer>(peer,
                        "ContentDialog.OnCreateAutomationPeer must return a ContentDialogAutomationPeer.");
                    Assert.AreEqual(AutomationControlType.Window, peer.GetAutomationControlType(),
                        "ContentDialog must report the Window control type so assistive technologies treat it as a modal dialog surface.");
                    Assert.AreEqual("Delete file?", peer.GetName(),
                        "ContentDialog automation name must come from Title so the live-region announcement reads it when the dialog opens.");
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
