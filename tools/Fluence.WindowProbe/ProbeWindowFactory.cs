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
using System.Windows;
using System.Windows.Automation;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Shell;
using Fluence.Wpf;

namespace Fluence.WindowProbe
{
    internal static class ProbeWindowFactory
    {
        internal static Action<string, Action>? RecordAction { get; set; }

        internal static Window Create(ProbeOptions options)
        {
            Window window = string.Equals(options.Mode, "fluence", StringComparison.Ordinal) ? new Fluence.Wpf.Controls.FluenceWindow { SystemBackdropType = ToBackdrop(options.Backdrop) } : new Window();
            window.Title = "Fluence window probe - " + options.Mode;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Left = 80;
            window.Top = 80;
            window.Topmost = options.Topmost;
            if (string.Equals(options.Size, "content", StringComparison.Ordinal))
            {
                window.SizeToContent = SizeToContent.WidthAndHeight;
            }
            else
            {
                window.Width = 640;
                window.Height = 440;
                window.SizeToContent = SizeToContent.Manual;
            }
            if (string.Equals(options.Mode, "plain", StringComparison.Ordinal))
            {
                window.Background = new SolidColorBrush(FallbackColor(options.Theme));
            }
            else if (string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal))
            {
                UpdateChromeSurface(window, options, afterAction: false);
            }

#if NET10_0_OR_GREATER
            if (string.Equals(options.Mode, "wpf-fluent", StringComparison.Ordinal))
            {
#pragma warning disable S1075 // WPF's documented Fluent resource pack URI is intentionally fixed.
                window.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = new Uri("pack://application:,,,/PresentationFramework.Fluent;component/Themes/Fluent.xaml", UriKind.Absolute) });
#pragma warning restore S1075
                window.ThemeMode = string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? ThemeMode.Dark : ThemeMode.Light;
            }
