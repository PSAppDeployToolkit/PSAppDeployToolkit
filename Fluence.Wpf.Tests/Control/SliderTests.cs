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
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Shapes;
using Fluence.Wpf.Controls;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;
using static Fluence.Wpf.Tests.Infrastructure.VisualTree;

namespace Fluence.Wpf.Tests.Control
{
    /// <summary>
    /// Fluent <see cref="Slider"/> control: thumb scale animations.
    /// WinUI canonical: hover 1.167, pressed 0.86, ControlFastOutSlowIn easing.
    /// </summary>
    public sealed class SliderTests : IClassFixture<LightThemeFixture>
    {
        public SliderTests(LightThemeFixture fixture)
        {
            _ = fixture;
        }

        // ---------------------------------------------------------------------------
        // WI-3 B11  Slider thumb scale
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Slider_StyleApplies_PartTrackFoundAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Track track = Assert.IsType<Track>(FindVisualChildByName<Track>(slider, "PART_Track"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Theory]
        [InlineData(System.Windows.Controls.Orientation.Horizontal)]
        [InlineData(System.Windows.Controls.Orientation.Vertical)]
        public Task Slider_SnapToTick_LandsOnATickWhenDraggedAsync(System.Windows.Controls.Orientation orientation)
        {
            return WpfTestSta.RunOnStaAsync(() =>
            {
                Slider slider = new()
                {
                    Orientation = orientation,
                    Width = orientation is System.Windows.Controls.Orientation.Horizontal ? 240 : 40,
                    Height = orientation is System.Windows.Controls.Orientation.Horizontal ? 40 : 240,
                    Minimum = 0,
                    Maximum = 100,
                    TickFrequency = 10,
                    IsSnapToTickEnabled = true,
                    Value = 40,
                };
                Window window = new() { Content = slider, Width = 320, Height = 320 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Thumb thumb = Assert.IsType<Thumb>(FindVisualChild<Thumb>(slider), exactMatch: false);

                    // A drag the length of a third of a tick must still settle on a tick: the
                    // snapping is the Slider's own, and it has to hold on both axes.
                    DragDeltaEventArgs drag = new(horizontalChange: 8, verticalChange: -8)
                    {
                        RoutedEvent = Thumb.DragDeltaEvent,
                        Source = thumb,
                    };
                    thumb.RaiseEvent(drag);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(0.0, slider.Value % slider.TickFrequency, 0.001);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task Slider_DefaultState_ThumbInnerDotScaleIsRestValueAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 50, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    // Only the inner dot carries the ScaleTransform named ThumbScale (WinUI 3 scales the
                    // inner dot, not the fixed outer capsule); its rest value is 0.86, not 1.0
                    // (Slider_themeresources.xaml: "0.86 is relative scale from 14px to 12px").
                    Thumb thumb = Assert.IsType<Thumb>(FindVisualChild<Thumb>(slider), exactMatch: false);

                    Ellipse innerDot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(thumb, "ThumbInnerDot"), exactMatch: false);

                    ScaleTransform scale = Assert.IsType<ScaleTransform>(innerDot.RenderTransform);
                    Assert.Equal(0.86, scale.ScaleX, 0.001);
                    Assert.Equal(0.86, scale.ScaleY, 0.001);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Slider_ThumbTemplate_HasEllipseAndInnerDotAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 30, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    Ellipse thumbEllipse = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(slider, "ThumbEllipse"), exactMatch: false);
                    Ellipse innerDot = Assert.IsType<Ellipse>(FindVisualChildByName<Ellipse>(slider, "ThumbInnerDot"), exactMatch: false);
                }
                finally
                {
                    w.Close();
                }
            });
        }

        // ---------------------------------------------------------------------------
        // A3  Unfilled track painted exactly once
        // ---------------------------------------------------------------------------

