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
using System.Diagnostics;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Settings page for the demo shell.
    /// </summary>
    public partial class GallerySettingsPage : UserControl
    {
        private const double PageHorizontalMargin = 72.0;
        private const double PageMaxWidth = 1064.0;
        private const double CompactSettingsWidth = 640.0;
        private const double RegularPickerWidth = 240.0;
        private const double CompactPickerWidth = 180.0;
        private const double RegularCaptionPickerWidth = 160.0;
        private const double CompactCaptionPickerWidth = 140.0;
        private static readonly Uri RepositoryUri = new UriBuilder("https", "github.com", -1, "sintaxasn/fluence.wpf").Uri;
        private static readonly Uri DemoAppIconUri = new(
            "pack://application:,,,/Fluence.Wpf.Demo;component/Resources/fluence-wpf-appicon-256.ico",
            UriKind.Absolute);
        private readonly MainWindow? _owner;
        private bool _syncing;

        /// <summary>
        /// Initializes a new instance of the <see cref="GallerySettingsPage"/> class.
        /// </summary>
        public GallerySettingsPage()
            : this(Application.Current?.MainWindow as MainWindow)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="GallerySettingsPage"/> class.
        /// </summary>
        /// <param name="owner">The shell window that owns navigation and backdrop settings.</param>
        public GallerySettingsPage(MainWindow? owner)
        {
            _owner = owner;
            InitializeComponent();

            Version version = typeof(GallerySettingsPage).Assembly.GetName().Version ?? new Version(0, 0);
            VersionTextBlock.Text = "Version " + version.ToString(3);
            Loaded += GallerySettingsPage_Loaded;
            Unloaded += GallerySettingsPage_Unloaded;
        }

        private void GallerySettingsPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (_owner is MainWindow owner)
            {
                owner.DemoNavigationPaneStateChanged -= Owner_DemoNavigationPaneStateChanged;
                owner.DemoNavigationPaneStateChanged += Owner_DemoNavigationPaneStateChanged;
            }

            UpdatePageContentWidth(SettingsScrollViewer.ActualWidth);
            SyncSelections();
            UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
            WindowChromeToggle_Changed(sender: null, e: null);
            CaptionVisibilityCombo_SelectionChanged(sender: null, e: null);
        }

        private void GallerySettingsPage_Unloaded(object sender, RoutedEventArgs e)
        {
            if (_owner is MainWindow owner)
            {
                owner.DemoNavigationPaneStateChanged -= Owner_DemoNavigationPaneStateChanged;
            }
        }

        private void Owner_DemoNavigationPaneStateChanged(object? sender, EventArgs e)
        {
            SyncSelections();
        }

        private void SettingsScrollViewer_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdatePageContentWidth(e.NewSize.Width);
        }

        private void UpdatePageContentWidth(double viewportWidth)
        {
            if (PageContent is null)
            {
                return;
            }

            double availableWidth = Math.Max(0.0, viewportWidth - PageHorizontalMargin);
            double contentWidth = Math.Min(PageMaxWidth, availableWidth);
            if (contentWidth > 0.0)
            {
                PageContent.Width = contentWidth;
                UpdateResponsiveSettingsLayout(contentWidth);
            }
        }

        private void UpdateResponsiveSettingsLayout(double contentWidth)
        {
            bool compact = contentWidth < CompactSettingsWidth;
            double pickerWidth = compact ? CompactPickerWidth : RegularPickerWidth;
            double captionPickerWidth = compact ? CompactCaptionPickerWidth : RegularCaptionPickerWidth;

            AppThemeComboBox.Width = pickerWidth;
            NavigationStyleComboBox.Width = pickerWidth;
            BackdropComboBox.Width = pickerWidth;

            MinimizeVisibilityCombo.Width = captionPickerWidth;
            MaximizeVisibilityCombo.Width = captionPickerWidth;
            CloseVisibilityCombo.Width = captionPickerWidth;

            AccentPickerPanel.Orientation = compact ? Orientation.Vertical : Orientation.Horizontal;
            AccentSwatchRow.Columns = compact ? 4 : 7;
            AccentSwatchRow.Rows = compact ? 2 : 1;
            AccentSwatchRow.Margin = compact ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 12, 0);
            SystemAccentButton.MinWidth = compact ? 112.0 : 84.0;
            SystemAccentButton.HorizontalAlignment = compact ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;

            RepositoryActionsPanel.Orientation = compact ? Orientation.Vertical : Orientation.Horizontal;
            CopyRepositoryButton.Margin = compact ? new Thickness(0, 0, 0, 8) : new Thickness(0, 0, 8, 0);
            CopyRepositoryButton.HorizontalAlignment = compact ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
            OpenRepositoryButton.HorizontalAlignment = compact ? HorizontalAlignment.Stretch : HorizontalAlignment.Left;
        }

        private void SyncSelections()
        {
            _syncing = true;
            try
            {
                SelectComboItemByTag(AppThemeComboBox, GetCurrentThemeOption());
                SelectComboItemByTag(NavigationStyleComboBox, GetCurrentNavigationOption());
                SelectComboItemByTag(BackdropComboBox, GetCurrentBackdropOption());
                UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
            }
            finally
            {
                _syncing = false;
            }
        }

        private static SettingsThemeOption GetCurrentThemeOption()
        {
            return ApplicationThemeManager.CurrentTheme switch
            {
                ApplicationTheme.Auto => SettingsThemeOption.System,
                ApplicationTheme.Light => SettingsThemeOption.Light,
                ApplicationTheme.Dark => SettingsThemeOption.Dark,
                ApplicationTheme.HighContrast => SettingsThemeOption.HighContrast,
                _ => SettingsThemeOption.System,
            };
        }

        private SettingsNavigationOption GetCurrentNavigationOption()
        {
            NavigationViewPaneDisplayMode? mode = _owner?.GetDemoNavigationPaneDisplayMode();
            bool isPaneOpen = _owner?.IsDemoNavigationPaneOpen() == true;

            return mode switch
            {
                NavigationViewPaneDisplayMode.Left => isPaneOpen ? SettingsNavigationOption.Left : SettingsNavigationOption.LeftCompact,
                NavigationViewPaneDisplayMode.LeftCompact => SettingsNavigationOption.LeftCompact,
                NavigationViewPaneDisplayMode.Top => SettingsNavigationOption.Top,
                null => SettingsNavigationOption.Top,
                _ => SettingsNavigationOption.Top,
            };
        }

        private SettingsBackdropOption GetCurrentBackdropOption()
        {
            return _owner?.SystemBackdropType switch
            {
                BackdropType.Auto => SettingsBackdropOption.Auto,
                BackdropType.Mica => SettingsBackdropOption.Mica,
                BackdropType.Acrylic => SettingsBackdropOption.Acrylic,
                BackdropType.Tabbed => SettingsBackdropOption.Tabbed,
                BackdropType.None => SettingsBackdropOption.None,
                null => SettingsBackdropOption.Auto,
                _ => SettingsBackdropOption.Auto,
            };
        }

        private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing || GetSelectedTag(AppThemeComboBox) is not SettingsThemeOption option)
            {
                return;
            }

            ApplicationThemeManager.Apply(MapTheme(option), _owner?.SystemBackdropType ?? BackdropType.Auto);
            UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
        }

        private void NavigationStyleComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (!IsLoaded || _syncing || GetSelectedTag(NavigationStyleComboBox) is not SettingsNavigationOption option || _owner is null)
            {
                return;
            }

            _owner.SetDemoNavigationPaneDisplayMode(MapNavigation(option));
        }

        private void BackdropComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_syncing || GetSelectedTag(BackdropComboBox) is not SettingsBackdropOption option)
            {
                return;
            }

            BackdropType backdrop = MapBackdrop(option);
            if (_owner is MainWindow owner)
            {
                owner.SystemBackdropType = backdrop;
            }

            ApplicationThemeManager.Apply(ApplicationThemeManager.CurrentTheme, backdrop);
        }

        private void ThemeWatcherToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (!IsLoaded)
            {
                return;
            }

            Window? host = _owner ?? Window.GetWindow(this);
            if (host is null)
            {
                return;
            }

            if (ThemeWatcherToggle.IsChecked == true)
            {
                SystemThemeWatcher.Watch(host);
                SystemThemeLabel.Text = "Watching: Yes";
            }
            else
            {
                SystemThemeWatcher.UnWatch(host);
                SystemThemeLabel.Text = "Watching: No";
            }
        }

        private void AccentSwatch_Click(object sender, RoutedEventArgs e)
        {
            if (sender is not FrameworkElement swatch || !TryGetAccentSwatchColor(swatch.Tag?.ToString(), out Color accentColor))
            {
                return;
            }

            ApplicationAccentColorManager.ApplyCustomAccent(accentColor);
        }

        private void SystemAccentButton_Click(object sender, RoutedEventArgs e)
        {
            ApplicationAccentColorManager.ApplySystemAccent();
        }

        private void CaptionVisibilityCombo_SelectionChanged(object? sender, SelectionChangedEventArgs? e)
        {
            if (!IsLoaded || _owner is null)
            {
                return;
            }

            ApplyCaptionVisibility(MinimizeVisibilityCombo, value => _owner.IsMinimizeButtonVisible = value, enabled => _owner.IsMinimizable = enabled);
            ApplyCaptionVisibility(MaximizeVisibilityCombo, value => _owner.IsMaximizeButtonVisible = value, enabled => _owner.IsMaximizable = enabled);
            ApplyCaptionVisibility(CloseVisibilityCombo, value => _owner.IsCloseButtonVisible = value, enabled => _owner.IsClosable = enabled);
        }

        private void WindowChromeToggle_Changed(object? sender, RoutedEventArgs? e)
        {
            if (!IsLoaded || _owner is null)
            {
                return;
            }

            bool showTitle = ShowWindowTitleToggle.IsChecked == true;
            string title = showTitle ? MainWindow.GalleryWindowTitle : string.Empty;
            _owner.SetUserShowTitle(showTitle, title);

            bool showIcon = ShowWindowIconToggle.IsChecked == true;
            ImageSource? icon = showIcon ? new BitmapImage(DemoAppIconUri) : null;
            _owner.SetUserShowIcon(showIcon, icon);
        }

        private void CopyRepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            Clipboard.SetText(RepositoryUri.AbsoluteUri);
        }

        private void OpenRepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            ProcessStartInfo startInfo = new(RepositoryUri.AbsoluteUri)
            {
                UseShellExecute = true,
            };
            _ = Process.Start(startInfo);
        }

        private void UpdateThemeStateLabel(ApplicationTheme theme)
        {
            ThemeStateLabel.Text = string.Format(CultureInfo.CurrentCulture, "Current: {0}", theme);
        }

        private static ApplicationTheme MapTheme(SettingsThemeOption option)
        {
            return option switch
            {
                SettingsThemeOption.System => ApplicationTheme.Auto,
                SettingsThemeOption.Light => ApplicationTheme.Light,
                SettingsThemeOption.Dark => ApplicationTheme.Dark,
                SettingsThemeOption.HighContrast => ApplicationTheme.HighContrast,
                _ => ApplicationTheme.Auto,
            };
        }

        private static NavigationViewPaneDisplayMode MapNavigation(SettingsNavigationOption option)
        {
            return option switch
            {
                SettingsNavigationOption.Top => NavigationViewPaneDisplayMode.Top,
                SettingsNavigationOption.Left => NavigationViewPaneDisplayMode.Left,
                SettingsNavigationOption.LeftCompact => NavigationViewPaneDisplayMode.LeftCompact,
                _ => NavigationViewPaneDisplayMode.Left,
            };
        }

        private static BackdropType MapBackdrop(SettingsBackdropOption option)
        {
            return option switch
            {
                SettingsBackdropOption.Auto => BackdropType.Auto,
                SettingsBackdropOption.Mica => BackdropType.Mica,
                SettingsBackdropOption.Acrylic => BackdropType.Acrylic,
                SettingsBackdropOption.Tabbed => BackdropType.Tabbed,
                SettingsBackdropOption.None => BackdropType.None,
                _ => BackdropType.Auto,
            };
        }

        private static object? GetSelectedTag(ComboBox comboBox)
        {
            return comboBox.SelectedItem is ComboBoxItem item ? item.Tag : null;
        }

        private static void SelectComboItemByTag(ComboBox comboBox, object tag)
        {
            foreach (object item in comboBox.Items)
            {
                if (item is ComboBoxItem comboBoxItem && Equals(comboBoxItem.Tag, tag))
                {
                    comboBox.SelectedItem = comboBoxItem;
                    return;
                }
            }
        }

        private static bool TryGetAccentSwatchColor(string? hex, out Color accentColor)
        {
            accentColor = default;
            if (string.IsNullOrWhiteSpace(hex))
            {
                return false;
            }

            try
            {
                object converted = ColorConverter.ConvertFromString(hex);
                if (converted is Color color)
                {
                    accentColor = color;
                    return true;
                }
            }
            catch (FormatException)
            {
                return false;
            }

            return false;
        }

        private static void ApplyCaptionVisibility(
            ComboBox combo,
            Action<Visibility> setVisibility,
            Action<bool> setEnabled)
        {
            ComboBoxItem? item = combo.SelectedItem as ComboBoxItem;
            string? content = item?.Content as string;

            if (string.Equals(content, "Hidden", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Hidden);
                setEnabled(false);
            }
            else if (string.Equals(content, "Collapsed", StringComparison.Ordinal) ||
                string.Equals(content, "Hide", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Collapsed);
                setEnabled(false);
            }
            else if (string.Equals(content, "Disable", StringComparison.Ordinal) ||
                string.Equals(content, "Disabled", StringComparison.Ordinal))
            {
                setVisibility(Visibility.Visible);
                setEnabled(false);
            }
            else
            {
                setVisibility(Visibility.Visible);
                setEnabled(true);
            }
        }
    }
}
