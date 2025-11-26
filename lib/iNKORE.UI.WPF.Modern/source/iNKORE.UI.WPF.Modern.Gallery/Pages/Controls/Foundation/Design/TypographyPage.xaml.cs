// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Windows;
using System.Windows.Navigation;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern.Gallery.Helpers;
using iNKORE.UI.WPF.Modern;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Foundation
{
    /// <summary>
    /// Typography page showcasing Windows typography styles and system fonts.
    /// </summary>
    public sealed partial class TypographyPage : Page
    {
        private DispatcherTimer _themeMonitorTimer;
        private ElementTheme _lastKnownTheme = ElementTheme.Default;

        public TypographyPage()
        {
            this.InitializeComponent();
            Loaded += TypographyPage_Loaded;
            
            ThemeManager.Current.ActualApplicationThemeChanged += OnThemeChanged;
            ThemeManager.AddActualThemeChangedHandler(this, OnElementThemeChanged);
            
            DependencyPropertyDescriptor.FromProperty(ThemeManager.RequestedThemeProperty, typeof(FrameworkElement))
                ?.AddValueChanged(this, OnRequestedThemeChanged);
            
            _themeMonitorTimer = new DispatcherTimer 
            {
                Interval = TimeSpan.FromMilliseconds(200)
            };
            _themeMonitorTimer.Tick += ThemeMonitorTimer_Tick;
            _themeMonitorTimer.Start();
        }

        private void TypographyPage_Loaded(object sender, RoutedEventArgs e)
        {
            if (NavigationRootPage.Current?.NavigationView != null)
            {
                NavigationRootPage.Current.NavigationView.Header = "Typography";
            }
            
            UpdateTypographyImage();
            
            UpdateExampleCode();
        }

        //FAILED TRIALS but keeping for reference - The image should switch when toggle theme clicked
        private void UpdateTypographyImage()
        {
            if (TypographyHeaderImage == null) return;

            // Multi-level theme detection to catch both application and element-level changes
            var pageTheme = ThemeManager.GetActualTheme(this);
            var parentTheme = ElementTheme.Default;
            var controlExampleTheme = ElementTheme.Default;
            
            // Check parent elements for theme overrides (catches toggle theme changes)
            var parentElement = this.Parent as FrameworkElement;
            while (parentElement != null)
            {
                var currentParentTheme = ThemeManager.GetActualTheme(parentElement);
                if (currentParentTheme != ElementTheme.Default)
                {
                    parentTheme = currentParentTheme;
                    break;
                }
                parentElement = parentElement.Parent as FrameworkElement;
            }
            
            // Check ControlExample elements for theme changes (this is where toggle theme applies changes)
            if (Example1 != null)
            {
                try
                {
                    var exampleTheme = ThemeManager.GetActualTheme(Example1);
                    if (exampleTheme != ElementTheme.Default)
                    {
                        controlExampleTheme = exampleTheme;
                    }
                    else if (Example1.ExampleContainer != null)
                    {
                        var containerTheme = ThemeManager.GetActualTheme(Example1.ExampleContainer);
                        if (containerTheme != ElementTheme.Default)
                        {
                            controlExampleTheme = containerTheme;
                        }
                    }
                }
                catch (Exception ex)
                {
                    // Handle potential issues with theme detection during control initialization
                    System.Diagnostics.Debug.WriteLine($"Theme detection error: {ex.Message}");
                }
            }
            
            // Use the most specific theme available (ControlExample > Page > Parent > Application)
            var effectiveTheme = controlExampleTheme != ElementTheme.Default ? controlExampleTheme :
                                 pageTheme != ElementTheme.Default ? pageTheme : parentTheme;
            var isDarkTheme = effectiveTheme == ElementTheme.Dark || 
                             (effectiveTheme == ElementTheme.Default && ThemeHelper.IsDarkTheme());
            
            var imageName = isDarkTheme ? "Typography.dark.png" : "Typography.light.png";
            var uri = new System.Uri($"pack://application:,,,/iNKORE.UI.WPF.Modern.Gallery;component/Assets/Design/{imageName}");
            
            try
            {
                // Force refresh the BitmapImage to ensure it updates
                var bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.UriSource = uri;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.CreateOptions = BitmapCreateOptions.IgnoreImageCache;
                bitmapImage.EndInit();
                bitmapImage.Freeze();
                TypographyHeaderImage.Source = bitmapImage;
                
                System.Diagnostics.Debug.WriteLine($"Typography image updated to: {imageName} (Page: {pageTheme}, Parent: {parentTheme}, ControlExample: {controlExampleTheme}, Effective: {effectiveTheme}, IsDark: {isDarkTheme})");
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Failed to load typography image: {ex.Message}");
                // Fallback to dark image if there's an issue
                var fallbackUri = new System.Uri("pack://application:,,,/iNKORE.UI.WPF.Modern.Gallery;component/Assets/Design/Typography.dark.png");
                TypographyHeaderImage.Source = new BitmapImage(fallbackUri);
            }
        }

        private void OnThemeChanged(ThemeManager sender, object args)
        {
            // Update the image when theme changes
            UpdateTypographyImage();
        }

        private void OnElementThemeChanged(object sender, RoutedEventArgs e)
        {
            // Update the image when element theme changes (for theme toggle)
            UpdateTypographyImage();
        }

        private void ThemeMonitorTimer_Tick(object sender, EventArgs e)
        {
            // Check if the theme has changed by monitoring our current actual theme
            var currentTheme = ThemeManager.GetActualTheme(this);
            if (currentTheme != _lastKnownTheme)
            {
                _lastKnownTheme = currentTheme;
                UpdateTypographyImage();
                System.Diagnostics.Debug.WriteLine($"Theme change detected: {currentTheme}");
            }
            
            // Also check for element-level theme changes by examining parent elements
            // This catches toggle theme changes that affect control examples
            var parentElement = this.Parent as FrameworkElement;
            while (parentElement != null)
            {
                var parentTheme = ThemeManager.GetActualTheme(parentElement);
                if (parentTheme != currentTheme)
                {
                    // Found a parent with different theme - this indicates element-level theme change
                    UpdateTypographyImage();
                    System.Diagnostics.Debug.WriteLine($"Element-level theme change detected: Parent={parentTheme}, Current={currentTheme}");
                    break;
                }
                parentElement = parentElement.Parent as FrameworkElement;
            }
            
            if (Example1 != null)
            {
                try
                {
                    var controlExampleTheme = ThemeManager.GetActualTheme(Example1);
                    var containerTheme = ElementTheme.Default;
                    
                    if (Example1.ExampleContainer != null)
                    {
                        containerTheme = ThemeManager.GetActualTheme(Example1.ExampleContainer);
                    }
                    
                    if (controlExampleTheme != currentTheme || containerTheme != currentTheme)
                    {
                        UpdateTypographyImage();
                        System.Diagnostics.Debug.WriteLine($"ControlExample theme change detected: ControlExample={controlExampleTheme}, Container={containerTheme}, Page={currentTheme}");
                    }
                }
                catch (Exception ex)
                {
                    // Handle potential issues with theme detection during control initialization
                    System.Diagnostics.Debug.WriteLine($"Theme monitor error: {ex.Message}");
                }
            }
        }

        private void OnRequestedThemeChanged(object sender, EventArgs e)
        {
            Dispatcher.BeginInvoke(new System.Action(() => {
                UpdateTypographyImage();
            }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            
            var timer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(100) };
            timer.Tick += (s, args) => {
                timer.Stop();
                UpdateTypographyImage();
            };
            timer.Start();
        }

        private void Hyperlink_RequestNavigate(object sender, RequestNavigateEventArgs e)
        {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri) { UseShellExecute = true });
            e.Handled = true;
        }

        // Button click handlers for typography info buttons
        private void ShowTypographyButtonClick1(object sender, RoutedEventArgs e)
        {
            // Caption button clicked - could show teaching tip
            // TeachingTip not available yet, so using simple message for now
            System.Windows.MessageBox.Show("Caption: Small, Regular - 12/16 epx\nStyle: CaptionTextBlockStyle", "Typography Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ShowTypographyButtonClick2(object sender, RoutedEventArgs e)
        {
            // Body button clicked
            System.Windows.MessageBox.Show("Body: Text, Regular - 14/20 epx\nStyle: BodyTextBlockStyle", "Typography Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ShowTypographyButtonClick3(object sender, RoutedEventArgs e)
        {
            // Body Strong button clicked
            System.Windows.MessageBox.Show("Body Strong: Text, SemiBold - 14/20 epx\nStyle: BodyStrongTextBlockStyle", "Typography Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ShowTypographyButtonClick4(object sender, RoutedEventArgs e)
        {
            // Title button clicked
            System.Windows.MessageBox.Show("Title: Display, SemiBold - 28/36 epx\nStyle: TitleTextBlockStyle", "Typography Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void ShowTypographyButtonClick5(object sender, RoutedEventArgs e)
        {
            // Display button clicked
            System.Windows.MessageBox.Show("Display: Display, SemiBold - 68/92 epx\nStyle: DisplayTextBlockStyle", "Typography Info", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Information);
        }

        private void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        string Example1Xaml => @"
<TextBlock Text=""Caption"" Style=""{StaticResource {x:Static ui:ThemeKeys.CaptionTextBlockStyleKey}}""/>
<TextBlock Text=""Body"" Style=""{StaticResource {x:Static ui:ThemeKeys.BodyTextBlockStyleKey}}""/>
<TextBlock Text=""Body Strong"" Style=""{StaticResource {x:Static ui:ThemeKeys.BodyStrongTextBlockStyleKey}}""/>
<TextBlock Text=""Subtitle"" Style=""{StaticResource {x:Static ui:ThemeKeys.SubtitleTextBlockStyleKey}}""/>
<TextBlock Text=""Title"" Style=""{StaticResource {x:Static ui:ThemeKeys.TitleTextBlockStyleKey}}""/>
<TextBlock Text=""Title Large"" Style=""{StaticResource {x:Static ui:ThemeKeys.TitleLargeTextBlockStyleKey}}""/>
<TextBlock Text=""Display"" Style=""{StaticResource {x:Static ui:ThemeKeys.DisplayTextBlockStyleKey}}""/>
";
    }
}
