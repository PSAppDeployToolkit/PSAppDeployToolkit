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
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.SelectorBar"/> /
    /// <see cref="Controls.SelectorBarItem"/> family: container generation, the single-selection
    /// clamp, the label fallback for data items, and the accent pill reveal.
    /// </summary>
    public sealed class SelectorBarTests : IAsyncLifetime
    {
        private static readonly string[] SectionLabels = ["Text", "Fill", "Stroke"];

        /// <summary>
        /// A data item with a named label, for the DisplayMemberPath case.
        /// </summary>
        /// <param name="label">The label the bar should show.</param>
        private sealed class LabelledItem(string label)
        {
            public string Label { get; } = label;
        }

        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () =>
            {
                Helpers.MotionHelper.OverrideIsMotionEnabled = null;
                _ = TestApp.EnsureLibraryTheme();
            }));
        }

        [Fact]
        public Task SelectorBar_DefaultStyle_AppliesAndGeneratesSelectorBarItemContainersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.SelectorBar)));
                _ = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.SelectorBarItem)));

                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new() { ItemsSource = SectionLabels };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(3, bar.Items.Count);

                    // The template presents Text, not Content, so the bar fills the label in for
                    // a container it generated for a plain data item.
                    Controls.SelectorBarItem container =
                        Assert.IsType<Controls.SelectorBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));
                    Assert.Equal("Fill", container.Text);

                    TextBlock label = FindVisualChildByName<TextBlock>(container, "TextVisual")
                        ?? throw new System.InvalidOperationException("The SelectorBarItem template has no TextVisual.");
                    Assert.Equal("Fill", label.Text);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBar_SelectionMode_IsClampedToSingleAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.SelectorBar bar = new();
                bar.SetCurrentValue(ListBox.SelectionModeProperty, SelectionMode.Multiple);

                // WinUI's SelectorBar has no multi-select mode, so the coercion wins over the set.
                Assert.Equal(SelectionMode.Single, bar.SelectionMode);
            });
        }

        [Fact]
        public Task SelectorBarItem_DeclaredInline_KeepsItsOwnTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem first = new() { Text = "XAML" };
                Controls.SelectorBarItem second = new() { Text = "C#" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Same(first, bar.ItemContainerGenerator.ContainerFromIndex(0));
                    Assert.Equal("XAML", first.Text);
                    Assert.Equal("C#", second.Text);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        /// <summary>
        /// DisplayMemberPath has to be bound, not read off the container: WPF turns the path into
        /// an ItemTemplateSelector and leaves Content as the raw data item, which the template
        /// never presents. Without the binding the label read as the type name.
        /// </summary>
        [Fact]
        public Task SelectorBar_DisplayMemberPath_DrivesTheItemLabelAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new()
                {
                    DisplayMemberPath = "Label",
                    ItemsSource = new[] { new LabelledItem("Recent"), new LabelledItem("Shared") },
                };

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Controls.SelectorBarItem container =
                        Assert.IsType<Controls.SelectorBarItem>(bar.ItemContainerGenerator.ContainerFromIndex(1));
                    Assert.Equal("Shared", container.Text);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBar_KeyboardFocus_SelectsTheFirstItemWhenNothingIsSelectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem first = new() { Text = "Text" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(new Controls.SelectorBarItem { Text = "Fill" });

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.False(bar.Focusable, "WinUI's SelectorBar is not a tab stop; its items are.");
                    Assert.Equal(-1, bar.SelectedIndex);

                    _ = first.Focus();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // WinUI selects the current item, or the first one, when the bar takes focus
                    // with nothing selected, so a keyboard user never sees a pill-less bar.
                    Assert.Equal(0, bar.SelectedIndex);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBarItem_Selected_RevealsAccentPillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem first = new() { Text = "Text" };
                Controls.SelectorBarItem second = new() { Text = "Fill" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Rectangle firstPill = FindVisualChildByName<Rectangle>(first, "PART_SelectionVisual")
                        ?? throw new System.InvalidOperationException("The SelectorBarItem template has no PART_SelectionVisual.");
                    ScaleTransform firstScale = Assert.IsType<ScaleTransform>(firstPill.RenderTransform);

                    // An unselected item keeps the pill hidden at its 4 px rest width.
                    Assert.Equal(0.0, firstPill.Opacity, 3);
                    Assert.Equal(1.0, firstScale.ScaleX, 3);
                    Assert.Equal(4.0, firstPill.Width, 3);
                    Assert.Equal(3.0, firstPill.Height, 3);

                    bar.SelectedItem = first;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // WinUI scales the pill to four times its rest width while selected.
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => firstPill.Opacity > 0.99 && firstScale.ScaleX > 3.99).ConfigureAwait(true),
                        "The selection reveal should settle at full opacity and four times the rest width.");

                    bar.SelectedItem = second;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // WinUI's unselected state carries no storyboard, so the old pill snaps away.
                    Assert.Equal(0.0, firstPill.Opacity, 3);
                    Assert.Equal(1.0, firstScale.ScaleX, 3);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBarItem_ReducedMotion_StampsPillWithoutAnimatingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Helpers.MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem item = new() { Text = "Text" };
                _ = bar.Items.Add(item);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Rectangle pill = FindVisualChildByName<Rectangle>(item, "PART_SelectionVisual")
                        ?? throw new System.InvalidOperationException("The SelectorBarItem template has no PART_SelectionVisual.");
                    ScaleTransform scale = Assert.IsType<ScaleTransform>(pill.RenderTransform);

                    bar.SelectedItem = item;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // No clock runs with motion off: the pose is stamped on the spot.
                    Assert.Equal(1.0, pill.Opacity, 3);
                    Assert.Equal(4.0, scale.ScaleX, 3);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBarItem_AutomationPeer_ReportsTextAsNameAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.SelectorBarItem item = new() { Text = "High Contrast" };

                AutomationPeer peer = UIElementAutomationPeer.CreatePeerForElement(item);
                Assert.Equal("SelectorBarItem", peer.GetClassName());
                Assert.Equal("High Contrast", peer.GetName());
            });
        }

        [Fact]
        public Task SelectorBar_ThemeCycle_KeepsPillAndLabelBrushesResolvableAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 200 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem item = new() { Text = "Text" };
                _ = bar.Items.Add(item);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bar.SelectedItem = item;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    ThemeTestHelpers.ApplyStandardThemeCycle();

                    Rectangle pill = FindVisualChildByName<Rectangle>(item, "PART_SelectionVisual")
                        ?? throw new System.InvalidOperationException("The SelectorBarItem template has no PART_SelectionVisual.");
                    Assert.NotNull(pill.Fill);

                    // The key, not a colour that happens to match it: the pill takes the library's
                    // shared selection accent, which is the key that carries the high contrast
                    // override. AccentFillColorDefaultBrush has the same value in Light and Dark,
                    // so asserting that one would pass on coincidence and miss a regression here.
                    BrushAssert.AssertBrushColor(pill.Fill, "NavigationViewSelectionIndicatorForeground");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SelectorBar_EmptiedSelection_PutsThePreviousItemBackAsync()
        {
            // WPF's ListBox honours Ctrl+Click and Ctrl+Space deselection even in Single mode, which
            // left the bar with no pill and SelectedItem null, contradicting the control's own
            // single-select contract. SelectorBarItem swallows those two gestures; this is the net
            // under everything else that empties the selection.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 400, Height = 120 };
                Controls.SelectorBar bar = new();
                Controls.SelectorBarItem first = new() { Text = "Recent" };
                Controls.SelectorBarItem second = new() { Text = "Shared" };
                _ = bar.Items.Add(first);
                _ = bar.Items.Add(second);

                try
                {
                    window.Content = bar;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    bar.SetCurrentValue(System.Windows.Controls.Primitives.Selector.SelectedItemProperty, second);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Same(second, bar.SelectedItem);

                    // The shape every deselect path ends in, whatever raised it.
                    second.SetCurrentValue(ListBoxItem.IsSelectedProperty, value: false);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Same(second, bar.SelectedItem);
                    Assert.True(second.IsSelected, "The bar must not be left without a selected item.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }
    }
}
