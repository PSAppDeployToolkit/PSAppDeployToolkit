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

namespace Fluence.Wpf.Demo.Pages
{
    /// <summary>
    /// Settings page for the demo shell.
    /// </summary>
    public partial class GallerySettingsPage : Page
    {
        // The page-level horizontal inset (DemoPageContentMargin) now lives on the outer
        // PageContentGrid via GalleryPageContentGridStyle, so SettingsScrollViewer.ActualWidth
        // is already net of that margin, and the host borrows the scrollbar rail back out of it
        // (DemoPageScrollHostMargin). This constant only needs the scrolling StackPanel's own
        // right-hand gutter (DemoPageScrollContentMargin = 0,0,44,48); only its horizontal part matters here.
        private const double PageHorizontalMargin = 44.0;
        private const double PageMaxWidth = 1064.0;
        private const double CompactSettingsWidth = 640.0;
        private const double RegularPickerWidth = 240.0;
        private const double CompactPickerWidth = 180.0;
        private const double RegularCaptionPickerWidth = 160.0;
        private const double CompactCaptionPickerWidth = 140.0;
        private static readonly Uri RepositoryUri = new UriBuilder("https", "github.com", -1, "sintaxasn/fluence.wpf").Uri;
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

            // The gallery page header's theme toggle changes the theme through
            // ApplicationThemeManager.Apply directly, bypassing AppThemeComboBox_SelectionChanged.
            // Subscribing here (and unsubscribing on Unloaded, never in the constructor, see
            // AGENTS.md section 9) keeps the combo in sync with an external theme change.
            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
            ApplicationThemeManager.Changed += ApplicationThemeManager_Changed;

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

            ApplicationThemeManager.Changed -= ApplicationThemeManager_Changed;
        }

        private void Owner_DemoNavigationPaneStateChanged(object? sender, EventArgs e)
        {
            SyncSelections();
        }

        private void ApplicationThemeManager_Changed(object? sender, ThemeChangedEventArgs e)
        {
            // Re-entrant guard: selecting the combo item here must not fire
            // AppThemeComboBox_SelectionChanged back into ApplicationThemeManager.Apply.
            _syncing = true;
            try
            {
                SelectComboItemByTag(AppThemeComboBox, GetCurrentThemeOption());
                UpdateThemeStateLabel(ApplicationThemeManager.CurrentTheme);
            }
            finally
            {
                _syncing = false;
            }
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
            bool isPaneOpen = (_owner?.IsDemoNavigationPaneOpen()) is true;

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
                WindowBackdropType.Auto => SettingsBackdropOption.Auto,
                WindowBackdropType.Mica => SettingsBackdropOption.Mica,
                WindowBackdropType.Acrylic => SettingsBackdropOption.Acrylic,
                WindowBackdropType.Tabbed => SettingsBackdropOption.Tabbed,
                WindowBackdropType.None => SettingsBackdropOption.None,
                null => SettingsBackdropOption.Auto,
                _ => SettingsBackdropOption.Auto,
            };
        }

        private void AppThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Fluence's ComboBox auto-selects its first item as soon as its items are populated
            // (see Controls/ComboBox.cs OnItemsChanged/TryAutoSelectFirstItem), which happens
            // synchronously during InitializeComponent, before this page has loaded and before
            // SyncSelections has run. Without the IsLoaded guard that auto-select fires this
            // handler with "Use system setting" and forces ApplicationThemeManager.Apply(Auto, ...)
            // on the very first construction of the page, silently discarding whatever theme was
            // already in effect. NavigationStyleComboBox_SelectionChanged already guards the same
            // way for the same reason.
            if (!IsLoaded || _syncing || GetSelectedTag(AppThemeComboBox) is not SettingsThemeOption option)
            {
                return;
            }

            ApplicationThemeManager.Apply(MapTheme(option), _owner?.SystemBackdropType ?? WindowBackdropType.Auto);
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
            // Same construction-time auto-select hazard as AppThemeComboBox_SelectionChanged: guard
            // with IsLoaded so the very first page construction cannot force the backdrop back to
            // the first ComboBoxItem ("Auto") before SyncSelections has synced the real value.
            if (!IsLoaded || _syncing || GetSelectedTag(BackdropComboBox) is not SettingsBackdropOption option)
            {
                return;
            }

            WindowBackdropType backdrop = MapBackdrop(option);
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

            if (ThemeWatcherToggle.IsChecked is true)
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

            bool showTitle = ShowWindowTitleToggle.IsChecked is true;
            string title = showTitle ? MainWindow.GalleryWindowTitle : string.Empty;
            _owner.SetUserShowTitle(showTitle, title);

            bool showIcon = ShowWindowIconToggle.IsChecked is true;
            // Use FluenceWindow's rasterized brand icon (the same BitmapSource it applies by default),
            // not the raw vector DrawingImage resource: Window.Icon drives the Win32 HICON, which does
            // not reliably render a DrawingImage, so a re-toggle would otherwise blank the taskbar icon.
            ImageSource? icon = showIcon ? Controls.FluenceWindow.DefaultIcon : null;
            _owner.SetUserShowIcon(showIcon, icon);
        }

        private void CopyRepositoryButton_Click(object sender, RoutedEventArgs e)
        {
            DemoClipboard.SetText(RepositoryUri.AbsoluteUri);
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

        private static WindowBackdropType MapBackdrop(SettingsBackdropOption option)
        {
            return option switch
            {
                SettingsBackdropOption.Auto => WindowBackdropType.Auto,
                SettingsBackdropOption.Mica => WindowBackdropType.Mica,
                SettingsBackdropOption.Acrylic => WindowBackdropType.Acrylic,
                SettingsBackdropOption.Tabbed => WindowBackdropType.Tabbed,
                SettingsBackdropOption.None => WindowBackdropType.None,
                _ => WindowBackdropType.Auto,
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
