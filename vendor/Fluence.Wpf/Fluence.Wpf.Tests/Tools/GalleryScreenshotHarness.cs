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
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Markup;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;
using Fluence.Wpf.Tests.Infrastructure;
using Xunit;

namespace Fluence.Wpf.Tests.Tools
{
    /// <summary>
    /// Maintainer-driven, opt-in harness that renders the representative demo surfaces used in the
    /// documentation and writes PNGs under <c language="text">docs/screenshots/</c>: the gallery shell in its three
    /// navigation modes (Home / Left, Buttons / LeftCompact, Status / Top), the MVVM task-manager
    /// app, and the PowerShell controls-tour window, each in Light and Dark.
    /// </summary>
    /// <remarks>
    /// Capture is gated behind the <c language="text">FLUENCE_CAPTURE_SCREENSHOTS</c> environment variable, so a
    /// normal test run reports these tests as inconclusive and never overwrites the committed
    /// images; set the variable to <c language="text">1</c> to regenerate them. Set
    /// <c language="text">FLUENCE_CAPTURE_OUTPUT_DIRECTORY</c> to render into a separate directory.
    /// Only the WPF visual tree is
    /// captured. DWM Mica / Acrylic backdrops and transparent window shadows are composited
    /// outside WPF and are not included in <see cref="RenderTargetBitmap"/> output. The harness
    /// does not simulate them. These screenshots document control surfaces and theme resources.
    /// </remarks>
    [Trait("Category", "Screenshots")]
    public class GalleryScreenshotHarness
    {
        private const string OptInEnvironmentVariable = "FLUENCE_CAPTURE_SCREENSHOTS";
        private const string ControlsOnlyEnvironmentVariable = "FLUENCE_CAPTURE_CONTROLS_ONLY";
        private const string OutputDirectoryEnvironmentVariable = "FLUENCE_CAPTURE_OUTPUT_DIRECTORY";
        private const int GalleryCaptureWidth = 1280;
        private const int GalleryCaptureHeight = 900;
        private const double CardCaptureScale = 0.8;
        private const int PowerShellCaptureWidth = 620;
        private const int PowerShellCaptureHeight = 640;
#if NET10_0_OR_GREATER
        private const int AppCaptureWidth = 960;
        private const int AppCaptureHeight = 740;
#endif
        private const double BaseDpi = 96.0;
        private const double ReferenceScale = 1.0;
        private const int ControlHorizontalPadding = 32;
        private const int ControlVerticalPadding = 64;

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

        // The page slug is the documentation filename. A null type selects the complete live
        // DemoContent group when that group is itself the control example (such as a button set).
        private sealed class ControlCaptureSpec(string slug, string route, int ordinal, string? typeName = null)
        {
            public string Slug { get; } = slug;
            public string Route { get; } = route;
            public int Ordinal { get; } = ordinal;
            public string? TypeName { get; } = typeName;
        }

        private static readonly ControlCaptureSpec[] ControlCaptureSpecs =
        [
            new("auto-suggest-box", "inputs", 3, "AutoSuggestBox"),
            new("border", "layout", 1, "Border"),
            new("breadcrumb-bar-item", "navigation", 5, "BreadcrumbBarItem"),
            new("breadcrumb-bar", "navigation", 5, "BreadcrumbBar"),
            new("button", "buttons", 3, "Button"),
            new("card", "data", 5, "Card"),
            new("check-box", "selection", 1, "CheckBox"),
            new("color-picker", "forms", 3, "ColorPicker"),
            new("combo-box", "selection", 5, "ComboBox"),
            new("date-picker", "forms", 2, "DatePicker"),
            new("dock-panel", "layout", 2, "DockPanel"),
            new("drop-down-button", "buttons", 6, "DropDownButton"),
            new("expander", "layout", 3, "Expander"),
            new("hyperlink-button", "buttons", 5, "HyperlinkButton"),
            new("image", "data", 6, "Image"),
            new("info-badge", "navigation", 4, "InfoBadge"),
            new("info-bar", "status", 5, "InfoBar"),
            new("list-box-item", "data", 3, "ListBoxItem"),
            new("list-box", "data", 3, "ListBox"),
            new("list-view", "data", 7, "ListView"),
            new("menu-item", "menus", 1, "MenuItem"),
            new("menu", "menus", 1, "Menu"),
            new("navigation-view-item-header", "navigation", 1, "NavigationViewItemHeader"),
            new("navigation-view-item-separator", "navigation", 1, "NavigationViewItemSeparator"),
            new("navigation-view-item", "navigation", 4, "NavigationViewItem"),
            new("navigation-view", "navigation", 1, "NavigationView"),
            new("number-box", "inputs", 5, "NumberBox"),
            new("password-box-extensions", "inputs", 4, "PasswordBox"),
            new("person-picture", "data", 4, "PersonPicture"),
            new("pips-pager", "navigation", 6, "PipsPager"),
            new("progress-bar", "status", 1, "ProgressBar"),
            new("progress-ring", "status", 4, "ProgressRing"),
            new("radio-button", "selection", 2, "RadioButton"),
            new("rating-control", "selection", 4, "RatingControl"),
            new("repeat-button", "buttons", 8, "RepeatButton"),
            new("scroll-bar-extensions", "layout", 5, "ScrollBar"),
            new("selector-bar-item", "navigation", 7, "SelectorBarItem"),
            new("selector-bar", "navigation", 7, "SelectorBar"),
            new("separator", "layout", 1, "Separator"),
            new("slide-navigation-presenter", "navigation", 7, "SlideNavigationPresenter"),
            new("slider", "inputs", 6, "Slider"),
            new("smooth-scroll-viewer", "layout", 5, "SmoothScrollViewer"),
            new("split-button", "buttons", 7, "SplitButton"),
            new("stack-panel", "layout", 1, "StackPanel"),
            new("tab-view-item", "tabs", 3, "TabViewItem"),
            new("tab-view", "tabs", 3, "TabView"),
            new("text-block-extensions", "typography", 1, "TextBlock"),
            new("text-block", "typography", 1, "TextBlock"),
            new("text-box", "inputs", 2, "TextBox"),
            new("time-picker", "forms", 1, "TimePicker"),
            new("toggle-button", "buttons", 9, "ToggleButton"),
            new("toggle-split-button", "buttons", 10, "ToggleSplitButton"),
            new("toggle-switch", "selection", 3, "ToggleSwitch"),
            new("tree-view-item", "trees", 1, "TreeViewItem"),
            new("tree-view", "trees", 1, "TreeView"),
        ];

        /// <summary>
        /// Declarative skip condition: capture tests run only when
        /// <c language="text">FLUENCE_CAPTURE_SCREENSHOTS</c> is set, so the screenshots are never regenerated
        /// during an ordinary test run.
        /// </summary>
        public static bool ScreenshotCaptureEnabled
        {
            get
            {
                string? flag = Environment.GetEnvironmentVariable(OptInEnvironmentVariable);
                return string.Equals(flag, "1", StringComparison.Ordinal)
                    || string.Equals(flag, "true", StringComparison.OrdinalIgnoreCase);
            }
        }

