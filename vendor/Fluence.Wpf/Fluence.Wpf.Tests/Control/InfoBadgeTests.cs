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
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="InfoBadge"/> control: DisplayKindStates VSM group.
    /// </summary>
    public sealed class InfoBadgeTests : IClassFixture<LightThemeFixture>
    {
        public InfoBadgeTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 A10  InfoBadge DisplayKindStates VSM group
        // ---------------------------------------------------------------------------

        [Fact]
        public Task InfoBadge_DisplayKindStates_GroupExistsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // Verify the VSM group is present in the template.
                    IList groups = VisualStateManager.GetVisualStateGroups(
                        FindVisualChild<Grid>(badge));
                    bool found = false;
                    if (groups is not null)
                    {
                        foreach (object? g in groups)
                        {
                            if (g is VisualStateGroup vsg && string.Equals(vsg.Name, "DisplayKindStates", StringComparison.Ordinal))
                            { found = true; break; }
                        }
                    }
                    Assert.True(found, "InfoBadge template must contain a VisualStateGroup named 'DisplayKindStates'.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_DefaultState_IsDotAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                // Default: Value=-1, no IconSource → Dot state.
                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // DotIndicator should be visible; BadgeBorder should be collapsed.
                    Ellipse dot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(badge, "DotIndicator"), exactMatch: false);
                    System.Windows.Controls.Border border = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, dot.Visibility);
                    Assert.Equal(Visibility.Collapsed, border.Visibility);

                    // WinUI leaves a dot badge at its 4 dip minimum (InfoBadge_themeresources.xaml:7-8).
                    Assert.Equal(4.0, dot.Width, 0.1);
                    Assert.Equal(4.0, dot.Height, 0.1);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_ValueSet_ShowsBadgeBorderAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new() { Value = 5 };
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse? dot = FindVisualChildByName<Ellipse>(badge, "DotIndicator");
                    System.Windows.Controls.Border? border = FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder");
                    Assert.Equal(Visibility.Collapsed, dot?.Visibility);
                    Assert.Equal(Visibility.Visible, border?.Visibility);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_ValueText_SitsCentredInsideThePillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new()
                {
                    Value = 12,
                    VerticalAlignment = VerticalAlignment.Top,
                    HorizontalAlignment = HorizontalAlignment.Left,
                };
                Window w = new() { Content = badge, Width = 200, Height = 120 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    System.Windows.Controls.Border border = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder"), exactMatch: false);
                    System.Windows.Controls.TextBlock text = Assert.IsType<System.Windows.Controls.TextBlock>(
                        FindVisualChild<System.Windows.Controls.TextBlock>(border), exactMatch: false);

                    // The value arrives as a string, so the presenter generates this TextBlock, and the
                    // implicit TextBlock style in Typography.xaml beats any inherited TextElement value.
                    // Unscoped it sized the numeral at 14 in a 16 dip pill, whose 18.67 line box spilled
                    // out of the capsule and sat low in it.
                    Assert.Equal(11.0, text.FontSize, 0.01);

                    // WinUI InfoBadgeMaxHeight 16 (InfoBadge_themeresources.xaml:9), kept a capsule by
                    // the half-height radius InfoBadge recomputes on every size change.
                    Assert.Equal(16.0, border.ActualHeight, 0.01);
                    Assert.Equal(8.0, badge.CornerRadius.TopLeft, 0.01);

                    double textTop = text.TransformToAncestor(border).Transform(new Point(0, 0)).Y;
                    double textBottom = border.ActualHeight - textTop - text.ActualHeight;
                    Assert.True(
                        text.ActualHeight <= border.ActualHeight,
                        string.Format(CultureInfo.InvariantCulture, "The numeral must fit the pill; it measured {0} in {1}.", text.ActualHeight, border.ActualHeight));

                    // Centred within a dip rather than exactly, because the split depends on the
                    // numeral's line box and that depends on which font in the FluentFontFamily
                    // chain is installed. A machine with Segoe UI Variable measures 14.67 at 11 dip
                    // and splits the 1.33 remainder evenly; one that falls back to Segoe UI measures
                    // 15, and UseLayoutRounding sends the odd 1 dip remainder to a single side. Both
                    // are centred. Neither is the defect this test guards, which was an 18.67 line
                    // box spilling out of the 16 dip capsule and sitting low in it, caught by the
                    // fit assertion above and by a gap difference far wider than a dip.
                    Assert.True(
                        Math.Abs(textTop - textBottom) <= 1.0,
                        string.Format(CultureInfo.InvariantCulture, "The numeral must sit centred in the pill; it measured {0} above and {1} below in {2}.", textTop, textBottom, border.ActualHeight));
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_ValueBadge_UsesStableScreenshotPillMetricsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new() { Value = 12 };
                Window w = new() { Content = badge, Width = 100, Height = 80 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);
                    w.UpdateLayout();

                    System.Windows.Controls.Border border = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder"), exactMatch: false);
                    ContentPresenter content = Assert.IsType<ContentPresenter>(FindVisualChildByName<ContentPresenter>(badge, "ContentArea"), exactMatch: false);
                    // WinUI InfoBadge metrics (InfoBadge_themeresources.xaml:7-15): a 4 dip floor for
                    // the dot, a 16 dip ceiling for the pill, no padding, and the capsule's
                    // breathing room carried by the content margin instead.
                    Assert.Equal(4.0, border.MinWidth, 0.1);
                    Assert.Equal(16.0, border.MaxHeight, 0.1);
                    Assert.Equal(new Thickness(0), border.Padding);

                    // The capsule stands at WinUI's 16 dip ceiling rather than floating at the height
                    // of its own text: WinUI reaches 16 through the 2 dip bottom inset on the text
                    // margin, which WPF's line box does not need, so the pill asks for the height and
                    // the control's own 4 dip MinHeight stays with the dot beside it.
                    Assert.Equal(16.0, border.MinHeight, 0.1);
                    Assert.Equal(4.0, badge.MinHeight, 0.1);

                    // The horizontal 4s are WinUI's; the vertical 2 is not carried, because WPF's
                    // line box at 11 dip already centres in the pill without it.
                    Assert.Equal(new Thickness(4, 0, 4, 0), content.Margin);
                    Assert.Equal(16.0, badge.ActualHeight, 0.5);
                    Assert.Equal(11.0, TextElement.GetFontSize(content), 0.1);
                    Assert.Equal(HorizontalAlignment.Center, content.HorizontalAlignment);
                    Assert.Equal(VerticalAlignment.Center, content.VerticalAlignment);
                    // WinUI's ValueTextBlock sets no weight (InfoBadge.xaml:82), so the numeral
                    // inherits the default body Normal rather than the SemiBold Fluence stamped.
                    Assert.Equal(FontWeights.Normal, TextElement.GetFontWeight(content));
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_DisplayKindStates_HasAllFourStatesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new();
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    IList groups = VisualStateManager.GetVisualStateGroups(FindVisualChild<Grid>(badge));
                    VisualStateGroup dkg = Assert.IsType<VisualStateGroup>(groups.OfType<VisualStateGroup>().FirstOrDefault(static vsg => string.Equals(vsg.Name, "DisplayKindStates", StringComparison.Ordinal)));
                    HashSet<string> stateNames = new(StringComparer.OrdinalIgnoreCase);
                    foreach (object? s in dkg.States)
                    {
                        if (s is VisualState vs)
                        {
                            _ = stateNames.Add(vs.Name);
                        }
                    }
                    Assert.True(stateNames.Contains("Dot"), "DisplayKindStates must include 'Dot'.");
                    Assert.True(stateNames.Contains("Icon"), "DisplayKindStates must include 'Icon'.");
                    Assert.True(stateNames.Contains("FontIcon"), "DisplayKindStates must include 'FontIcon'.");
                    Assert.True(stateNames.Contains("Value"), "DisplayKindStates must include 'Value'.");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_Foreground_IsTextOnAccentFillColorPrimaryAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();

                // WinUI keys the badge's own foreground (InfoBadgeForeground,
                // InfoBadge_themeresources.xaml:5) rather than reusing a text token, because the
                // numeral has to pair with the plate it sits on. Outside high contrast that key
                // resolves to the on-accent text colour, which is the accent-derived default
                // background's partner, and not TextFillColorInverseBrush.
                InfoBadge badge = new() { Value = 5 };
                Window w = new() { Content = badge, Width = 60, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Assert.Equal(app.TryFindResource("InfoBadgeAttentionForegroundBrush"), badge.Foreground);
                    BrushAssert.AssertBrushColor(badge.Foreground, "TextOnAccentFillColorPrimaryBrush");
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_Value_RoundtripsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new() { Value = 9 };
                Assert.Equal(9, badge.Value);
            });
        }

        [Fact]
        public Task InfoBadge_Template_AppliesAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                InfoBadge badge = new() { Value = 2, Width = 32, Height = 32 };
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
        public Task InfoBadge_DisplayKind_PrefersValueOverIconAndFallsBackAsync()
        {
            // WinUI resolves the display kind value first, then icon, then dot (InfoBadge.cpp
            // OnPropertyChanged tests Value() >= 0 before IconSource). Fluence had it the other way
            // round, and a value set on a badge that already had an icon was dropped on the floor.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 120, Height = 80 };
                InfoBadge badge = new() { IconSource = new FontIcon { Glyph = "" } };

                try
                {
                    window.Content = badge;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = Assert.IsType<FontIcon>(badge.Content);

                    badge.SetCurrentValue(InfoBadge.ValueProperty, 7);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal("7", badge.Content);

                    // Clearing the value falls back to the icon rather than leaving the badge blank.
                    badge.SetCurrentValue(InfoBadge.ValueProperty, -1);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = Assert.IsType<FontIcon>(badge.Content);

                    // And with neither, the dot.
                    badge.ClearValue(InfoBadge.IconSourceProperty);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Null(badge.Content);
                    Ellipse dot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(badge, "DotIndicator"), exactMatch: false);
                    Assert.Equal(Visibility.Visible, dot.Visibility);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_ValueBadge_IsNeverNarrowerThanItIsTallAsync()
        {
            // WinUI squares the badge up when its natural width comes out under its height
            // (InfoBadge.cpp MeasureOverride), which is what makes a single digit a circle rather
            // than a squashed oval, and it recomputes the radius as half the height on every size
            // change (OnSizeChanged). With WinUI's own 4 dip minimum width, nothing else holds that
            // shape; the old 34 dip minimum width had been masking it.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 120, Height = 80 };
                InfoBadge badge = new() { Value = 3, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

                try
                {
                    window.Content = badge;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(16.0, badge.ActualHeight, 0.5);
                    Assert.True(
                        badge.ActualWidth >= badge.ActualHeight - 0.5,
                        "A one digit badge must be at least as wide as it is tall.");
                    Assert.Equal(badge.ActualHeight / 2, badge.CornerRadius.TopLeft, 0.5);

                    // A radius the consumer pins is left alone, as WinUI leaves a local value alone.
                    badge.SetCurrentValue(InfoBadge.CornerRadiusProperty, new CornerRadius(2));
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(2.0, badge.CornerRadius.TopLeft, 0.01);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_Value_RejectsAnythingBelowMinusOneAsync()
        {
            // WinUI throws hresult_out_of_bounds for a value under -1 (InfoBadge.cpp). -1 is the
            // dot; anything below it has no rendering of its own, so accepting it would draw a dot
            // and hide the mistake. WPF's ValidateValueCallback surfaces as ArgumentException.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                InfoBadge badge = new();

                _ = Assert.Throws<ArgumentException>(() => badge.Value = -2);
                _ = Assert.Throws<ArgumentException>(() => badge.SetValue(InfoBadge.ValueProperty, -17));

                badge.Value = -1;
                Assert.Equal(-1, badge.Value);
                badge.Value = 0;
                Assert.Equal(0, badge.Value);
            });
        }

        [Fact]
        public static void InfoBadge_GetStyleGlyph_ReturnsWinUiGlyphPerSeverity()
        {
            // WinUI carries these on its per-severity icon styles
            // (InfoBadge_themeresources.xaml:99,111,122,133,144). The helper hands them to a
            // consumer who wants the icon form, in the same shape as InfoBar.GetSeverityGlyph.
            Assert.Equal("", InfoBadge.GetStyleGlyph(InfoBadgeStyle.Attention));
            Assert.Equal("", InfoBadge.GetStyleGlyph(InfoBadgeStyle.Informational));
            Assert.Equal("", InfoBadge.GetStyleGlyph(InfoBadgeStyle.Success));
            Assert.Equal("", InfoBadge.GetStyleGlyph(InfoBadgeStyle.Caution));
            Assert.Equal("", InfoBadge.GetStyleGlyph(InfoBadgeStyle.Critical));

            _ = Assert.Throws<ArgumentOutOfRangeException>(static () => InfoBadge.GetStyleGlyph((InfoBadgeStyle)99));
        }

        [Fact]
        public Task InfoBadge_SeverityWithAValue_ShowsTheValueNotAGlyphAsync()
        {
            // The glyph is opt-in precisely so it cannot pre-empt a value: WinUI's severity comes
            // in a dot, a value and an icon style, and Fluence has one BadgeStyle property, so
            // supplying the glyph automatically would take the dot and value forms away.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 120, Height = 80 };
                InfoBadge badge = new() { BadgeStyle = InfoBadgeStyle.Critical, Value = 2 };

                try
                {
                    window.Content = badge;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal("2", badge.Content);
                    Assert.Null(badge.IconSource);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task InfoBadge_ValueText_FitsThePillAndIsCentredAsync()
        {
            // Reported from the gallery: the numeral in a NavigationViewItem badge was clipped and
            // sat low. WinUI's 4,0,4,2 text margin nudges its own font's digits optically; WPF's
            // line box for the same 11 dip size is taller, so the bottom inset pushed the text out
            // of the 16 dip pill instead of seating it.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 160, Height = 100 };
                InfoBadge badge = new() { Value = 12, HorizontalAlignment = HorizontalAlignment.Left, VerticalAlignment = VerticalAlignment.Top };

                try
                {
                    window.Content = badge;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border pill = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(badge, "BadgeBorder"), exactMatch: false);
                    ContentPresenter content = Assert.IsType<ContentPresenter>(
                        FindVisualChildByName<ContentPresenter>(badge, "ContentArea"), exactMatch: false);

                    // Nothing is cut off: the text's own box fits inside the pill.
                    Assert.True(
                        content.ActualHeight <= pill.ActualHeight + 0.5,
                        FormatFailure("The value text is taller than the pill", content.ActualHeight, pill.ActualHeight));
                    Assert.True(
                        content.DesiredSize.Height <= content.ActualHeight + 0.5,
                        FormatFailure("The value text is arranged shorter than it measured, so it is clipped", content.ActualHeight, content.DesiredSize.Height));

                    // And it is centred in the pill rather than riding low or high.
                    Point contentTopLeft = content.TransformToAncestor(pill).Transform(new Point(0, 0));
                    double above = contentTopLeft.Y;
                    double below = pill.ActualHeight - (contentTopLeft.Y + content.ActualHeight);
                    Assert.True(
                        Math.Abs(above - below) <= 1.0,
                        FormatFailure("The value text is not vertically centred in the pill", above, below));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Builds an assertion message carrying the two measurements that disagree, because a bare
        /// true/false says nothing about how far out the layout is.
        /// </summary>
        /// <param name="what">What went wrong.</param>
        /// <param name="first">The first measurement.</param>
        /// <param name="second">The measurement it is compared against.</param>
        /// <returns>The formatted message.</returns>
        private static string FormatFailure(string what, double first, double second)
        {
            // string.Create with a culture takes an interpolated string handler, which net472 does
            // not have, so this is the portable spelling.
            return string.Format(CultureInfo.InvariantCulture, "{0}: {1} against {2}.", what, first, second);
        }
    }
}
