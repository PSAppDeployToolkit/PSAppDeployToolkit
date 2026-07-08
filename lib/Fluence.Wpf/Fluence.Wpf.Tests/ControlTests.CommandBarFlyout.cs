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
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Tests for the WinUI-style <see cref="Controls.CommandBarFlyout"/> /
    /// <see cref="Controls.CommandBarFlyoutPresenter"/> / <see cref="Controls.AppBarButton"/>
    /// family.
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void AppBarButton_DefaultStyle_AppliesCompactChromeAndLabelTooltip()
        {
            RunOnStaThread(static () =>
            {
                Application? app = EnsureApplication();
                _ = MergeGenericDictionary(app);

                Style? style = app?.TryFindResource(typeof(Controls.AppBarButton)) as Style;
                Assert.IsNotNull(style, "A default Style must be registered for Fluence.Wpf.Controls.AppBarButton.");

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

                    Assert.AreSame(icon, labeled.Icon, "AppBarButton.Icon must round-trip.");
                    Assert.AreEqual("Copy", labeled.Label, "AppBarButton.Label must round-trip.");
                    Assert.AreEqual(string.Empty, iconOnly.Label, "AppBarButton.Label must default to an empty string.");
                    Assert.AreEqual(40.0, labeled.MinWidth, 0.01, "Compact AppBarButton must keep the 40px hit-target width.");
                    Assert.AreEqual(40.0, labeled.MinHeight, 0.01, "Compact AppBarButton must keep the 40px hit-target height.");
                    Assert.AreEqual("Copy", labeled.ToolTip, "Compact AppBarButton must surface its Label as a tooltip.");
                    Assert.IsNull(iconOnly.ToolTip, "Unlabeled AppBarButton must not show an empty tooltip balloon.");
                    Assert.IsTrue(icon.IsVisible, "The compact template should render the hosted icon.");
                }
                finally
                {
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Popup? popup = flyout.HostPopup;
                    Assert.IsNotNull(popup, "ShowAt should lazily create the host popup.");
                    Assert.IsFalse(popup.StaysOpen, "CommandBarFlyout popups must be light-dismiss (StaysOpen=false).");

                    Controls.CommandBarFlyoutPresenter? presenter = popup.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a CommandBarFlyoutPresenter.");

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ItemsControl? primaryItems = presenter.Template.FindName("PART_PrimaryItemsControl", presenter) as ItemsControl;
                    Assert.IsNotNull(primaryItems, "The presenter template must expose PART_PrimaryItemsControl.");
                    Assert.AreSame(flyout.PrimaryCommands, primaryItems.ItemsSource,
                        "PrimaryCommands must feed the primary items control.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => copyButton.IsVisible),
                        "Primary AppBarButtons must materialize in the opened bar.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a CommandBarFlyoutPresenter.");

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    Assert.IsNotNull(moreButton, "The presenter template must expose PART_MoreButton.");
                    Assert.AreEqual(Visibility.Collapsed, moreButton.Visibility,
                        "The more button must stay hidden while SecondaryCommands is empty.");

                    flyout.SecondaryCommands.Add(new Controls.AppBarButton
                    {
                        Icon = new Controls.FontIcon { Glyph = "\uE74D" },
                        Label = "Delete",
                    });
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => moreButton.Visibility is Visibility.Visible),
                        "The more button must become visible once SecondaryCommands is non-empty.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a CommandBarFlyoutPresenter.");

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    FrameworkElement? secondaryHost = presenter.Template.FindName("PART_SecondaryHost", presenter) as FrameworkElement;
                    Assert.IsNotNull(moreButton, "The presenter template must expose PART_MoreButton.");
                    Assert.IsNotNull(secondaryHost, "The presenter template must expose PART_SecondaryHost.");
                    Assert.IsFalse(presenter.IsExpanded, "The presenter must open collapsed (AlwaysExpanded is omitted for v1).");
                    Assert.AreEqual(Visibility.Collapsed, secondaryHost.Visibility,
                        "The secondary host must stay collapsed until the more button is clicked.");

                    System.Windows.Media.ScaleTransform? hostScale =
                        presenter.Template.FindName("SecondaryHostScale", presenter) as System.Windows.Media.ScaleTransform;
                    Assert.IsNotNull(hostScale, "The presenter template must expose the SecondaryHostScale reveal transform.");
                    System.Windows.Media.RotateTransform? chevronRotation =
                        presenter.Template.FindName("MoreButtonIconRotation", presenter) as System.Windows.Media.RotateTransform;
                    Assert.IsNotNull(chevronRotation, "The presenter template must expose the MoreButtonIconRotation transform.");

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => presenter.IsExpanded),
                        "Clicking the more button must expand the presenter.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => secondaryHost.IsVisible),
                        "Expanding must make the secondary host visible.");
                    Assert.IsTrue(flyout.IsOpen, "The more button must toggle the overflow without dismissing the flyout.");

                    // The 167ms expand storyboard must settle the host scale at 1 and rotate
                    // the more-button glyph to 180.
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => hostScale.ScaleY >= 1.0),
                        "Expanding must animate the secondary host ScaleY up to 1.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => chevronRotation.Angle >= 180.0),
                        "Expanding must rotate the more-button glyph to 180 degrees.");

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !presenter.IsExpanded),
                        "Clicking the more button again must collapse the presenter.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !secondaryHost.IsVisible),
                        "Collapsing must hide the secondary host (the exit storyboard collapses it at the end).");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => hostScale.ScaleY <= 0.0),
                        "Collapsing must animate the secondary host ScaleY back to 0.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => chevronRotation.Angle <= 0.0),
                        "Collapsing must rotate the more-button glyph back to 0 degrees.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => copyButton.IsVisible),
                        "The primary command must materialize before it is clicked.");

                    copyButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.IsTrue(clickRaised, "The command's own Click handlers must run.");
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => !flyout.IsOpen),
                        "Invoking a command must dismiss the flyout, per WinUI.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => flyout.IsOpen),
                        "ShowAt should open the command bar flyout popup.");

                    Controls.CommandBarFlyoutPresenter? presenter = flyout.HostPopup?.Child as Controls.CommandBarFlyoutPresenter;
                    Assert.IsNotNull(presenter, "The popup child must be a CommandBarFlyoutPresenter.");

                    DrainDispatcher(window.Dispatcher);
                    _ = presenter.ApplyTemplate();
                    ButtonBase? moreButton = presenter.Template.FindName("PART_MoreButton", presenter) as ButtonBase;
                    Assert.IsNotNull(moreButton, "The presenter template must expose PART_MoreButton.");

                    moreButton.RaiseEvent(new RoutedEventArgs(ButtonBase.ClickEvent));
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000, () => deleteButton.IsVisible),
                        "Expanding the overflow must materialize the secondary command.");

                    Style? secondaryStyle = app?.TryFindResource("CommandBarFlyoutSecondaryAppBarButtonStyle") as Style;
                    Assert.IsNotNull(secondaryStyle, "The overflow AppBarButton style must be a reachable resource.");
                    Assert.IsNotNull(deleteButton.Style, "The secondary items control must apply an implicit overflow style.");
                    Assert.AreSame(secondaryStyle, deleteButton.Style.BasedOn,
                        "Secondary commands must pick up CommandBarFlyoutSecondaryAppBarButtonStyle via the implicit style.");

                    TextBlock? labelText = FindVisualChildren<TextBlock>(deleteButton)
                        .FirstOrDefault(textBlock => string.Equals(textBlock.Text, "Delete", System.StringComparison.Ordinal));
                    Assert.IsNotNull(labelText, "Overflow AppBarButtons must render their Label next to the icon.");
                }
                finally
                {
                    flyout.Hide();
                    window.Close();
                }
            });
        }

        [TestMethod]
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
                        Assert.IsNotNull(app?.TryFindResource(key),
                            string.Format("Resource '{0}' must resolve in CommandBarFlyout theme cycle step: {1}", key, theme));
                    }
                }
            });
        }

        [TestMethod]
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
                    Assert.IsNotNull(pressScale, "The AppBarButton template must expose the PressScale transform.");
                    Assert.AreEqual(1.0, pressScale.ScaleX, 0.001, "The backplate must rest at 1.0 scale.");

                    // Press: the Button.xaml press-scale storyboard settles at 0.98.
                    button.SetPressed(pressed: true);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
                            () => pressScale.ScaleX <= 0.98 && pressScale.ScaleY <= 0.98),
                        "Pressing must animate the backplate down to the 0.98 press scale.");

                    // Release: the release storyboard restores 1.0.
                    button.SetPressed(pressed: false);
                    Assert.IsTrue(WaitUntil(window.Dispatcher, 2000,
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
