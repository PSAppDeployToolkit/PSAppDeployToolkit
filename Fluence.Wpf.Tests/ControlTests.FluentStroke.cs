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

using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;
using Xunit;

namespace Fluence.Wpf.Tests
{
    public partial class ControlTests
    {
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
        public void RadioButton_OuterRing_UsesControlStrongStrokeBrush()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse outerEllipse = Assert.IsAssignableFrom<Ellipse>(FindVisualChildByName<Ellipse>(radio, "OuterEllipse"));

                    Brush expected = Assert.IsAssignableFrom<Brush>(radio.FindResource("ControlStrongStrokeColorDefaultBrush"));
                    Assert.Same(expected, outerEllipse.Stroke);

                    Color strokeColor = ((SolidColorBrush)outerEllipse.Stroke).Color;
                    Assert.Equal(0x72, strokeColor.A);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void RadioButton_OuterRing_SwitchesToDisabledStrokeWhenDisabled()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    radio.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Ellipse outerEllipse = Assert.IsAssignableFrom<Ellipse>(FindVisualChildByName<Ellipse>(radio, "OuterEllipse"));

                    Brush expected = Assert.IsAssignableFrom<Brush>(radio.FindResource("ControlStrongStrokeColorDisabledBrush"));
                    Assert.Same(expected, outerEllipse.Stroke);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void CheckBox_CheckedGlyph_UsesIndeterminateDashStrokeWeight()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    Path checkGlyph = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(checkBox, "CheckGlyph"));
                    Border indeterminateDash = Assert.IsAssignableFrom<Border>(FindVisualChildByName<Border>(checkBox, "IndeterminateDash"));

                    // The check-in storyboard now fades the glyph in, so sample until it
                    // settles at the trigger setter steady state instead of asserting
                    // immediately after the window shows.
                    Assert.True(WaitUntil(window.Dispatcher, 3000, () => checkGlyph.Opacity >= 0.99),
                        "Checked CheckBox state should show the check glyph once the check-in animation settles.");
                    Assert.Equal(0.0, indeterminateDash.Opacity, 0.01);
                    Assert.Equal(indeterminateDash.Height, checkGlyph.StrokeThickness, 0.01);
                    Assert.Same(indeterminateDash.Background, checkGlyph.Stroke);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void CheckBox_CheckIn_GlyphAnimatesInAndUncheckRevertsInstantly()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    Path checkGlyph = Assert.IsAssignableFrom<Path>(FindVisualChildByName<Path>(checkBox, "CheckGlyph"));
                    Assert.Equal(0.0, checkGlyph.Opacity, 0.001);

                    checkBox.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);

                    // The check-in storyboard uses FillBehavior="Stop", so once the clocks
                    // finish the glyph must fall back to the setter-provided steady state:
                    // opacity 1 from the checked trigger and 1.0/1.0 from the inline
                    // ScaleTransform. WPF keeps the finished clocks attached (so
                    // HasAnimatedProperties stays true); the observable contract is that
                    // the stopped clocks hold nothing and the base values win.
                    bool settled = WaitUntil(window.Dispatcher, 5000, () =>
                        checkGlyph.RenderTransform is ScaleTransform liveScale
                        && checkGlyph.Opacity >= 0.9999
                        && liveScale.ScaleX >= 0.9999
                        && liveScale.ScaleY >= 0.9999);
                    Assert.True(settled,
                        "The check-in storyboard should complete and hand the glyph back to the trigger setter steady state.");

                    ScaleTransform scale = Assert.IsType<ScaleTransform>(checkGlyph.RenderTransform);
                    Assert.Equal(1.0, checkGlyph.Opacity, 0.001);
                    Assert.Equal(1.0, scale.ScaleX, 0.001);
                    Assert.Equal(1.0, scale.ScaleY, 0.001);

                    checkBox.IsChecked = false;
                    DrainDispatcher(window.Dispatcher);

                    // Uncheck is deliberately not animated: the trigger setters revert
                    // instantly and the finished Stop storyboard holds nothing, so the
                    // glyph disappears in the same dispatcher pass.
                    Assert.Equal(0.0, checkGlyph.Opacity, 0.001);
                    Assert.Equal(1.0, scale.ScaleX, 0.001);
                    Assert.Equal(1.0, scale.ScaleY, 0.001);

                    // A second check-in must replay the animation and settle again
                    // (SnapshotAndReplace hands off the finished clocks).
                    checkBox.IsChecked = true;
                    DrainDispatcher(window.Dispatcher);
                    bool resettled = WaitUntil(window.Dispatcher, 5000, () =>
                        checkGlyph.RenderTransform is ScaleTransform liveScale
                        && checkGlyph.Opacity >= 0.9999
                        && liveScale.ScaleX >= 0.9999
                        && liveScale.ScaleY >= 0.9999);
                    Assert.True(resettled,
                        "Re-checking must replay the check-in storyboard and settle at the steady state again.");
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void Card_Click_FiresOnMouseDownThenUp_WhenIsClickable()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void Card_Click_DoesNotFire_WhenNotClickable()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void NavigationView_Left_ContentBorder_HasWinUiCornerRadiusAndStroke()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
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

                    Brush expectedStroke = Assert.IsAssignableFrom<Brush>(nav.FindResource("NavigationViewContentSeparatorBrush"));
                    Assert.Same(expectedStroke, strokeBorder.BorderBrush);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void NavigationView_LeftCompact_ContentBorder_HasWinUiCornerRadiusAndStroke()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
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

                    Brush expectedStroke = Assert.IsAssignableFrom<Brush>(nav.FindResource("NavigationViewContentSeparatorBrush"));
                    Assert.Same(expectedStroke, strokeBorder.BorderBrush);
                }
                finally
                {
                    window.Content = null;
                    window.UpdateLayout();
                    window.Close();
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }

        [Fact]
        public void NavigationView_DefaultStyle_AppliesLeftTemplate()
        {
            RunOnStaThread(static () =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
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
                    DrainDispatcher(window.Dispatcher);
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
                    if (genericDictionary is not null)
                    {
                        _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                    }
                }
            });
        }
    }
}
