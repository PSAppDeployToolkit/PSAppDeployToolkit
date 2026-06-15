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
using System.Windows;
using System.Windows.Controls;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
        [TestMethod]
        public void NavigationView_AfterUnloadReload_SelectionIndicatorStillUpdates()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                ContentControl host = new();
                window.Content = host;

                try
                {
                    NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    NavigationViewItem home = new() { Content = "Home", Icon = new FontIcon { Glyph = "" } };
                    NavigationViewItem files = new() { Content = "Files", Icon = new FontIcon { Glyph = "" } };
                    _ = nav.Items.Add(home);
                    _ = nav.Items.Add(files);

                    host.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    nav.SelectedItem = home;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WaitForAnimationAndDrain(window.Dispatcher, 400);

                    // Simulate navigating away from the cached page and back: the NavigationView is
                    // unloaded (template parts nulled) and reloaded against the same instance.
                    host.Content = null;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    host.Content = nav;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    WaitForAnimationAndDrain(window.Dispatcher, 400);

                    FrameworkElement? indicator = nav.GetSelectionIndicatorForTesting();
                    Assert.IsNotNull(indicator, "Indicator part should be resolved after reload.");
                    double homeY = GetSelectionIndicatorTranslate(indicator).Y;

                    nav.InvokeItem(files);
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Settle until the indicator slide animation has reached its hold-end (no longer
                    // animating), so the sampled filesY is the final settled offset.
                    _ = WaitUntil(window.Dispatcher, 2000, () => !GetSelectionIndicatorTranslate(indicator).HasAnimatedProperties);

                    Assert.AreSame(files, nav.SelectedItem, "Invoking the second item should change the selection after reload.");
                    double filesY = GetSelectionIndicatorTranslate(indicator).Y;
                    Assert.AreNotEqual(homeY, filesY, 0.5,
                        "After unload/reload, the selection indicator must still move to the newly selected item.");
                    Assert.AreEqual(1.0, indicator.Opacity, 0.01,
                        "The selection indicator must be visible at the new selection after reload.");
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
