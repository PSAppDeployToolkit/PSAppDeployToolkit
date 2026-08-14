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

using System.Collections.ObjectModel;
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class TabViewTests
    {
        private static ResourceDictionary? MergeGenericDictionary(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            return dictionaries?.Count > 0 ? dictionaries[^1] : null;
        }

        // ---- TabViewItem defaults ----

        [Fact]
        public Task TabViewItem_DefaultIsClosable_IsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                Assert.True(tab.IsClosable);
            });
        }

        [Fact]
        public Task TabViewItem_DefaultIcon_IsNullAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                Assert.Null(tab.Icon);
            });
        }

        [Fact]
        public Task TabViewItem_IconProperty_RoundTripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabViewItem tab = new();
                FontIcon icon = new() { Glyph = "\uE8A5" };
                tab.Icon = icon;

                Assert.Same(icon, tab.Icon);
            });
        }

        // ---- TabView defaults ----

        [Fact]
        public Task TabView_DefaultIsAddTabButtonVisible_IsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.True(tabs.IsAddTabButtonVisible);
            });
        }

        [Fact]
        public Task TabView_DefaultTabWidthMode_IsSizeToContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.Equal(TabViewWidthMode.SizeToContent, tabs.TabWidthMode);
            });
        }

        [Fact]
        public Task TabView_DefaultCloseButtonOverlayMode_IsAutoAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                Assert.Equal(TabViewCloseButtonOverlayMode.Auto, tabs.CloseButtonOverlayMode);
            });
        }

        [Fact]
        public Task TabView_ContainerGeneration_UsesTabViewItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    DependencyObject container = tabs.ItemContainerGenerator.ContainerFromIndex(0);
                    _ = Assert.IsAssignableFrom<TabViewItem>(container);
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

        [Fact]
        public Task TabView_IsItemItsOwnContainerOverride_TrueForTabViewItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TabView tabs = new();
                TabViewItem candidate = new();

                MethodInfo? method = typeof(TabView).GetMethod(
                    "IsItemItsOwnContainerOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
                Assert.NotNull(method);
                bool? result = (bool?)method.Invoke(tabs, [candidate]);
                Assert.True(result, "A TabViewItem should be recognized as its own container.");

                bool? nonTab = (bool?)method.Invoke(tabs, ["Alpha"]);
                Assert.False(nonTab, "Plain objects should require container generation.");
            });
        }

        // ---- Template parts & events ----

        [Fact]
        public Task TabView_AddTabButtonClick_RaisesAddTabButtonClickEventAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = true };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ButtonBase addButton = Assert.IsAssignableFrom<ButtonBase>(tabs.Template.FindName("PART_AddTabButton", tabs));

                    int raised = 0;
                    tabs.AddTabButtonClick += (s, e) => raised++;
                    ButtonAutomationPeer peer = new(addButton as System.Windows.Controls.Button);
                    IInvokeProvider invoke = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));
                    invoke.Invoke();

                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, raised);
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

        [Fact]
        public Task TabViewItem_CloseButton_RaisesCloseRequestedAndBubblesToTabViewAsync()
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Application application = WpfTestSta.EnsureApplication();
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
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    // Force template application on the first tab so its PART_CloseButton is realized.
                    _ = first.ApplyTemplate();

                    ButtonBase closeButton = Assert.IsAssignableFrom<ButtonBase>(first.Template.FindName("PART_CloseButton", first));

                    TabViewTabCloseRequestedEventArgs? viewArgs = null;
                    int itemRaised = 0;
                    first.CloseRequested += (s, e) => itemRaised++;
                    tabs.TabCloseRequested += (s, e) => viewArgs = e as TabViewTabCloseRequestedEventArgs;

                    ButtonAutomationPeer peer = new(closeButton as System.Windows.Controls.Button);
                    IInvokeProvider invoke = Assert.IsAssignableFrom<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke));
                    invoke.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(1, itemRaised);
                    Assert.NotNull(viewArgs);
                    Assert.Same(first, viewArgs.Tab);
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

        [Fact]
        public Task TabViewItem_IsClosableFalse_HidesCloseButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem locked = new() { Header = "Pinned", IsClosable = false, IsSelected = true };
                _ = tabs.Items.Add(locked);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = locked.ApplyTemplate();

                    FrameworkElement closeButton = Assert.IsAssignableFrom<FrameworkElement>(locked.Template.FindName("PART_CloseButton", locked));
                    Assert.False(closeButton.IsVisible,
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

        [Fact]
        public Task TabView_AddTabButtonHidden_WhenIsAddTabButtonVisibleFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200, IsAddTabButtonVisible = false };

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement addButton = Assert.IsAssignableFrom<FrameworkElement>(tabs.Template.FindName("PART_AddTabButton", tabs));
                    Assert.False(addButton.IsVisible,
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

        [Fact]
        public Task TabView_Items_AddsAndRemovesTabsOnDemandAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                Window window = new();
                TabView tabs = new() { Width = 420, Height = 200 };
                TabViewItem first = new() { Header = "Alpha", IsSelected = true };
                _ = tabs.Items.Add(first);

                try
                {
                    window.Content = tabs;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabViewItem added = new() { Header = "Beta" };
                    _ = tabs.Items.Add(added);
                    tabs.SelectedItem = added;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(2, tabs.Items.Count);
                    Assert.Same(added, tabs.SelectedItem);

                    tabs.Items.Remove(first);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    _ = Assert.Single(tabs.Items);
                    Assert.Same(added, tabs.Items[0]);
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
