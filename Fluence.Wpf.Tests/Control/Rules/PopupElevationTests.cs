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
using System.Windows.Media.Effects;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control.Rules
{
    /// <summary>
    /// Popup elevation rules. WPF sizes a popup HWND to exactly its child's layout size, with no
    /// gutter of any kind: PopupRoot, its Decorator and its AdornerDecorator all report the child's
    /// size, with no margin and no clip. A <see cref="DropShadowEffect"/> on a child that fills the
    /// popup is therefore clipped away entirely and survives only in the rounded-corner notches,
    /// where it reads as a dark square plate behind a rounded card. Every popup presenter reserves a
    /// 16px transparent margin so the effect has somewhere to render, and every host popup subtracts
    /// the same 16px from its offset so the plate lands where it did before. These tests pin both
    /// halves, because it is the layout gutter, not the effect, that was missing.
    /// </summary>
    public sealed class PopupElevationTests : IClassFixture<LightThemeFixture>
    {
        private const double ShadowGutter = 16.0;

        public PopupElevationTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task FlyoutPresenter_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(presenter), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task FlyoutPresenter_ShadowCaster_CarriesTheFlyoutShadowEffectAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border caster = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(presenter, "ShadowCaster"), exactMatch: false);

                    _ = Assert.IsType<DropShadowEffect>(caster.Effect);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task FlyoutPresenter_DesiredSize_ExceedsThePlateByTwiceTheGutterAsync()
        {
            // This is the assertion that would have caught the original defect: the effect was
            // present all along, the layout gutter was not.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                FlyoutPresenter presenter = new() { Content = "Quick note" };
                Window window = new() { Content = presenter, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Controls.Border surface = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(presenter, "PresenterSurface"), exactMatch: false);

                    Assert.Equal(surface.ActualWidth + (2 * ShadowGutter), presenter.ActualWidth, 0.01);
                    Assert.Equal(surface.ActualHeight + (2 * ShadowGutter), presenter.ActualHeight, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ToolTip_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            // A System.Windows.Controls.ToolTip throws if parented directly (its OnAncestorChanged
            // override forbids a logical or visual parent), so the tip must be opened the way WPF
            // actually opens one: assigned to a target's ToolTip property and shown via IsOpen. Its
            // template still applies in place on the ToolTip instance itself once open, so the
            // visual-tree walk below runs directly on it.
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new() { Width = 320, Height = 200 };
                System.Windows.Controls.Button target = new() { Content = "Hover me" };
                Controls.ToolTip toolTip = new() { Content = "Save changes" };
                target.ToolTip = toolTip;
                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    toolTip.PlacementTarget = target;
                    toolTip.IsOpen = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChild<Grid>(toolTip) is not null).ConfigureAwait(true),
                        "The tooltip template must apply once the tooltip opens.");

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(toolTip), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                    Assert.Equal(-ShadowGutter, toolTip.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, toolTip.VerticalOffset, 0.01);
                }
                finally
                {
                    toolTip.IsOpen = false;
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TeachingTip_TipRoot_ReservesTheShadowGutterAsync()
        {
            // TeachingTip stays Visibility.Collapsed, and its template unapplied, until it opens
            // (TeachingTip.EnsurePopup moves it into its host popup and makes it visible), so the
            // tip must be opened the way WPF actually opens one: given a Target and IsOpen=true.
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new() { Width = 420, Height = 300 };
                System.Windows.Controls.Button target = new() { Content = "Anchor" };
                TeachingTip tip = new()
                {
                    Title = "Pro tip",
                    Subtitle = "A TeachingTip points at a target.",
                    Target = target,
                };
                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChildByName<Grid>(tip, "TipRoot") is not null).ConfigureAwait(true),
                        "The teaching tip template must apply once the tip opens.");

                    Grid root = Assert.IsType<Grid>(FindVisualChildByName<Grid>(tip, "TipRoot"), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    tip.IsOpen = false;
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TeachingTip_TipSurface_PlateWidthMatchesWinUiMinWidthAsync()
        {
            // Pins the fix for the size-shrink defect: TeachingTipMinWidth 320 (WinUI
            // TeachingTip_themeresources.xaml) must land on the plate (TipSurface), not on the
            // gutter-inclusive control, or the effective plate width shrinks by 32px (the 16px
            // gutter on each side). With short content, MinWidth is the binding constraint, so the
            // plate's ActualWidth is exactly the WinUI token.
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new() { Width = 420, Height = 300 };
                System.Windows.Controls.Button target = new() { Content = "Anchor" };
                TeachingTip tip = new()
                {
                    Title = "Pro tip",
                    Subtitle = "A TeachingTip points at a target.",
                    Target = target,
                };
                try
                {
                    window.Content = target;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tip.IsOpen = true;
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => FindVisualChildByName<System.Windows.Controls.Border>(tip, "TipSurface") is not null).ConfigureAwait(true),
                        "The teaching tip template must apply once the tip opens.");
                    window.UpdateLayout();

                    System.Windows.Controls.Border surface = Assert.IsType<System.Windows.Controls.Border>(FindVisualChildByName<System.Windows.Controls.Border>(tip, "TipSurface"), exactMatch: false);

                    Assert.Equal(320.0, surface.ActualWidth, 0.01);
                }
                finally
                {
                    tip.IsOpen = false;
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task CommandBarFlyoutPresenter_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                CommandBarFlyoutPresenter presenter = new();
                Window window = new() { Content = presenter, Width = 420, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(presenter), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ComboBox_DropdownPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ComboBox comboBox = new();
                Window window = new() { Content = comboBox, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = comboBox.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(comboBox.Template.FindName("PART_Popup", comboBox));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task AutoSuggestBox_SuggestionsPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                AutoSuggestBox box = new();
                Window window = new() { Content = box, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = box.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(box.Template.FindName("PART_SuggestionsPopup", box));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DatePicker_DropdownPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.DatePicker picker = new();
                Window window = new() { Content = picker, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = picker.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(picker.Template.FindName("PART_Popup", picker));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task TimePicker_DropdownPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                TimePicker picker = new();
                Window window = new() { Content = picker, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = picker.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(picker.Template.FindName("PART_Popup", picker));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task DropDownButton_FlyoutPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                DropDownButton button = new();
                Window window = new() { Content = button, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = button.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SplitButton_FlyoutPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                SplitButton button = new();
                Window window = new() { Content = button, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = button.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ToggleSplitButton_FlyoutPopup_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                ToggleSplitButton button = new();
                Window window = new() { Content = button, Width = 320, Height = 120 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    _ = button.ApplyTemplate();

                    Popup popup = Assert.IsType<Popup>(button.Template.FindName("PART_Popup", button));

                    Assert.Equal(-ShadowGutter, popup.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, popup.VerticalOffset, 0.01);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task ContextMenu_TemplateRoot_ReservesTheShadowGutterAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.ContextMenu menu = new();
                _ = menu.Items.Add(new Controls.MenuItem { Header = "Cut" });
                System.Windows.Controls.Border host = new() { ContextMenu = menu };
                Window window = new() { Content = host, Width = 320, Height = 200 };
                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    menu.IsOpen = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Grid root = Assert.IsType<Grid>(FindVisualChild<Grid>(menu), exactMatch: false);

                    Assert.Equal(new Thickness(ShadowGutter), root.Margin);
                    Assert.Equal(-ShadowGutter, menu.HorizontalOffset, 0.01);
                    Assert.Equal(-ShadowGutter, menu.VerticalOffset, 0.01);

                    menu.IsOpen = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
