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
using System.Threading.Tasks;
using System.Windows;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class ListViewIsItemSelectableTests
    {
        private static ResourceDictionary? MergeGenericDictionary(Application application)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            application?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            Collection<ResourceDictionary>? dictionaries = application?.Resources.MergedDictionaries;
            ResourceDictionary? genericDictionary = dictionaries?.Count > 0 ? dictionaries[^1] : null;

            ResourceDictionary demoShared = new()
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            };
            application?.Resources.MergedDictionaries.Add(demoShared);

            return genericDictionary;
        }

        [Fact]
        public Task IsItemSelectable_DefaultIsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new();
                Assert.True(lv.IsItemSelectable);
            });
        }

        [Fact]
        public Task IsItemSelectable_False_ClearsSelectionWhenSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new() { Width = 260, Height = 120 };
                _ = lv.Items.Add("a");
                _ = lv.Items.Add("b");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(0, lv.SelectedIndex);

                    lv.IsItemSelectable = false;
                    Assert.Equal(-1, lv.SelectedIndex);
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
        public Task IsItemSelectable_False_SelectedIndexStaysMinusOne_AfterDirectSetAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    lv.SelectedIndex = 0;
                    Assert.Equal(-1, lv.SelectedIndex);
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
        public Task IsItemSelectable_False_ContainerIsNotFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = false,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.ListViewItem container = Assert.IsType<System.Windows.Controls.ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.False(container.Focusable);
                    Assert.False(Controls.ListView.GetParentIsItemSelectable(container));
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
        public Task IsItemSelectable_True_ContainerIsFocusableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                Window window = new();
                Controls.ListView lv = new()
                {
                    Width = 260,
                    Height = 120,
                    IsItemSelectable = true,
                };
                _ = lv.Items.Add("a");

                try
                {
                    window.Content = lv;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.ListViewItem container = Assert.IsType<System.Windows.Controls.ListViewItem>(lv.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.True(container.Focusable);
                    Assert.True(Controls.ListView.GetParentIsItemSelectable(container));
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
        public Task ItemAnimationsEnabled_IndependentOfIsItemSelectableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListView lv = new() { IsItemSelectable = false, ItemAnimationsEnabled = true };
                Assert.False(lv.IsItemSelectable);
                Assert.True(lv.ItemAnimationsEnabled);

                lv.ItemAnimationsEnabled = false;
                lv.IsItemSelectable = true;
                Assert.True(lv.IsItemSelectable);
                Assert.False(lv.ItemAnimationsEnabled);
            });
        }
    }
}
