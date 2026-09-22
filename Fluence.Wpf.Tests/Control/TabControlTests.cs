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
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// The native <see cref="TabControl"/>'s Fluent template: the selected tab uses a card-like
    /// content surface, the header panel uses a sequential (non-overlapping) layout with a
    /// centered selection indicator, and left/bottom strip placements keep the header and
    /// content regions properly separated.
    /// </summary>
    public sealed class TabControlTests : IClassFixture<LightThemeFixture>
    {
        public TabControlTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        [Fact]
        public Task FluentTabControl_SelectedTabUsesFluentCardSurfaceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    TabControl tabControl = new();
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tabControl.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TabItem selectedTab = Assert.IsType<TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(1));
                    FrameworkElement contentPanel = Assert.IsType<FrameworkElement>(tabControl.Template.FindName("ContentPanel", tabControl), exactMatch: false);

                    Point selectedOrigin = selectedTab.TransformToAncestor(window).Transform(new Point(0, 0));
                    Point contentOrigin = contentPanel.TransformToAncestor(window).Transform(new Point(0, 0));
                    double selectedBottom = selectedOrigin.Y + selectedTab.ActualHeight;

                    Assert.True(contentOrigin.Y - selectedBottom >= 6.0,
                        "Fluent TabControl should separate selected tabs from the card-like content surface.");
                    _ = Assert.IsType<Border>(contentPanel, exactMatch: false);

                    Border contentBorder = (Border)contentPanel;
                    Assert.NotNull(contentBorder.Background);
                    Assert.NotNull(contentBorder.BorderBrush);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluentTabControl_SelectedHeaderUsesSequentialPanelAndCenteredIndicatorAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                Window window = new();

                try
                {
                    TabControl tabControl = new();
                    _ = tabControl.Items.Add(new TabItem { Header = "Overview", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Activity", Content = new TextBlock { Text = "B" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Settings", Content = new TextBlock { Text = "C" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    tabControl.SelectedIndex = 1;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    await WaitForAnimationAndDrainAsync(window.Dispatcher, 250).ConfigureAwait(true);

                    FrameworkElement headerPanel = Assert.IsType<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl), exactMatch: false);
                    TabItem selectedTab = Assert.IsType<TabItem>(tabControl.ItemContainerGenerator.ContainerFromIndex(1));
                    _ = Assert.IsType<StackPanel>(headerPanel, exactMatch: false);
                    Assert.False(headerPanel is TabPanel,
                        "TabControl should not use TabPanel for Fluent headers because its selection overlap can clip rounded corners.");
                    Assert.Equal(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation);

                    Border selectedBackground = Assert.IsType<Border>(FindVisualChildByName<Border>(selectedTab, "SelectedBackground"), exactMatch: false);
                    Border selectionIndicator = Assert.IsType<Border>(FindVisualChildByName<Border>(selectedTab, "SelectionIndicator"), exactMatch: false);

                    double backgroundX = selectedBackground.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double indicatorX = selectionIndicator.TransformToAncestor(selectedTab).Transform(new Point(0, 0)).X;
                    double backgroundCenter = backgroundX + (selectedBackground.ActualWidth / 2.0);
                    double indicatorCenter = indicatorX + (selectionIndicator.ActualWidth / 2.0);
                    Assert.Equal(backgroundCenter, indicatorCenter, 0.5);
                    Assert.Equal(selectedTab.ActualWidth, selectedBackground.ActualWidth, 0.5);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluentTabControl_LeftPlacement_SeparatesHeadersAndContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    TabControl tabControl = new()
                    {
                        TabStripPlacement = Dock.Left,
                    };
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement headerPanel = Assert.IsType<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl), exactMatch: false);
                    FrameworkElement contentPanel = Assert.IsType<FrameworkElement>(tabControl.Template.FindName("ContentPanel", tabControl), exactMatch: false);

                    Assert.Equal(0, Grid.GetColumn(headerPanel));
                    Assert.Equal(1, Grid.GetColumn(contentPanel));
                    Assert.Equal(new Thickness(0, 0, 9, 0), headerPanel.Margin);
                    _ = Assert.IsType<StackPanel>(headerPanel, exactMatch: false);
                    Assert.Equal(Orientation.Vertical, ((StackPanel)headerPanel).Orientation);

                    TabItem firstItem = Assert.IsType<TabItem>(tabControl.Items[0]);
                    Assert.Equal(new Thickness(0, 0, 8, 2), firstItem.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task FluentTabControl_BottomPlacement_LeavesBorderBreathingRoomAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();

                try
                {
                    TabControl tabControl = new()
                    {
                        TabStripPlacement = Dock.Bottom,
                    };
                    _ = tabControl.Items.Add(new TabItem { Header = "First", Content = new TextBlock { Text = "A" } });
                    _ = tabControl.Items.Add(new TabItem { Header = "Second", Content = new TextBlock { Text = "B" } });
                    window.Content = tabControl;
                    window.Width = 640;
                    window.Height = 480;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    FrameworkElement headerPanel = Assert.IsType<FrameworkElement>(tabControl.Template.FindName("HeaderPanel", tabControl), exactMatch: false);

                    Assert.Equal(new Thickness(0, 8, 1, 0), headerPanel.Margin);
                    _ = Assert.IsType<StackPanel>(headerPanel, exactMatch: false);
                    Assert.Equal(Orientation.Horizontal, ((StackPanel)headerPanel).Orientation);

                    TabItem firstItem = Assert.IsType<TabItem>(tabControl.Items[0]);
                    Assert.Equal(new Thickness(0, 0, 8, 2), firstItem.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
