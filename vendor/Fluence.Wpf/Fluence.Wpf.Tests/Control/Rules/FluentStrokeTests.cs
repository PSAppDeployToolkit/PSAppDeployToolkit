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
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Helpers;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// Cross-control Fluent stroke and glyph compositing: RadioButton ring alpha, CheckBox
    /// check-in glyph animation and stroke weight, Card click semantics, and NavigationView
    /// content-border corner radius and stroke placement.
    /// </summary>
    public sealed class FluentStrokeTests : IClassFixture<LightThemeFixture>
    {
        public FluentStrokeTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // Lightweight subclass that exposes the protected mouse button overrides so we
        // can assert Card click semantics without relying on a real input device.
        private sealed class ClickableCardProbe : Controls.Card
        {
            public void SimulateMouseDown()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonDownEvent,
                    Source = this,
                };
                OnMouseLeftButtonDown(args);
            }

            public void SimulateMouseUp()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonUpEvent,
                    Source = this,
                };
                OnMouseLeftButtonUp(args);
            }
        }

        [Fact]
        public Task RadioButton_OuterRing_UsesControlStrongStrokeBrushAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.RadioButton radio = new()
                    {
                        Content = "Ring",
                        Width = 200,
                        Height = 40,
                    };
                    window.Content = radio;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse outerEllipse = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(radio, "OuterEllipse"), exactMatch: false);

                    Brush expected = Assert.IsType<Brush>(radio.FindResource("ControlStrongStrokeColorDefaultBrush"), exactMatch: false);
                    Assert.Same(expected, outerEllipse.Stroke);

                    Color strokeColor = ((SolidColorBrush)outerEllipse.Stroke).Color;
                    Assert.Equal(0x72, strokeColor.A);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task RadioButton_OuterRing_SwitchesToDisabledStrokeWhenDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.RadioButton radio = new()
                    {
                        Content = "Disabled ring",
                        Width = 200,
                        Height = 40,
                    };
                    window.Content = radio;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    radio.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Ellipse outerEllipse = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(radio, "OuterEllipse"), exactMatch: false);

                    Brush expected = Assert.IsType<Brush>(radio.FindResource("ControlStrongStrokeColorDisabledBrush"), exactMatch: false);
                    Assert.Same(expected, outerEllipse.Stroke);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task CheckBox_CheckedGlyph_UsesIndeterminateDashStrokeWeightAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.CheckBox checkBox = new()
                    {
                        Content = "Check",
                        IsChecked = true,
                        IsHitTestVisible = false,
                        Width = 200,
                        Height = 40,
                    };
                    window.Content = checkBox;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    Path checkGlyph = Assert.IsType<Path>(FindVisualChildByName<Path>(checkBox, "PART_CheckGlyph"), exactMatch: false);
                    Path indeterminateDash = Assert.IsType<Path>(FindVisualChildByName<Path>(checkBox, "PART_IndeterminateGlyph"), exactMatch: false);

                    Assert.Equal(1.0, checkGlyph.Opacity, 0.01);
                    Assert.Equal(0.0, indeterminateDash.Opacity, 0.01);
                    Assert.Equal(1.225, checkGlyph.StrokeThickness, 0.001);
                    Assert.Equal(checkGlyph.StrokeThickness, indeterminateDash.StrokeThickness, 0.001);
                    Assert.Equal(PenLineCap.Round, checkGlyph.StrokeStartLineCap);
                    Assert.Equal(PenLineCap.Round, checkGlyph.StrokeDashCap);
                    Assert.Same(checkBox.FindResource("CheckBoxCheckGlyphForegroundCheckedBrush"), checkGlyph.Stroke);
                    Assert.Same(checkBox.FindResource("CheckBoxCheckGlyphForegroundIndeterminateBrush"), indeterminateDash.Stroke);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task CheckBox_CheckIn_DrawsPathAndUncheckErasesFromStartAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                bool? previousMotion = MotionHelper.OverrideIsMotionEnabled;
                MotionHelper.OverrideIsMotionEnabled = true;
                Window window = new();

                try
                {
                    Controls.CheckBox checkBox = new()
                    {
                        Content = "Animated check",
                        IsChecked = false,
                        IsHitTestVisible = false,
                        Width = 200,
                        Height = 40,
                    };
                    window.Content = checkBox;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    Path checkGlyph = Assert.IsType<Path>(FindVisualChildByName<Path>(checkBox, "PART_CheckGlyph"), exactMatch: false);
                    Assert.Equal(0.0, checkGlyph.Opacity, 0.001);

                    checkBox.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 1000,
                        () => checkGlyph.Opacity > 0 && checkGlyph.StrokeDashOffset > 0.1
                            && checkGlyph.StrokeDashOffset < 10.7).ConfigureAwait(true),
                        "Check-in must reveal a partial path by arc length rather than fading or scaling the whole tick.");
                    Assert.Equal(1.0, checkGlyph.Opacity, 0.001);
                    Assert.True(checkGlyph.RenderTransform?.Value.IsIdentity ?? true);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 2000,
                        () => System.Math.Abs(checkGlyph.StrokeDashOffset) < 0.01).ConfigureAwait(true));

                    checkBox.IsChecked = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 1000,
                        () => checkGlyph.StrokeDashOffset < -0.1 && checkGlyph.Opacity > 0).ConfigureAwait(true),
                        "Uncheck must trim the stroke from its start before the 66.7-ms erase completes.");
                    Assert.True(await WaitUntilAsync(window.Dispatcher, 1000,
                        () => checkGlyph.Opacity < 0.01).ConfigureAwait(true));
                }
                finally
                {
                    MotionHelper.OverrideIsMotionEnabled = previousMotion;
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task CheckBox_DefaultStyle_UsesWinUiLeftAlignmentAndMinWidthAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.CheckBox checkBox = new()
                    {
                        Content = "Ok",
                    };
                    Grid grid = new()
                    {
                        Width = 400,
                    };
                    _ = grid.Children.Add(checkBox);
                    window.Content = grid;
                    window.Width = 440;
                    window.Height = 80;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();

                    // WinUI parity: DefaultCheckBoxStyle sets HorizontalAlignment=Left
                    // (CheckBox_themeresources.xaml:284) and MinWidth to CheckBoxMinWidth,
                    // 120 (CheckBox_themeresources.xaml:273,290), so the box, label, and focus
                    // visual sit flush against the parent's left edge with a 120 dp floor
                    // instead of stretching to the parent's width.
                    Point leftEdge = checkBox.TranslatePoint(new Point(0, 0), grid);
                    Assert.True(checkBox.ActualWidth >= 120.0,
                        "CheckBox should keep the 120 dp MinWidth floor for a short label.");
                    Assert.True(checkBox.ActualWidth < 200.0,
                        "CheckBox should not stretch to the 400 dp parent width.");
                    Assert.True(System.Math.Abs(leftEdge.X) < 0.5,
                        "CheckBox should sit flush against the parent's left edge.");

                    // Secondary checks: the setters that drive the layout above.
                    Assert.Equal(HorizontalAlignment.Left, checkBox.HorizontalAlignment);
                    Assert.Equal(120.0, checkBox.MinWidth);

                    Border indicator = Assert.IsType<Border>(FindVisualChildByName<Border>(checkBox, "IndicatorHost"), exactMatch: false);
                    Assert.Equal(20.0, indicator.ActualWidth, 0.01);
                    Assert.Equal(20.0, indicator.ActualHeight, 0.01);
                    Assert.True(checkBox.ActualHeight >= 32.0, "The WinUI CheckBox row has a 32-DIP height floor.");
                    Assert.Equal(new Thickness(8, 5, 0, 0), checkBox.Padding);

                    double indicatorTop = indicator.TransformToAncestor(checkBox).Transform(new Point()).Y;
                    checkBox.Description = "A second line explains this option.";
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    Assert.Equal(indicatorTop,
                        indicator.TransformToAncestor(checkBox).Transform(new Point()).Y, 0.5);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Card_Click_FiresOnMouseDownThenUp_WhenIsClickableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    ClickableCardProbe card = new()
                    {
                        IsClickable = true,
                        Content = "Home",
                        Width = 200,
                        Height = 120,
                    };
                    window.Content = card;
                    window.Width = 240;
                    window.Height = 160;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    int clicks = 0;
                    void handler(object sender, RoutedEventArgs e) { clicks++; }
                    card.Click += handler;

                    card.SimulateMouseDown();
                    Assert.True(card.IsPressed, "Card.IsPressed should flip true after a left-button press while clickable.");

                    card.SimulateMouseUp();
                    Assert.False(card.IsPressed, "Card.IsPressed should clear after left-button release.");

                    Assert.Equal(1, clicks);

                    card.Click -= handler;
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Card_Click_DoesNotFire_WhenNotClickableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    ClickableCardProbe card = new()
                    {
                        IsClickable = false,
                        Content = "Static",
                        Width = 200,
                        Height = 120,
                    };
                    window.Content = card;
                    window.Width = 240;
                    window.Height = 160;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    int clicks = 0;
                    void handler(object sender, RoutedEventArgs e) { clicks++; }
                    card.Click += handler;

                    card.SimulateMouseDown();
                    Assert.False(card.IsPressed, "Card.IsPressed must stay false when IsClickable is false.");

                    card.SimulateMouseUp();
                    Assert.Equal(0, clicks);

                    card.Click -= handler;
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task NavigationView_Left_ContentBorder_HasWinUiCornerRadiusAndStrokeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NavigationView nav = new()
                    {
                        Width = 640,
                        Height = 400,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left,
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    ContentPresenter contentPresenter = Assert.IsType<ContentPresenter>(nav.Template.FindName("PART_ContentPresenter", nav));

                    Border contentBorder = Assert.IsType<Border>(VisualTreeHelper.GetParent(contentPresenter));

                    Assert.Equal(new CornerRadius(8, 0, 0, 0), contentBorder.CornerRadius);

                    // The 1,1,0,0 stroke sits on a sibling decorative Border so
                    // PART_ContentPresenter lines up with the pane column edge.
                    // Wrapping the presenter in a BorderThickness=1 Border introduces
                    // layout-rounding drift at 150% DPI.
                    Grid contentGrid = Assert.IsType<Grid>(VisualTreeHelper.GetParent(contentBorder));
                    Assert.Equal(2, VisualTreeHelper.GetChildrenCount(contentGrid));

                    Border strokeBorder = Assert.IsType<Border>(VisualTreeHelper.GetChild(contentGrid, 1));
                    Assert.False(strokeBorder.IsHitTestVisible, "The decorative stroke Border must not capture hit-tests.");
                    Assert.Equal(new CornerRadius(8, 0, 0, 0), strokeBorder.CornerRadius);
                    Assert.Equal(new Thickness(1, 1, 0, 0), strokeBorder.BorderThickness);

                    Brush expectedStroke = Assert.IsType<Brush>(nav.FindResource("CardStrokeColorDefaultBrush"), exactMatch: false);
                    Assert.Same(expectedStroke, strokeBorder.BorderBrush);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task NavigationView_LeftCompact_ContentBorder_HasWinUiCornerRadiusAndStrokeAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NavigationView nav = new()
                    {
                        Width = 640,
                        Height = 400,
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact,
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    ContentPresenter contentPresenter = Assert.IsType<ContentPresenter>(nav.Template.FindName("PART_ContentPresenter", nav));

                    Border contentBorder = Assert.IsType<Border>(VisualTreeHelper.GetParent(contentPresenter));

                    Assert.Equal(new CornerRadius(8, 0, 0, 0), contentBorder.CornerRadius);

                    // The 1,1,0,0 stroke sits on a sibling decorative Border so
                    // PART_ContentPresenter lines up with the pane column edge without
                    // layout-rounding drift.
                    Grid contentGrid = Assert.IsType<Grid>(VisualTreeHelper.GetParent(contentBorder));
                    Assert.Equal(2, VisualTreeHelper.GetChildrenCount(contentGrid));

                    Border strokeBorder = Assert.IsType<Border>(VisualTreeHelper.GetChild(contentGrid, 1));
                    Assert.False(strokeBorder.IsHitTestVisible, "The decorative stroke Border must not capture hit-tests.");
                    Assert.Equal(new CornerRadius(8, 0, 0, 0), strokeBorder.CornerRadius);
                    Assert.Equal(new Thickness(1, 1, 0, 0), strokeBorder.BorderThickness);

                    Brush expectedStroke = Assert.IsType<Brush>(nav.FindResource("CardStrokeColorDefaultBrush"), exactMatch: false);
                    Assert.Same(expectedStroke, strokeBorder.BorderBrush);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }

        [Fact]
        public Task NavigationView_DefaultStyle_AppliesLeftTemplateAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    Controls.NavigationView nav = new()
                    {
                        Width = 400,
                        Height = 320,
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();

                    Assert.Equal(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode);

                    Button paneToggle = Assert.IsType<Button>(nav.Template.FindName("PART_PaneToggleButton", nav));

                    Button backButton = Assert.IsType<Button>(nav.Template.FindName("PART_BackButton", nav));
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                }
            });
        }
    }
}