        private static bool ControlsOnlyCaptureEnabled =>
            string.Equals(Environment.GetEnvironmentVariable(ControlsOnlyEnvironmentVariable), "1", StringComparison.Ordinal);

        private static string FindRepoRoot()
        {
            DirectoryInfo? directory = new(AppContext.BaseDirectory);
            while (directory is not null)
            {
                if (File.Exists(Path.Join(directory.FullName, "Fluence.Wpf.sln")))
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
            string? outputOverride = Environment.GetEnvironmentVariable(OutputDirectoryEnvironmentVariable);
            string path = string.IsNullOrWhiteSpace(outputOverride)
                ? Path.Join(FindRepoRoot(), "docs", "screenshots")
                : Path.GetFullPath(outputOverride);
            _ = Directory.CreateDirectory(path);
            return path;
        }

        private static string EnsureGalleryOutputDirectory()
        {
            string path = Path.Join(EnsureOutputDirectory(), "gallery");
            _ = Directory.CreateDirectory(path);
            return path;
        }

        private static string EnsureControlOutputDirectory()
        {
            string path = Path.Join(EnsureOutputDirectory(), "controls");
            _ = Directory.CreateDirectory(path);
            return path;
        }

        private sealed class CaptureEntry(string kind, string route, string title, string theme, string state, string file)
        {
            public string Kind { get; } = kind;
            public string Route { get; } = route;
            public string Title { get; } = title;
            public string Theme { get; } = theme;
            public string State { get; } = state;
            public string File { get; } = file;
        }

        private static string JsonString(string value)
        {
            StringBuilder result = new("\"");
            foreach (char character in value)
            {
                string escaped = character switch
                {
                    '\\' => "\\\\",
                    '"' => "\\\"",
                    '\n' => "\\n",
                    '\r' => "\\r",
                    '\t' => "\\t",
                    _ when character < ' ' => Invariant("\\u{0:X4}", (int)character),
                    _ => character.ToString(),
                };
                _ = result.Append(escaped);
            }

            return result.Append('"').ToString();
        }

        private static async Task WriteGalleryManifestAsync(string directory, IList<CaptureEntry> entries)
        {
            StringBuilder json = new();
            _ = json.Append("{\n  \"captureMethod\": \"WPF RenderTargetBitmap\",\n");
            _ = json.Append("  \"limitation\": \"Offscreen WPF captures omit DWM window shadow and Mica or Acrylic backdrop; no shadow has been simulated.\",\n");
            _ = json.Append("  \"entries\": [\n");
            for (int index = 0; index < entries.Count; index++)
            {
                CaptureEntry entry = entries[index];
                _ = json.Append("    {\"kind\": ").Append(JsonString(entry.Kind))
                    .Append(", \"route\": ").Append(JsonString(entry.Route))
                    .Append(", \"title\": ").Append(JsonString(entry.Title))
                    .Append(", \"theme\": ").Append(JsonString(entry.Theme))
                    .Append(", \"state\": ").Append(JsonString(entry.State))
                    .Append(", \"file\": ").Append(JsonString(entry.File)).Append('}');
                _ = json.Append(index == entries.Count - 1 ? "\n" : ",\n");
            }

            _ = json.Append("  ]\n}\n");
            await File.WriteAllTextAsync(Path.Join(directory, "manifest.json"), json.ToString(), new UTF8Encoding(encoderShouldEmitUTF8Identifier: true), TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        private static async Task SavePngAsync(Visual visual, int pixelWidth, int pixelHeight, double dpi, string fullPath)
        {
            RenderTargetBitmap bitmap = new(pixelWidth, pixelHeight, dpi, dpi, PixelFormats.Pbgra32);
            bitmap.Render(visual);

            PngBitmapEncoder encoder = new();
            encoder.Frames.Add(BitmapFrame.Create(bitmap));

            byte[] pngBytes;
#if NET10_0_OR_GREATER
            MemoryStream buffer = new(); await using (buffer.ConfigureAwait(true))
#else
            using (MemoryStream buffer = new())
#endif
            {
                encoder.Save(buffer);
                pngBytes = buffer.ToArray();
            }
            await File.WriteAllBytesAsync(fullPath, pngBytes, TestContext.Current.CancellationToken).ConfigureAwait(true);
        }

        private static async Task SaveFlattenedPngAsync(FrameworkElement element, int pixelWidth, int pixelHeight, double dpi, string fullPath)
        {
            Rect bounds = new(0.0, 0.0, element.ActualWidth, element.ActualHeight);
            Brush background = element.TryFindResource("SolidBackgroundFillColorBaseBrush") as Brush
                ?? element.TryFindResource("ApplicationBackgroundBrush") as Brush
                ?? Brushes.Transparent;

            DrawingVisual flattened = new();
            using (DrawingContext context = flattened.RenderOpen())
            {
                context.DrawRectangle(background, pen: null, bounds);
                context.DrawRectangle(new VisualBrush(element), pen: null, bounds);
            }

            await SavePngAsync(flattened, pixelWidth, pixelHeight, dpi, fullPath).ConfigureAwait(true);
        }

        private static async Task SaveElementPngAsync(FrameworkElement element, double scale, string fullPath)
        {
            int pixelWidth = Math.Max(1, (int)Math.Round(element.ActualWidth * scale, MidpointRounding.ToEven));
            int pixelHeight = Math.Max(1, (int)Math.Round(element.ActualHeight * scale, MidpointRounding.ToEven));
            double dpi = BaseDpi * scale;

            await SaveFlattenedPngAsync(element, pixelWidth, pixelHeight, dpi, fullPath).ConfigureAwait(true);
            Assert.True(File.Exists(fullPath), Invariant("Expected to write {0}", fullPath));
        }

        private static async Task SavePaddedControlPngAsync(FrameworkElement element, string fullPath, bool allowEmpty = false)
        {
            double contentWidth = element.ActualWidth;
            double contentHeight = element.ActualHeight;
            if (!allowEmpty)
            {
                Assert.True(element.IsVisible && contentWidth > 0 && contentHeight > 0,
                    Invariant("Control {0} has no rendered area.", element.GetType().Name));
            }

            // A dismissed or collapsed control intentionally leaves a blank themed field.
            contentWidth = Math.Max(160.0, contentWidth);
            contentHeight = Math.Max(24.0, contentHeight);
            int pixelWidth = (int)Math.Ceiling(contentWidth + (2 * ControlHorizontalPadding));
            int pixelHeight = (int)Math.Ceiling(contentHeight + (2 * ControlVerticalPadding));
            Brush background = element.TryFindResource("SolidBackgroundFillColorBaseBrush") as Brush
                ?? element.TryFindResource("ApplicationBackgroundBrush") as Brush
                ?? Brushes.White;
            DrawingVisual visual = new();
            using (DrawingContext context = visual.RenderOpen())
            {
                context.DrawRectangle(background, pen: null, new Rect(0, 0, pixelWidth, pixelHeight));
                if (!allowEmpty)
                {
                    PresentationSource presentation = PresentationSource.FromVisual(element)
                        ?? throw new InvalidOperationException("Control is not attached to a presentation source.");
                    Visual root = presentation.RootVisual;
                    Rect rootBounds = VisualTreeHelper.GetDescendantBounds(root);
                    double rootWidth = root is FrameworkElement rootElement
                        ? Math.Max(rootElement.ActualWidth, rootBounds.Right)
                        : rootBounds.Right;
                    double rootHeight = root is FrameworkElement rootFrameworkElement
                        ? Math.Max(rootFrameworkElement.ActualHeight, rootBounds.Bottom)
                        : rootBounds.Bottom;
                    int rootPixelWidth = Math.Max(1, (int)Math.Ceiling(rootWidth));
                    int rootPixelHeight = Math.Max(1, (int)Math.Ceiling(rootHeight));
                    RenderTargetBitmap rootBitmap = new(rootPixelWidth, rootPixelHeight, BaseDpi, BaseDpi, PixelFormats.Pbgra32);
                    rootBitmap.Render(root);

                    Rect elementBounds = ReferenceEquals(element, root)
                        ? new Rect(0, 0, element.ActualWidth, element.ActualHeight)
                        : element.TransformToAncestor(root).TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                    int left = Math.Max(0, (int)Math.Floor(elementBounds.Left));
                    int top = Math.Max(0, (int)Math.Floor(elementBounds.Top));
                    int right = Math.Min(rootPixelWidth, (int)Math.Ceiling(elementBounds.Right));
                    int bottom = Math.Min(rootPixelHeight, (int)Math.Ceiling(elementBounds.Bottom));
                    Assert.True(right > left && bottom > top,
                        Invariant("Control {0} lies outside its render root.", element.GetType().Name));
                    CroppedBitmap crop = new(rootBitmap, new Int32Rect(left, top, right - left, bottom - top));
                    context.DrawImage(crop, new Rect(ControlHorizontalPadding, ControlVerticalPadding,
                        crop.PixelWidth, crop.PixelHeight));
                }
            }

            await SavePngAsync(visual, pixelWidth, pixelHeight, BaseDpi, fullPath).ConfigureAwait(true);
            Assert.True(File.Exists(fullPath), Invariant("Expected to write {0}", fullPath));
        }

        private static FrameworkElement ResolveControlVisual(Demo.Pages.DemoSampleControl card, ControlCaptureSpec spec, bool allowEmpty)
        {
            if (card.DemoContent is not FrameworkElement content)
            {
                throw new InvalidOperationException("No live demo content for " + spec.Slug);
            }

            if (spec.TypeName is null)
            {
                return content;
            }

            foreach (FrameworkElement candidate in WpfTestSta.FindLogicalAndVisualDescendants<FrameworkElement>(content))
            {
                if (string.Equals(candidate.GetType().Name, spec.TypeName, StringComparison.Ordinal)
                    && (allowEmpty || (candidate.IsVisible && candidate.ActualWidth > 0 && candidate.ActualHeight > 0)))
                {
                    return candidate;
                }
            }

            throw new InvalidOperationException(
                Invariant("No rendered {0} visual in {1} sample {2}.", spec.TypeName, spec.Route, spec.Ordinal));
        }

        private static async Task CaptureControlAsync(
            Demo.MainWindow window,
            Demo.Pages.DemoSampleControl card,
            ControlCaptureSpec spec,
            string theme,
            string directory,
            string? state = null,
            bool allowEmpty = false)
        {
            card.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            FrameworkElement element = ResolveControlVisual(card, spec, allowEmpty);
            string suffix = state is null ? string.Empty : "-" + state;
            string file = Invariant("{0}-{1}{2}.png", spec.Slug, theme, suffix);
            await SavePaddedControlPngAsync(element, Path.Join(directory, file), allowEmpty).ConfigureAwait(true);
        }

        private static void RevealScrollBar(Demo.Pages.DemoSampleControl card)
        {
            foreach (System.Windows.Controls.Primitives.ScrollBar bar in
                WpfTestSta.FindLogicalAndVisualDescendants<System.Windows.Controls.Primitives.ScrollBar>(card))
            {
                if (bar.Orientation is Orientation.Vertical && bar.IsVisible)
                {
                    Controls.ScrollBarExtensions.SetIndicatorMode(bar, ScrollingIndicatorMode.MouseIndicator);
                    return;
                }
            }

            throw new InvalidOperationException("ScrollBarExtensions sample has no visible vertical ScrollBar.");
        }

        private static Application ResetApplication(ApplicationTheme theme, bool includeDemoSharedStyles)
        {
            Application application = WpfTestSta.EnsureApplication() ?? throw new InvalidOperationException("Could not create WPF Application for screenshot capture.");
            application.Resources.MergedDictionaries.Clear();
            ApplicationThemeManager.ResetForTesting();
            ApplicationAccentColorManager.ResetForTesting();
            ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
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
            window.SetResourceReference(System.Windows.Controls.Control.BackgroundProperty, "SolidBackgroundFillColorBaseBrush");

            if (window is Controls.FluenceWindow fluenceWindow)
            {
                fluenceWindow.SystemBackdropType = WindowBackdropType.None;
            }
        }

        private static async Task ShowSettleAndCaptureAsync(Window window, ApplicationTheme theme, string fullPath)
        {
            window.Show();
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

            await SaveElementPngAsync(window, ReferenceScale, fullPath).ConfigureAwait(true);
        }

        private static void PumpDispatcher(Dispatcher dispatcher, TimeSpan duration)
        {
            DispatcherFrame frame = new();
            DispatcherTimer timer = new(DispatcherPriority.Background, dispatcher)
            {
                Interval = duration,
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
        /// Captures the gallery shell (<see cref="Demo.MainWindow"/>) at <paramref name="route"/>
        /// with the navigation pane forced to <paramref name="paneMode"/>, writing
        /// <c language="text">{outputName}-{themeSlug}.png</c>.
        /// </summary>
        /// <param name="theme">The theme to apply.</param>
        /// <param name="themeSlug">The slug representing the theme.</param>
        /// <param name="route">The route to navigate to.</param>
        /// <param name="paneMode">The navigation pane display mode.</param>
        /// <param name="outputName">The name of the output file.</param>
        /// <param name="outputDirectory">The directory to save the output file.</param>
        private static async Task CaptureGalleryShellAtAsync(
            ApplicationTheme theme,
            string themeSlug,
            string route,
            NavigationViewPaneDisplayMode paneMode,
            string outputName,
            string outputDirectory)
        {
            _ = ResetApplication(theme, includeDemoSharedStyles: true);

            Demo.MainWindow? window = null;
            try
            {
                window = new Demo.MainWindow();
                PrepareCaptureWindow(window, GalleryCaptureWidth, GalleryCaptureHeight);
                window.Show();
                WpfTestSta.DrainDispatcher(window.Dispatcher);

                if (window.DemoNav is not null)
                {
                    window.DemoNav.PaneDisplayMode = paneMode;
                    window.DemoNav.IsPaneOpen = paneMode is NavigationViewPaneDisplayMode.Left;
                }

                window.NavigateTo(route);
                ApplicationThemeManager.Apply(theme, WindowBackdropType.None);
                WpfTestSta.DrainDispatcher(window.Dispatcher);
                PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
                window.UpdateLayout();
                await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);

                string fullPath = Path.Join(outputDirectory, Invariant("{0}-{1}.png", outputName, themeSlug));
                await SaveElementPngAsync(window, ReferenceScale, fullPath).ConfigureAwait(true);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            }
        }

        private static void CollectSampleCards(DependencyObject root, IList<Demo.Pages.DemoSampleControl> cards)
        {
            if (root is Demo.Pages.DemoSampleControl card)
            {
                cards.Add(card);
                return;
            }

            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                CollectSampleCards(VisualTreeHelper.GetChild(root, index), cards);
            }
        }

        private static T? FindDescendant<T>(DependencyObject root)
            where T : DependencyObject
        {
            if (root is T match)
            {
                return match;
            }

            for (int index = 0; index < VisualTreeHelper.GetChildrenCount(root); index++)
            {
                T? child = FindDescendant<T>(VisualTreeHelper.GetChild(root, index));
                if (child is not null)
                {
                    return child;
                }
            }

            return null;
        }

        private static async Task SettleGalleryAsync(Demo.MainWindow window)
        {
            WpfTestSta.DrainDispatcher(window.Dispatcher);
            PumpDispatcher(window.Dispatcher, AnimationSettleDelay);
            window.UpdateLayout();
            await window.Dispatcher.InvokeAsync(static () => { }, priority: DispatcherPriority.Render, cancellationToken: TestContext.Current.CancellationToken).Task.ConfigureAwait(true);
        }

        private static async Task CaptureGalleryCardAsync(
            Demo.MainWindow window,
            Demo.Pages.DemoSampleControl card,
            string route,
            string theme,
            int ordinal,
            string state,
            string directory,
            IList<CaptureEntry> entries)
        {
            card.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            Assert.True(card.ActualWidth > 0 && card.ActualHeight > 0, Invariant("Card {0} on {1} has no rendered area.", ordinal, route));
            string stateSuffix = string.Equals(state, "default", StringComparison.Ordinal) ? string.Empty : "-" + state;
            string file = Invariant("{0}-sample-{1:00}-{2}{3}.png", route, ordinal, theme, stateSuffix);
            await SaveElementPngAsync(card, CardCaptureScale, Path.Join(directory, file)).ConfigureAwait(true);
            entries.Add(new CaptureEntry("card", route, card.SampleDescription, theme, state, file));
        }

        private static async Task CaptureGalleryChangedStateAsync(
            Demo.MainWindow window,
            IList<Demo.Pages.DemoSampleControl> cards,
            string route,
            string theme,
            string directory,
            IList<CaptureEntry> entries)
        {
            int ordinal;
            string state;
            if (string.Equals(route, "buttons", StringComparison.Ordinal))
            {
                ordinal = 1;
                state = "disabled";
                if (FindDescendant<Controls.Button>(cards[ordinal - 1]) is not Controls.Button button)
                {
                    throw new InvalidOperationException("Buttons sample has no Button for disabled state.");
                }

                button.IsEnabled = false;
            }
            else if (string.Equals(route, "selection", StringComparison.Ordinal))
            {
                ordinal = 3;
                state = "toggled";
                if (FindDescendant<Controls.ToggleSwitch>(cards[ordinal - 1]) is not Controls.ToggleSwitch toggle)
                {
                    throw new InvalidOperationException("Selection sample has no ToggleSwitch for toggled state.");
                }

                toggle.IsChecked = !toggle.IsChecked;
            }
            else if (string.Equals(route, "layout", StringComparison.Ordinal))
            {
                ordinal = 3;
                state = "expanded";
                if (FindDescendant<Controls.Expander>(cards[ordinal - 1]) is not Controls.Expander expander)
                {
                    throw new InvalidOperationException("Layout sample has no Expander for expanded state.");
                }

                expander.IsExpanded = true;
            }
            else if (string.Equals(route, "inputs", StringComparison.Ordinal))
            {
                ordinal = 1;
                state = "entered";
                if (FindDescendant<Controls.TextBox>(cards[ordinal - 1]) is not Controls.TextBox input)
                {
                    throw new InvalidOperationException("Inputs sample has no TextBox for entered state.");
                }

                input.Text = "Fluent controls";
            }
            else if (string.Equals(route, "navigation", StringComparison.Ordinal))
            {
                ordinal = 6;
                state = "page-selected";
                if (FindDescendant<Controls.PipsPager>(cards[ordinal - 1]) is not Controls.PipsPager pager)
                {
                    throw new InvalidOperationException("Navigation sample has no PipsPager for selected state.");
                }

                pager.SelectedPageIndex = 3;
            }
            else if (string.Equals(route, "status", StringComparison.Ordinal))
            {
                ordinal = 5;
                state = "dismissed";
                if (FindDescendant<Controls.InfoBar>(cards[ordinal - 1]) is not Controls.InfoBar infoBar)
                {
                    throw new InvalidOperationException("Status sample has no InfoBar for dismissed state.");
                }

                infoBar.IsOpen = false;
            }
            else if (string.Equals(route, "tabs", StringComparison.Ordinal))
            {
                ordinal = 1;
                state = "selected";
                if (FindDescendant<TabControl>(cards[ordinal - 1]) is not TabControl tabs)
                {
                    throw new InvalidOperationException("Tabs sample has no TabControl for selected state.");
                }

                tabs.SelectedIndex = 1;
            }
            else
            {
                return;
            }

            await CaptureGalleryCardAsync(window, cards[ordinal - 1], route, theme, ordinal, state, directory, entries).ConfigureAwait(true);
        }

        private static async Task CaptureGalleryPopupStatesAsync(
            Demo.MainWindow window,
            IList<Demo.Pages.DemoSampleControl> cards,
            string theme,
            string directory,
            IList<CaptureEntry> entries)
        {
            Demo.Pages.DemoSampleControl contextCard = cards[1];
            contextCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Border>(contextCard) is not Controls.Border contextTarget ||
                contextTarget.ContextMenu is not ContextMenu contextMenu)
            {
                throw new InvalidOperationException("Menus sample has no ContextMenu to capture.");
            }

            contextMenu.PlacementTarget = contextTarget;
            contextMenu.IsOpen = true;
            await SettleGalleryAsync(window).ConfigureAwait(true);
            string contextFile = Invariant("menus-context-menu-open-{0}.png", theme);
            await SaveElementPngAsync(contextMenu, ReferenceScale, Path.Join(directory, contextFile)).ConfigureAwait(true);
            entries.Add(new CaptureEntry("popup", "menus", "Context menu open", theme, "open", contextFile));
            contextMenu.IsOpen = false;

            Demo.Pages.DemoSampleControl flyoutCard = cards[3];
            flyoutCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Button>(flyoutCard) is not Controls.Button flyoutButton ||
                Controls.FlyoutBase.GetAttachedFlyout(flyoutButton) is not Controls.FlyoutBase flyout)
            {
                throw new InvalidOperationException("Menus sample has no Flyout to capture.");
            }

            flyout.ShowAt(flyoutButton);
            await SettleGalleryAsync(window).ConfigureAwait(true);
            FrameworkElement flyoutPresenter = flyout.Presenter
                ?? throw new InvalidOperationException("Flyout did not create a presenter.");
            string flyoutFile = Invariant("menus-flyout-open-{0}.png", theme);
            await SaveElementPngAsync(flyoutPresenter, ReferenceScale, Path.Join(directory, flyoutFile)).ConfigureAwait(true);
            entries.Add(new CaptureEntry("popup", "menus", "Flyout open", theme, "open", flyoutFile));
            flyout.Hide();

            Demo.Pages.DemoSampleControl commandBarCard = cards[6];
            commandBarCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Button>(commandBarCard) is not Controls.Button commandBarButton ||
                Controls.FlyoutBase.GetAttachedFlyout(commandBarButton) is not Controls.CommandBarFlyout commandBarFlyout)
            {
                throw new InvalidOperationException("Menus sample has no CommandBarFlyout to capture.");
            }

            commandBarFlyout.ShowAt(commandBarButton);
            await SettleGalleryAsync(window).ConfigureAwait(true);
            FrameworkElement commandBarPresenter = commandBarFlyout.Presenter!;
            string commandBarFile = Invariant("menus-command-bar-flyout-open-{0}.png", theme);
            await SaveElementPngAsync(commandBarPresenter, ReferenceScale, Path.Join(directory, commandBarFile)).ConfigureAwait(true);
            entries.Add(new CaptureEntry("popup", "menus", "Command bar flyout open", theme, "open", commandBarFile));
            commandBarFlyout.Hide();

            Controls.ContentDialog dialog = new()
            {
                Title = "Delete file?",
                Content = "Roadmap.md will be permanently deleted. This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            Application application = Application.Current ?? throw new InvalidOperationException("No WPF Application is running.");
            application.MainWindow = window;
            Task dialogCompletion = dialog.ShowAsync();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            string dialogFile = Invariant("menus-content-dialog-open-{0}.png", theme);
            await SaveElementPngAsync(window, ReferenceScale, Path.Join(directory, dialogFile)).ConfigureAwait(true);
            entries.Add(new CaptureEntry("overlay", "menus", "Content dialog open", theme, "open", dialogFile));
            dialog.Hide();
            // Hide animates the smoke layer out. Wait for teardown before capturing the next route.
            await dialogCompletion.ConfigureAwait(true);
        }

        private static async Task CaptureGalleryScrolledStateAsync(
            Demo.MainWindow window,
            IList<Demo.Pages.DemoSampleControl> cards,
            string theme,
            string directory,
            IList<CaptureEntry> entries)
        {
            const int ordinal = 5;
            Demo.Pages.DemoSampleControl card = cards[ordinal - 1];
            if (FindDescendant<Controls.SmoothScrollViewer>(card) is not Controls.SmoothScrollViewer viewer)
            {
                throw new InvalidOperationException("Layout sample has no SmoothScrollViewer to capture.");
            }

            Assert.True(viewer.ScrollableHeight > 0, "Layout SmoothScrollViewer sample must have content below the viewport.");
            viewer.ScrollToVerticalOffset(viewer.ScrollableHeight);
            await CaptureGalleryCardAsync(window, card, "layout", theme, ordinal, "scrolled", directory, entries).ConfigureAwait(true);
        }

        private static async Task CaptureControlStatesAsync(
            Demo.MainWindow window,
            IList<Demo.Pages.DemoSampleControl> cards,
            string route,
            string theme,
            string directory)
        {
            if (string.Equals(route, "buttons", StringComparison.Ordinal))
            {
                ControlCaptureSpec disabled = new("button", route, 1, "Button");
                if (FindDescendant<Controls.Button>(cards[0]) is not Controls.Button button)
                {
                    throw new InvalidOperationException("Buttons sample has no Button for disabled state.");
                }

                button.IsEnabled = false;
                await CaptureControlAsync(window, cards[0], disabled, theme, directory, "disabled").ConfigureAwait(true);
                if (string.Equals(theme, "light", StringComparison.Ordinal))
                {
                    ControlCaptureSpec accent = new("button", route, 3, "Button");
                    foreach ((Color color, string name) in new[]
                    {
                        (Color.FromRgb(0, 120, 212), "blue"),
                        (Color.FromRgb(189, 64, 0), "orange"),
                    })
                    {
                        ApplicationAccentColorManager.ApplyCustomAccent(color);
                        await CaptureControlAsync(window, cards[2], accent, theme, directory, "accent-" + name).ConfigureAwait(true);
                    }

                    ApplicationAccentColorManager.ApplySystemAccent();
                }
            }
            else if (string.Equals(route, "selection", StringComparison.Ordinal))
            {
                if (FindDescendant<Controls.ToggleSwitch>(cards[2]) is not Controls.ToggleSwitch toggle)
                {
                    throw new InvalidOperationException("Selection sample has no ToggleSwitch for toggled state.");
                }

                toggle.IsChecked = !toggle.IsChecked;
                await CaptureControlAsync(window, cards[2], new("toggle-switch", route, 3, "ToggleSwitch"), theme, directory, "toggled").ConfigureAwait(true);
            }
            else if (string.Equals(route, "layout", StringComparison.Ordinal))
            {
                if (FindDescendant<Controls.Expander>(cards[2]) is not Controls.Expander expander
                    || FindDescendant<Controls.SmoothScrollViewer>(cards[4]) is not Controls.SmoothScrollViewer viewer)
                {
                    throw new InvalidOperationException("Layout sample is missing an Expander or SmoothScrollViewer.");
                }

                expander.IsExpanded = true;
                await CaptureControlAsync(window, cards[2], new("expander", route, 3, "Expander"), theme, directory, "expanded").ConfigureAwait(true);
                cards[4].BringIntoView();
                await SettleGalleryAsync(window).ConfigureAwait(true);
                viewer.ScrollToVerticalOffset(viewer.ScrollableHeight);
                await CaptureControlAsync(window, cards[4], new("smooth-scroll-viewer", route, 5, "SmoothScrollViewer"), theme, directory, "scrolled").ConfigureAwait(true);
                RevealScrollBar(cards[4]);
                await CaptureControlAsync(window, cards[4], new("scroll-bar-extensions", route, 5, "ScrollBar"), theme, directory, "scrolled").ConfigureAwait(true);
            }
            else if (string.Equals(route, "inputs", StringComparison.Ordinal))
            {
                if (FindDescendant<Controls.TextBox>(cards[0]) is not Controls.TextBox input)
                {
                    throw new InvalidOperationException("Inputs sample has no TextBox for entered state.");
                }

                input.Text = "Fluent controls";
                await CaptureControlAsync(window, cards[0], new("text-box", route, 1, "TextBox"), theme, directory, "entered").ConfigureAwait(true);
            }
            else if (string.Equals(route, "navigation", StringComparison.Ordinal))
            {
                if (FindDescendant<Controls.PipsPager>(cards[5]) is not Controls.PipsPager pager)
                {
                    throw new InvalidOperationException("Navigation sample has no PipsPager for selected state.");
                }

                pager.SelectedPageIndex = 3;
                await CaptureControlAsync(window, cards[5], new("pips-pager", route, 6, "PipsPager"), theme, directory, "page-selected").ConfigureAwait(true);
            }
            else if (string.Equals(route, "status", StringComparison.Ordinal))
            {
                if (FindDescendant<Controls.InfoBar>(cards[4]) is not Controls.InfoBar infoBar)
                {
                    throw new InvalidOperationException("Status sample has no InfoBar for dismissed state.");
                }

                infoBar.IsOpen = false;
                await CaptureControlAsync(window, cards[4], new("info-bar", route, 5, "InfoBar"), theme, directory, "dismissed", allowEmpty: true).ConfigureAwait(true);
            }
        }

        private static async Task CaptureControlCatalogAtAsync(ApplicationTheme theme, string themeSlug, string directory)
        {
            _ = ResetApplication(theme, includeDemoSharedStyles: true);
            Demo.MainWindow window = new();
            try
            {
                PrepareCaptureWindow(window, GalleryCaptureWidth, GalleryCaptureHeight);
                window.Show();
                await SettleGalleryAsync(window).ConfigureAwait(true);

                foreach (Demo.DemoNavigationItem item in Demo.DemoNavigationCatalog.Items)
                {
                    string route = item.Route.Replace(' ', '-');
                    window.NavigateTo(string.Equals(route, "data-binding", StringComparison.Ordinal) ? "data binding" : route);
                    await SettleGalleryAsync(window).ConfigureAwait(true);
                    if (window.PageFrame.Content is not DependencyObject page)
                    {
                        throw new InvalidOperationException("Gallery route " + route + " did not load a page.");
                    }

                    List<Demo.Pages.DemoSampleControl> cards = [];
                    CollectSampleCards(page, cards);
                    foreach (ControlCaptureSpec spec in ControlCaptureSpecs)
                    {
                        if (string.Equals(spec.Route, route, StringComparison.Ordinal))
                        {
                            if (string.Equals(spec.Slug, "scroll-bar-extensions", StringComparison.Ordinal))
                            {
                                RevealScrollBar(cards[spec.Ordinal - 1]);
                            }

                            await CaptureControlAsync(window, cards[spec.Ordinal - 1], spec, themeSlug, directory).ConfigureAwait(true);
                        }
                    }

                    if (cards.Count > 0)
                    {
                        await CaptureControlStatesAsync(window, cards, route, themeSlug, directory).ConfigureAwait(true);
                    }

                    if (string.Equals(route, "menus", StringComparison.Ordinal))
                    {
                        await CaptureControlPopupsAsync(window, cards, themeSlug, directory).ConfigureAwait(true);
                    }
                }

                await CaptureControlWindowExamplesAsync(window, themeSlug, directory).ConfigureAwait(true);
            }
            finally
            {
                window.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            }
        }

        private static async Task SaveNamedControlAsync(FrameworkElement element, string slug, string theme, string directory, string? state = null)
        {
            string suffix = state is null ? string.Empty : "-" + state;
            await SavePaddedControlPngAsync(element,
                Path.Join(directory, Invariant("{0}-{1}{2}.png", slug, theme, suffix))).ConfigureAwait(true);
        }

        private static async Task CaptureControlPopupsAsync(
            Demo.MainWindow window,
            IList<Demo.Pages.DemoSampleControl> cards,
            string theme,
            string directory)
        {
            Demo.Pages.DemoSampleControl contextCard = cards[1];
            contextCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Border>(contextCard) is not Controls.Border contextTarget
                || contextTarget.ContextMenu is not ContextMenu contextMenu)
            {
                throw new InvalidOperationException("Menus sample has no ContextMenu to capture.");
            }

            contextMenu.PlacementTarget = contextTarget;
            contextMenu.IsOpen = true;
            await SettleGalleryAsync(window).ConfigureAwait(true);
            await SaveNamedControlAsync(contextMenu, "context-menu", theme, directory).ConfigureAwait(true);
            contextMenu.IsOpen = false;

            Demo.Pages.DemoSampleControl toolTipCard = cards[2];
            toolTipCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Button>(toolTipCard) is not Controls.Button toolTipButton
                || toolTipButton.ToolTip is not ToolTip toolTip)
            {
                throw new InvalidOperationException("Menus sample has no ToolTip to capture.");
            }

