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

using System.Windows;
using System.Windows.Automation;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Task-A2 tests: automation peer SetValue implementations reject writes to a
    /// disabled control by throwing <see cref="ElementNotEnabledException"/>, matching
    /// the UIA IValueProvider/IRangeValueProvider contract already honored by
    /// <see cref="Fluence.Wpf.Automation.RatingControlAutomationPeer"/>.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public void NumberBox_Disabled_RangeValueProvider_SetValue_ThrowsElementNotEnabledException()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NumberBox numberBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = numberBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    IRangeValueProvider rangeValueProvider = Assert.IsAssignableFrom<IRangeValueProvider>(peer.GetPattern(PatternInterface.RangeValue));

                    _ = Assert.Throws<ElementNotEnabledException>(() => rangeValueProvider.SetValue(5d));
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
        public void NumberBox_Disabled_RangeValueProvider_IsReadOnly_ReturnsFalse()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NumberBox numberBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = numberBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = numberBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(numberBox);
                    IRangeValueProvider rangeValueProvider = Assert.IsAssignableFrom<IRangeValueProvider>(peer.GetPattern(PatternInterface.RangeValue));

                    Assert.False(rangeValueProvider.IsReadOnly,
                        "A disabled NumberBox has no read-only mode; disabled state is conveyed by IsEnabled, not IsReadOnly.");
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
        public void AutoSuggestBox_Disabled_ValueProvider_SetValue_ThrowsElementNotEnabledException()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    AutoSuggestBox autoSuggestBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = autoSuggestBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(autoSuggestBox);
                    IValueProvider valueProvider = Assert.IsAssignableFrom<IValueProvider>(peer.GetPattern(PatternInterface.Value));

                    _ = Assert.Throws<ElementNotEnabledException>(() => valueProvider.SetValue("hello"));
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
        public void AutoSuggestBox_Disabled_ValueProvider_IsReadOnly_ReturnsFalse()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    AutoSuggestBox autoSuggestBox = new()
                    {
                        IsEnabled = false,
                        Width = 200,
                        Height = 32,
                    };
                    window.Content = autoSuggestBox;
                    window.Width = 300;
                    window.Height = 100;
                    window.Show();
                    _ = autoSuggestBox.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(autoSuggestBox);
                    IValueProvider valueProvider = Assert.IsAssignableFrom<IValueProvider>(peer.GetPattern(PatternInterface.Value));

                    Assert.False(valueProvider.IsReadOnly,
                        "A disabled AutoSuggestBox has no read-only mode; disabled state is conveyed by IsEnabled, not IsReadOnly.");
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
