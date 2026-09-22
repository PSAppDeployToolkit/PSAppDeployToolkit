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
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Automation;
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
    /// Fluent <see cref="RadioButton"/> control: Description surfaces as
    /// AutomationProperties.HelpText.
    /// </summary>
    public sealed class RadioButtonTests : IAsyncLifetime
    {
        public ValueTask InitializeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        public ValueTask DisposeAsync()
        {
            return new ValueTask(WpfTestSta.RunOnStaAsync(static () => _ = TestApp.EnsureLibraryTheme()));
        }

        [Fact]
        public Task RadioButton_StateSizeStoryboards_ReleaseRatherThanStampOnExitAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radio = new() { Content = "Option" };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    // Hover, press and disabled each resize the dot while they hold. Their exits must
                    // remove that animation and let the check state decide the size again: an exit
                    // that stamped the checked size of its own could land on an unchecked button,
                    // which is what left a white dot sitting in an empty ring.
                    int inspected = 0;
                    foreach (TriggerBase trigger in radio.Template.Triggers)
                    {
                        if (trigger is not MultiTrigger multi || !StartsADotResize(multi.EnterActions))
                        {
                            continue;
                        }

                        inspected++;
                        Assert.All(multi.ExitActions, static action => Assert.IsType<RemoveStoryboard>(action, exactMatch: false));
                        Assert.NotEmpty(multi.ExitActions);
                    }

                    Assert.Equal(3, inspected);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        [Fact]
        public Task RadioButton_CheckedDot_KeepsItsCheckedSizeWhenAStateStoryboardIsReleasedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static async () =>
            {
                RadioButton radio = new() { Content = "Option" };
                Window window = new() { Content = radio, Width = 240, Height = 120 };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    radio.IsChecked = true;
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);

                    Ellipse dot = FindVisualChildByName<Ellipse>(radio, "InnerDot")
                        ?? throw new InvalidOperationException("The RadioButton template has no InnerDot.");
                    Assert.Equal(8.0, dot.Width);
                    Assert.Equal(8.0, dot.Height);

                    // The hover, pressed and disabled triggers all resize the checked dot with a named
                    // storyboard and release it with RemoveStoryboard on exit. Rendered, the release
                    // has to land back on the checked 8, not on the template's resting 0: WPF layers a
                    // later trigger's storyboard over an earlier trigger's rather than replacing it,
                    // so removing the top layer uncovers the checked grow again. The disabled trigger
                    // is the one a test can drive without a pointer, and it has the same shape as
                    // the two pointer triggers.
                    radio.IsEnabled = false;
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);
                    Assert.Equal(9.33, dot.Width, 2);
                    Assert.Equal(9.33, dot.Height, 2);

                    radio.IsEnabled = true;
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 100).ConfigureAwait(true);
                    Assert.Equal(8.0, dot.Width);
                    Assert.Equal(8.0, dot.Height);

                    // And the release must not leave a dot in an unchecked ring either, which is the
                    // defect RemoveStoryboard was adopted to fix.
                    radio.IsEnabled = false;
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);
                    radio.IsChecked = false;
                    radio.IsEnabled = true;
                    await DispatcherWaits.WaitForAnimationAndDrainAsync(window.Dispatcher, 400).ConfigureAwait(true);
                    Assert.Equal(0.0, dot.Width);
                    Assert.Equal(0.0, dot.Height);
                }
                finally
                {
                    CloseWindowAndDrain(window);
                }
            });
        }

        private static bool StartsADotResize(IEnumerable<TriggerAction> actions)
        {
            foreach (TriggerAction action in actions)
            {
                if (action is not BeginStoryboard begin || begin.Storyboard is null)
                {
                    continue;
                }

                foreach (Timeline timeline in begin.Storyboard.Children)
                {
                    if (string.Equals(Storyboard.GetTargetName(timeline), "InnerDot", StringComparison.Ordinal))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        // ---------------------------------------------------------------------------
        // RadioButton Description -> HelpText
        // ---------------------------------------------------------------------------

        [Fact]
        public Task RadioButton_Description_SetsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Choose this option for better performance.",
                };

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Choose this option for better performance.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description must be surfaced as AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_DescriptionChanges_UpdatesAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Initial description.",
                };

                radioButton.Description = "Revised description.";
                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.Equals("Revised description.", helpText, StringComparison.Ordinal),
                    $"RadioButton.Description change must update AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_NullDescription_ClearsAutomationHelpTextAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                RadioButton radioButton = new()
                {
                    Content = "Option A",
                    Description = "Some description.",
                };
                radioButton.Description = null;

                string helpText = AutomationProperties.GetHelpText(radioButton);
                Assert.True(
                    string.IsNullOrWhiteSpace(helpText),
                    $"Null RadioButton.Description must clear AutomationProperties.HelpText. Actual: '{helpText}'.");
            });
        }

        [Fact]
        public Task RadioButton_Checked_HasAccentFillAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));
                Window window = new();
                RadioButton radio = new()
                {
                    Content = "Test",
                    IsChecked = true,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    Ellipse checkedEllipse = Assert.IsType<Ellipse>(radio.Template.FindName("CheckedEllipse", radio));
                    SolidColorBrush accentBrush = Assert.IsType<SolidColorBrush>(application.Resources["AccentFillColorDefaultBrush"]);

                    Assert.Equal(1.0, checkedEllipse.Opacity);
                    _ = Assert.IsType<SolidColorBrush>(checkedEllipse.Fill, exactMatch: false);
                    Assert.Equal(accentBrush.Color, ((SolidColorBrush)checkedEllipse.Fill).Color);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task RadioButton_ContentAlignment_CentersTextWithIndicatorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                RadioButton radio = new()
                {
                    Content = "Standard",
                    Width = 240,
                    Height = 40,
                };

                try
                {
                    window.Content = radio;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    _ = radio.ApplyTemplate();
                    System.Windows.Controls.Grid indicatorHost = Assert.IsType<System.Windows.Controls.Grid>(FindVisualChildByName<System.Windows.Controls.Grid>(radio, "IndicatorHost"), exactMatch: false);
                    System.Windows.Controls.ContentPresenter contentPresenter = Assert.IsType<System.Windows.Controls.ContentPresenter>(FindVisualChildByName<System.Windows.Controls.ContentPresenter>(radio, "ContentPresenter"), exactMatch: false);

                    Assert.Equal(VerticalAlignment.Center, radio.VerticalContentAlignment);
                    Assert.Equal(VerticalAlignment.Center, indicatorHost.VerticalAlignment);
                    Assert.Equal(new Thickness(0), indicatorHost.Margin);
                    Assert.Equal(VerticalAlignment.Center, contentPresenter.VerticalAlignment);
                    Assert.Equal(new Thickness(8, 0, 0, 0), contentPresenter.Margin);
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public Task RadioButton_GroupExclusivity_UnchecksOthersAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Window window = new();
                System.Windows.Controls.StackPanel panel = new();
                RadioButton radio1 = new() { Content = "A", GroupName = "TestGroup", IsChecked = true };
                RadioButton radio2 = new() { Content = "B", GroupName = "TestGroup" };
                RadioButton radio3 = new() { Content = "C", GroupName = "TestGroup" };
                _ = panel.Children.Add(radio1);
                _ = panel.Children.Add(radio2);
                _ = panel.Children.Add(radio3);

                try
                {
                    window.Content = panel;
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.True(radio1.IsChecked is true);

                    radio2.IsChecked = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);

                    Assert.Equal(false, radio1.IsChecked);
                    Assert.Equal(true, radio2.IsChecked);
                    Assert.Equal(false, radio3.IsChecked);
                }
                finally
                {
                    window.Close();
                }
            });
        }
    }
}