            toolTip.PlacementTarget = toolTipButton;
            toolTip.IsOpen = true;
            await SettleGalleryAsync(window).ConfigureAwait(true);
            await SaveNamedControlAsync(toolTip, "tool-tip", theme, directory).ConfigureAwait(true);
            toolTip.IsOpen = false;

            Demo.Pages.DemoSampleControl flyoutCard = cards[3];
            flyoutCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Button>(flyoutCard) is not Controls.Button flyoutButton
                || Controls.FlyoutBase.GetAttachedFlyout(flyoutButton) is not Controls.FlyoutBase flyout)
            {
                throw new InvalidOperationException("Menus sample has no Flyout to capture.");
            }

            flyout.ShowAt(flyoutButton);
            await SettleGalleryAsync(window).ConfigureAwait(true);
            FrameworkElement flyoutPresenter = flyout.Presenter
                ?? throw new InvalidOperationException("Flyout did not create a presenter.");
            foreach (string slug in new[] { "flyout", "flyout-base", "flyout-presenter" })
            {
                await SaveNamedControlAsync(flyoutPresenter, slug, theme, directory).ConfigureAwait(true);
            }

            flyout.Hide();

            Demo.Pages.DemoSampleControl teachingTipCard = cards[5];
            teachingTipCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.TeachingTip>(teachingTipCard) is not Controls.TeachingTip teachingTip)
            {
                throw new InvalidOperationException("Menus sample has no TeachingTip to capture.");
            }

            teachingTip.IsOpen = true;
            await SettleGalleryAsync(window).ConfigureAwait(true);
            await SaveNamedControlAsync(teachingTip, "teaching-tip", theme, directory).ConfigureAwait(true);
            teachingTip.IsOpen = false;

            Demo.Pages.DemoSampleControl commandCard = cards[6];
            commandCard.BringIntoView();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            if (FindDescendant<Controls.Button>(commandCard) is not Controls.Button commandButton
                || Controls.FlyoutBase.GetAttachedFlyout(commandButton) is not Controls.CommandBarFlyout commandFlyout)
            {
                throw new InvalidOperationException("Menus sample has no CommandBarFlyout to capture.");
            }

            await SaveNamedControlAsync(commandButton, "command-bar-flyout", theme, directory).ConfigureAwait(true);
            commandFlyout.ShowAt(commandButton);
            await SettleGalleryAsync(window).ConfigureAwait(true);
            FrameworkElement commandPresenter = commandFlyout.Presenter
                ?? throw new InvalidOperationException("CommandBarFlyout did not create a presenter.");
            await SaveNamedControlAsync(commandPresenter, "command-bar-flyout", theme, directory, "open").ConfigureAwait(true);
            await SaveNamedControlAsync(commandPresenter, "command-bar-flyout-presenter", theme, directory, "open").ConfigureAwait(true);

            if (FindDescendant<Controls.AppBarButton>(commandPresenter) is not Controls.AppBarButton appBarButton)
            {
                throw new InvalidOperationException("CommandBarFlyout has no rendered AppBarButton.");
            }

            await SaveNamedControlAsync(appBarButton, "app-bar-button", theme, directory).ConfigureAwait(true);
            commandFlyout.Hide();

            Controls.ContentDialog dialog = new()
            {
                Title = "Delete file?",
                Content = "Roadmap.md will be permanently deleted. This cannot be undone.",
                PrimaryButtonText = "Delete",
                CloseButtonText = "Cancel",
                DefaultButton = ContentDialogButton.Close,
            };
            Application application = Application.Current ?? throw new InvalidOperationException("No WPF Application is running.");
            application.MainWindow = window;
            _ = dialog.ShowAsync();
            await SettleGalleryAsync(window).ConfigureAwait(true);
            await SaveNamedControlAsync(dialog, "content-dialog", theme, directory).ConfigureAwait(true);
            dialog.Hide();
        }

        private static async Task CaptureControlWindowExamplesAsync(Demo.MainWindow gallery, string theme, string directory)
        {
            Controls.TitleBar titleBar = new() { Title = "Fluence.Wpf" };
            Controls.FluenceWindow window = new()
            {
                Title = "Fluence.Wpf",
                TitleBar = titleBar,
                ExtendsContentIntoTitleBar = true,
                Content = new TextBlock { Text = "", Margin = new Thickness(24) },
            };
            try
            {
                PrepareCaptureWindow(window, 480, 240);
                window.Show();
                await SettleGalleryAsync(gallery).ConfigureAwait(true);
                await SaveNamedControlAsync(window, "fluence-window", theme, directory).ConfigureAwait(true);
                await SaveNamedControlAsync(titleBar, "title-bar", theme, directory).ConfigureAwait(true);
            }
            finally
            {
                window.Close();
            }

            Controls.FontIcon icon = new() { Glyph = "\uE713", IconFontSize = 48 };
            Window iconWindow = new() { Content = icon };
            try
            {
                PrepareCaptureWindow(iconWindow, 192, 160);
                iconWindow.Show();
                await SettleGalleryAsync(gallery).ConfigureAwait(true);
                await SaveNamedControlAsync(icon, "font-icon", theme, directory).ConfigureAwait(true);
            }
            finally
            {
                iconWindow.Close();
            }
        }

        private static async Task CaptureGalleryCatalogAtAsync(
            ApplicationTheme theme,
            string themeSlug,
            string outputDirectory,
            IList<CaptureEntry> entries)
        {
            _ = ResetApplication(theme, includeDemoSharedStyles: true);
            Demo.MainWindow window = new();
            try
            {
                PrepareCaptureWindow(window, GalleryCaptureWidth, GalleryCaptureHeight);
                window.Show();
                await SettleGalleryAsync(window).ConfigureAwait(true);

                List<(string Route, string Title)> routes = [];
                foreach (Demo.DemoNavigationItem item in Demo.DemoNavigationCatalog.Items)
                {
                    routes.Add((item.Route.Replace(' ', '-'), item.Title));
                }

                routes.Add(("settings", "Settings"));
                foreach ((string route, string title) in routes)
                {
                    window.NavigateTo(string.Equals(route, "data-binding", StringComparison.Ordinal) ? "data binding" : route);
                    await SettleGalleryAsync(window).ConfigureAwait(true);
                    string routeFile = Invariant("{0}-{1}.png", route, themeSlug);
                    await SaveElementPngAsync(window, ReferenceScale, Path.Join(outputDirectory, routeFile)).ConfigureAwait(true);
                    entries.Add(new CaptureEntry("route", route, title, themeSlug, "default", routeFile));

                    if (window.PageFrame.Content is not DependencyObject page)
                    {
                        throw new InvalidOperationException("Gallery route " + route + " did not load a page.");
                    }

                    List<Demo.Pages.DemoSampleControl> cards = [];
                    CollectSampleCards(page, cards);
                    for (int ordinal = 1; ordinal <= cards.Count; ordinal++)
                    {
                        await CaptureGalleryCardAsync(window, cards[ordinal - 1], route, themeSlug, ordinal, "default", outputDirectory, entries).ConfigureAwait(true);
                    }

                    if (string.Equals(route, "buttons", StringComparison.Ordinal) && theme is ApplicationTheme.Light)
                    {
                        cards[0].BringIntoView();
                        await SettleGalleryAsync(window).ConfigureAwait(true);
                        foreach ((Color color, string accent) in new[]
                        {
                            (Color.FromRgb(0, 120, 212), "blue"),
                            (Color.FromRgb(189, 64, 0), "orange"),
                        })
                        {
                            ApplicationAccentColorManager.ApplyCustomAccent(color);
                            await SettleGalleryAsync(window).ConfigureAwait(true);
                            string accentFile = Invariant("buttons-light-accent-{0}.png", accent);
                            await SaveElementPngAsync(window, ReferenceScale, Path.Join(outputDirectory, accentFile)).ConfigureAwait(true);
                            entries.Add(new CaptureEntry("accent", route, "Buttons", themeSlug, accent, accentFile));
                        }

                        ApplicationAccentColorManager.ApplySystemAccent();
                    }

                    if (cards.Count > 0)
                    {
                        await CaptureGalleryChangedStateAsync(window, cards, route, themeSlug, outputDirectory, entries).ConfigureAwait(true);
                    }

                    if (string.Equals(route, "layout", StringComparison.Ordinal))
                    {
                        await CaptureGalleryScrolledStateAsync(window, cards, themeSlug, outputDirectory, entries).ConfigureAwait(true);
                    }

                    if (string.Equals(route, "menus", StringComparison.Ordinal))
                    {
                        await CaptureGalleryPopupStatesAsync(window, cards, themeSlug, outputDirectory, entries).ConfigureAwait(true);
                    }
                }
            }
            finally
            {
                window.Close();

                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            }
        }

        /// <summary>
        /// Reads the inline XAML here-string from <c language="text">06-ControlsTour.ps1</c> so the captured window
        /// stays in lock-step with the script the screenshot documents.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if the XAML here-string cannot be located.</exception>
        private static async Task<string> ExtractControlsTourXamlAsync()
        {
            string scriptPath = Path.Join(FindRepoRoot(), "Fluence.Wpf.PowerShell.Module", "examples", "06-ControlsTour.ps1");
            string[] lines = await File.ReadAllLinesAsync(scriptPath, TestContext.Current.CancellationToken).ConfigureAwait(true);

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
                    "Could not locate the XAML here-string in 06-ControlsTour.ps1.");
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

        private static async Task CapturePowerShellControlsAtAsync(ApplicationTheme theme, string themeSlug, string outputDirectory)
        {
            _ = ResetApplication(theme, includeDemoSharedStyles: false);

            Window? window = null;
            try
            {
                window = XamlReader.Parse(await ExtractControlsTourXamlAsync().ConfigureAwait(true)) as Window
                    ?? throw new InvalidOperationException("06-ControlsTour.ps1 XAML did not load as a WPF Window.");

                PrepareCaptureWindow(window, PowerShellCaptureWidth, PowerShellCaptureHeight);
                string fullPath = Path.Join(outputDirectory, Invariant("powershell-{0}.png", themeSlug));
                await ShowSettleAndCaptureAsync(window, theme, fullPath).ConfigureAwait(true);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            }
        }

