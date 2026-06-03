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
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Shapes;

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
                    Source = this
                };
                OnMouseLeftButtonDown(args);
            }

            public void SimulateMouseUp()
            {
                MouseButtonEventArgs args = new(Mouse.PrimaryDevice, 0, MouseButton.Left)
                {
                    RoutedEvent = MouseLeftButtonUpEvent,
                    Source = this
                };
                OnMouseLeftButtonUp(args);
            }
        }

        [TestMethod]
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
                        Height = 40
                    };
                    window.Content = radio;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse? outerEllipse = FindVisualChildByName<Ellipse>(radio, "OuterEllipse");
                    Assert.IsNotNull(outerEllipse, "RadioButton template should contain OuterEllipse.");

                    Brush? expected = radio.FindResource("ControlStrongStrokeColorDefaultBrush") as Brush;
                    Assert.IsNotNull(expected, "ControlStrongStrokeColorDefaultBrush should be defined in the theme.");
                    Assert.AreSame(expected, outerEllipse.Stroke,
                        "Unchecked RadioButton ring must use ControlStrongStrokeColorDefaultBrush for WinUI 3 visibility parity.");

                    Color strokeColor = ((SolidColorBrush)outerEllipse.Stroke).Color;
                    Assert.AreEqual(0x72, strokeColor.A,
                        "Light theme ControlStrongStrokeColorDefault alpha must be 0x72 (WinUI canonical value).");
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

        [TestMethod]
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
                        Height = 40
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

                    Ellipse? outerEllipse = FindVisualChildByName<Ellipse>(radio, "OuterEllipse");
                    Assert.IsNotNull(outerEllipse);

                    Brush? expected = radio.FindResource("ControlStrongStrokeColorDisabledBrush") as Brush;
                    Assert.IsNotNull(expected, "ControlStrongStrokeColorDisabledBrush must exist in the theme.");
                    Assert.AreSame(expected, outerEllipse.Stroke,
                        "Disabled RadioButton ring must swap to ControlStrongStrokeColorDisabledBrush.");
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

        [TestMethod]
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
                        Height = 40
                    };
                    window.Content = checkBox;
                    window.Width = 240;
                    window.Height = 80;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = checkBox.ApplyTemplate();
                    Path? checkGlyph = FindVisualChildByName<Path>(checkBox, "CheckGlyph");
                    Border? indeterminateDash = FindVisualChildByName<Border>(checkBox, "IndeterminateDash");
                    Assert.IsNotNull(checkGlyph, "CheckBox template should contain CheckGlyph.");
                    Assert.IsNotNull(indeterminateDash, "CheckBox template should contain IndeterminateDash.");

                    Assert.AreEqual(1.0, checkGlyph.Opacity, 0.01,
                        "Checked CheckBox state should show the check glyph.");
                    Assert.AreEqual(0.0, indeterminateDash.Opacity, 0.01,
                        "Checked CheckBox state should hide the indeterminate dash.");
                    Assert.AreEqual(indeterminateDash.Height, checkGlyph.StrokeThickness, 0.01,
                        "Checked CheckBox glyph stroke should be as prominent as the indeterminate dash.");
                    Assert.AreSame(indeterminateDash.Background, checkGlyph.Stroke,
                        "Checked CheckBox glyph should use the same on-accent brush as the indeterminate dash.");
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

        [TestMethod]
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
                        Height = 120
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
                    Assert.IsTrue(card.IsPressed, "Card.IsPressed should flip true after a left-button press while clickable.");

                    card.SimulateMouseUp();
                    Assert.IsFalse(card.IsPressed, "Card.IsPressed should clear after left-button release.");

                    Assert.AreEqual(1, clicks, "Card.Click must fire exactly once on a press-then-release cycle.");

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

        [TestMethod]
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
                        Height = 120
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
                    Assert.IsFalse(card.IsPressed, "Card.IsPressed must stay false when IsClickable is false.");

                    card.SimulateMouseUp();
                    Assert.AreEqual(0, clicks, "Card.Click must not fire when IsClickable is false.");

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

        [TestMethod]
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
                        PaneDisplayMode = NavigationViewPaneDisplayMode.Left
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    ContentPresenter? contentPresenter = nav.Template.FindName("PART_ContentPresenter", nav) as ContentPresenter;
                    Assert.IsNotNull(contentPresenter, "Left template must expose PART_ContentPresenter.");

                    Border? contentBorder = VisualTreeHelper.GetParent(contentPresenter) as Border;
                    Assert.IsNotNull(contentBorder, "PART_ContentPresenter must be hosted by a Border in the Left template.");

                    Assert.AreEqual(new CornerRadius(8, 0, 0, 0), contentBorder.CornerRadius,
                        "Left-mode content background Border must carry an 8,0,0,0 corner radius so the content presenter keeps the page corner rounding.");

                    // The 1,1,0,0 stroke sits on a sibling decorative Border so
                    // PART_ContentPresenter lines up with the pane column edge.
                    // Wrapping the presenter in a BorderThickness=1 Border introduces
                    // layout-rounding drift at 150% DPI.
                    Grid? contentGrid = VisualTreeHelper.GetParent(contentBorder) as Grid;
                    Assert.IsNotNull(contentGrid, "The content Border must be hosted in a Grid that also carries the decorative stroke Border.");
                    Assert.AreEqual(2, VisualTreeHelper.GetChildrenCount(contentGrid),
                        "The content Grid must contain exactly two children: the background Border and the decorative stroke Border.");

                    Border? strokeBorder = VisualTreeHelper.GetChild(contentGrid, 1) as Border;
                    Assert.IsNotNull(strokeBorder, "The second child of the content Grid must be the decorative stroke Border.");
                    Assert.IsFalse(strokeBorder.IsHitTestVisible, "The decorative stroke Border must not capture hit-tests.");
                    Assert.AreEqual(new CornerRadius(8, 0, 0, 0), strokeBorder.CornerRadius,
                        "The decorative stroke Border must share the 8,0,0,0 corner radius of the content background Border.");
                    Assert.AreEqual(new Thickness(1, 1, 0, 0), strokeBorder.BorderThickness,
                        "Left-mode content region must draw a 1,1,0,0 stroke separating it from the pane and top chrome.");

                    Brush? expectedStroke = nav.FindResource("CardStrokeColorDefaultBrush") as Brush;
                    Assert.IsNotNull(expectedStroke, "CardStrokeColorDefaultBrush should be available from the active theme.");
                    Assert.AreSame(expectedStroke, strokeBorder.BorderBrush,
                        "Left-mode content region stroke must bind to CardStrokeColorDefaultBrush so theme switching updates it.");
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

        [TestMethod]
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
                        PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Home" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();
                    ContentPresenter? contentPresenter = nav.Template.FindName("PART_ContentPresenter", nav) as ContentPresenter;
                    Assert.IsNotNull(contentPresenter, "LeftCompact template must expose PART_ContentPresenter.");

                    Border? contentBorder = VisualTreeHelper.GetParent(contentPresenter) as Border;
                    Assert.IsNotNull(contentBorder, "PART_ContentPresenter must be hosted by a Border in the LeftCompact template.");

                    Assert.AreEqual(new CornerRadius(8, 0, 0, 0), contentBorder.CornerRadius,
                        "LeftCompact-mode content background Border must carry the same 8,0,0,0 corner radius as Left mode.");

                    // The 1,1,0,0 stroke sits on a sibling decorative Border so
                    // PART_ContentPresenter lines up with the pane column edge without
                    // layout-rounding drift.
                    Grid? contentGrid = VisualTreeHelper.GetParent(contentBorder) as Grid;
                    Assert.IsNotNull(contentGrid, "The content Border must be hosted in a Grid that also carries the decorative stroke Border.");
                    Assert.AreEqual(2, VisualTreeHelper.GetChildrenCount(contentGrid),
                        "The content Grid must contain exactly two children: the background Border and the decorative stroke Border.");

                    Border? strokeBorder = VisualTreeHelper.GetChild(contentGrid, 1) as Border;
                    Assert.IsNotNull(strokeBorder, "The second child of the content Grid must be the decorative stroke Border.");
                    Assert.IsFalse(strokeBorder.IsHitTestVisible, "The decorative stroke Border must not capture hit-tests.");
                    Assert.AreEqual(new CornerRadius(8, 0, 0, 0), strokeBorder.CornerRadius,
                        "The decorative stroke Border must share the 8,0,0,0 corner radius of the content background Border.");
                    Assert.AreEqual(new Thickness(1, 1, 0, 0), strokeBorder.BorderThickness,
                        "LeftCompact-mode content region must draw a 1,1,0,0 stroke consistent with Left mode.");

                    Brush? expectedStroke = nav.FindResource("CardStrokeColorDefaultBrush") as Brush;
                    Assert.IsNotNull(expectedStroke, "CardStrokeColorDefaultBrush should be available from the active theme.");
                    Assert.AreSame(expectedStroke, strokeBorder.BorderBrush,
                        "LeftCompact-mode content region stroke must bind to CardStrokeColorDefaultBrush so theme switching updates it.");
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

        [TestMethod]
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
                        Height = 320
                    };
                    _ = nav.Items.Add(new Controls.NavigationViewItem { Content = "Item" });
                    window.Content = nav;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = nav.ApplyTemplate();

                    Assert.AreEqual(NavigationViewPaneDisplayMode.Left, nav.PaneDisplayMode,
                        "Default PaneDisplayMode should be Left to match WinUI 3 default presentation.");

                    Button? paneToggle = nav.Template.FindName("PART_PaneToggleButton", nav) as Button;
                    Assert.IsNotNull(paneToggle, "Default (Left) template must expose PART_PaneToggleButton.");

                    Button? backButton = nav.Template.FindName("PART_BackButton", nav) as Button;
                    Assert.IsNotNull(backButton, "Default (Left) template must expose PART_BackButton.");
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
