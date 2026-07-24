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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls;
using System.Windows.Input;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void PasswordBox_AutomationPeer_ReportsPasswordEdit()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.PasswordBox box = new()
                    {
                        Password = "secret",
                        Width = 200,
                    };
                    window.Content = box;
                    window.Width = 280;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = box.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(box);
                    Assert.IsInstanceOfType(peer, typeof(Fluence.Wpf.Automation.PasswordBoxAutomationPeer),
                        "PasswordBox must return a PasswordBoxAutomationPeer.");
                    Assert.AreEqual(AutomationControlType.Edit, peer.GetAutomationControlType(),
                        "PasswordBox peer must report ControlType.Edit so assistive tools treat it as a text field.");
                    Assert.IsTrue(peer.IsPassword(),
                        "PasswordBox peer must report IsPassword=true so Narrator suppresses reading the value aloud.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void PasswordBox_RevealButton_IsKeyboardOperableAndNamed()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.PasswordBox box = new()
                    {
                        Password = "secret",
                        RevealButtonEnabled = true,
                        Width = 200,
                    };
                    window.Content = box;
                    window.Width = 280;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = box.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    Button? revealButton = FindVisualChildByName<Button>(box, "PART_RevealButton");
                    Assert.IsNotNull(revealButton, "PART_RevealButton must be present in the PasswordBox template.");

                    string accessibleName = AutomationProperties.GetName(revealButton);
                    Assert.IsFalse(string.IsNullOrWhiteSpace(accessibleName),
                        "PART_RevealButton must have a non-empty AutomationProperties.Name for screen readers.");

                    Assert.IsTrue(revealButton.Focusable,
                        "PART_RevealButton must be focusable so keyboard users can Tab to it.");

                    // Invoking via IInvokeProvider simulates Space/Enter keyboard activation.
                    AutomationPeer revealPeer = UIElementAutomationPeer.CreatePeerForElement(revealButton);
                    IInvokeProvider invoke = (IInvokeProvider)revealPeer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(box.IsPasswordRevealed,
                        "Invoking PART_RevealButton (keyboard Space/Enter path) must reveal the password.");

                    // Second invoke should toggle off.
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(box.IsPasswordRevealed,
                        "A second invocation of PART_RevealButton must hide the password again.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void PasswordBox_RevealButton_MousePressAndHold_IsTransient()
        {
            // Regression test: a mouse press-and-release must NOT leave the password revealed.
            // Contract: press-and-hold = transient reveal; release = hide immediately.
            // Prior to the fix, OnRevealButtonUp reset _isMouseRevealActive before Click fired,
            // causing the Click toggle branch to run and leaving IsPasswordRevealed = true.
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Controls.PasswordBox box = new()
                    {
                        Password = "secret",
                        RevealButtonEnabled = true,
                        Width = 200,
                    };
                    window.Content = box;
                    window.Width = 280;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    _ = box.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    Button? revealButton = FindVisualChildByName<Button>(box, "PART_RevealButton");
                    Assert.IsNotNull(revealButton, "PART_RevealButton must be present in the PasswordBox template.");

                    // Simulate PreviewMouseLeftButtonDown - password should reveal while held.
                    MouseButtonEventArgs downArgs = new(
                        Mouse.PrimaryDevice,
                        0,
                        MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonDownEvent,
                        Source = revealButton,
                    };
                    revealButton.RaiseEvent(downArgs);
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsTrue(box.IsPasswordRevealed,
                        "Password must be revealed while the mouse button is held down (press-and-hold).");
                    Assert.AreEqual("Hide password", AutomationProperties.GetName(revealButton),
                        "The reveal button's accessible name must reflect the revealed state during a mouse press-and-hold.");

                    // Simulate PreviewMouseLeftButtonUp - password should hide on release.
                    MouseButtonEventArgs upArgs = new(
                        Mouse.PrimaryDevice,
                        0,
                        MouseButton.Left)
                    {
                        RoutedEvent = UIElement.PreviewMouseLeftButtonUpEvent,
                        Source = revealButton,
                    };
                    revealButton.RaiseEvent(upArgs);
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(box.IsPasswordRevealed,
                        "Password must be hidden immediately after mouse button is released.");
                    Assert.AreEqual("Show password", AutomationProperties.GetName(revealButton),
                        "The reveal button's accessible name must reset once the mouse press-and-hold ends.");

                    // WPF fires Click after MouseLeftButtonUp completes. Explicitly raise it
                    // here to reproduce the regression: without the fix the Click handler
                    // toggled IsPasswordRevealed back to true because _isMouseRevealActive
                    // had already been reset to false in OnRevealButtonUp.
                    RoutedEventArgs clickArgs = new(Button.ClickEvent, revealButton);
                    revealButton.RaiseEvent(clickArgs);
                    DrainDispatcher(window.Dispatcher);

                    Assert.IsFalse(box.IsPasswordRevealed,
                        "Password must stay hidden after Click fires following a mouse press-and-release (press-and-hold is transient, not a toggle).");

                    // Verify the gesture left no stale state: keyboard invocation must still toggle correctly.
                    AutomationPeer revealPeer = UIElementAutomationPeer.CreatePeerForElement(revealButton);
                    IInvokeProvider invoke = (IInvokeProvider)revealPeer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsTrue(box.IsPasswordRevealed,
                        "Keyboard invocation after a mouse press-and-release must be able to reveal the password.");
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);
                    Assert.IsFalse(box.IsPasswordRevealed,
                        "Second keyboard invocation must hide the password again.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