#if NET10_0_OR_GREATER
        private static void AddScreenshotTask(Demo.Mvvm.ViewModels.MainViewModel viewModel, string title, bool isCompleted)
        {
            viewModel.NewTaskText = title;
            if (viewModel.AddCommand.CanExecute(parameter: null))
            {
                viewModel.AddCommand.Execute(parameter: null);
            }

            if (isCompleted && viewModel.DisplayedTasks.Count > 0)
            {
                viewModel.DisplayedTasks[^1].IsCompleted = true;
            }
        }

        private static void SeedMvvmScreenshotData(Demo.Mvvm.MainWindow window)
        {
            if (window.DataContext is not Demo.Mvvm.ViewModels.MainViewModel viewModel)
            {
                return;
            }

            AddScreenshotTask(viewModel, "Review theme dictionary slots", isCompleted: true);
            AddScreenshotTask(viewModel, "Polish NavigationView samples", isCompleted: false);
            AddScreenshotTask(viewModel, "Capture release screenshots", isCompleted: false);
            AddScreenshotTask(viewModel, "Update API docs", isCompleted: true);
        }

        private static async Task CaptureMvvmDemoAtAsync(ApplicationTheme theme, string themeSlug, string outputDirectory)
        {
            _ = ResetApplication(theme, includeDemoSharedStyles: false);

            Demo.Mvvm.MainWindow? window = null;
            try
            {
                window = new Demo.Mvvm.MainWindow();
                SeedMvvmScreenshotData(window);
                PrepareCaptureWindow(window, AppCaptureWidth, AppCaptureHeight);
                string fullPath = Path.Join(outputDirectory, Invariant("mvvm-{0}.png", themeSlug));
                await ShowSettleAndCaptureAsync(window, theme, fullPath).ConfigureAwait(true);
            }
            finally
            {
                window?.Close();
                ApplicationThemeManager.Apply(ApplicationTheme.Light, WindowBackdropType.None);
            }
        }
