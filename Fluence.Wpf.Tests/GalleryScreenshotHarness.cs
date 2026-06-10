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
using System;
using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using DemoMainWindow = Fluence.Wpf.Demo.MainWindow;
using FluenceWindow = Fluence.Wpf.Controls.FluenceWindow;
#if NET10_0_OR_GREATER
using MvvmMainWindow = Fluence.Wpf.Demo.Mvvm.MainWindow;
using MvvmMainViewModel = Fluence.Wpf.Demo.Mvvm.ViewModels.MainViewModel;
#endif

namespace Fluence.Wpf.Tests
{
    /// <summary>
    /// Maintainer-driven, opt-in harness that renders the representative demo surfaces used in the
    /// documentation and writes PNGs under <c>docs/screenshots/</c>: the gallery shell in its three
    /// navigation modes (Home / Left, Buttons / LeftCompact, Status / Top), the MVVM task-manager
    /// app, and the PowerShell controls-tour window, each in Light and Dark.
    /// </summary>
    /// <remarks>
    /// Capture is gated behind the <c>FLUENCE_CAPTURE_SCREENSHOTS</c> environment variable, so a
    /// normal test run reports these tests as inconclusive and never overwrites the committed
    /// images; set the variable to <c>1</c> to regenerate them. Only the WPF visual tree is
    /// captured - DWM Mica / Acrylic backdrops are composited outside WPF and are not included in
    /// <see cref="RenderTargetBitmap"/> output, which is intended: the screenshots document control
    /// surfaces and theme resources, not DWM composition.
    /// </remarks>
    [TestClass]
    [TestCategory("Screenshots")]
    public class GalleryScreenshotHarness
    {
        private const string OptInEnvironmentVariable = "FLUENCE_CAPTURE_SCREENSHOTS";
        private const int GalleryCaptureWidth = 1280;
        private const int GalleryCaptureHeight = 900;
        private const int PowerShellCaptureWidth = 620;
        private const int PowerShellCaptureHeight = 560;
#if NET10_0_OR_GREATER
        private const int AppCaptureWidth = 960;
        private const int AppCaptureHeight = 740;
#endif
        private const double BaseDpi = 96.0;
        private const double ReferenceScale = 1.0;

        // Fluent transitions run ~100-167 ms; 150 ms is ample headroom for the storyboard to
        // settle before RenderTargetBitmap capture without padding the per-route cost.
        private static readonly TimeSpan AnimationSettleDelay = TimeSpan.FromMilliseconds(150);
        private static readonly Uri DemoSharedStylesUri = new(
            "/Fluence.Wpf.Demo;component/Resources/DemoSharedStyles.xaml",
            UriKind.Relative);

        private static readonly (ApplicationTheme theme, string slug)[] DocumentationThemes =
        [
            (ApplicationTheme.Light, "light"),
            (ApplicationTheme.Dark, "dark"),
        ];

        /// <summary>
        /// Skips the calling capture test unless <c>FLUENCE_CAPTURE_SCREENSHOTS</c> is set, so the
        /// screenshots are never regenerated during an ordinary test run.
        /// </summary>
        private static void RequireScreenshotOptIn()
        {
            string? flag = Environment.GetEnvironmentVariable(OptInEnvironmentVariable);
            bool enabled = string.Equals(flag, "1", StringComparison.Ordinal)
                || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
            if (!enabled)
            {
                Assert.Inconclusive(
                    "Screenshot capture is opt-in; set " + OptInEnvironmentVariable
                    + "=1 to regenerate docs/screenshots.");
            }
        }

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