        [Fact]
        public Task Slider_UnfilledTrack_PaintedOnceAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Value = 30, Minimum = 0, Maximum = 100 };
                Window w = new() { Content = slider, Width = 300, Height = 60 };
                try
                {
                    w.Show();
                    WpfTestSta.DrainDispatcher(w.Dispatcher);

                    System.Windows.Controls.Border trackBackground = Assert.IsType<System.Windows.Controls.Border>(
                        FindVisualChildByName<System.Windows.Controls.Border>(slider, "TrackBackground"), exactMatch: false);
                    System.Windows.Controls.Primitives.RepeatButton increaseButton = Assert.IsType<System.Windows.Controls.Primitives.RepeatButton>(
                        FindVisualChildByName<System.Windows.Controls.Primitives.RepeatButton>(slider, "IncreaseButton"), exactMatch: false);

                    // The unfilled track must be painted exactly once: TrackBackground carries
                    // ControlStrongFillColorDefaultBrush and IncreaseButton stays Transparent, so the
                    // two partial-alpha fills do not stack into a darker composite than WinUI's single
                    // HorizontalTrackRect (see the WinUI-authority comment on IncreaseButton in Slider.xaml).
                    BrushAssert.AssertBrushColor(trackBackground.Background, "ControlStrongFillColorDefaultBrush");
                    Assert.Equal(Colors.Transparent, BrushAssert.SolidColor(increaseButton.Background));
                }
                finally
                {
                    w.Close();
                }
            });
        }

        [Fact]
        public Task Slider_Template_HasTrackAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                Slider slider = new() { Width = 220, Minimum = 0, Maximum = 100, Value = 30 };

                try
                {
                    window.Content = slider;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.NotNull(slider.Template.FindName("PART_Track", slider));
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task Slider_ThumbScale_TakesTheDurationOfTheStateItEntersAsync()
        {
            // WinUI gives each thumb state its own duration rather than one shared value:
            // Normal and Disabled take ControlFastAnimationDuration (167 ms), PointerOver and
            // Pressed take ControlNormalAnimationDuration (250 ms), all on the 0,0,0,1 spline
            // (Slider_themeresources.xaml:206-251). Every transition here used to run at 100 ms.
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Slider slider = new() { Width = 200 };
                Window window = new() { Content = slider, Width = 260, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Thumb thumb = Assert.IsType<Thumb>(FindVisualChild<Thumb>(slider), exactMatch: false);
                    System.Windows.Controls.ControlTemplate template = Assert.IsType<System.Windows.Controls.ControlTemplate>(thumb.Template);

                    Dictionary<string, TimeSpan> expected = new(StringComparer.Ordinal)
                    {
                        ["1.167"] = TimeSpan.FromMilliseconds(250),
                        ["0.71"] = TimeSpan.FromMilliseconds(250),
                        ["0.86"] = TimeSpan.FromMilliseconds(167),
                    };

                    List<string> mismatches = [];
                    foreach (TriggerBase triggerBase in template.Triggers)
                    {
                        if (triggerBase is not Trigger trigger)
                        {
                            continue;
                        }

                        foreach (TriggerAction action in trigger.EnterActions.Concat(trigger.ExitActions))
                        {
                            if (action is not BeginStoryboard begin || begin.Storyboard is null)
                            {
                                continue;
                            }

                            foreach (Timeline timeline in begin.Storyboard.Children)
                            {
                                if (timeline is not DoubleAnimationUsingKeyFrames frames)
                                {
                                    continue;
                                }

                                foreach (DoubleKeyFrame frame in frames.KeyFrames)
                                {
                                    string value = frame.Value.ToString(CultureInfo.InvariantCulture);
                                    if (expected.TryGetValue(value, out TimeSpan want) && frame.KeyTime.TimeSpan != want)
                                    {
                                        mismatches.Add(string.Format(
                                            CultureInfo.InvariantCulture,
                                            "scale {0} runs for {1} ms, expected {2} ms",
                                            value,
                                            frame.KeyTime.TimeSpan.TotalMilliseconds,
                                            want.TotalMilliseconds));
                                    }
                                }
                            }
                        }
                    }

                    Assert.Empty(mismatches);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