#endif
            if (string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal))
            {
                WindowChrome.SetWindowChrome(window, new WindowChrome
                {
                    CaptionHeight = 0,
                    ResizeBorderThickness = new Thickness(4),
                    CornerRadius = new CornerRadius(0),
                    UseAeroCaptionButtons = false,
                    NonClientFrameEdges = NonClientFrameEdges.None,
                    GlassFrameThickness = options.FullGlass ? WindowChrome.GlassFrameCompleteThickness : new Thickness(0),
                });
                window.SourceInitialized += (_, _) =>
                {
                    IntPtr hwnd = new WindowInteropHelper(window).Handle;
                    if (options.DwmAttribute)
                    {
                        options.ProbeBackdropSetHResult = NativeSnapshot.SetBackdrop(hwnd, options.Backdrop);
                        options.ProbeBackdropSetStatus = "issued";
                    }

                    ApplyDarkMode(hwnd, options);
                    options.ProbeCornerSetHResult = NativeSnapshot.SetCornerPreference(hwnd);
                    options.ProbeCornerSetStatus = "issued";
                    UpdateChromeSurface(window, options, afterAction: false);
                };
            }

            StackPanel root = new() { Margin = new Thickness(24), MinWidth = 540 };
            window.Tag = root;
            if (!string.Equals(options.Mode, "wpf-fluent", StringComparison.Ordinal))
            {
                TextElement.SetForeground(root, new SolidColorBrush(string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? Colors.White : Colors.Black));
            }

            root.Children.Add(new TextBlock { Text = "Fluence window probe", FontSize = 24, FontWeight = FontWeights.SemiBold, Margin = new Thickness(0, 0, 0, 12) });
            TextBlock statusText = new() { Text = "Mode: " + options.Mode + "  |  Requested backdrop: " + options.Backdrop, Margin = new Thickness(0, 0, 0, 12) };
            root.Children.Add(statusText);
            StackPanel actions = new() { Orientation = Orientation.Horizontal, Margin = new Thickness(0, 0, 0, 12) };
            Button theme = MakeButton("Next theme", "NextTheme");
            Button backdrop = MakeButton("Next backdrop", "NextBackdrop");
            Button grow = MakeButton("Grow content", "GrowContent");
            backdrop.IsEnabled = string.Equals(options.Mode, "fluence", StringComparison.Ordinal) || (string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal) && options.DwmAttribute);
            grow.IsEnabled = string.Equals(options.Size, "content", StringComparison.Ordinal);
            actions.Children.Add(theme);
            actions.Children.Add(backdrop);
            actions.Children.Add(grow);
            root.Children.Add(actions);
            StackPanel rows = new();
            rows.Children.Add(new TextBlock { Text = "Baseline content row", Margin = new Thickness(0, 4, 0, 4) });
            root.Children.Add(rows);
            TextBlock? probeTitle = null;
            if (string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal))
            {
                Grid shell = new();
                shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(48) });
                shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                Grid titleArea = new() { Background = Brushes.Transparent };
                probeTitle = new TextBlock { Text = window.Title, VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(16, 0, 0, 0), Foreground = new SolidColorBrush(string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? Colors.White : Colors.Black) };
                titleArea.Children.Add(probeTitle);
                Button close = MakeButton("Close", "CloseProbe");
                close.Width = 46;
                close.HorizontalAlignment = HorizontalAlignment.Right;
                close.Margin = new Thickness(0);
                close.Click += (_, _) => SystemCommands.CloseWindow(window);
                WindowChrome.SetIsHitTestVisibleInChrome(close, hitTestVisible: true);
                titleArea.Children.Add(close);
                shell.Children.Add(titleArea);
                Grid.SetRow(root, 1);
                shell.Children.Add(root);
                window.Content = shell;
            }
            else if (string.Equals(options.Mode, "fluence", StringComparison.Ordinal))
            {
                window.Content = root;
            }
            else
            {
                Grid shell = new();
                double nativeCaption = SystemParameters.WindowCaptionHeight + SystemParameters.ResizeFrameHorizontalBorderHeight;
                shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(Math.Max(0, 48 - nativeCaption)) });
                shell.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
                Grid.SetRow(root, 1);
                shell.Children.Add(root);
                window.Content = shell;
            }

            theme.Click += (_, _) => Run("NextTheme", () =>
            {
                options.Theme = string.Equals(options.Theme, "Light", StringComparison.Ordinal) ? "Dark" : "Light";
                if (string.Equals(options.Mode, "wpf-fluent", StringComparison.Ordinal))
                {
#if NET10_0_OR_GREATER
                    window.ThemeMode = string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? ThemeMode.Dark : ThemeMode.Light;
#endif
                }
                else
                {
                    ApplicationThemeManager.Apply(ToTheme(options.Theme), ToBackdrop(options.Backdrop));
                    TextElement.SetForeground(root, new SolidColorBrush(string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? Colors.White : Colors.Black));
                    if (string.Equals(options.Mode, "plain", StringComparison.Ordinal))
                    {
                        window.Background = new SolidColorBrush(FallbackColor(options.Theme));
                    }

                    if (string.Equals(options.Mode, "chrome-dwm", StringComparison.Ordinal))
                    {
                        probeTitle?.SetValue(TextBlock.ForegroundProperty, new SolidColorBrush(string.Equals(options.Theme, "Dark", StringComparison.Ordinal) ? Colors.White : Colors.Black));

                        ApplyDarkMode(new WindowInteropHelper(window).Handle, options);
                        UpdateChromeSurface(window, options, afterAction: true);
                    }
                }
            });

            backdrop.Click += (_, _) => Run("NextBackdrop", () =>
            {
                options.Backdrop = options.Backdrop switch { "Mica" => "Acrylic", "Acrylic" => "Tabbed", "Tabbed" => "None", _ => "Mica" };
                statusText.Text = "Mode: " + options.Mode + "  |  Requested backdrop: " + options.Backdrop;
                if (window is Fluence.Wpf.Controls.FluenceWindow fluence)
                {
                    fluence.SystemBackdropType = ToBackdrop(options.Backdrop);
                    ApplicationThemeManager.Apply(ToTheme(options.Theme), ToBackdrop(options.Backdrop));
                }
                else if (options.DwmAttribute)
                {
                    options.ProbeBackdropSetHResult = NativeSnapshot.SetBackdrop(new WindowInteropHelper(window).Handle, options.Backdrop);
                    options.ProbeBackdropSetStatus = "issued";
                    UpdateChromeSurface(window, options, afterAction: true);
                }
            });

            int rowNumber = 0;
            grow.Click += (_, _) => Run("GrowContent", () =>
            {
                rowNumber++;
                rows.Children.Add(new TextBlock { Text = "Added row " + rowNumber.ToString(System.Globalization.CultureInfo.InvariantCulture), Margin = new Thickness(0, 4, 0, 4) });
            });
            return window;
        }

        private static Button MakeButton(string label, string automationId)
        {
            Button button = new() { Content = label, Padding = new Thickness(10, 6, 10, 6), Margin = new Thickness(0, 0, 8, 0) };
            AutomationProperties.SetAutomationId(button, automationId);
            return button;
        }

        private static Color FallbackColor(string theme)
        {
            return string.Equals(theme, "Dark", StringComparison.Ordinal) ? Color.FromRgb(32, 32, 32) : Colors.White;
        }

        private static void UpdateChromeSurface(Window window, ProbeOptions options, bool afterAction)
        {
            bool materialRequested = !string.Equals(options.Backdrop, "None", StringComparison.Ordinal);
            bool materialAvailable = options.DwmAttribute && materialRequested && (options.ProbeBackdropSetHResult is null || options.ProbeBackdropSetHResult >= 0);
            Color fallback = FallbackColor(options.Theme);
            window.Background = new SolidColorBrush(materialAvailable ? Colors.Transparent : fallback);

            IntPtr hwnd = new WindowInteropHelper(window).Handle;
            if (hwnd == IntPtr.Zero)
            {
                return;
            }

            HwndSource? source = HwndSource.FromHwnd(hwnd);
            if (source?.CompositionTarget is null)
            {
                return;
            }

            if (!materialAvailable || options.ForceTargetOpaque)
            {
                source.CompositionTarget.BackgroundColor = fallback;
            }
            else if (options.ExplicitTargetClear || afterAction)
            {
                source.CompositionTarget.BackgroundColor = Colors.Transparent;
            }
        }

        private static void ApplyDarkMode(IntPtr hwnd, ProbeOptions options)
        {
            NativeSnapshot.SetDarkMode(hwnd, options.Theme, out int primaryHResult, out int? fallbackHResult);
            options.ProbeDarkModePrimaryHResult = primaryHResult;
            options.ProbeDarkModeFallbackHResult = fallbackHResult;
            options.ProbeDarkModeSetHResult = fallbackHResult ?? primaryHResult;
            options.ProbeDarkModeSetStatus = fallbackHResult is null ? "attribute 20 issued" : "attribute 20 failed; attribute 19 issued";
        }

        private static void Run(string name, Action action)
        {
            if (RecordAction is not null)
            {
                RecordAction(name, action);
            }
            else
            {
                action();
            }
        }

        private static ApplicationTheme ToTheme(string theme)
        {
            return string.Equals(theme, "Dark", StringComparison.Ordinal) ? ApplicationTheme.Dark : ApplicationTheme.Light;
        }

        private static WindowBackdropType ToBackdrop(string backdrop)
        {
            return backdrop switch
            {
                "Mica" => WindowBackdropType.Mica,
                "Acrylic" => WindowBackdropType.Acrylic,
                "Tabbed" => WindowBackdropType.Tabbed,
                _ => WindowBackdropType.None,
            };
        }
    }
}
