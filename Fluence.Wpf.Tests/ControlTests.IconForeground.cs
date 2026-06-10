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
using System.Windows.Documents;
using System.Windows.Media;
using Fluent = Fluence.Wpf.Controls;

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// A control's user-supplied Icon must render in the same color as that control's text,
    /// through theme switches and visual states, unless the consumer sets an explicit
    /// Foreground on the icon element (a local value must still win).
    /// </summary>
    public partial class ControlTests
    {
        [TestMethod]
        public void Button_FontIconIcon_MatchesTextForeground_AcrossStatesAndThemes()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                Fluent.Button button = new()
                {
                    Content = "Send",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                Window window = new() { Content = button };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(button, "MainContentPresenter",
                        "Button icon at rest must match the button text foreground.");

                    button.Appearance = ControlAppearance.Accent;
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();
                    AssertIconMatchesText(button, "MainContentPresenter",
                        "Button icon with Appearance=Accent must follow the on-accent text foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextOnAccentFillColorPrimaryBrush"),
                        GetIconForegroundColor(button),
                        "Accent button icon must use the on-accent primary text color.");

                    button.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(button, "MainContentPresenter",
                        "Disabled accent button icon must follow the disabled on-accent text foreground.");
                    ContentPresenter? iconPresenter = FindVisualChildByName<ContentPresenter>(button, "IconPresenter");
                    Assert.IsNotNull(iconPresenter, "Button template should expose IconPresenter.");
                    Assert.AreEqual(1.0, iconPresenter.Opacity, 0.001,
                        "Disabled icon must not be double-dimmed; the disabled foreground brush carries the dim level.");

                    button.IsEnabled = true;
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    Assert.IsTrue(
                        WaitUntil(window.Dispatcher, 2000, () => IconMatchesText(button, "MainContentPresenter")),
                        "Button icon must keep matching the text foreground after a Light to Dark theme switch.");
                }
                finally
                {
                    window.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Button_ExplicitIconForeground_LocalValueStillWins()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.Button button = new()
                {
                    Appearance = ControlAppearance.Accent,
                    Content = "Send",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724", Foreground = Brushes.Red }
                };
                Window window = new() { Content = button };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(Colors.Red, GetIconForegroundColor(button),
                        "A consumer-set icon Foreground (local value) must beat the icon-follows-text wiring.");

                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Colors.Red, GetIconForegroundColor(button),
                        "A consumer-set icon Foreground must survive a theme switch.");
                }
                finally
                {
                    window.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void HyperlinkButton_FontIconIcon_MatchesTextForeground_AtRestAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.HyperlinkButton link = new()
                {
                    Content = "Learn more",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                Window window = new() { Content = link };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(link, "MainContentPresenter",
                        "HyperlinkButton icon at rest must match the accent link text foreground.");
                    Assert.AreEqual(
                        GetResourceColor("AccentTextFillColorPrimaryBrush"),
                        GetIconForegroundColor(link),
                        "HyperlinkButton icon at rest must use the accent link text color.");

                    link.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(link, "MainContentPresenter",
                        "Disabled HyperlinkButton icon must follow the disabled text foreground.");
                    ContentPresenter? iconPresenter = FindVisualChildByName<ContentPresenter>(link, "IconPresenter");
                    Assert.IsNotNull(iconPresenter, "HyperlinkButton template should expose IconPresenter.");
                    Assert.AreEqual(1.0, iconPresenter.Opacity, 0.001,
                        "Disabled icon must not be double-dimmed; the disabled foreground brush carries the dim level.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void NavigationViewItem_FontIconIcon_MatchesTextForeground_RestSelectedAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.NavigationViewItem item = new()
                {
                    Content = "Home",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                Window window = new()
                {
                    Content = item,
                    Width = 240,
                    Height = 80
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(item, "ContentPresenter",
                        "NavigationViewItem icon at rest must match the item text foreground.");

                    item.IsSelected = true;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(item, "ContentPresenter",
                        "Selected NavigationViewItem icon must match the item text foreground.");

                    item.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(item, "ContentPresenter",
                        "Disabled NavigationViewItem icon must follow the disabled text foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(item),
                        "Disabled NavigationViewItem icon must use the disabled text color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TabViewItem_FontIconIcon_MatchesTextForeground_UnselectedAndSelected()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.TabViewItem iconTab = new()
                {
                    Header = "Details",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                Fluent.TabView tabView = new();
                _ = tabView.Items.Add(new Fluent.TabViewItem { Header = "First" });
                _ = tabView.Items.Add(iconTab);
                tabView.SelectedIndex = 0;
                Window window = new()
                {
                    Content = tabView,
                    Width = 420,
                    Height = 200
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(iconTab, "HeaderHost",
                        "Unselected TabViewItem icon must match the secondary tab header foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorSecondaryBrush"),
                        GetIconForegroundColor(iconTab),
                        "Unselected TabViewItem icon must use the secondary text color.");

                    iconTab.IsSelected = true;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(iconTab, "HeaderHost",
                        "Selected TabViewItem icon must follow the promoted primary header foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorPrimaryBrush"),
                        GetIconForegroundColor(iconTab),
                        "Selected TabViewItem icon must use the primary text color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void MenuItem_CustomFontIconIcon_MatchesTextForeground_RestAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.MenuItem menuItem = new()
                {
                    Header = "Open",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                Window window = new()
                {
                    Content = menuItem,
                    Width = 240,
                    Height = 80
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(GetControlForegroundColor(menuItem), GetIconForegroundColor(menuItem),
                        "MenuItem custom icon at rest must match the header text foreground.");

                    menuItem.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(GetControlForegroundColor(menuItem), GetIconForegroundColor(menuItem),
                        "Disabled MenuItem custom icon must follow the disabled header foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(menuItem),
                        "Disabled MenuItem custom icon must use the disabled text color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void InfoBar_CustomFontIconIcon_FollowsTextForeground_NotSeverityColor()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.InfoBar custom = new()
                {
                    Title = "Saved",
                    Message = "All changes were written.",
                    Severity = InfoBarSeverity.Error,
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" },
                    IsOpen = true
                };
                Fluent.InfoBar standard = new()
                {
                    Title = "Failure",
                    Message = "Something broke.",
                    Severity = InfoBarSeverity.Error,
                    IsOpen = true
                };
                StackPanel panel = new();
                _ = panel.Children.Add(custom);
                _ = panel.Children.Add(standard);
                Window window = new()
                {
                    Content = panel,
                    Width = 420,
                    Height = 220
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    TextBlock? title = FindVisualChildByName<TextBlock>(custom, "TitleTextBlock");
                    Assert.IsNotNull(title, "InfoBar template should expose TitleTextBlock.");
                    SolidColorBrush? titleBrush = title.Foreground as SolidColorBrush;
                    Assert.IsNotNull(titleBrush, "InfoBar title foreground should be a SolidColorBrush.");
                    Assert.AreEqual(titleBrush.Color, GetIconForegroundColor(custom),
                        "InfoBar custom icon must follow the InfoBar text foreground.");
                    Assert.AreNotEqual(GetResourceColor("SystemFillColorCriticalBrush"), GetIconForegroundColor(custom),
                        "InfoBar custom icon must not inherit the semantic severity color.");

                    custom.Foreground = Brushes.DarkOrchid;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(Colors.DarkOrchid, GetIconForegroundColor(custom),
                        "InfoBar custom icon must track a consumer-set control Foreground exactly like the title text.");

                    TextBlock? defaultIcon = FindVisualChildByName<TextBlock>(standard, "DefaultIcon");
                    Assert.IsNotNull(defaultIcon, "InfoBar template should expose DefaultIcon.");
                    SolidColorBrush? defaultIconBrush = defaultIcon.Foreground as SolidColorBrush;
                    Assert.IsNotNull(defaultIconBrush, "InfoBar default icon foreground should be a SolidColorBrush.");
                    Assert.AreEqual(GetResourceColor("SystemFillColorCriticalBrush"), defaultIconBrush.Color,
                        "The default severity icon must keep its semantic severity color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void Card_FontIconIcon_MatchesHeaderForeground_RestAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.Card card = new()
                {
                    Header = "Storage",
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" },
                    Content = "Body"
                };
                Window window = new()
                {
                    Content = card,
                    Width = 320,
                    Height = 200
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    AssertIconMatchesText(card, "HeaderPresenter",
                        "Card icon at rest must match the card header foreground.");

                    card.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    AssertIconMatchesText(card, "HeaderPresenter",
                        "Disabled Card icon must follow the disabled header foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(card),
                        "Disabled Card icon must use the disabled text color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void ComboBox_FontIconIcon_MatchesTextForeground_RestAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.ComboBox combo = new()
                {
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" }
                };
                _ = combo.Items.Add("First");
                combo.SelectedIndex = 0;
                Window window = new()
                {
                    Content = combo,
                    Width = 240,
                    Height = 80
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(GetControlForegroundColor(combo), GetIconForegroundColor(combo),
                        "ComboBox icon at rest must match the selection text foreground.");

                    combo.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(GetControlForegroundColor(combo), GetIconForegroundColor(combo),
                        "Disabled ComboBox icon must follow the disabled selection foreground.");
                    ContentPresenter? leftIcon = FindVisualChildByName<ContentPresenter>(combo, "LeftIcon");
                    Assert.IsNotNull(leftIcon, "ComboBox template should expose LeftIcon.");
                    Assert.AreEqual(1.0, leftIcon.Opacity, 0.001,
                        "Disabled icon must not be double-dimmed; the disabled foreground brush carries the dim level.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void TextBox_FontIconIcon_MatchesTextForeground_RestAndDisabled()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);

                Fluent.TextBox textBox = new()
                {
                    Icon = new Fluent.FontIcon { Glyph = "\uE724" },
                    IconPlacement = ElementPlacement.Left,
                    Text = "Value"
                };
                Window window = new()
                {
                    Content = textBox,
                    Width = 240,
                    Height = 80
                };

                try
                {
                    window.Show();
                    DrainDispatcher(window.Dispatcher);
                    window.UpdateLayout();

                    Assert.AreEqual(GetControlForegroundColor(textBox), GetIconForegroundColor(textBox),
                        "TextBox icon at rest must match the input text foreground.");

                    textBox.IsEnabled = false;
                    DrainDispatcher(window.Dispatcher);
                    Assert.AreEqual(GetControlForegroundColor(textBox), GetIconForegroundColor(textBox),
                        "Disabled TextBox icon must follow the disabled input foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(textBox),
                        "Disabled TextBox icon must use the disabled text color.");
                }
                finally
                {
                    window.Close();
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        [TestMethod]
        public void AppBarButton_FontIconIcon_MatchesTextForeground_SecondaryAndCompact()
        {
            RunOnStaThread(() =>
            {
                Application? application = EnsureApplication();
                ResourceDictionary? genericDictionary = MergeGenericDictionary(application);
                ApplicationAccentColorManager.ApplyCustomAccent(Color.FromRgb(0x00, 0x78, 0xD4));

                // Secondary / overflow style: icon column + label column side by side.
                Fluent.AppBarButton secondary = new()
                {
                    Label = "Share",
                    Icon = new Fluent.FontIcon { Glyph = "\uE72A" },
                    Style = (Style)Application.Current.TryFindResource("CommandBarFlyoutSecondaryAppBarButtonStyle")
                };
                Window secondaryWindow = new()
                {
                    Content = secondary,
                    Width = 240,
                    Height = 60
                };

                try
                {
                    secondaryWindow.Show();
                    DrainDispatcher(secondaryWindow.Dispatcher);
                    secondaryWindow.UpdateLayout();

                    // At rest: icon Foreground must match the control Foreground (both bound via TemplateBinding Foreground).
                    Assert.AreEqual(
                        GetControlForegroundColor(secondary),
                        GetIconForegroundColor(secondary),
                        "Secondary AppBarButton icon at rest must match the control Foreground (= label foreground).");

                    // Disabled: icon tracks disabled foreground; icon opacity stays 1.0 (no double-dim).
                    secondary.IsEnabled = false;
                    DrainDispatcher(secondaryWindow.Dispatcher);
                    Assert.AreEqual(
                        GetControlForegroundColor(secondary),
                        GetIconForegroundColor(secondary),
                        "Disabled secondary AppBarButton icon must follow the disabled control Foreground.");
                    Assert.AreEqual(
                        GetResourceColor("TextFillColorDisabledBrush"),
                        GetIconForegroundColor(secondary),
                        "Disabled secondary AppBarButton icon must use the disabled text color.");
                    ContentPresenter? secondaryIconPresenter = FindVisualChildByName<ContentPresenter>(secondary, "IconPresenter");
                    Assert.IsNotNull(secondaryIconPresenter, "Secondary AppBarButton template should expose IconPresenter.");
                    Assert.AreEqual(1.0, secondaryIconPresenter.Opacity, 0.001,
                        "Disabled secondary AppBarButton icon must not be double-dimmed; the disabled foreground brush carries the dim level.");
                }
                finally
                {
                    secondaryWindow.Close();
                }

                // Compact / primary style (implicit style, no key) with Light->Dark switch.
                Fluent.AppBarButton compact = new()
                {
                    Label = "Copy",
                    Icon = new Fluent.FontIcon { Glyph = "\uE72A" }
                };
                Window compactWindow = new()
                {
                    Content = compact,
                    Width = 60,
                    Height = 60
                };

                try
                {
                    compactWindow.Show();
                    DrainDispatcher(compactWindow.Dispatcher);
                    compactWindow.UpdateLayout();

                    Assert.AreEqual(
                        GetControlForegroundColor(compact),
                        GetIconForegroundColor(compact),
                        "Compact AppBarButton icon at rest must match the control Foreground.");

                    // Switch to Dark theme; icon must re-resolve to the new Foreground brush value.
                    ApplicationThemeManager.Apply(ApplicationTheme.Dark, BackdropType.None, true);
                    Assert.IsTrue(
                        WaitUntil(compactWindow.Dispatcher, 2000, () =>
                            GetControlForegroundColor(compact) == GetIconForegroundColor(compact)),
                        "Compact AppBarButton icon must keep matching the control Foreground after a Light to Dark theme switch.");
                }
                finally
                {
                    compactWindow.Close();
                    ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
                    _ = application?.Resources.MergedDictionaries.Remove(genericDictionary);
                }
            });
        }

        private static Color GetControlForegroundColor(System.Windows.Controls.Control control)
        {
            SolidColorBrush? brush = control.Foreground as SolidColorBrush;
            Assert.IsNotNull(brush, "The control Foreground should be a SolidColorBrush.");
            return brush.Color;
        }

        private static Color GetResourceColor(string brushKey)
        {
            SolidColorBrush? brush = Application.Current.TryFindResource(brushKey) as SolidColorBrush;
            Assert.IsNotNull(brush, "Resource should resolve to a SolidColorBrush: " + brushKey);
            return brush.Color;
        }

        private static Color GetIconForegroundColor(DependencyObject root)
        {
            Fluent.FontIcon? icon = FindVisualChildren<Fluent.FontIcon>(root).FirstOrDefault();
            Assert.IsNotNull(icon, "A FontIcon icon should be present in the visual tree.");
            SolidColorBrush? brush = icon.Foreground as SolidColorBrush;
            Assert.IsNotNull(brush, "The FontIcon Foreground should be a SolidColorBrush.");
            return brush.Color;
        }

        private static Color GetPresenterTextColor(DependencyObject root, string presenterName)
        {
            ContentPresenter? presenter = FindVisualChildByName<ContentPresenter>(root, presenterName);
            Assert.IsNotNull(presenter, "The template should expose the text presenter: " + presenterName);
            SolidColorBrush? brush = TextElement.GetForeground(presenter) as SolidColorBrush;
            Assert.IsNotNull(brush, "The text presenter foreground should be a SolidColorBrush.");
            return brush.Color;
        }

        private static bool IconMatchesText(DependencyObject root, string presenterName)
        {
            return GetIconForegroundColor(root) == GetPresenterTextColor(root, presenterName);
        }

        private static void AssertIconMatchesText(DependencyObject root, string presenterName, string message)
        {
            Assert.AreEqual(GetPresenterTextColor(root, presenterName), GetIconForegroundColor(root), message);
        }
    }
}