        private static string EnsureOutputDirectory()
        {
            string path = Path.Combine(FindRepoRoot(), "docs", "screenshots");
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

        private static string Invariant(string format, params object[] args)
        {
            return string.Format(System.Globalization.CultureInfo.InvariantCulture, format, args);
        }

        /// <summary>
        /// Captures the gallery shell (<see cref="DemoMainWindow"/>) at <paramref name="route"/>
        /// with the navigation pane forced to <paramref name="paneMode"/>, writing
        /// <c>{outputName}-{themeSlug}.png</c>.
        /// </summary>
        private static void CaptureGalleryShellAt(
            ApplicationTheme theme,
            string themeSlug,
            string route,
            NavigationViewPaneDisplayMode paneMode,
            string outputName,
            string outputDirectory)
        {
            _ = ResetApplication(theme, true);

            DemoMainWindow? window = null;
            try
            {
                window = new DemoMainWindow();
                PrepareCaptureWindow(window, GalleryCaptureWidth, GalleryCaptureHeight);
                window.Show();
                DrainDispatcher(window.Dispatcher);

                if (window.DemoNav is not null)
                {
                    window.DemoNav.PaneDisplayMode = paneMode;
                    window.DemoNav.IsPaneOpen = paneMode == NavigationViewPaneDisplayMode.Left;
                }

                window.NavigateTo(route);
                ApplicationThemeManager.Apply(theme, BackdropType.None, true);
                DrainDispatcher(window.Dispatcher);
                PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
                window.UpdateLayout();
                _ = window.Dispatcher.Invoke(DispatcherPriority.Render, new Action(delegate { }));

                string fullPath = Path.Combine(outputDirectory, Invariant("{0}-{1}.png", outputName, themeSlug));
                SaveElementPng(window, ReferenceScale, fullPath);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, BackdropType.None, true);
            }
        }

        /// <summary>
        /// Reads the inline XAML here-string from <c>03-ControlsTour.ps1</c> so the captured window
        /// stays in lock-step with the script the screenshot documents.
        /// </summary>
        private static string ExtractControlsTourXaml()
        {
            string scriptPath = Path.Combine(FindRepoRoot(), "Fluence.Wpf.Demo.PowerShell", "03-ControlsTour.ps1");
            string[] lines = File.ReadAllLines(scriptPath);

            int start = -1;
            int end = -1;
            for (int i = 0; i < lines.Length; i++)
            {
                if (start < 0)
                {
                    if (lines[i].TrimEnd().EndsWith("@'", StringComparison.Ordinal))
                    {
                        start = i + 1;
                    }

                    continue;
                }

                if (lines[i].TrimStart().StartsWith("'@", StringComparison.Ordinal))
                {
                    end = i;
                    break;
                }
            }

            if (start < 0 || end < 0)
            {
                throw new InvalidOperationException(
                    "Could not locate the XAML here-string in 03-ControlsTour.ps1.");
            }

            StringBuilder builder = new();
            for (int i = start; i < end; i++)
            {
                if (builder.Length > 0)
                {
                    _ = builder.Append('\n');
                }

                _ = builder.Append(lines[i]);
            }

            return builder.ToString();
        }

        private static void CapturePowerShellControlsAt(ApplicationTheme theme, string themeSlug, string outputDirectory)
        {
            _ = ResetApplication(theme, false);

            Window? window = null;
            try
            {
                window = XamlReader.Parse(ExtractControlsTourXaml()) as Window
                    ?? throw new InvalidOperationException("03-ControlsTour.ps1 XAML did not load as a WPF Window.");

                PrepareCaptureWindow(window, PowerShellCaptureWidth, PowerShellCaptureHeight);
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
        public void CaptureGalleryShellNavigationModes()
        {
            RequireScreenshotOptIn();
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    CaptureGalleryShellAt(theme, themeSlug, "home", NavigationViewPaneDisplayMode.Left, "gallery-home", output);
                    CaptureGalleryShellAt(theme, themeSlug, "buttons", NavigationViewPaneDisplayMode.LeftCompact, "gallery-buttons", output);
                    CaptureGalleryShellAt(theme, themeSlug, "status", NavigationViewPaneDisplayMode.Top, "gallery-status", output);
                }
            });
        }

        [TestMethod]
        public void CapturePowerShellControlsTour()
        {
            RequireScreenshotOptIn();
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    CapturePowerShellControlsAt(theme, themeSlug, output);
                }
            });
        }

#if NET10_0_OR_GREATER
        [TestMethod]
        public void CaptureMvvmTaskManager()
        {
            RequireScreenshotOptIn();
            RunOnStaThread(() =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    CaptureMvvmDemoAt(theme, themeSlug, output);
                }
            });
        }
#endif
    }
}
