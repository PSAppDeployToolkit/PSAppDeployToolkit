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
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Gallery.Pages
{
    /// <summary>
    /// Covers <see cref="GalleryAccessibilityPage"/>.
    /// </summary>
    public sealed class GalleryAccessibilityPageTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureDemoTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return default;
        }

        // This test drives keyboard focus across the page, so it builds its own instance
        // rather than mutating the one the class shares.
        [Fact]
        public Task GalleryAccessibilityPage_KeyboardSamplesUseAlignedRowsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                GalleryAccessibilityPage page = new();
                Window window = DemoTestHost.CreateHostWindow(page);
                try
                {
                    Grid primary = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "KeyboardSupportPrimaryControls"), exactMatch: false);
                    Assert.Equal(4, primary.ColumnDefinitions.Count);
                    Assert.Equal(2, primary.RowDefinitions.Count);
                    Assert.Equal(8, primary.Children.Count);

                    AssertGridCell(primary, static child => child is Controls.Button button && string.Equals(button.Content as string, "Button 1", StringComparison.Ordinal), 0, 0, "Button 1");
                    AssertGridCell(primary, static child => child is Controls.Button button && string.Equals(button.Content as string, "Button 2", StringComparison.Ordinal), 0, 1, "Button 2");
                    AssertGridCell(primary, static child => child is Controls.TextBox, 0, 2, "TextBox");
                    AssertGridCell(primary, static child => child is Controls.ComboBox, 0, 3, "ComboBox");
                    AssertGridCell(primary, static child => child is Controls.CheckBox, 1, 0, "CheckBox");
                    AssertGridCell(primary, static child => child is Controls.ToggleSwitch, 1, 1, "ToggleSwitch");
                    AssertGridCell(primary, static child => child is Controls.Slider, 1, 2, "Slider");
                    AssertGridCell(primary, static child => child is Controls.HyperlinkButton, 1, 3, "HyperlinkButton");

                    Grid tabOrder = Assert.IsType<Grid>(DemoTestHost.FindByName<Grid>(page, "KeyboardSupportExplicitOrderControls"), exactMatch: false);
                    Assert.Equal(3, tabOrder.ColumnDefinitions.Count);
                    Assert.Equal(3, tabOrder.Children.Count);
                    Assert.Equal(KeyboardNavigationMode.Local, KeyboardNavigation.GetTabNavigation(tabOrder));

                    Controls.HyperlinkButton hyperlink = Assert.IsType<Controls.HyperlinkButton>(DemoTestHost.FindVisualChildren<Controls.HyperlinkButton>(primary).FirstOrDefault(), exactMatch: false);
                    Controls.Button? tabOrderFirst = DemoTestHost.FindByName<Controls.Button>(page, "ExplicitTabOrderFirstButton");
                    Controls.Button? tabOrderSecond = DemoTestHost.FindByName<Controls.Button>(page, "ExplicitTabOrderSecondButton");
                    Controls.Button? tabOrderThird = DemoTestHost.FindByName<Controls.Button>(page, "ExplicitTabOrderThirdButton");
                    AssertTabOrderButton(tabOrderFirst, 1, "Tab order: 1 (first)");
                    AssertTabOrderButton(tabOrderSecond, 2, "Tab order: 2");
                    AssertTabOrderButton(tabOrderThird, 3, "Tab order: 3");
                    AssertNextFocus(window, hyperlink, tabOrderFirst, "Tab should enter the explicit tab-order group after the preceding hyperlink.");
                    AssertNextFocus(window, tabOrderFirst, tabOrderSecond, "Explicit tab-order group should move from 1 to 2.");
                    AssertNextFocus(window, tabOrderSecond, tabOrderThird, "Explicit tab-order group should move from 2 to 3.");

                    List<DemoSampleControl> samples = [.. DemoTestHost.FindVisualChildren<DemoSampleControl>(page)];
                    Assert.Equal(6, samples.Count);
                    Assert.True(samples.TrueForAll(static sample => !string.IsNullOrWhiteSpace(sample.XamlSource)),
                        "Every accessibility sample should have inline XAML source.");
                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "FocusAndTabOrderSourceLink"));
                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "HighContrastMappingSourceLink"));
                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "AutomationPropertiesSourceLink"));
                    Assert.Null(DemoTestHost.FindByName<FrameworkElement>(page, "RtlLayoutSourceLink"));

                    Controls.ToggleSwitch rtlToggle = Assert.IsType<Controls.ToggleSwitch>(DemoTestHost.FindByName<Controls.ToggleSwitch>(page, "RtlToggle"), exactMatch: false);
                    Controls.Card rtlCard = Assert.IsType<Controls.Card>(DemoTestHost.FindByName<Controls.Card>(page, "RtlDemoCard"), exactMatch: false);
                    Assert.True(rtlToggle.IsChecked, "Accessibility RTL should be enabled by default.");
                    Assert.Equal(FlowDirection.RightToLeft, rtlCard.FlowDirection);
                }
                finally
                {
                    DemoTestHost.CloseWindow(window);
                }
            });
        }

        [Fact]
        public Task GalleryAccessibilityPage_RtlSampleDefaultsOnAsync()
        {
            return DemoTestHost.RunDemoPageTestAsync(static () => new GalleryAccessibilityPage(), static window =>
            {
                Controls.ToggleSwitch toggle = Assert.IsType<Controls.ToggleSwitch>(FindVisualChildByName<Controls.ToggleSwitch>(window, "RtlToggle"), exactMatch: false);
                Controls.Card card = Assert.IsType<Controls.Card>(FindVisualChildByName<Controls.Card>(window, "RtlDemoCard"), exactMatch: false);

                Assert.True(toggle.IsChecked.GetValueOrDefault(),
                    "RTL sample should default to On.");
                Assert.Equal(FlowDirection.RightToLeft, card.FlowDirection);
            });
        }

        private static void AssertGridCell(Grid grid, Predicate<UIElement> match, int expectedRow, int expectedColumn, string name)
        {
            foreach (UIElement child in grid.Children)
            {
                if (match(child))
                {
                    Assert.Equal(expectedRow, Grid.GetRow(child));
                    Assert.Equal(expectedColumn, Grid.GetColumn(child));
                    return;
                }
            }

            Assert.Fail("Expected control was not found in the grid: " + name);
        }

        private static void AssertTabOrderButton(Controls.Button? button, int expectedTabIndex, string expectedContent)
        {
            if (button is null)
            {
                Assert.Fail("Expected explicit tab-order button was not found: " + expectedContent);
                return;
            }

            Assert.Equal(expectedContent, button.Content as string, StringComparer.Ordinal);
            Assert.Equal(expectedTabIndex, button.TabIndex);
            Assert.True(button.Focusable, "Explicit tab-order button should accept keyboard focus.");
            Assert.True(button.IsTabStop, "Explicit tab-order button should participate in keyboard tab navigation.");
        }

        private static void AssertNextFocus(
            Window window,
            FrameworkElement? source,
            FrameworkElement? expected,
            string message)
        {
            if (source is null)
            {
                Assert.Fail("Focus source was not found. " + message);
                return;
            }

            if (expected is null)
            {
                Assert.Fail("Expected next focus target was not found. " + message);
                return;
            }

            _ = source.Focus();
            FocusManager.SetFocusedElement(window, source);
            _ = Keyboard.Focus(source);
            WpfTestSta.DrainDispatcher(window.Dispatcher);

            TraversalRequest request = new(FocusNavigationDirection.Next);
            bool moved = source.MoveFocus(request);
            WpfTestSta.DrainDispatcher(window.Dispatcher);

            Assert.True(moved, "Keyboard focus should move to the next tab stop. " + message);
            Assert.Same(expected, Keyboard.FocusedElement);
        }
    }
}
