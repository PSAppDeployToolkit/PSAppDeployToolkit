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

using Fluence.Wpf.Demo.Pages;
using Fluence.Wpf.Demo;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using System.Xml;
using DemoMainWindow = Fluence.Wpf.Demo.MainWindow;
using FluenceWindow = Fluence.Wpf.Controls.FluenceWindow;
#if NET10_0_OR_GREATER
using MvvmMainWindow = Fluence.Wpf.Demo.Mvvm.MainWindow;
using MvvmMainViewModel = Fluence.Wpf.Demo.Mvvm.ViewModels.MainViewModel;
#endif

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Maintainer-driven harness that renders representative demo surfaces with
    /// <see cref="RenderTargetBitmap"/> and writes PNGs under
    /// <c>docs/screenshots/</c>. The screenshot capture runs as part of the normal
    /// full test suite so documentation images stay current with visual changes.
    /// </summary>
    /// <remarks>
    /// Captures only the WPF visual tree - DWM Mica / Acrylic backdrops are composited
    /// outside WPF and are not included in RenderTargetBitmap output. That is the
    /// intended behavior: the screenshots document control surfaces and theme resources,
    /// not DWM composition.
    /// </remarks>
    [TestClass]
    [TestCategory("Screenshots")]
    public class GalleryScreenshotHarness
    {
        private const int CaptureWidth = 1280;
        private const int CaptureHeight = 800;
        private const int GalleryCaptureWidth = 1280;
        private const int GalleryCaptureHeight = 900;
        private const int AppCaptureWidth = 960;
        private const int AppCaptureHeight = 740;
        private const double BaseDpi = 96.0;
        private const double ReferenceScale = 1.0;

        // Fluent transitions run ~100-167 ms; 150 ms is ample headroom for the storyboard to
        // settle before RenderTargetBitmap capture without padding the per-route cost.
        private static readonly TimeSpan AnimationSettleDelay = TimeSpan.FromMilliseconds(150);
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        private static readonly double[] BannerScales = [1.0, 1.5];

        private static readonly (ApplicationTheme theme, string slug)[] Themes =
        [
            (ApplicationTheme.Light, "light"),
            (ApplicationTheme.Dark, "dark"),
            (ApplicationTheme.HighContrast, "highcontrast"),
        ];

        private static readonly (ApplicationTheme theme, string slug)[] DocumentationThemes =
        [
            (ApplicationTheme.Light, "light"),
            (ApplicationTheme.Dark, "dark"),
        ];

        private static void RunOnStaThread(Action action)
        {
            WpfTestSta.RunOnSta(action);
        }

        private static Application? EnsureApplication()
        {
            return WpfTestSta.EnsureApplication();
        }

        private static void DrainDispatcher(Dispatcher dispatcher)
        {
            WpfTestSta.DrainDispatcher(dispatcher);
        }

        private static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Combine(directory.FullName, "Fluence.Wpf.sln")))
                {
                    return directory.FullName;
                }

                directory = directory.Parent;
            }

            throw new InvalidOperationException(
                "Could not locate Fluence.Wpf.sln ancestor directory from " + AppContext.BaseDirectory);
        }

        private static string EnsureOutputDirectory(params string[] segments)
        {
            string root = FindRepoRoot();
            string path = Path.Combine(root, "docs", "screenshots");
            for (int i = 0; i < segments.Length; i++)
            {
                path = Path.Combine(path, segments[i]);
            }

            _ = Directory.CreateDirectory(path);
            return path;
        }

        private static void SavePng(Visual visual, int pixelWidth, int pixelHeight, double dpi, string fullPath)
        {
            RenderTargetBitmap bitmap = new(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            using FileStream stream = File.Create(fullPath);
            encoder.Save(stream);
        }

        private static void SaveFlattenedPng(FrameworkElement element, int pixelWidth, int pixelHeight, double dpi, string fullPath)
        {
            Rect bounds = new(0.0, 0.0, element.ActualWidth, element.ActualHeight);
            Brush background = element.TryFindResource("SolidBackgroundFillColorBaseBrush") as Brush
                ?? element.TryFindResource("ApplicationBackgroundBrush") as Brush
                ?? Brushes.Transparent;

            DrawingVisual flattened = new();
            using (DrawingContext context = flattened.RenderOpen())
            {
                context.DrawRectangle(background, null, bounds);
                context.DrawRectangle(new VisualBrush(element), null, bounds);
            }

            SavePng(flattened, pixelWidth, pixelHeight, dpi, fullPath);
        }

        private static void SaveElementPng(FrameworkElement element, double scale, string fullPath)
        {
            int pixelWidth = Math.Max(1, (int)Math.Round(element.ActualWidth * scale));
            int pixelHeight = Math.Max(1, (int)Math.Round(element.ActualHeight * scale));
            double dpi = BaseDpi * scale;

            SaveFlattenedPng(element, pixelWidth, pixelHeight, dpi, fullPath);
            Assert.IsTrue(File.Exists(fullPath), Invariant("Expected to write {0}", fullPath));
        }

        private static Application ResetApplication(ApplicationTheme theme, bool includeDemoSharedStyles)
        {
            Application application = EnsureApplication() ?? throw new InvalidOperationException("Could not create WPF Application for screenshot capture.");
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
            if (includeDemoSharedStyles)
            {
                application.Resources.MergedDictionaries.Add(new ResourceDictionary { Source = DemoSharedStylesUri });
            }

            return application;
        }

        private static void PrepareCaptureWindow(Window window, int width, int height)
        {
            window.Width = width;
            window.Height = height;
            window.WindowStartupLocation = WindowStartupLocation.Manual;
            window.Top = -10000;
            window.Left = -10000;
            window.ShowInTaskbar = false;
            window.ResizeMode = ResizeMode.NoResize;
            window.SizeToContent = SizeToContent.Manual;
            window.SetResourceReference(Control.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");

            if (window is FluenceWindow fluenceWindow)
            {
                fluenceWindow.SystemBackdropType = BackdropType.None;
            }
        }

        private static void ShowSettleAndCapture(Window window, ApplicationTheme theme, string fullPath)
        {
            window.Show();
            DrainDispatcher(window.Dispatcher);
            ApplicationThemeManager.Apply(theme, BackdropType.None, true);
            DrainDispatcher(window.Dispatcher);
            PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
            window.UpdateLayout();
            _ = window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));

            SaveElementPng(window, ReferenceScale, fullPath);
        }

        private static void PumpDispatcher(Dispatcher dispatcher, TimeSpan duration)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(DispatcherPriority.Background, dispatcher)
            {
                Interval = duration
            };
            timer.Tick += delegate
            {
                timer.Stop();
                frame.Continue = false;
            };
            timer.Start();
            Dispatcher.PushFrame(frame);
        }

        private static void CaptureHomeAt(ApplicationTheme theme, string themeSlug, double scale, string outputDirectory)
        {
            _ = ResetApplication(theme, true);

            Window? window = null;
            try
            {
                Border host = new()
                {
                    Width = CaptureWidth,
                    Height = CaptureHeight,
                };
                host.SetResourceReference(Border.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");

                GalleryHomePage page = new();
                host.Child = page;

                // Plain Window avoids the FluenceWindow DWM backdrop (which RenderTargetBitmap
                // can't capture) and keeps the capture focused on the control surface.
                window = new Window
                {
                    SizeToContent = SizeToContent.WidthAndHeight,
                    WindowStartupLocation = WindowStartupLocation.Manual,
                    WindowStyle = WindowStyle.None,
                    ShowInTaskbar = false,
                    Top = -10000,
                    Left = -10000,
                    AllowsTransparency = false,
                    Content = host,
                };

                window.Show();
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();

                // Re-fire the theme apply *after* the page has subscribed in its Loaded
                // handler. This guarantees the banner image (and any Changed-driven state)
                // reflects the requested theme.
                ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                DrainDispatcher(window.Dispatcher);
                window.UpdateLayout();
                _ = window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));

                string slug = Invariant("banner-{0}-{1}x.png", themeSlug, scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture));
                string fullPath = Path.Combine(outputDirectory, slug);

                SaveElementPng(host, scale, fullPath);
            }
            finally
            {
                window?.Close();

                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }

        private static string Invariant(string format, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
        }

        private static string ToFileSlug(string value)
        {
            StringBuilder builder = new();
            bool previousHyphen = false;
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (char.IsLetterOrDigit(character))
                {
                    _ = builder.Append(char.ToLowerInvariant(character));
                    previousHyphen = false;
                    continue;
                }

                if (!previousHyphen && builder.Length > 0)
                {
                    _ = builder.Append('-');
                    previousHyphen = true;
                }
            }

            string slug = builder.ToString().Trim('-');
            return string.IsNullOrWhiteSpace(slug) ? "capture" : slug;
        }

        private static int CountGalleryRoutesWithSettings()
        {
            int count = 1;
            foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
            {
                count++;
            }

            return count;
        }

        private static void CaptureGalleryShellAt(ApplicationTheme theme, string themeSlug, string route, string outputDirectory)
        {
            _ = ResetApplication(theme, true);

            DemoMainWindow? window = null;
            try
            {
                window = new DemoMainWindow();
                PrepareCaptureWindow(window, GalleryCaptureWidth, GalleryCaptureHeight);
                window.Show();
                DrainDispatcher(window.Dispatcher);

                window.NavigateTo(route);
                ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                DrainDispatcher(window.Dispatcher);
                PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
                window.UpdateLayout();
                _ = window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));

                string slug = Invariant("gallery-{0}-{1}.png", ToFileSlug(route), themeSlug);
                string fullPath = Path.Combine(outputDirectory, slug);
                SaveElementPng(window, ReferenceScale, fullPath);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }

        private static void CapturePowerShellDemoAt(ApplicationTheme theme, string themeSlug, string outputDirectory)
        {
            _ = ResetApplication(theme, false);

            Window? window = null;
            try
            {
                string xamlPath = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell", "MainWindow.xaml");
                using XmlReader reader = XmlReader.Create(xamlPath);
                window = XamlReader.Load(reader) as Window
                    ?? throw new InvalidOperationException("PowerShell MainWindow.xaml did not load as a WPF Window.");

                PrepareCaptureWindow(window, AppCaptureWidth, AppCaptureHeight);
                string fullPath = Path.Combine(outputDirectory, Invariant("powershell-{0}.png", themeSlug));
                ShowSettleAndCapture(window, theme, fullPath);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }

#if NET10_0_OR_GREATER
        private static void AddScreenshotTask(MvvmMainViewModel viewModel, string title, bool isCompleted)
        {
            viewModel.NewTaskText = title;
            if (viewModel.AddCommand.CanExecute(null))
            {
                viewModel.AddCommand.Execute(null);
            }

            if (isCompleted && viewModel.DisplayedTasks.Count > 0)
            {
                viewModel.DisplayedTasks[viewModel.DisplayedTasks.Count - 1].IsCompleted = true;
            }
        }

        private static void SeedMvvmScreenshotData(MvvmMainWindow window)
        {
            if (window.DataContext is not MvvmMainViewModel viewModel)
            {
                return;
            }

            AddScreenshotTask(viewModel, "Review theme dictionary slots", true);
            AddScreenshotTask(viewModel, "Polish NavigationView samples", false);
            AddScreenshotTask(viewModel, "Capture release screenshots", false);
            AddScreenshotTask(viewModel, "Update API docs", true);
        }

        private static void CaptureMvvmDemoAt(ApplicationTheme theme, string themeSlug, string outputDirectory)
        {
            _ = ResetApplication(theme, false);

            MvvmMainWindow? window = null;
            try
            {
                window = new MvvmMainWindow();
                SeedMvvmScreenshotData(window);
                PrepareCaptureWindow(window, AppCaptureWidth, AppCaptureHeight);
                string fullPath = Path.Combine(outputDirectory, Invariant("mvvm-{0}.png", themeSlug));
                ShowSettleAndCapture(window, theme, fullPath);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }
#endif

        [TestMethod]
        public void CaptureBannerAcrossThemesAndScales()
        {
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory();
                List<string> written = [];
                foreach ((ApplicationTheme theme, string? slug) in Themes)
                {
                    foreach (double scale in BannerScales)
                    {
                        CaptureHomeAt(theme, slug, scale, output);
                        written.Add(Invariant("banner-{0}-{1}x.png", slug, scale.ToString("0.##", System.Globalization.CultureInfo.InvariantCulture)));
                    }
                }

                Assert.AreEqual(Themes.Length * BannerScales.Length, written.Count);
            });
        }

        [TestMethod]
        public void CaptureGalleryPagesAcrossLightAndDarkThemes()
        {
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory("gallery");
                int written = 0;
                foreach ((ApplicationTheme theme, string? themeSlug) in DocumentationThemes)
                {
                    foreach (DemoNavigationItem item in DemoNavigationCatalog.Items)
                    {
                        CaptureGalleryShellAt(theme, themeSlug, item.Route, output);
                        written++;
                    }

                    CaptureGalleryShellAt(theme, themeSlug, "settings", output);
                    written++;
                }

                Assert.AreEqual(CountGalleryRoutesWithSettings() * DocumentationThemes.Length, written);
            });
        }

        [TestMethod]
        public void CaptureSecondaryDemoSurfacesAcrossLightAndDarkThemes()
        {
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory("apps");
                int written = 0;
                foreach ((ApplicationTheme theme, string? themeSlug) in DocumentationThemes)
                {
                    CapturePowerShellDemoAt(theme, themeSlug, output);
                    written++;

#if NET10_0_OR_GREATER
                    CaptureMvvmDemoAt(theme, themeSlug, output);
                    written++;
#endif
                }

#if NET10_0_OR_GREATER
                Assert.AreEqual(DocumentationThemes.Length * 2, written);
#else
                Assert.AreEqual(DocumentationThemes.Length, written);
#endif
            });
        }
    }
}
