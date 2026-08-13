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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Task-13 tests: clickable Card Button automation peer and CheckBox/RadioButton
    /// Description property surfaces as AutomationProperties.HelpText.
    /// </summary>
    public partial class ControlTests
    {
        // ---------------------------------------------------------------------------
        // Clickable Card - automation peer
        // ---------------------------------------------------------------------------

        [Fact]
        public void ClickableCard_AutomationPeer_IsCardAutomationPeer()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    _ = Assert.IsAssignableFrom<CardAutomationPeer>(peer);
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

        [Fact]
        public void ClickableCard_AutomationControlType_IsButton()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
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

        [Fact]
        public void ClickableCard_GetPattern_Invoke_ReturnsInvokeProvider()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    object? pattern = peer.GetPattern(PatternInterface.Invoke);
                    Assert.NotNull(pattern);
                    _ = Assert.IsAssignableFrom<IInvokeProvider>(pattern);
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

        [Fact]
        public void ClickableCard_InvokePattern_RaisesClickEvent()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    bool clickRaised = false;
                    card.Click += (_, _) => clickRaised = true;

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    IInvokeProvider? invokeProvider = peer.GetPattern(PatternInterface.Invoke) as IInvokeProvider;
                    Assert.NotNull(invokeProvider);
                    invokeProvider.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.True(clickRaised,
                        "IInvokeProvider.Invoke() must raise the Card Click routed event.");
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

        [Fact]
        public void ClickableCard_IsTabStop_IsTrue()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Card card = new() { IsClickable = true };
                    Assert.True(card.IsTabStop,
                        "A clickable Card must be IsTabStop=true so keyboard users can reach it.");
                    Assert.True(card.Focusable,
                        "A clickable Card must be Focusable=true.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void NonClickableCard_AutomationControlType_IsNotButton()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = false,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    Assert.NotEqual(AutomationControlType.Button, peer.GetAutomationControlType());
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

        [Fact]
        public void NonClickableCard_GetPattern_Invoke_ReturnsNull()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = false,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    object? pattern = peer.GetPattern(PatternInterface.Invoke);
                    Assert.Null(pattern);
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

        [Fact]
        public void NonClickableCard_IsTabStop_IsFalse()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    Card card = new() { IsClickable = false };
                    Assert.False(card.IsTabStop,
                        "A non-clickable Card must not be in the tab order.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // ---------------------------------------------------------------------------
        // CheckBox Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public void CheckBox_Description_SetsAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    CheckBox checkBox = new()
                    {
                        Content = "Enable feature",
                        Description = "Enables the optional feature for this session.",
                    };

                    string helpText = AutomationProperties.GetHelpText(checkBox);
                    Assert.True(
                        string.Equals("Enables the optional feature for this session.", helpText, StringComparison.Ordinal),
                        $"CheckBox.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void CheckBox_DescriptionChanges_UpdatesAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    CheckBox checkBox = new()
                    {
                        Content = "Enable feature",
                        Description = "First description.",
                    };

                    checkBox.Description = "Updated description.";
                    string helpText = AutomationProperties.GetHelpText(checkBox);
                    Assert.True(
                        string.Equals("Updated description.", helpText, StringComparison.Ordinal),
                        $"CheckBox.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void CheckBox_NullDescription_ClearsAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    CheckBox checkBox = new()
                    {
                        Content = "Enable feature",
                        Description = "Some description.",
                    };
                    checkBox.Description = null;

                    string helpText = AutomationProperties.GetHelpText(checkBox);
                    Assert.True(
                        string.IsNullOrWhiteSpace(helpText),
                        $"Null CheckBox.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        // ---------------------------------------------------------------------------
        // RadioButton Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public void RadioButton_Description_SetsAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    RadioButton radioButton = new()
                    {
                        Content = "Option A",
                        Description = "Choose this option for better performance.",
                    };

                    string helpText = AutomationProperties.GetHelpText(radioButton);
                    Assert.True(
                        string.Equals("Choose this option for better performance.", helpText, StringComparison.Ordinal),
                        $"RadioButton.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void RadioButton_DescriptionChanges_UpdatesAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    RadioButton radioButton = new()
                    {
                        Content = "Option A",
                        Description = "Initial description.",
                    };

                    radioButton.Description = "Revised description.";
                    string helpText = AutomationProperties.GetHelpText(radioButton);
                    Assert.True(
                        string.Equals("Revised description.", helpText, StringComparison.Ordinal),
                        $"RadioButton.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void RadioButton_NullDescription_ClearsAutomationHelpText()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                try
                {
                    RadioButton radioButton = new()
                    {
                        Content = "Option A",
                        Description = "Some description.",
                    };
                    radioButton.Description = null;

                    string helpText = AutomationProperties.GetHelpText(radioButton);
                    Assert.True(
                        string.IsNullOrWhiteSpace(helpText),
                        $"Null RadioButton.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
                }
                finally
                {
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
