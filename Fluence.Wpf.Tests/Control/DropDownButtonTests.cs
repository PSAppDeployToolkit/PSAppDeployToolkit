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

using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Controls.DropDownButton"/> control: the flyout popup template part,
    /// CloseFlyout tearing down an open popup and unchecking the button, and the flyout
    /// presenter stretching to fit left-aligned flyout content.
    /// </summary>
    public sealed class DropDownButtonTests : IClassFixture<LightThemeFixture>
    {
        public DropDownButtonTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task DropDownButton_Template_HasFlyoutPresenterNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
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
        public Task DropDownButton_CloseFlyout_ClosesOpenPopupAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.DropDownButton btn = new() { Content = "Open", Width = 120, Flyout = new TextBlock { Text = "Flyout" } };
                try
                {
                    window.Content = btn;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    _ = btn.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(btn.Template.FindName("PART_Popup", btn));
                    btn.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(popup.IsOpen, "Checking the button should open the dropdown popup.");

                    // The application close path after handling a click on arbitrary flyout
                    // content (WinUI parity: plain flyouts never dismiss themselves).
                    btn.CloseFlyout();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.False(popup.IsOpen, "CloseFlyout should close the dropdown popup.");
                    Assert.False(btn.IsChecked is true, "CloseFlyout should uncheck the button.");

                    // Closing an already-closed flyout is a no-op.
                    btn.CloseFlyout();
                    Assert.False(popup.IsOpen);
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
    }
}
