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
using System.Reflection;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public class AdditionalControlsTests
    {
        private static void MergeGeneric(Application app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            app.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            });
        }

        [Fact]
        public Task NumberBox_DefaultStyle_LoadsPartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.NumberBox numberBox = new() { Width = 160, Value = 3 };
                try
                {
                    window.Content = numberBox;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = numberBox.ApplyTemplate();
                    Assert.NotNull(numberBox.Template.FindName("PART_TextBox", numberBox));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task NumberBox_Value_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.NumberBox box = new() { Value = 42.5 };
                Assert.Equal(42.5, box.Value, 0.001);
            });
        }

        [Fact]
        public Task Expander_CornerRadius_DefaultAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.Expander ex = new();
                Assert.Equal(new CornerRadius(4), ex.CornerRadius);
            });
        }

        [Fact]
        public Task Expander_Template_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.Expander ex = new() { Header = "H", Content = new TextBlock { Text = "C" }, Width = 200 };
                try
                {
                    window.Content = ex;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = ex.ApplyTemplate();
                    Assert.NotNull(ex.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DropDownButton_Template_HasFlyoutPresenterNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.DropDownButton btn = new() { Content = "Open", Width = 120, Flyout = new TextBlock { Text = "Flyout" } };
                try
                {
                    window.Content = btn;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = btn.ApplyTemplate();
                    Assert.NotNull(btn.Template.FindName("PART_Popup", btn));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task DropDownButton_FlyoutPresenter_StretchesForLeftAlignedItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.DropDownButton btn = new() { Content = "Open", Width = 160, Flyout = new StackPanel() };
                try
                {
                    window.Content = btn;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = btn.ApplyTemplate();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(btn.Template.FindName("FlyoutContentPresenter", btn));
                    Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task SplitButton_FlyoutPresenter_StretchesForLeftAlignedItemsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.SplitButton btn = new() { Content = "Export", Width = 180, Flyout = new StackPanel() };
                try
                {
                    window.Content = btn;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = btn.ApplyTemplate();

                    ContentPresenter presenter = Assert.IsType<ContentPresenter>(btn.Template.FindName("FlyoutContentPresenter", btn));
                    Assert.Equal(HorizontalAlignment.Stretch, presenter.HorizontalAlignment);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_Value_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.InfoBadge badge = new() { Value = 9 };
                Assert.Equal(9, badge.Value);
            });
        }

        [Fact]
        public Task InfoBadge_Template_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                MergeGeneric(app);
                Window window = new();
                Controls.InfoBadge badge = new() { Value = 2, Width = 32, Height = 32 };
                try
                {
                    window.Content = badge;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = badge.ApplyTemplate();
                    Assert.NotNull(badge.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task ListBox_GetContainerForItemOverride_ReturnsFluentListBoxItemAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ListBox list = new();
                MethodInfo m = Assert.IsAssignableFrom<MethodInfo>(typeof(Controls.ListBox).GetMethod(
                    "GetContainerForItemOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic));
                object? container = m.Invoke(list, []);
                _ = Assert.IsAssignableFrom<Controls.ListBoxItem>(container);
            });
        }
    }
}
