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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.DispatcherWaits;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Tests for <see cref="Controls.SlideNavigationPresenter"/>: the two-presenter template, the
    /// direction the incoming content enters from, the settle back to a single live content tree,
    /// and the reduced-motion path.
    /// </summary>
    public sealed class SlideNavigationPresenterTests : IAsyncLifetime
    {
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
        public Task SlideNavigationPresenter_DefaultStyle_AppliesTemplatePartsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application app = WpfTestSta.EnsureApplication();
                _ = Assert.IsType<Style>(app.TryFindResource(typeof(Controls.SlideNavigationPresenter)));

                Window window = new() { Width = 500, Height = 300 };
                Controls.SlideNavigationPresenter presenter = new() { Content = new TextBlock { Text = "First" } };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(presenter.Template.FindName("PART_CurrentPresenter", presenter));
                    Assert.NotNull(presenter.Template.FindName("PART_PreviousPresenter", presenter));
                    _ = Assert.IsType<TranslateTransform>(presenter.Template.FindName("PART_CurrentTranslate", presenter));
                    _ = Assert.IsType<TranslateTransform>(presenter.Template.FindName("PART_PreviousTranslate", presenter));

                    // The first content is not a navigation, so nothing is left behind and the
                    // incoming presenter sits at rest.
                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));
                    Assert.Null(previous.Content);
                    Assert.Equal(SlideNavigationTransitionEffect.FromRight, presenter.TransitionEffect);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Theory]
        [InlineData(SlideNavigationTransitionEffect.FromRight, -1.0)]
        [InlineData(SlideNavigationTransitionEffect.FromLeft, 1.0)]
        public Task SlideNavigationPresenter_ContentChanged_SlidesFromTheRequestedSideAsync(
            SlideNavigationTransitionEffect effect,
            double direction)
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Window window = new() { Width = 500, Height = 300 };
                Controls.SlideNavigationPresenter presenter = new()
                {
                    TransitionEffect = effect,
                    Content = new TextBlock { Text = "First" },
                };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));
                    TranslateTransform currentTranslate = Assert.IsType<TranslateTransform>(presenter.Template.FindName("PART_CurrentTranslate", presenter));

                    TextBlock second = new() { Text = "Second" };
                    presenter.Content = second;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // The outgoing content moves to the second presenter, and the incoming one
                    // starts 200 px out on the side the effect names (WinUI
                    // translationEntranceOffset, mirrored by the effect).
                    Assert.NotNull(previous.Content);
                    Assert.Equal(-200.0 * direction, currentTranslate.X, 1);

                    // The whole transition is 450 ms (150 ms exit plus 300 ms entrance), after
                    // which the presenter holds one content tree at rest.
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, () => previous.Content is null).ConfigureAwait(true),
                        "The transition should drop the outgoing content once it settles.");
                    Assert.Equal(0.0, currentTranslate.X, 1);
                    Assert.Same(second, presenter.Content);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        /// <summary>
        /// Regression guard: the two halves must hold their end pose until the transition settles.
        /// With FillBehavior.Stop the outgoing content reverted to full opacity at its rest
        /// position the moment its 150 ms clock ended, and because it paints after the incoming
        /// presenter it covered the arriving content for the remaining 300 ms.
        /// </summary>
        [Fact]
        public Task SlideNavigationPresenter_MidTransition_KeepsOutgoingContentHiddenAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 500, Height = 300 };
                Controls.SlideNavigationPresenter presenter = new() { Content = new TextBlock { Text = "First" } };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter current = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_CurrentPresenter", presenter));
                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));

                    presenter.Content = new TextBlock { Text = "Second" };
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Sample inside the window the exit clock has already finished but the
                    // entrance clock has not: 150 ms < t < 450 ms.
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, () => current.Opacity > 0.99).ConfigureAwait(true),
                        "The incoming content should be opaque once the outgoing content has left.");
                    Assert.True(previous.Opacity < 0.01, "The outgoing content must stay hidden until the transition settles.");
                    Assert.NotNull(previous.Content);

                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, () => previous.Content is null).ConfigureAwait(true),
                        "The transition should still settle.");
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SlideNavigationPresenter_ReducedMotion_SwapsWithoutSlidingAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Helpers.MotionHelper.OverrideIsMotionEnabled = false;

                Window window = new() { Width = 500, Height = 300 };
                Controls.SlideNavigationPresenter presenter = new() { Content = new TextBlock { Text = "First" } };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter current = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_CurrentPresenter", presenter));
                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));
                    TranslateTransform currentTranslate = Assert.IsType<TranslateTransform>(presenter.Template.FindName("PART_CurrentTranslate", presenter));

                    presenter.Content = new TextBlock { Text = "Second" };
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // With motion off the swap is immediate: nothing slides and nothing is held
                    // in the outgoing presenter.
                    Assert.Null(previous.Content);
                    Assert.Equal(0.0, currentTranslate.X, 3);
                    Assert.Equal(1.0, current.Opacity, 3);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SlideNavigationPresenter_RapidChanges_KeepOnlyTheNewestContentAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                Window window = new() { Width = 500, Height = 300 };
                Controls.SlideNavigationPresenter presenter = new() { Content = new TextBlock { Text = "First" } };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));

                    presenter.Content = new TextBlock { Text = "Second" };
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    TextBlock third = new() { Text = "Third" };
                    presenter.Content = third;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 3000, () => previous.Content is null).ConfigureAwait(true),
                        "A transition interrupted by a second content change must still settle.");
                    Assert.Same(third, presenter.Content);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SlideNavigationPresenter_OutgoingContent_KeepsTheContentTemplateAsync()
        {
            // Regression: only PART_CurrentPresenter carried the template contract, so a data item
            // with a ContentTemplate left through the exit slide as its own ToString(). Every other
            // test in this class uses UIElement content, which presents identically either way.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new() { Width = 500, Height = 300 };
                DataTemplate template = new()
                {
                    VisualTree = new FrameworkElementFactory(typeof(TextBlock)),
                };
                template.VisualTree.SetBinding(TextBlock.TextProperty, new System.Windows.Data.Binding("."));
                template.Seal();

                Controls.SlideNavigationPresenter presenter = new()
                {
                    ContentTemplate = template,
                    Content = "First",
                };

                try
                {
                    window.Content = presenter;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    ContentPresenter previous = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_PreviousPresenter", presenter));
                    ContentPresenter current = Assert.IsType<ContentPresenter>(presenter.Template.FindName("PART_CurrentPresenter", presenter));
                    Assert.Same(template, current.ContentTemplate);

                    presenter.SetCurrentValue(ContentControl.ContentProperty, "Second");
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    // The outgoing item is mid-slide here, and it has to be presented through the
                    // same template the incoming one uses.
                    Assert.Same("First", previous.Content);
                    Assert.Same(template, previous.ContentTemplate);
                    Assert.NotNull(FindVisualChild<TextBlock>(previous));
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task SlideNavigationPresenter_TransitionEffect_RejectsAnUndeclaredValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Controls.SlideNavigationPresenter presenter = new();

                // The enumeration keeps WinUI's numbers and leaves 0 to the unported FromBottom, so
                // default(SlideNavigationTransitionEffect) is not a declared member. A caller who
                // ports WinUI code that used FromBottom, or writes default, must hear about it
                // rather than get a horizontal slide they did not ask for.
                Assert.Equal(SlideNavigationTransitionEffect.FromRight, presenter.TransitionEffect);
                _ = Assert.Throws<ArgumentException>(() => presenter.TransitionEffect = default);
                _ = Assert.Throws<ArgumentException>(() => presenter.TransitionEffect = (SlideNavigationTransitionEffect)7);
                Assert.Equal(SlideNavigationTransitionEffect.FromRight, presenter.TransitionEffect);

                presenter.TransitionEffect = SlideNavigationTransitionEffect.FromLeft;
                Assert.Equal(SlideNavigationTransitionEffect.FromLeft, presenter.TransitionEffect);
            });
        }
    }
}
