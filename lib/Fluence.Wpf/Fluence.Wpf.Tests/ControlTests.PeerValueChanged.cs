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

using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Windows;
using System.Windows.Automation.Peers;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Task-A3 tests: <see cref="DropDownButtonAutomationPeer"/> reports the correct UIA control
    /// type, and <see cref="NumberBox"/> routes value changes to its automation peer so UIA
    /// clients (Narrator) observe the current value instead of a stale one.
    /// </summary>
    public partial class ControlTests
    {
        // Spy automation peer that records whether NumberBox routed a Value change through
        // RaiseValueChanged, so the OnValueChanged wiring can be verified without standing up a
        // real UIA client listener.
        private sealed class NumberBoxValueChangedSpyPeer(NumberBox owner) : NumberBoxAutomationPeer(owner)
        {
            public int RaiseValueChangedCallCount { get; private set; }

            public double LastOldValue { get; private set; }

            public double LastNewValue { get; private set; }

            internal override void RaiseValueChanged(double oldValue, double newValue)
            {
                RaiseValueChangedCallCount++;
                LastOldValue = oldValue;
                LastNewValue = newValue;
                base.RaiseValueChanged(oldValue, newValue);
            }
        }

        // Installs the spy peer above in place of the real NumberBoxAutomationPeer.
        private sealed class NumberBoxWithSpyPeer : NumberBox
        {
            public NumberBoxValueChangedSpyPeer? SpyPeer { get; private set; }

            protected override AutomationPeer OnCreateAutomationPeer()
            {
                SpyPeer = new NumberBoxValueChangedSpyPeer(this);
                return SpyPeer;
            }
        }

        [TestMethod]
        public void DropDownButton_AutomationPeer_ReportsButtonControlType()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    DropDownButton dropDownButton = new() { Content = "Open", Width = 120, Height = 32 };
                    window.Content = dropDownButton;
                    window.Width = 200;
                    window.Height = 80;
                    window.Show();
                    _ = dropDownButton.ApplyTemplate();
                    DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(dropDownButton);
                    Assert.AreEqual(AutomationControlType.Button, peer.GetAutomationControlType(),
                        "DropDownButton is not a split control; WinUI reports it as Button.");
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
        public void NumberBox_ValueChanged_RaisesAutomationPeerValueChanged()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();

                try
                {
                    NumberBoxWithSpyPeer numberBox = new()
                    {
                        Minimum = 0,
                        Maximum = 100,
                        Value = 10,
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
                    _ = Assert.IsInstanceOfType<NumberBoxValueChangedSpyPeer>(peer,
                        "Test double peer must be installed so the wiring can be observed.");

                    numberBox.Value = 42;

                    Assert.AreEqual(1, numberBox.SpyPeer!.RaiseValueChangedCallCount,
                        "Changing NumberBox.Value must raise a UIA ValueChanged notification through the automation peer.");
                    Assert.AreEqual(10d, numberBox.SpyPeer.LastOldValue,
                        "The automation peer must be notified of the previous value.");
                    Assert.AreEqual(42d, numberBox.SpyPeer.LastNewValue,
                        "The automation peer must be notified of the new value.");
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
