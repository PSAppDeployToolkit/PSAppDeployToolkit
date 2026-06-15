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
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using System.Windows.Threading;
using Fluence.Wpf.Controls;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class TabViewTests
    {
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
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            return dictionaries?.Count > 0 ? dictionaries[^1] : null;
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            _ = dispatcher.Invoke(DispatcherPriority.ApplicationIdle, new Action(static delegate { }));
        }

        // ---- TabViewItem defaults ----

        [TestMethod]
        public void TabViewItem_DefaultIsClosable_IsTrue()
        {
            RunOnFreshStaThread(static () =>
            {
                TabViewItem tab = new();
                Assert.IsTrue(tab.IsClosable);
            });
        }

        [TestMethod]
        public void TabViewItem_DefaultIcon_IsNull()
        {
            RunOnFreshStaThread(static () =>
            {
                TabViewItem tab = new();
                Assert.IsNull(tab.Icon);
            });
        }

        [TestMethod]
        public void TabViewItem_IconProperty_RoundTrips()
        {
            RunOnFreshStaThread(static () =>
            {
                TabViewItem tab = new();
                FontIcon icon = new() { Glyph = "\uE8A5" };
                tab.Icon = icon;

                Assert.AreSame(icon, tab.Icon);
            });
        }

        // ---- TabView defaults ----

        [TestMethod]
        public void TabView_DefaultIsAddTabButtonVisible_IsTrue()
        {
            RunOnFreshStaThread(static () =>
            {
                TabView tabs = new();
                Assert.IsTrue(tabs.IsAddTabButtonVisible);
            });
        }

        [TestMethod]
        public void TabView_DefaultTabWidthMode_IsSizeToContent()
        {
            RunOnFreshStaThread(static () =>
            {
                TabView tabs = new();
                Assert.AreEqual(TabViewWidthMode.SizeToContent, tabs.TabWidthMode);
            });
        }

        [TestMethod]
        public void TabView_DefaultCloseButtonOverlayMode_IsAuto()
        {
            RunOnFreshStaThread(static () =>
            {
                TabView tabs = new();
                Assert.AreEqual(TabViewCloseButtonOverlayMode.Auto, tabs.CloseButtonOverlayMode);
            });
        }

        [TestMethod]
        public void TabView_ContainerGeneration_UsesTabViewItem()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new()
                {
                    Width = 420,
                    Height = 200,
                    ItemsSource = new[] { "Alpha", "Beta" },
                };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    DependencyObject container = tabs.ItemContainerGenerator.ContainerFromIndex(0);
                    Assert.IsInstanceOfType(container, typeof(TabViewItem),
                        "TabView should generate TabViewItem containers, not base TabItem.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TabView_IsItemItsOwnContainerOverride_TrueForTabViewItem()
        {
            RunOnFreshStaThread(static () =>
            {
                TabView tabs = new();
                TabViewItem candidate = new();

                MethodInfo? method = typeof(TabView).GetMethod(
                    "IsItemItsOwnContainerOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.IsNotNull(method, "TabView must override IsItemItsOwnContainerOverride.");
                bool? result = (bool?)method.Invoke(tabs, [candidate]);
                Assert.IsTrue(result, "A TabViewItem should be recognized as its own container.");

                bool? nonTab = (bool?)method.Invoke(tabs, ["Alpha"]);
                Assert.IsFalse(nonTab, "Plain objects should require container generation.");
            });
        }

        // ---- Template parts & events ----

        [TestMethod]
        public void TabView_AddTabButtonClick_RaisesAddTabButtonClickEvent()
        {
            RunOnFreshStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = true };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ButtonBase? addButton = tabs.Template.FindName("PART_AddTabButton", tabs) as ButtonBase;
                    Assert.IsNotNull(addButton, "TabView template must expose PART_AddTabButton.");

                    int raised = 0;
                    tabs.AddTabButtonClick += (s, e) => raised++;
                    ButtonAutomationPeer peer = new(addButton as System.Windows.Controls.Button);
                    IInvokeProvider? invoke = peer.GetPattern(PatternInterface.Invoke)
                        as IInvokeProvider;
                    Assert.IsNotNull(invoke, "PART_AddTabButton must expose an Invoke pattern.");
                    invoke.Invoke();

                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, raised, "AddTabButtonClick should bubble to the TabView handler once per press.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TabViewItem_CloseButton_RaisesCloseRequestedAndBubblesToTabView()
        {
            RunOnFreshStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem first = new() { Header = "Alpha", IsSelected = true };
                TabViewItem second = new() { Header = "Beta" };
                _ = tabs.Items.Add(first);
                _ = tabs.Items.Add(second);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Force template application on the first tab so its PART_CloseButton is realized.
                    _ = first.ApplyTemplate();

                    ButtonBase? closeButton = first.Template.FindName("PART_CloseButton", first) as ButtonBase;
                    Assert.IsNotNull(closeButton, "TabViewItem template must expose PART_CloseButton.");

                    TabViewTabCloseRequestedEventArgs? viewArgs = null;
                    int itemRaised = 0;
                    first.CloseRequested += (s, e) => itemRaised++;
                    tabs.TabCloseRequested += (s, e) => viewArgs = e as TabViewTabCloseRequestedEventArgs;

                    ButtonAutomationPeer peer = new(closeButton as System.Windows.Controls.Button);
                    IInvokeProvider? invoke = peer.GetPattern(PatternInterface.Invoke)
                        as IInvokeProvider;
                    Assert.IsNotNull(invoke, "PART_CloseButton must expose an Invoke pattern.");
                    invoke.Invoke();
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, itemRaised, "TabViewItem.CloseRequested should fire exactly once per click.");
                    Assert.IsNotNull(viewArgs, "TabView.TabCloseRequested should aggregate the item event.");
                    Assert.AreSame(first, viewArgs.Tab, "TabView.TabCloseRequested should carry the originating tab.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TabViewItem_IsClosableFalse_HidesCloseButton()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem locked = new() { Header = "Pinned", IsClosable = false, IsSelected = true };
                _ = tabs.Items.Add(locked);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = locked.ApplyTemplate();

                    FrameworkElement? closeButton = locked.Template.FindName("PART_CloseButton", locked) as FrameworkElement;
                    Assert.IsNotNull(closeButton, "Template should still create PART_CloseButton, just hidden.");
                    Assert.IsFalse(closeButton.IsVisible,
                        "IsClosable=false should hide the close button regardless of overlay mode.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TabView_AddTabButtonHidden_WhenIsAddTabButtonVisibleFalse()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = false };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement? addButton = tabs.Template.FindName("PART_AddTabButton", tabs) as FrameworkElement;
                    Assert.IsNotNull(addButton, "Template always produces PART_AddTabButton.");
                    Assert.IsFalse(addButton.IsVisible,
                        "IsAddTabButtonVisible=false should collapse the add button.");
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [TestMethod]
        public void TabView_Items_AddsAndRemovesTabsOnDemand()
        {
            RunOnFreshStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem first = new() { Header = "Alpha", IsSelected = true };
                _ = tabs.Items.Add(first);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewItem added = new() { Header = "Beta" };
                    _ = tabs.Items.Add(added);
                    tabs.SelectedItem = added;
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(2, tabs.Items.Count);
                    Assert.AreSame(added, tabs.SelectedItem);

                    tabs.Items.Remove(first);
                    DrainDispatcher(window.Dispatcher);

                    Assert.AreEqual(1, tabs.Items.Count);
                    Assert.AreSame(added, tabs.Items[0]);
                }
                finally
                {
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
