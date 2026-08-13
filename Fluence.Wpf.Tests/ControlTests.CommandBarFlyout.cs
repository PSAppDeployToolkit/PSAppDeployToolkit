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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.CommandBarFlyout"/> /
    /// <see cref="Controls.CommandBarFlyoutPresenter"/> / <see cref="Controls.AppBarButton"/>
    /// family.
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public void AppBarButton_DefaultStyle_AppliesCompactChromeAndLabelTooltip()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.AppBarButton)) as Style;
                Assert.NotNull(style);

                Window window = new() { Width = 400, Height = 300 };
                Controls.FontIcon icon = new() { Glyph = "\uE8C8" };
                Controls.AppBarButton labeled = new() { Icon = icon, Label = "Copy" };
                Controls.AppBarButton iconOnly = new() { Icon = new Controls.FontIcon { Glyph = "\uE712" } };
                StackPanel panel = new();
                _ = panel.Children.Add(labeled);
                _ = panel.Children.Add(iconOnly);

                try
                {
                    window.Content = panel;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Same(icon, labeled.Icon);
                    Assert.Equal("Copy", labeled.Label, StringComparer.Ordinal);
                    Assert.Equal(string.Empty, iconOnly.Label, StringComparer.Ordinal);
                    Assert.Equal(40.0, labeled.MinWidth, 0.01);
                    Assert.Equal(40.0, labeled.MinHeight, 0.01);
                    Assert.Equal("Copy", labeled.ToolTip);
                    Assert.Null(iconOnly.ToolTip);
                    Assert.True(icon.IsVisible, "The compact template should render the hosted icon.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyout_ShowAt_PresentsPrimaryCommands()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.CommandBarFlyout flyout = new();
                Controls.AppBarButton copyButton = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                };
                flyout.PrimaryCommands.Add(copyButton);

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Popup? popup = flyout.HostPopup;
                    Assert.NotNull(popup);
                    Assert.False(popup.StaysOpen, "CommandBarFlyout popups must be light-dismiss (StaysOpen=false).");

                    Controls.CommandBarFlyoutPresenter? presenter = popup.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.NotNull(presenter);

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ItemsControl? primaryItems = presenter.Template.FindName("PART_PrimaryItemsControl", presenter) as ItemsControl;
                    Assert.NotNull(primaryItems);
                    Assert.Same(flyout.PrimaryCommands, primaryItems.ItemsSource);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => copyButton.IsVisible),
                        "Primary AppBarButtons must materialize in the opened bar.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyout_MoreButton_TracksSecondaryCommands()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.CommandBarFlyout flyout = new();
                flyout.PrimaryCommands.Add(new Controls.AppBarButton
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                });

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.NotNull(presenter);

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    Assert.NotNull(moreButton);
                    Assert.Equal(Visibility.Collapsed, moreButton.Visibility);

                    flyout.SecondaryCommands.Add(new Controls.AppBarButton
                    {
                        Icon = new Controls.FontIcon { Glyph = "\uE74D" },
                        Label = "Delete",
                    });
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => moreButton.Visibility is Visibility.Visible),
                        "The more button must become visible once SecondaryCommands is non-empty.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyout_MoreButton_TogglesSecondaryOverflow()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.CommandBarFlyout flyout = new();
                flyout.PrimaryCommands.Add(new Controls.AppBarButton
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                });
                flyout.SecondaryCommands.Add(new Controls.AppBarButton
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE74D" },
                    Label = "Delete",
                });

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.NotNull(presenter);

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    FrameworkElement? secondaryHost = presenter.Template.FindName("PART_SecondaryHost", presenter) as FrameworkElement;
                    Assert.NotNull(moreButton);
                    Assert.NotNull(secondaryHost);
                    Assert.False(presenter.IsExpanded, "The presenter must open collapsed (AlwaysExpanded is omitted for v1).");
                    Assert.Equal(Visibility.Collapsed, secondaryHost.Visibility);

                    System.Windows.Media.ScaleTransform? hostScale =
                        presenter.Template.FindName("SecondaryHostScale", presenter) as System.Windows.Media.ScaleTransform;
                    Assert.NotNull(hostScale);
                    System.Windows.Media.RotateTransform? chevronRotation =
                        presenter.Template.FindName("MoreButtonIconRotation", presenter) as System.Windows.Media.RotateTransform;
                    Assert.NotNull(chevronRotation);

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => presenter.IsExpanded),
                        "Clicking the more button must expand the presenter.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => secondaryHost.IsVisible),
                        "Expanding must make the secondary host visible.");
                    Assert.True(flyout.IsOpen, "The more button must toggle the overflow without dismissing the flyout.");

                    // The 167ms expand storyboard must settle the host scale at 1 and rotate
                    // the more-button glyph to 180.
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => hostScale.ScaleY >= 1.0),
                        "Expanding must animate the secondary host ScaleY up to 1.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => chevronRotation.Angle >= 180.0),
                        "Expanding must rotate the more-button glyph to 180 degrees.");

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => !presenter.IsExpanded),
                        "Clicking the more button again must collapse the presenter.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => !secondaryHost.IsVisible),
                        "Collapsing must hide the secondary host (the exit storyboard collapses it at the end).");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => hostScale.ScaleY <= 0.0),
                        "Collapsing must animate the secondary host ScaleY back to 0.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => chevronRotation.Angle <= 0.0),
                        "Collapsing must rotate the more-button glyph back to 0 degrees.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyout_PrimaryCommandClick_RaisesClickAndHidesFlyout()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.CommandBarFlyout flyout = new();
                Controls.AppBarButton copyButton = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                };
                flyout.PrimaryCommands.Add(copyButton);
                bool clickRaised = false;
                copyButton.Click += (_, _) => clickRaised = true;

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => copyButton.IsVisible),
                        "The primary command must materialize before it is clicked.");

                    copyButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.True(clickRaised, "The command's own Click handlers must run.");
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Invoking a command must dismiss the flyout, per WinUI.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyout_SecondaryCommands_UseOverflowStyleAndRenderLabels()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                Button target = new() { Content = "Anchor" };
                Controls.CommandBarFlyout flyout = new();
                flyout.PrimaryCommands.Add(new Controls.AppBarButton
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                });
                Controls.AppBarButton deleteButton = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE74D" },
                    Label = "Delete",
                };
                flyout.SecondaryCommands.Add(deleteButton);

                try
                {
                    window.Content = target;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    flyout.ShowAt(target);
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.NotNull(presenter);

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    Assert.NotNull(moreButton);

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.True(WaitUntil(window.Dispatcher, 2000, () => deleteButton.IsVisible),
                        "Expanding the overflow must materialize the secondary command.");

                    Style? secondaryStyle = app?.TryFindResource("CommandBarFlyoutSecondaryAppBarButtonStyle") as Style;
                    Assert.NotNull(secondaryStyle);
                    Assert.NotNull(deleteButton.Style);
                    Assert.Same(secondaryStyle, deleteButton.Style.BasedOn);

                    TextBlock? labelText = FindVisualChildren<TextBlock>(deleteButton)
                        .FirstOrDefault(textBlock => string.Equals(textBlock.Text, "Delete", StringComparison.Ordinal));
                    Assert.NotNull(labelText);
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [Fact]
        public void CommandBarFlyoutPresenter_ThemeCycle_SurfaceBrushesResolve()
        {
            WpfTestSta.Invoke(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                string[] brushKeys =
                [
                    "SolidBackgroundFillColorTertiaryBrush",
                    "SurfaceStrokeColorFlyoutBrush",
                    "SubtleFillColorSecondaryBrush",
                    "SubtleFillColorTertiaryBrush",
                    "DividerStrokeColorDefaultBrush",
                    "TextFillColorPrimaryBrush",
                    "TextFillColorDisabledBrush",
                ];

                foreach (ApplicationTheme theme in new[] { ApplicationTheme.Dark, ApplicationTheme.HighContrast, ApplicationTheme.Light })
                {
                    ApplicationThemeManager.Apply(theme, BackdropType.None, updateAccent: true);
                    foreach (string? key in brushKeys)
                    {
                        Assert.NotNull(app?.TryFindResource(key));
                    }
                }
            });
        }

        [Fact]
        public void AppBarButton_Pressed_AnimatesBackplatePressScale()
        {
            RunOnStaThread(() =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Window window = new() { Width = 400, Height = 300 };
                PressScaleAppBarButtonProbe button = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE8C8" },
                    Label = "Copy",
                };

                // The implicit AppBarButton style only applies to the exact type, so the
                // probe subclass resolves it explicitly by the implicit-style resource key.
                button.SetResourceReference(FrameworkElement.StyleProperty, typeof(Controls.AppBarButton));

                try
                {
                    window.Content = button;
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    System.Windows.Media.ScaleTransform? pressScale =
                        button.Template.FindName("PressScale", button) as System.Windows.Media.ScaleTransform;
                    Assert.NotNull(pressScale);
                    Assert.Equal(1.0, pressScale.ScaleX, 0.001);

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    button.SetPressed(pressed: true);
                    Assert.True(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98),
                        "Pressing must animate the backplate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    button.SetPressed(pressed: false);
                    Assert.True(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX >= 1.0 && pressScale.ScaleY >= 1.0),
                        "Releasing must animate the backplate back to 1.0 scale.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        /// <summary>
        /// Exposes the protected <see cref="ButtonBase.IsPressed"/> setter so the press-scale
        /// storyboards can be driven without a real input device.
        /// </summary>
        private sealed class PressScaleAppBarButtonProbe : Controls.AppBarButton
        {
            public void SetPressed(bool pressed)
            {
                IsPressed = pressed;
            }
        }
    }
}
