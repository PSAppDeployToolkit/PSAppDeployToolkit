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
using System.Windows.Automation.Peers;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    public sealed class HyperlinkButtonTests : IClassFixture<LightThemeFixture>
    {
        public HyperlinkButtonTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task HyperlinkButton_Peer_IsHyperlinkButtonAutomationPeerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.HyperlinkButton button = new() { Content = "Visit site" };
                    window.Content = button;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    _ = button.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    _ = Assert.IsType<HyperlinkButtonAutomationPeer>(peer, exactMatch: false);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task HyperlinkButton_Peer_ReportsHyperlinkControlTypeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.HyperlinkButton button = new() { Content = "Visit site" };
                    window.Content = button;
                    window.Width = 240;
                    window.Height = 120;
                    window.Show();
                    _ = button.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(button);
                    Assert.Equal(
                        AutomationControlType.Hyperlink,
                        peer.GetAutomationControlType());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task HyperlinkButton_DefaultForeground_IsAccentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                Window window = new();
                Controls.HyperlinkButton button = new()
                {
                    Content = "Link",
                    Width = 120,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentTextFillColorPrimaryBrush"]);
                    _ = Assert.IsType<SolidColorBrush>(button.Foreground, exactMatch: false);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)button.Foreground).Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task HyperlinkButton_Click_WithNavigateUri_DoesNotThrowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Controls.HyperlinkButton button = new()
                {
                    Content = "Link",
                    Width = 120,
                };

                try
                {
                    window.Content = button;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    button.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(button.IsLoaded,
                        "HyperlinkButton should remain loaded after click dispatch.");
                    Assert.Null(button.NavigateUri);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
