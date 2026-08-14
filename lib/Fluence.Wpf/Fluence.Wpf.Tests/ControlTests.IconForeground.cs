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

using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// A control's user-supplied Icon must render in the same color as that control's text,
    /// through theme switches and visual states, unless the consumer sets an explicit
    /// Foreground on the icon element (a local value must still win).
    /// </summary>
    public partial class ControlTests
    {
        [Fact]
        public Task Button_FontIconIcon_MatchesTextForeground_AcrossStatesAndThemesAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Controls.Button button = new()
                {
                    Content = "Send",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                Window window = new() { Content = button };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(button, "MainContentPresenter");

                    button.Appearance = ControlAppearance.Accent;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    AssertIconMatchesText(button, "MainContentPresenter");
                    Assert.Equal(
                        GetResourceColor("TextOnAccentFillColorPrimaryBrush"),
                        GetIconForegroundColor(button));

                    button.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(button, "MainContentPresenter");
                    ContentPresenter iconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(button, "IconPresenter"));
                    Assert.Equal(1.0, iconPresenter.Opacity, 0.001);

                    button.IsEnabled = true;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    Assert.True(
                        await WaitUntilAsync(window.Dispatcher, 2000, () => IconMatchesText(button, "MainContentPresenter")).ConfigureAwait(true),
                        "Button icon must keep matching the text foreground after a Light to Dark theme switch.");
                }
                finally
                {
                    window.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task Button_ExplicitIconForeground_LocalValueStillWinsAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.Button button = new()
                {
                    Appearance = ControlAppearance.Accent,
                    Content = "Send",
                    Icon = new Controls.FontIcon { Glyph = "\uE724", Foreground = Brushes.Red },
                };
                Window window = new() { Content = button };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(Colors.Red, GetIconForegroundColor(button));

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Colors.Red, GetIconForegroundColor(button));
                }
                finally
                {
                    window.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task HyperlinkButton_FontIconIcon_MatchesTextForeground_AtRestAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.HyperlinkButton link = new()
                {
                    Content = "Learn more",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                Window window = new() { Content = link };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(link, "MainContentPresenter");
                    Assert.Equal(
                        GetResourceColor("AccentTextFillColorPrimaryBrush"),
                        GetIconForegroundColor(link));

                    link.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(link, "MainContentPresenter");
                    ContentPresenter iconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(link, "IconPresenter"));
                    Assert.Equal(1.0, iconPresenter.Opacity, 0.001);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task NavigationViewItem_FontIconIcon_MatchesTextForeground_RestSelectedAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.NavigationViewItem item = new()
                {
                    Content = "Home",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                Window window = new()
                {
                    Content = item,
                    Width = 240,
                    Height = 80,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(item, "ContentPresenter");

                    item.IsSelected = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(item, "ContentPresenter");

                    item.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(item, "ContentPresenter");
                    Assert.Equal(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(item));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task TabViewItem_FontIconIcon_MatchesTextForeground_UnselectedAndSelectedAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.TabViewItem iconTab = new()
                {
                    Header = "Details",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                Controls.TabView tabView = new();
                _ = tabView.Items.Add(new Controls.TabViewItem { Header = "First" });
                _ = tabView.Items.Add(iconTab);
                tabView.SelectedIndex = 0;
                Window window = new()
                {
                    Content = tabView,
                    Width = 420,
                    Height = 200,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(iconTab, "HeaderHost");
                    Assert.Equal(
                        GetResourceColor("TextFillColorSecondaryBrush"),
                        GetIconForegroundColor(iconTab));

                    iconTab.IsSelected = true;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(iconTab, "HeaderHost");
                    Assert.Equal(
                        GetResourceColor("TextFillColorPrimaryBrush"),
                        GetIconForegroundColor(iconTab));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task MenuItem_CustomFontIconIcon_MatchesTextForeground_RestAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.MenuItem menuItem = new()
                {
                    Header = "Open",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                Window window = new()
                {
                    Content = menuItem,
                    Width = 240,
                    Height = 80,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(GetControlForegroundColor(menuItem), GetIconForegroundColor(menuItem));

                    menuItem.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(GetControlForegroundColor(menuItem), GetIconForegroundColor(menuItem));
                    Assert.Equal(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(menuItem));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task InfoBar_CustomFontIconIcon_FollowsTextForeground_NotSeverityColorAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.InfoBar custom = new()
                {
                    Title = "Saved",
                    Message = "All changes were written.",
                    Severity = InfoBarSeverity.Error,
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                    IsOpen = true,
                };
                Controls.InfoBar standard = new()
                {
                    Title = "Failure",
                    Message = "Something broke.",
                    Severity = InfoBarSeverity.Error,
                    IsOpen = true,
                };
                StackPanel panel = new();
                _ = panel.Children.Add(custom);
                _ = panel.Children.Add(standard);
                Window window = new()
                {
                    Content = panel,
                    Width = 420,
                    Height = 220,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock title = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(custom, "TitleTextBlock"));
                    SolidColorBrush titleBrush = Assert.IsType<SolidColorBrush>(title.Foreground);
                    Assert.Equal(titleBrush.Color, GetIconForegroundColor(custom));
                    Assert.NotEqual(GetResourceColor("SystemFillColorCriticalBrush"), GetIconForegroundColor(custom));

                    custom.Foreground = Brushes.DarkOrchid;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(Colors.DarkOrchid, GetIconForegroundColor(custom));

                    TextBlock defaultIcon = Assert.IsAssignableFrom<TextBlock>(FindVisualChildByName<TextBlock>(standard, "DefaultIcon"));
                    SolidColorBrush defaultIconBrush = Assert.IsType<SolidColorBrush>(defaultIcon.Foreground);
                    Assert.Equal(GetResourceColor("SystemFillColorCriticalBrush"), defaultIconBrush.Color);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task Card_FontIconIcon_MatchesHeaderForeground_RestAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.Card card = new()
                {
                    Header = "Storage",
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                    Content = "Body",
                };
                Window window = new()
                {
                    Content = card,
                    Width = 320,
                    Height = 200,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(card, "HeaderPresenter");

                    card.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(card, "HeaderPresenter");
                    Assert.Equal(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(card));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task ComboBox_FontIconIcon_MatchesTextForeground_RestAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.ComboBox combo = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                };
                _ = combo.Items.Add("First");
                combo.SelectedIndex = 0;
                Window window = new()
                {
                    Content = combo,
                    Width = 240,
                    Height = 80,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(GetControlForegroundColor(combo), GetIconForegroundColor(combo));

                    combo.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(GetControlForegroundColor(combo), GetIconForegroundColor(combo));
                    ContentPresenter leftIcon = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(combo, "LeftIcon"));
                    Assert.Equal(1.0, leftIcon.Opacity, 0.001);
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task TextBox_FontIconIcon_MatchesTextForeground_RestAndDisabledAsync()
        {
            return WpfTestSta.RunOnStaAsync(static () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Controls.TextBox textBox = new()
                {
                    Icon = new Controls.FontIcon { Glyph = "\uE724" },
                    IconPlacement = ElementPlacement.Left,
                    Text = "Value",
                };
                Window window = new()
                {
                    Content = textBox,
                    Width = 240,
                    Height = 80,
                };

                try
                {
                    window.Show();
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.Equal(GetControlForegroundColor(textBox), GetIconForegroundColor(textBox));

                    textBox.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(window.Dispatcher);
                    Assert.Equal(GetControlForegroundColor(textBox), GetIconForegroundColor(textBox));
                    Assert.Equal(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(textBox));
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [Fact]
        public Task AppBarButton_FontIconIcon_MatchesTextForeground_SecondaryAndCompactAsync()
        {
            return WpfTestSta.RunOnStaAsync(async () =>
            {
                Application application = WpfTestSta.EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                // Secondary / overflow style: icon column + label column side by side.
                Controls.AppBarButton secondary = new()
                {
                    Label = "Share",
                    Icon = new Controls.FontIcon { Glyph = "\uE72A" },
                    Style = (Style)Application.Current.TryFindResource("CommandBarFlyoutSecondaryAppBarButtonStyle"),
                };
                Window secondaryWindow = new()
                {
                    Content = secondary,
                    Width = 240,
                    Height = 60,
                };

                try
                {
                    secondaryWindow.Show();
                    WpfTestSta.DrainDispatcher(secondaryWindow.Dispatcher);
                    secondaryWindow.UpdateLayout();

                    // At rest: icon Foreground must match the control Foreground (both bound via TemplateBinding Foreground).
                    Assert.Equal(
                        GetControlForegroundColor(secondary),
                        GetIconForegroundColor(secondary));

                    // Disabled: icon tracks disabled foreground; icon opacity stays 1.0 (no double-dim).
                    secondary.IsEnabled = false;
                    WpfTestSta.DrainDispatcher(secondaryWindow.Dispatcher);
                    Assert.Equal(
                        GetControlForegroundColor(secondary),
                        GetIconForegroundColor(secondary));
                    Assert.Equal(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(secondary));
                    ContentPresenter secondaryIconPresenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(secondary, "IconPresenter"));
                    Assert.Equal(1.0, secondaryIconPresenter.Opacity, 0.001);
                }
                finally
                {
                    secondaryWindow.Close();
                }

                // Compact / primary style (implicit style, no key) with Light->Dark switch.
                Controls.AppBarButton compact = new()
                {
                    Label = "Copy",
                    Icon = new Controls.FontIcon { Glyph = "\uE72A" },
                };
                Window compactWindow = new()
                {
                    Content = compact,
                    Width = 60,
                    Height = 60,
                };

                try
                {
                    compactWindow.Show();
                    WpfTestSta.DrainDispatcher(compactWindow.Dispatcher);
                    compactWindow.UpdateLayout();

                    Assert.Equal(
                        GetControlForegroundColor(compact),
                        GetIconForegroundColor(compact));

                    // Switch to Dark theme; icon must re-resolve to the new Foreground brush value.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, updateAccent: true);
                    Assert.True(
                        await WaitUntilAsync(compactWindow.Dispatcher, 2000, () =>
                            GetControlForegroundColor(compact) == GetIconForegroundColor(compact)).ConfigureAwait(true),
                        "Compact AppBarButton icon must keep matching the control Foreground after a Light to Dark theme switch.");
                }
                finally
                {
                    compactWindow.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, updateAccent: true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static Color GetControlForegroundColor(Control control)
        {
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(control.Foreground);
            return brush.Color;
        }

        private static Color GetResourceColor(string brushKey)
        {
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(Application.Current.TryFindResource(brushKey));
            return brush.Color;
        }

        private static Color GetIconForegroundColor(DependencyObject root)
        {
            Controls.FontIcon icon = Assert.IsAssignableFrom<Controls.FontIcon>(FindVisualChildren<Controls.FontIcon>(root).FirstOrDefault());
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(icon.Foreground);
            return brush.Color;
        }

        private static Color GetPresenterTextColor(DependencyObject root, string presenterName)
        {
            ContentPresenter presenter = Assert.IsAssignableFrom<ContentPresenter>(FindVisualChildByName<ContentPresenter>(root, presenterName));
            SolidColorBrush brush = Assert.IsType<SolidColorBrush>(TextElement.GetForeground(presenter));
            return brush.Color;
        }

        private static bool IconMatchesText(DependencyObject root, string presenterName)
        {
            return GetIconForegroundColor(root) == GetPresenterTextColor(root, presenterName);
        }

        private static void AssertIconMatchesText(DependencyObject root, string presenterName)
        {
            Assert.Equal(GetPresenterTextColor(root, presenterName), GetIconForegroundColor(root));
        }
    }
}
