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
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class SplitButtonTests
    {
        private static void RunOnSta(Action action)
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

        private static void Drain(Dispatcher dispatcher)
        {
            dispatcher.Invoke(new Action(delegate { }), DispatcherPriority.ApplicationIdle);
        }

        private static void MergeGeneric(Application? application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            application?.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative)
            });
        }

        #region Defaults and DPs

        [TestMethod]
        public void Defaults_AreWinUiCanon()
        {
            RunOnSta(() =>
            {
                SplitButton button = new();

                Assert.AreEqual(new CornerRadius(4), button.CornerRadius,
                    "SplitButton.CornerRadius must default to 4 (Fluent control corner).");
                Assert.AreEqual(new CornerRadius(8), button.DropdownCornerRadius,
                    "SplitButton.DropdownCornerRadius must default to 8 (Fluent flyout corner).");
                Assert.IsFalse(button.IsFlyoutOpen,
                    "SplitButton.IsFlyoutOpen must default to false.");
                Assert.IsNull(button.Command,
                    "SplitButton.Command must default to null.");
                Assert.IsNull(button.CommandParameter,
                    "SplitButton.CommandParameter must default to null.");
                Assert.IsNull(button.Flyout,
                    "SplitButton.Flyout must default to null.");
            });
        }

        [TestMethod]
        public void IsFlyoutOpen_IsReadOnlyDp()
        {
            RunOnSta(() =>
            {
                // Direct SetValue on the public IsFlyoutOpenProperty must fail: only the
                // internal PropertyKey may mutate it. Guard against accidental promotion
                // of the property to read/write.
                SplitButton button = new();
                bool threw = false;
                try
                {
                    button.SetValue(SplitButton.IsFlyoutOpenProperty, true);
                }
                catch (InvalidOperationException)
                {
                    threw = true;
                }

                Assert.IsTrue(threw,
                    "IsFlyoutOpen must be a read-only DP; external SetValue must throw.");
            });
        }

        #endregion

        #region Template parts

        [TestMethod]
        public void Template_ExposesPrimarySecondaryAndPopupParts()
        {
            RunOnSta(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                MergeGeneric(application);

                Window window = new();
                try
                {
                    SplitButton splitButton = new()
                    {
                        Content = "Action",
                        Flyout = new System.Windows.Controls.TextBlock { Text = "Flyout content" },
                        Width = 140
                    };
                    window.Content = splitButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = splitButton.ApplyTemplate();

                    System.Windows.Controls.Button? primary = splitButton.Template.FindName("PART_PrimaryButton", splitButton)
                        as System.Windows.Controls.Button;
                    Assert.IsNotNull(primary,
                        "Template must expose PART_PrimaryButton (the primary-action hit target).");

                    System.Windows.Controls.Primitives.ToggleButton? secondary = splitButton.Template.FindName("PART_SecondaryButton", splitButton)
                        as System.Windows.Controls.Primitives.ToggleButton;
                    Assert.IsNotNull(secondary,
                        "Template must expose PART_SecondaryButton (the flyout-toggle hit target).");

                    Popup? popup = splitButton.Template.FindName("PART_Popup", splitButton)
                        as Popup;
                    Assert.IsNotNull(popup,
                        "Template must expose PART_Popup (the flyout host).");
                    Assert.IsFalse(popup.StaysOpen,
                        "PART_Popup.StaysOpen must be false so outside-clicks close the flyout.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        #endregion

        #region Primary click

        [TestMethod]
        public void PrimaryButtonClick_RaisesSplitButtonClick()
        {
            RunOnSta(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                MergeGeneric(application);

                Window window = new();
                try
                {
                    SplitButton splitButton = new()
                    {
                        Content = "Action",
                        Width = 140
                    };
                    int clickCount = 0;
                    splitButton.Click += (s, e) => clickCount++;

                    window.Content = splitButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = splitButton.ApplyTemplate();

                    System.Windows.Controls.Button? primary = splitButton.Template.FindName("PART_PrimaryButton", splitButton)
                        as System.Windows.Controls.Button;
                    Assert.IsNotNull(primary);

                    // Use UI Automation peer -> IInvokeProvider.Invoke(), the canonical
                    // equivalent of a user press-release on the button.
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(primary);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(1, clickCount,
                        "PART_PrimaryButton.Click must raise SplitButton.Click exactly once.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void PrimaryButtonClick_ExecutesCommand()
        {
            RunOnSta(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                MergeGeneric(application);

                Window window = new();
                try
                {
                    int executed = 0;
                    RelayCommand command = new(p => executed++);

                    SplitButton splitButton = new()
                    {
                        Content = "Action",
                        Command = command,
                        Width = 140
                    };

                    window.Content = splitButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = splitButton.ApplyTemplate();

                    System.Windows.Controls.Button? primary = splitButton.Template.FindName("PART_PrimaryButton", splitButton)
                        as System.Windows.Controls.Button;
                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(primary);
                    IInvokeProvider invoke = (IInvokeProvider)peer.GetPattern(PatternInterface.Invoke);
                    invoke.Invoke();
                    Drain(window.Dispatcher);

                    Assert.AreEqual(1, executed,
                        "PART_PrimaryButton.Click must execute SplitButton.Command.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        #endregion

        #region Flyout open / close

        [TestMethod]
        public void SecondaryButtonChecked_OpensPopupAndFlipsIsFlyoutOpen()
        {
            RunOnSta(() =>
            {
                Application? application = WpfTestSta.EnsureApplication();
                MergeGeneric(application);

                Window window = new();
                try
                {
                    SplitButton splitButton = new()
                    {
                        Content = "Action",
                        Flyout = new System.Windows.Controls.TextBlock { Text = "Hello" },
                        Width = 140
                    };

                    window.Content = splitButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = splitButton.ApplyTemplate();

                    System.Windows.Controls.Primitives.ToggleButton? secondary = splitButton.Template.FindName("PART_SecondaryButton", splitButton)
                        as System.Windows.Controls.Primitives.ToggleButton;
                    Popup? popup = splitButton.Template.FindName("PART_Popup", splitButton)
                        as Popup;
                    Assert.IsNotNull(secondary);
                    Assert.IsNotNull(popup);

                    Assert.IsFalse(popup.IsOpen, "Popup should start closed.");
                    Assert.IsFalse(splitButton.IsFlyoutOpen, "IsFlyoutOpen should start false.");

                    secondary.IsChecked = true;
                    Drain(window.Dispatcher);

                    Assert.IsTrue(popup.IsOpen,
                        "Toggling PART_SecondaryButton on must open PART_Popup.");
                    Assert.IsTrue(splitButton.IsFlyoutOpen,
                        "IsFlyoutOpen must flip true when the secondary toggle is checked.");

                    secondary.IsChecked = false;
                    Drain(window.Dispatcher);

                    Assert.IsFalse(popup.IsOpen,
                        "Toggling PART_SecondaryButton off must close PART_Popup.");
                    Assert.IsFalse(splitButton.IsFlyoutOpen,
                        "IsFlyoutOpen must flip false when the secondary toggle is unchecked.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        #endregion

        #region Automation

        [TestMethod]
        public void AutomationPeer_IsSplitButton_WithInvokeAndExpandCollapse()
        {
            RunOnSta(() =>
            {
                SplitButton splitButton = new();
                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(splitButton);
                Assert.IsNotNull(peer, "SplitButton must return an AutomationPeer.");

                Assert.AreEqual(AutomationControlType.SplitButton,
                    peer.GetAutomationControlType(),
                    "AutomationControlType must be SplitButton.");
                Assert.IsNotNull(peer.GetPattern(PatternInterface.Invoke),
                    "SplitButton peer must expose the Invoke pattern (primary action).");
                Assert.IsNotNull(peer.GetPattern(PatternInterface.ExpandCollapse),
                    "SplitButton peer must expose the ExpandCollapse pattern (flyout).");
            });
        }

        #endregion

        private sealed class RelayCommand(Action<object?> execute) : ICommand
        {
            private readonly Action<object?> _execute = execute;

            public bool CanExecute(object? parameter) { return true; }
            public void Execute(object? parameter) { _execute(parameter); }
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Major Code Smell", "S108:Nested blocks of code should not be left empty", Justification = "This is just test code.")]
            public event EventHandler? CanExecuteChanged { add { } remove { } }
        }
    }
}