#endif

        [Fact(SkipUnless = nameof(ScreenshotCaptureEnabled), Skip = "Screenshot capture is opt-in; set FLUENCE_CAPTURE_SCREENSHOTS=1 to regenerate docs/screenshots.")]
        public Task CaptureGalleryShellNavigationModesAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    await CaptureGalleryShellAtAsync(theme, themeSlug, "home", NavigationViewPaneDisplayMode.Left, "gallery-home", output).ConfigureAwait(true);
                    await CaptureGalleryShellAtAsync(theme, themeSlug, "buttons", NavigationViewPaneDisplayMode.LeftCompact, "gallery-buttons", output).ConfigureAwait(true);
                    await CaptureGalleryShellAtAsync(theme, themeSlug, "status", NavigationViewPaneDisplayMode.Top, "gallery-status", output).ConfigureAwait(true);
                }
            });
        }

        [Fact(SkipUnless = nameof(ScreenshotCaptureEnabled), Skip = "Screenshot capture is opt-in; set FLUENCE_CAPTURE_SCREENSHOTS=1 to regenerate docs/screenshots.")]
        public Task CaptureGalleryCatalogAndCardsAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                if (ControlsOnlyCaptureEnabled)
                {
                    string controlsDirectory = EnsureControlOutputDirectory();
                    foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                    {
                        await CaptureControlCatalogAtAsync(theme, themeSlug, controlsDirectory).ConfigureAwait(true);
                    }

                    return;
                }

                string output = EnsureGalleryOutputDirectory();
                List<CaptureEntry> entries = [];
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    await CaptureGalleryCatalogAtAsync(theme, themeSlug, output, entries).ConfigureAwait(true);
                }

                int cardCount = 0;
                foreach (CaptureEntry entry in entries)
                {
                    if (string.Equals(entry.Kind, "card", StringComparison.Ordinal) &&
                        string.Equals(entry.State, "default", StringComparison.Ordinal))
                    {
                        cardCount++;
                    }
                }

                Assert.Equal(152, cardCount);
                await WriteGalleryManifestAsync(output, entries).ConfigureAwait(true);
            });
        }

        [Fact(SkipUnless = nameof(ScreenshotCaptureEnabled), Skip = "Screenshot capture is opt-in; set FLUENCE_CAPTURE_SCREENSHOTS=1 to regenerate docs/screenshots.")]
        public Task CapturePowerShellControlsTourAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    await CapturePowerShellControlsAtAsync(theme, themeSlug, output).ConfigureAwait(true);
                }
            });
        }

#if NET10_0_OR_GREATER
        [Fact(SkipUnless = nameof(ScreenshotCaptureEnabled), Skip = "Screenshot capture is opt-in; set FLUENCE_CAPTURE_SCREENSHOTS=1 to regenerate docs/screenshots.")]
        public Task CaptureMvvmTaskManagerAsync()
        {
            return WpfTestSta.RunOnStaAsync(async static () =>
            {
                string output = EnsureOutputDirectory();
                foreach ((ApplicationTheme theme, string themeSlug) in DocumentationThemes)
                {
                    await CaptureMvvmDemoAtAsync(theme, themeSlug, output).ConfigureAwait(true);
                }
            });
        }
#endif
    }
}
