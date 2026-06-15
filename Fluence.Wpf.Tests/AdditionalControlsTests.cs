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
using System;
using System.Reflection;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace Fluence.Wpf.Tests
{
    [TestClass]
    public class AdditionalControlsTests
    {
        private static void Drain(Dispatcher d)
        {
            d.Invoke(static () => { }, DispatcherPriority.ApplicationIdle);
        }

        private static Application? EnsureApp()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void MergeGeneric(Application? app)
        {
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            app?.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
            app?.Resources.MergedDictionaries.Add(new ResourceDictionary
            {
                Source = new Uri("/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml", UriKind.Relative),
            });
        }

        [TestMethod]
        public void NumberBox_DefaultStyle_LoadsParts()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.NumberBox numberBox = new() { Width = 160, Value = 3 };
                try
                {
                    window.Content = numberBox;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = numberBox.ApplyTemplate();
                    Assert.IsNotNull(numberBox.Template.FindName("PART_TextBox", numberBox));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void NumberBox_Value_Roundtrips()
        {
            WpfTestSta.Invoke(static () =>
            {
                Controls.NumberBox box = new() { Value = 42.5 };
                Assert.AreEqual(42.5, box.Value, 0.001);
            });
        }

        [TestMethod]
        public void Expander_CornerRadius_Default()
        {
            WpfTestSta.Invoke(static () =>
            {
                Controls.Expander ex = new();
                Assert.AreEqual(new CornerRadius(4), ex.CornerRadius);
            });
        }

        [TestMethod]
        public void Expander_Template_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.Expander ex = new() { Header = "H", Content = new TextBlock { Text = "C" }, Width = 200 };
                try
                {
                    window.Content = ex;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = ex.ApplyTemplate();
                    Assert.IsNotNull(ex.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DropDownButton_Template_HasFlyoutPresenterName()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.DropDownButton btn = new() { Content = "Open", Width = 120, Flyout = new TextBlock { Text = "Flyout" } };
                try
                {
                    window.Content = btn;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = btn.ApplyTemplate();
                    Assert.IsNotNull(btn.Template.FindName("PART_Popup", btn));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void DropDownButton_FlyoutPresenter_StretchesForLeftAlignedItems()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.DropDownButton btn = new() { Content = "Open", Width = 160, Flyout = new StackPanel() };
                try
                {
                    window.Content = btn;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = btn.ApplyTemplate();

                    ContentPresenter? presenter = btn.Template.FindName("FlyoutContentPresenter", btn) as ContentPresenter;
                    Assert.IsNotNull(presenter, "DropDownButton template must expose FlyoutContentPresenter.");
                    Assert.AreEqual(HorizontalAlignment.Stretch, presenter.HorizontalAlignment,
                        "Flyout presenter should stretch so left-aligned menu buttons fill the flyout width.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void SplitButton_FlyoutPresenter_StretchesForLeftAlignedItems()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.SplitButton btn = new() { Content = "Export", Width = 180, Flyout = new StackPanel() };
                try
                {
                    window.Content = btn;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = btn.ApplyTemplate();

                    ContentPresenter? presenter = btn.Template.FindName("FlyoutContentPresenter", btn) as ContentPresenter;
                    Assert.IsNotNull(presenter, "SplitButton template must expose FlyoutContentPresenter.");
                    Assert.AreEqual(HorizontalAlignment.Stretch, presenter.HorizontalAlignment,
                        "Flyout presenter should stretch so left-aligned menu buttons fill the flyout width.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void InfoBadge_Value_Roundtrips()
        {
            WpfTestSta.Invoke(static () =>
            {
                Controls.InfoBadge badge = new() { Value = 9 };
                Assert.AreEqual(9, badge.Value);
            });
        }

        [TestMethod]
        public void InfoBadge_Template_Applies()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApp();
                MergeGeneric(app);
                Window window = new();
                Controls.InfoBadge badge = new() { Value = 2, Width = 32, Height = 32 };
                try
                {
                    window.Content = badge;
                    window.Show();
                    Drain(window.Dispatcher);
                    _ = badge.ApplyTemplate();
                    Assert.IsNotNull(badge.Template);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
        public void ListBox_GetContainerForItemOverride_ReturnsFluentListBoxItem()
        {
            WpfTestSta.Invoke(static () =>
            {
                Controls.ListBox list = new();
                MethodInfo? m = typeof(Controls.ListBox).GetMethod(
                    "GetContainerForItemOverride",
                    BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.IsNotNull(m);
                object? container = m.Invoke(list, []);
                Assert.IsInstanceOfType(container, typeof(Controls.ListBoxItem));
            });
        }
    }
}
