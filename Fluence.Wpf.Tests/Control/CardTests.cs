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

using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation.Peers;
using System.Windows.Automation.Provider;
using System.Windows.Media;
using Fluence.Wpf.Automation;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Card"/> control: elevation shadow (Default variant has none; Subtle
    /// and other flat variants have none either), automation peer and clickability.
    /// Authority: WinUI 3 card elevation pattern (LayerFillColorDefaultBrush elevation context).
    /// </summary>
    public sealed class CardTests : IClassFixture<LightThemeFixture>
    {
        public CardTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 C21  Card elevation shadow
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Card_DefaultVariant_IsFlatAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // A Fluent card surface is flat: background, 1 px stroke, radius. Elevation belongs
                    // to transient surfaces, so no variant, Default included, carries an effect.
                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);
                    Assert.Null(outerBorder.Effect);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_Background_IsPaintedByExactlyOneLayerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    SolidColorBrush cardFill = Assert.IsType<SolidColorBrush>(app.TryFindResource("CardBackgroundFillColorDefaultBrush"));

                    // The card fill token is translucent (#B3FFFFFF in Light). Painting it on two nested
                    // borders composites it with itself and renders the card more opaque than the token
                    // specifies, so exactly one element in the template may carry it.
                    int painters = FindVisualChildren<System.Windows.Controls.Border>(card)
                        .Count(b => b.Background is SolidColorBrush brush && brush.Color == cardFill.Color);

                    Assert.Equal(1, painters);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_SubtleVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { Variant = CardVariant.Subtle, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    Assert.Null(outerBorder.Effect);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_OutlinedVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { Variant = CardVariant.Outlined, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    Assert.Null(outerBorder.Effect);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_FilledVariant_NoElevationShadowAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { Variant = CardVariant.Filled, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    Assert.Null(outerBorder.Effect);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_Surface_CarriesStrokeAndRadiusOnTheSameElementAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Card card = new() { Variant = CardVariant.Default, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    // One element owns fill, stroke and radius, so there is no second border to drift.
                    SolidColorBrush expectedStroke = Assert.IsType<SolidColorBrush>(app.TryFindResource("CardStrokeColorDefaultBrush"));
                    SolidColorBrush actualStroke = Assert.IsType<SolidColorBrush>(outerBorder.BorderBrush);
                    Assert.Equal(expectedStroke.Color, actualStroke.Color);
                    Assert.Equal(new Thickness(1), outerBorder.BorderThickness);
                    Assert.Equal(new CornerRadius(8), outerBorder.CornerRadius);

                    // The style routes the radius through OverlayCornerRadius so a consumer can retheme it.
                    Assert.Equal(app.TryFindResource("OverlayCornerRadius"), card.CornerRadius);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        // ---------------------------------------------------------------------------
        // WinUI parity: ItemContainer_themeresources.xaml:6-7 - clickable hover/press
        // reveal uses the Subtle fill family, not a 50% white ControlFill overlay.
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Card_ClickHoverAndPressLayers_UseSubtleFillFamilyAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Card card = new() { Variant = CardVariant.Default, IsClickable = true, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border hoverLayer = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "ClickHoverLayer"), exactMatch: false);
                    System.Windows.Controls.Border pressLayer = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "ClickPressLayer"), exactMatch: false);

                    object? expectedHover = app.TryFindResource("SubtleFillColorSecondaryBrush");
                    object? expectedPress = app.TryFindResource("SubtleFillColorTertiaryBrush");
                    Assert.Equal(expectedHover, hoverLayer.Background);
                    Assert.Equal(expectedPress, pressLayer.Background);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_OutlinedVariant_DisabledKeepsTransparentBackgroundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Card card = new() { Variant = CardVariant.Outlined, IsEnabled = false, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    // Only Default and Filled dim their background when disabled; Outlined must keep
                    // its transparent background instead of picking up ControlFillColorDisabledBrush.
                    SolidColorBrush background = Assert.IsType<SolidColorBrush>(outerBorder.Background);
                    Assert.Equal(Colors.Transparent, background.Color);

                    SolidColorBrush expectedStroke = Assert.IsType<SolidColorBrush>(app.TryFindResource("CardStrokeColorDefaultBrush"));
                    SolidColorBrush actualStroke = Assert.IsType<SolidColorBrush>(outerBorder.BorderBrush);
                    Assert.Equal(expectedStroke.Color, actualStroke.Color);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Card_DefaultVariant_DisabledDimsBackgroundAndBorderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                Card card = new() { Variant = CardVariant.Default, IsEnabled = false, Width = 200, Height = 100 };
                Window w = new() { Content = card, Width = 300, Height = 200 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border outerBorder = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(card, "OuterBorder"), exactMatch: false);

                    object? expectedBackground = app.TryFindResource("ControlFillColorDisabledBrush");
                    object? expectedStroke = app.TryFindResource("CardStrokeColorDefaultBrush");
                    Assert.Equal(expectedBackground, outerBorder.Background);
                    Assert.Equal(expectedStroke, outerBorder.BorderBrush);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        // ---------------------------------------------------------------------------
        // Clickable Card - automation peer
        // ---------------------------------------------------------------------------

        [Fact]
        public Task ClickableCard_AutomationPeer_IsCardAutomationPeerAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    _ = Assert.IsType<CardAutomationPeer>(peer, exactMatch: false);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ClickableCard_AutomationControlType_IsButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    Assert.Equal(AutomationControlType.Button, peer.GetAutomationControlType());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ClickableCard_GetPattern_Invoke_ReturnsInvokeProviderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    object pattern = Assert.IsType<object>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    _ = Assert.IsType<IInvokeProvider>(pattern, exactMatch: false);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ClickableCard_InvokePattern_RaisesClickEventAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = true,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    bool clickRaised = false;
                    card.Click += (_, _) => clickRaised = true;

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    IInvokeProvider invokeProvider = Assert.IsType<IInvokeProvider>(peer.GetPattern(PatternInterface.Invoke), exactMatch: false);
                    invokeProvider.Invoke();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(clickRaised,
                        "IInvokeProvider.Invoke() must raise the Card Click routed event.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ClickableCard_IsTabStop_IsTrueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { IsClickable = true };
                Assert.True(card.IsTabStop,
                    "A clickable Card must be IsTabStop=true so keyboard users can reach it.");
                Assert.True(card.Focusable,
                    "A clickable Card must be Focusable=true.");
            });
        }

        [Fact]
        public Task NonClickableCard_AutomationControlType_IsNotButtonAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = false,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    Assert.NotEqual(AutomationControlType.Button, peer.GetAutomationControlType());
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NonClickableCard_GetPattern_Invoke_ReturnsNullAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Card card = new()
                    {
                        IsClickable = false,
                        Width = 200,
                        Height = 100,
                    };
                    window.Content = card;
                    window.Width = 300;
                    window.Height = 200;
                    window.Show();
                    _ = card.ApplyTemplate();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(card);
                    object? pattern = peer.GetPattern(PatternInterface.Invoke);
                    Assert.Null(pattern);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task NonClickableCard_IsTabStop_IsFalseAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { IsClickable = false };
                Assert.False(card.IsTabStop,
                    "A non-clickable Card must not be in the tab order.");
            });
        }

        [Fact]
        public Task Stage3_Card_DefaultVariant_IsDefaultAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new();
                Assert.Equal(CardVariant.Default, card.Variant);
            });
        }

        [Fact]
        public Task Stage3_Card_IsClickable_ExposesIsPressedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Card card = new() { IsClickable = true };
                Assert.False(card.IsPressed);
            });
        }
    }
}
