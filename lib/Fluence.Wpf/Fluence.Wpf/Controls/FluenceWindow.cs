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

using Fluence.Wpf.Helpers;
using Fluence.Wpf.Native;
using System;
using System.Globalization;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shell;

namespace Fluence.Wpf.Controls
{
    /// <summary>
    /// A top-level window that recreates the Windows 11 Fluent / WinUI 3 chrome on WPF: a DWM
    /// system backdrop (Mica, Acrylic, or Tabbed), rounded corners, an extendable title bar, and
    /// custom caption buttons that integrate with the Windows 11 snap-layout flyout.
    /// </summary>
    /// <remarks>
    /// The window collapses the native non-client frame through <see cref="WindowChrome"/> and
    /// drives every caption interaction (drag, resize, snap-layout hover, maximize/restore) from a
    /// Win32 message hook so the custom chrome stays authoritative. Theme, accent, and backdrop are
    /// applied directly to the HWND via DWM attributes and kept in sync with the shared theme
    /// managers for the lifetime of the realised window.
    /// </remarks>
    [TemplatePart(Name = PART_MinimizeButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_MaximizeButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_RestoreButton, Type = typeof(System.Windows.Controls.Button))]
    [TemplatePart(Name = PART_CloseButton, Type = typeof(System.Windows.Controls.Button))]
    public class FluenceWindow : Window
    {
        #region Constants

        /// <summary>Template part name for the minimize caption button.</summary>
        private const string PART_MinimizeButton = "PART_MinimizeButton";

        /// <summary>Template part name for the maximize caption button.</summary>
        private const string PART_MaximizeButton = "PART_MaximizeButton";

        /// <summary>Template part name for the restore caption button.</summary>
        private const string PART_RestoreButton = "PART_RestoreButton";

        /// <summary>Template part name for the close caption button.</summary>
        private const string PART_CloseButton = "PART_CloseButton";

        /// <summary>
        /// Default <see cref="TitleBarHeight"/>. Matches the WinUI 3 canonical expanded title-bar
        /// height (48; the compact variant is 32).
        /// </summary>
        private const double DefaultTitleBarHeight = 48d;

        #endregion Constants

        #region Value converters

        /// <summary>
        /// Converts a value to <c>true</c> when it is not null; used by caption-button visibility
        /// bindings in the control template (referenced via <c>{x:Static}</c>).
        /// </summary>
        public static readonly IValueConverter IsNotNullConverter = new IsNotNullValueConverter();

        /// <summary>
        /// One-way converter that maps a non-null value to <see langword="true"/> and <see langword="null"/> to
        /// <see langword="false"/>. <see cref="ConvertBack(object, Type, object, CultureInfo)"/> is not
        /// supported and throws <see cref="NotSupportedException"/>.
        /// </summary>
        private sealed class IsNotNullValueConverter : IValueConverter
        {
            /// <inheritdoc />
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                return value is not null;
            }

            /// <inheritdoc />
            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotSupportedException();
            }
        }

        #endregion Value converters

        #region Dependency Properties

        /// <summary>
        /// Identifies the <see cref="SystemBackdropType"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty SystemBackdropTypeProperty =
            DependencyProperty.Register(
                "SystemBackdropType",
                typeof(BackdropType),
                typeof(FluenceWindow),
                new PropertyMetadata(BackdropType.Auto, OnSystemBackdropTypeChanged));

        /// <summary>
        /// Identifies the <see cref="CornerStyle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty CornerStyleProperty =
            DependencyProperty.Register(
                "CornerStyle",
                typeof(CornerPreference),
                typeof(FluenceWindow),
                new PropertyMetadata(CornerPreference.Round, OnCornerStyleChanged));

        /// <summary>
        /// Identifies the <see cref="MarginMaximized"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty MarginMaximizedProperty =
            DependencyProperty.Register(
                "MarginMaximized",
                typeof(Thickness),
                typeof(FluenceWindow),
                new PropertyMetadata(new Thickness(0)));

        /// <summary>
        /// Identifies the <see cref="ExtendsContentIntoTitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ExtendsContentIntoTitleBarProperty =
            DependencyProperty.Register(
                nameof(ExtendsContentIntoTitleBar),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: false, OnExtendsContentIntoTitleBarChanged));

        /// <summary>
        /// Identifies the <see cref="TitleBar"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarProperty =
            DependencyProperty.Register(
                nameof(TitleBar),
                typeof(UIElement),
                typeof(FluenceWindow),
                new PropertyMetadata(propertyChangedCallback: null));

        /// <summary>
        /// Identifies the <see cref="TitleBarHeight"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty TitleBarHeightProperty =
            DependencyProperty.Register(
                nameof(TitleBarHeight),
                typeof(double),
                typeof(FluenceWindow),
                new PropertyMetadata(DefaultTitleBarHeight, OnTitleBarHeightChanged));

        /// <summary>
        /// Identifies the <see cref="ShowIcon"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowIconProperty =
            DependencyProperty.Register(
                nameof(ShowIcon),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="ShowTitle"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty ShowTitleProperty =
            DependencyProperty.Register(
                nameof(ShowTitle),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="IsMinimizeButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinimizeButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsMinimizeButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMaximizeButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMaximizeButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsMaximizeButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsCloseButtonVisible"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsCloseButtonVisibleProperty =
            DependencyProperty.Register(
                nameof(IsCloseButtonVisible),
                typeof(Visibility),
                typeof(FluenceWindow),
                new PropertyMetadata(Visibility.Visible, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMinimizable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMinimizableProperty =
            DependencyProperty.Register(
                nameof(IsMinimizable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMaximizable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMaximizableProperty =
            DependencyProperty.Register(
                nameof(IsMaximizable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsClosable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsClosableProperty =
            DependencyProperty.Register(
                nameof(IsClosable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true, OnCaptionButtonChromeOverrideChanged));

        /// <summary>
        /// Identifies the <see cref="IsMoveable"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty IsMoveableProperty =
            DependencyProperty.Register(
                nameof(IsMoveable),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true));

        /// <summary>
        /// Identifies the <see cref="HasShadow"/> dependency property.
        /// </summary>
        public static readonly DependencyProperty HasShadowProperty =
            DependencyProperty.Register(
                nameof(HasShadow),
                typeof(bool),
                typeof(FluenceWindow),
                new PropertyMetadata(defaultValue: true, OnHasShadowChanged));

        #endregion Dependency Properties

        #region Properties

        /// <summary>
        /// Gets or sets the requested system backdrop (Mica, Acrylic, Tabbed, or none).
        /// </summary>
        public BackdropType SystemBackdropType
        {
            get => (BackdropType)GetValue(SystemBackdropTypeProperty);
            set => SetValue(SystemBackdropTypeProperty, value);
        }

        /// <summary>
        /// Gets or sets the preferred window corner rounding policy for DWM.
        /// </summary>
        public CornerPreference CornerStyle
        {
            get => (CornerPreference)GetValue(CornerStyleProperty);
            set => SetValue(CornerStyleProperty, value);
        }

        /// <summary>
        /// Gets or sets extra margin applied when the window is maximized to avoid overlap with the work area.
        /// </summary>
        public Thickness MarginMaximized
        {
            get => (Thickness)GetValue(MarginMaximizedProperty);
            set => SetValue(MarginMaximizedProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window content extends into the title bar area,
        /// replacing the system title bar with a custom one rendered by the control template.
        /// </summary>
        public bool ExtendsContentIntoTitleBar
        {
            get => (bool)GetValue(ExtendsContentIntoTitleBarProperty);
            set => SetValue(ExtendsContentIntoTitleBarProperty, value);
        }

        /// <summary>
        /// Gets or sets custom content displayed in the title bar region, or <see langword="null"/> to use the default title bar.
        /// </summary>
        /// <remarks>
        /// Assigning <see langword="null"/> clears custom title-bar content. When <see cref="ExtendsContentIntoTitleBar"/>
        /// is <see langword="true"/>, the control template falls back to the built-in icon and title presentation.
        /// </remarks>
        public UIElement? TitleBar
        {
            get => (UIElement?)GetValue(TitleBarProperty);
            set => SetValue(TitleBarProperty, value);
        }

        /// <summary>
        /// Gets or sets the height of the title bar region. Standard = 48, compact = 32.
        /// </summary>
        public double TitleBarHeight
        {
            get => (double)GetValue(TitleBarHeightProperty);
            set => SetValue(TitleBarHeightProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window icon is shown in the title bar.
        /// </summary>
        public bool ShowIcon
        {
            get => (bool)GetValue(ShowIconProperty);
            set => SetValue(ShowIconProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window title text is shown in the title bar.
        /// </summary>
        public bool ShowTitle
        {
            get => (bool)GetValue(ShowTitleProperty);
            set => SetValue(ShowTitleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the minimize button.
        /// </summary>
        public Visibility IsMinimizeButtonVisible
        {
            get => (Visibility)GetValue(IsMinimizeButtonVisibleProperty);
            set => SetValue(IsMinimizeButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the maximize button.
        /// </summary>
        public Visibility IsMaximizeButtonVisible
        {
            get => (Visibility)GetValue(IsMaximizeButtonVisibleProperty);
            set => SetValue(IsMaximizeButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets the visibility of the close button.
        /// </summary>
        public Visibility IsCloseButtonVisible
        {
            get => (Visibility)GetValue(IsCloseButtonVisibleProperty);
            set => SetValue(IsCloseButtonVisibleProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the minimize button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsMinimizable
        {
            get => (bool)GetValue(IsMinimizableProperty);
            set => SetValue(IsMinimizableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the maximize button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsMaximizable
        {
            get => (bool)GetValue(IsMaximizableProperty);
            set => SetValue(IsMaximizableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the close button is enabled.
        /// When false, the button is visible but grayed out.
        /// </summary>
        public bool IsClosable
        {
            get => (bool)GetValue(IsClosableProperty);
            set => SetValue(IsClosableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window can be moved by title-bar dragging or the system move command.
        /// </summary>
        public bool IsMoveable
        {
            get => (bool)GetValue(IsMoveableProperty);
            set => SetValue(IsMoveableProperty, value);
        }

        /// <summary>
        /// Gets or sets whether the window has a drop shadow. Defaults to true.
        /// </summary>
        public bool HasShadow
        {
            get => (bool)GetValue(HasShadowProperty);
            set => SetValue(HasShadowProperty, value);
        }

        #endregion Properties

        #region Construction

        /// <summary>
        /// The Fluence brand icon embedded in this assembly, loaded once and shared (frozen) as the
        /// default <see cref="Window.Icon"/> for every <see cref="FluenceWindow"/>. <see langword="null"/>
        /// only if the embedded resource cannot be loaded. Exposed so a consumer can apply the same
        /// square, no-background brand mark to its own windows.
        /// </summary>
        public static ImageSource? DefaultIcon { get; } = CreateDefaultIcon();

        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "This is an internal resource URI.")]
        private static ImageSource? CreateDefaultIcon()
        {
            try
            {
                // The window icon (the title-bar Image via TemplateBinding Icon, and the Win32 taskbar /
                // alt-tab HICON that Window.Icon drives) is the square, no-background Fluence mark,
                // embedded as a 256x256 PNG. 256 is the largest standard Windows icon size, so the shell
                // scales the single frame down crisply. A pre-squared source avoids the aspect distortion
                // that rasterizing the non-square brand vector into a square icon rect used to introduce.
                BitmapImage icon = new();
                icon.BeginInit();
                icon.UriSource = new Uri("pack://application:,,,/Fluence.Wpf;component/Themes/Icons/Fluence_Icon_NoBackground_256.png", UriKind.Absolute);
                icon.CacheOption = BitmapCacheOption.OnLoad;
                icon.EndInit();
                if (icon.CanFreeze)
                {
                    icon.Freeze();
                }

                return icon;
            }
            // The default icon is a best-effort enhancement. Any failure to load the embedded icon
            // resource (a missing or renamed resource, or a COM / GPU / memory failure under a headless
            // or session-0 host such as PSADT running as SYSTEM) must degrade to a null icon, never
            // escape this static field initializer and fault the FluenceWindow type with a
            // TypeInitializationException - that would break every window construction in the process.
            // The when-filter is always true (Exception.Message is never null); it catches broadly while
            // satisfying the no-general-catch analyzers, matching the filtered-catch idiom used elsewhere
            // in this assembly.
            catch (Exception ex) when (ex.Message is not null)
            {
                return null;
            }
        }

        /// <summary>
        /// Overrides the default style key so WPF resolves the <see cref="FluenceWindow"/> style by
        /// type. Runs once before any instance is created.
        /// </summary>
        static FluenceWindow()
        {
            DefaultStyleKeyProperty.OverrideMetadata(
                typeof(FluenceWindow),
                new FrameworkPropertyMetadata(typeof(FluenceWindow)));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="FluenceWindow"/> class: loads the default
        /// style explicitly, attaches the four <see cref="SystemCommands"/> command bindings, and
        /// installs the <see cref="WindowChrome"/> that collapses the native frame.
        /// </summary>
        /// <remarks>
        /// The theme-manager subscriptions are intentionally deferred to
        /// <see cref="OnSourceInitialized(EventArgs)"/> (HWND realisation) so a constructed-but-never-
        /// shown window does not pin itself to the static managers' invocation lists.
        /// </remarks>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Minor Code Smell", "S1075:URIs should not be hardcoded", Justification = "This is an internal resource URI.")]
        public FluenceWindow()
        {
            // Load the standalone control-template resource dictionary explicitly so the style
            // resolves even when the consuming application has not merged this assembly's
            // Generic.xaml. The DefaultStyleKey override in the static constructor covers the
            // merged-Generic path; this covers the unmerged path.
            ResourceDictionary resourceDictionary = new()
            {
                Source = new Uri("pack://application:,,,/Fluence.Wpf;component/Themes/Controls/FluenceWindow.xaml", UriKind.Absolute),
            };
            Style = resourceDictionary[typeof(FluenceWindow)] as Style;

            // Default the window icon to the embedded Fluence brand icon. A consumer-assigned Icon
            // (XAML attribute or code) is applied after construction and overrides this default.
            if (DefaultIcon is not null)
            {
                Icon = DefaultIcon;
            }

            _ = CommandBindings.Add(new CommandBinding(SystemCommands.CloseWindowCommand, OnCloseWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.MaximizeWindowCommand, OnMaximizeWindow, OnCanResizeWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.MinimizeWindowCommand, OnMinimizeWindow, OnCanMinimizeWindow));
            _ = CommandBindings.Add(new CommandBinding(SystemCommands.RestoreWindowCommand, OnRestoreWindow, OnCanResizeWindow));

            _windowChrome = WindowPolicy.CreateWindowChrome();
            SetValue(WindowChrome.WindowChromeProperty, _windowChrome);
            UpdateWindowChrome();
            UpdateShellMetrics();
        }

        #endregion Construction

        #region Public methods

        /// <summary>
        /// Sets custom title-bar content, or clears it to restore the default title bar.
        /// </summary>
        /// <param name="titleBar">The custom title-bar element, or <see langword="null"/> to clear custom content.</param>
        public void SetTitleBar(UIElement? titleBar)
        {
            TitleBar = titleBar;
        }

        #endregion Public methods

        #region Lifecycle overrides

        /// <inheritdoc />
        public override void OnApplyTemplate()
        {
            // Be tolerant of incomplete design-time templates: missing caption parts should
            // disable only caption-button behavior rather than failing the whole window.
            base.OnApplyTemplate();
            _minimizeButton = GetTemplateChild(PART_MinimizeButton) as System.Windows.Controls.Button;
            _maximizeButton = GetTemplateChild(PART_MaximizeButton) as System.Windows.Controls.Button;
            _restoreButton = GetTemplateChild(PART_RestoreButton) as System.Windows.Controls.Button;
            _closeButton = GetTemplateChild(PART_CloseButton) as System.Windows.Controls.Button;
            UpdateCaptionButtons();
        }

        /// <inheritdoc />
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _handle = new WindowInteropHelper(this).EnsureHandle();
            _hwndSource = HwndSource.FromHwnd(_handle);
            _hwndSource?.AddHook(WndProc);
            UpdateWindowChrome();
            ApplyWindowShell();

            // Subscribe AFTER realisation (not in the constructor) so only shown windows are pinned
            // to the static managers; OnClosed unsubscribes so the lifetimes match.
            SystemThemeWatcher.Watch(this);
            ApplicationThemeManager.Changed += OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged += OnAccentColorChanged;

            // SizeToContent leaves the template root arranged one layout pass behind the realised
            // client size (see FillClientAreaForSizeToContent); correct it once the window has its
            // SizeToContent-driven size and on every subsequent SizeToContent-driven resize.
            SizeChanged += OnSizeChangedForSizeToContent;
            FillClientAreaForSizeToContent();
        }

        /// <inheritdoc />
        protected override void OnStateChanged(EventArgs e)
        {
            ClearSnapHover();
            base.OnStateChanged(e);
            UpdateShellMetrics();
            ApplyFrame();
            UpdateCaptionButtons();
        }

        /// <inheritdoc />
        protected override void OnActivated(EventArgs e)
        {
            base.OnActivated(e);
            ApplyFrame();
        }

        /// <inheritdoc />
        protected override void OnDeactivated(EventArgs e)
        {
            base.OnDeactivated(e);
            ApplyFrame();
        }

        /// <inheritdoc />
        protected override void OnPropertyChanged(DependencyPropertyChangedEventArgs e)
        {
            base.OnPropertyChanged(e);
            if (e.Property == ResizeModeProperty)
            {
                UpdateShellMetrics();
                UpdateCaptionButtons();
                CommandManager.InvalidateRequerySuggested();
            }
            if (e.Property == WindowStateProperty)
            {
                UpdateCaptionButtons();
            }
        }

        /// <inheritdoc />
        protected override void OnClosed(EventArgs e)
        {
            SystemThemeWatcher.UnWatch(this);
            ApplicationThemeManager.Changed -= OnThemeChanged;
            ApplicationAccentColorManager.AccentColorChanged -= OnAccentColorChanged;
            SizeChanged -= OnSizeChangedForSizeToContent;

            // A FromHwnd source is WPF-owned; release the hook and the reference without disposing.
            _hwndSource?.RemoveHook(WndProc);
            _hwndSource = null;
            base.OnClosed(e);
        }

        #endregion Lifecycle overrides

        #region Dependency property change callbacks

        private static void OnSystemBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                // GlassFrameThickness depends on the backdrop too (dual-path); refresh chrome.
                window.UpdateWindowChrome();
                window.ApplyBackdrop();
            }
        }

        private static void OnCornerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.ApplyCornerPreference();
            }
        }

        private static void OnExtendsContentIntoTitleBarChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
                // Caption-button slot layout depends on the chrome mode; refresh it so a runtime
                // flip between extended and non-extended chrome does not leave the buttons stale.
                window.UpdateCaptionButtons();
            }
        }

        private static void OnTitleBarHeightChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnHasShadowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateWindowChrome();
            }
        }

        private static void OnCaptionButtonChromeOverrideChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is FluenceWindow window)
            {
                window.UpdateCaptionButtons();
                CommandManager.InvalidateRequerySuggested();
            }
        }

        #endregion Dependency property change callbacks

        #region Theme and accent manager handlers

        private void OnThemeChanged(object? sender, ThemeChangedEventArgs e)
        {
            // Only ApplyBackdrop here. Every theme change runs through the engine, which raises
            // AccentColorChanged (handled by OnAccentColorChanged -> ApplyFrame) before
            // ApplicationThemeManager raises Changed, so the frame is already refreshed by the time
            // this fires. Re-running ApplyFrame would redundantly re-issue the DWM border P/Invoke
            // and rebuild the border brush on every apply. Accent-only changes still drive ApplyFrame
            // via OnAccentColorChanged.
            if (!Dispatcher.CheckAccess())
            {
                _ = Dispatcher.BeginInvoke(new Action(ApplyBackdrop));
                return;
            }
            ApplyBackdrop();
        }

        private void OnAccentColorChanged(object? sender, EventArgs e)
        {
            if (!Dispatcher.CheckAccess())
            {
                _ = Dispatcher.BeginInvoke(new Action(ApplyFrame));
                return;
            }
            ApplyFrame();
        }

        #endregion Theme and accent manager handlers

        #region Window shell (chrome, backdrop, corners, frame)

        /// <summary>
        /// Applies the full native shell in the order required to avoid the first-paint flash: strip
        /// the native caption buttons, refresh the resize metrics, apply the backdrop (including the
        /// redirection-surface fix), set the corner preference, then paint the frame.
        /// </summary>
        private void ApplyWindowShell()
        {
            if (_handle != IntPtr.Zero)
            {
                HideNativeCaptionButtons();
                UpdateShellMetrics();
                ApplyBackdrop();
                ApplyCornerPreference();
                ApplyFrame();
            }
        }

        /// <summary>
        /// Strips the native window buttons (and <c>WS_SYSMENU</c>) so the OS caption never paints
        /// over the custom chrome.
        /// </summary>
        private void HideNativeCaptionButtons()
        {
            if (_handle != IntPtr.Zero)
            {
                NativeMethods.HideAllWindowButtons(_handle);
            }
        }

        /// <summary>
        /// Refreshes the <see cref="WindowChrome"/> values that do not depend on window state:
        /// caption height (always 0), aero caption buttons (always off), and the dual-path glass
        /// frame thickness.
        /// </summary>
        private void UpdateWindowChrome()
        {
            _windowChrome.CaptionHeight = 0;
            _windowChrome.UseAeroCaptionButtons = false;
            // GlassFrameThickness must reflect both the requested backdrop and shadow policy.
            // When SystemBackdropType is None and HasShadow is false, an unconditional -1
            // glass frame paints a visible artifact on Windows 11; using 0.00001 keeps the
            // resize border alive without that artifact. See WindowPolicy.GetGlassFrameThickness.
            _windowChrome.GlassFrameThickness = WindowPolicy.GetGlassFrameThickness(SystemBackdropType, HasShadow);
        }

        /// <summary>
        /// Refreshes the state-dependent shell metrics: the maximized inset margin and the resize
        /// border thickness.
        /// </summary>
        private void UpdateShellMetrics()
        {
            MarginMaximized = WindowState is WindowState.Maximized ? new Thickness(6) : new Thickness(0);
            _windowChrome.ResizeBorderThickness = WindowPolicy.GetResizeBorderThickness(WindowState, ResizeMode);
        }

        /// <summary>
        /// Applies the resolved backdrop plan to both WPF background layers and the DWM, including
        /// the redirection-surface fix that eliminates the first-paint black flash.
        /// </summary>
        private void ApplyBackdrop()
        {
            WindowCapabilities capabilities = WindowCapabilities.Current;
            BackdropPlan plan = WindowPolicy.BuildBackdropPlan(
                SystemBackdropType,
                ApplicationThemeManager.GetResolvedTheme(),
                capabilities,
                GetFallbackBackgroundColor());

            SolidColorBrush backgroundBrush = new(plan.BackgroundColor);
            backgroundBrush.Freeze();
            Background = backgroundBrush;

            // A top-level WPF window has two background layers: the WPF-painted content background
            // (Window.Background, set above) and the HWND redirection surface that WPF clears behind
            // that content (HwndTarget.BackgroundColor), which defaults to opaque black. With an
            // active DWM backdrop the content background is transparent, so the default-black
            // redirection surface is what flashes before the system backdrop composites (the
            // first-paint "black flash"). Clearing the redirection surface to the same color as the
            // content background (transparent for an active backdrop, the opaque theme fallback for
            // None) lets the DWM backdrop show through from the first composed frame, which is why
            // the reference Fluent window libraries need no first-paint cloak. Mirrors the WPF-UI
            // WindowBackdrop.RemoveBackground flow.
            if (_hwndSource?.CompositionTarget is not null)
            {
                _hwndSource.CompositionTarget.BackgroundColor = plan.BackgroundColor;
            }
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            if (capabilities.SupportsCaptionColor)
            {
                _ = NativeMethods.SetCaptionColor(_handle, plan.CaptionColor);
            }
            _ = NativeMethods.SetImmersiveDarkMode(_handle, plan.UseImmersiveDarkMode);
            // Suppress Win32 default caption painting so the DWM backdrop shows through cleanly.
            // Best-effort: returns S_FALSE on classic themes and is treated as a no-op.
            // Mirrors the Fischless ApplyBackdrop flow (WTA_NONCLIENT + WTNCA_NODRAWCAPTION).
            _ = NativeMethods.SuppressNonClientCaptionDraw(_handle);
            if (capabilities.SupportsSystemBackdropType)
            {
                _ = NativeMethods.SetSystemBackdropType(
                    _handle,
                    plan.SystemBackdropType ?? NativeConstants.DWMSBT_AUTO);
            }
            if (capabilities.SupportsMicaEffect)
            {
                _ = NativeMethods.SetMicaEffect(_handle, plan.UseLegacyMicaEffect);
            }
        }

        /// <summary>
        /// Applies the template border brush and the DWM border color for the current activation and
        /// window state. Called on activation, deactivation, state change, and accent change.
        /// </summary>
        private void ApplyFrame()
        {
            WindowCapabilities capabilities = WindowCapabilities.Current;
            FramePlan plan = WindowPolicy.BuildFramePlan(
                WindowState,
                IsActive,
                ApplicationAccentColorManager.IsAccentColorOnTitleBarsEnabled,
                capabilities,
                ApplicationAccentColorManager.SystemAccentColor);

            BorderBrush = TryFindResource(plan.TemplateBorderBrushResourceKey) as Brush ?? Brushes.Transparent;
            if (_handle != IntPtr.Zero && capabilities.SupportsBorderColor)
            {
                _ = NativeMethods.SetBorderColor(_handle, plan.DwmBorderColor);
            }
        }

        /// <summary>
        /// Applies the DWM corner preference when the OS supports rounded corners.
        /// </summary>
        private void ApplyCornerPreference()
        {
            if (_handle == IntPtr.Zero)
            {
                return;
            }

            WindowCapabilities capabilities = WindowCapabilities.Current;
            if (!capabilities.SupportsRoundedCorners)
            {
                return;
            }
            _ = NativeMethods.SetWindowCornerPreference(_handle, WindowPolicy.GetCornerPreference(CornerStyle));
        }

        /// <summary>
        /// Resolves the opaque background color used when no DWM backdrop is active, picked from the
        /// resolved theme.
        /// </summary>
        private static Color GetFallbackBackgroundColor()
        {
            ApplicationTheme resolvedTheme = ApplicationThemeManager.GetResolvedTheme();
            return resolvedTheme is ApplicationTheme.Dark
                ? Color.FromRgb(0x20, 0x20, 0x20)
                : resolvedTheme is ApplicationTheme.HighContrast
                ? SystemColors.WindowColor
                : Color.FromRgb(0xFA, 0xFA, 0xFA);
        }

        #endregion Window shell (chrome, backdrop, corners, frame)

        #region SizeToContent client-area fill

        /// <summary>
        /// <see cref="FrameworkElement.SizeChanged"/> handler that re-runs the SizeToContent
        /// client-area fill on every size change while <see cref="Window.SizeToContent"/> is active.
        /// </summary>
        /// <param name="sender">The event source.</param>
        /// <param name="e">The size-changed payload (unused).</param>
        private void OnSizeChangedForSizeToContent(object sender, SizeChangedEventArgs e)
        {
            FillClientAreaForSizeToContent();
        }

        /// <summary>
        /// Forces the template root visual to fill the realised client area when
        /// <see cref="Window.SizeToContent"/> is active.
        /// </summary>
        /// <remarks>
        /// A <see cref="Window"/> sizes its HWND to the latest content-desired size, but on a
        /// SizeToContent-driven resize the root visual's arrange lags one layout pass behind the new
        /// client size: the HWND (and <see cref="FrameworkElement.ActualWidth"/> /
        /// <see cref="FrameworkElement.ActualHeight"/>) already reflect the grown size while the
        /// template root <c>Border</c> is still arranged to the previous, smaller desired size. The
        /// gap reads as a rounded accent border floating inside the DWM border (set via
        /// <c>DWMWA_BORDER_COLOR</c>) on every edge, because the template border and the DWM border no
        /// longer coincide. An interactive resize hides it because it ends with a real <c>WM_SIZE</c>
        /// that re-arranges the content; a SizeToContent first paint or auto-grow never produces that
        /// <c>WM_SIZE</c>.
        /// <para>
        /// The correction directly arranges the single visual child to a rect of the window's current
        /// <see cref="FrameworkElement.ActualWidth"/> x <see cref="FrameworkElement.ActualHeight"/>
        /// (which equal the client area in DIPs), reproducing the re-arrange a real <c>WM_SIZE</c>
        /// would trigger without freezing <see cref="Window.SizeToContent"/> - so the window still
        /// grows when its content grows and stays single-bordered after growing. The
        /// <c>SizeToContent != Manual</c> guard makes it a no-op for fixed-size windows, which already
        /// render with the borders coincident, and a re-entrancy guard prevents the child arrange from
        /// recursing through <see cref="FrameworkElement.SizeChanged"/>.
        /// </para>
        /// </remarks>
        private void FillClientAreaForSizeToContent()
        {
            if (SizeToContent is SizeToContent.Manual || _isFillingClientArea)
            {
                return;
            }

            if (VisualChildrenCount is 0 || GetVisualChild(0) is not UIElement child)
            {
                return;
            }

            double width = ActualWidth;
            double height = ActualHeight;
            if (width <= 0.0 || height <= 0.0)
            {
                return;
            }

            // Already filling the client area: the child's arranged size already spans it. Skip to
            // avoid a redundant arrange pass (and the SizeChanged recursion it would otherwise risk).
            // A sub-pixel tolerance absorbs the layout-rounding error between the DIP client size and
            // the child's arranged render size.
            const double tolerance = 0.5;
            Size arranged = child.RenderSize;
            if (Math.Abs(arranged.Width - width) <= tolerance && Math.Abs(arranged.Height - height) <= tolerance)
            {
                return;
            }

            _isFillingClientArea = true;
            try
            {
                // Re-arrange the root visual to the full client area. This mirrors the re-arrange a
                // real WM_SIZE performs, collapsing the inset so the template border coincides with
                // the DWM border. SizeToContent stays active for the next content change.
                child.Arrange(new Rect(0.0, 0.0, width, height));
            }
            finally
            {
                _isFillingClientArea = false;
            }
        }

        #endregion SizeToContent client-area fill

        #region Caption-button reflow

        /// <summary>
        /// Recomputes the visibility and enabled state of all four caption buttons by blending the
        /// <see cref="CaptionButtonChrome"/> baselines (derived from <see cref="Window.ResizeMode"/>
        /// and <see cref="Window.WindowState"/>) with any explicitly-set caption-chrome DP overrides,
        /// then reflows the button slots so collapsed buttons leave no gap.
        /// </summary>
        private void UpdateCaptionButtons()
        {
            if (_minimizeButton is null || _maximizeButton is null || _restoreButton is null || _closeButton is null)
            {
                return;
            }

            // Minimize: an explicit IsMinimizeButtonVisible (e.g. flipped back to Visible under
            // ResizeMode=NoResize) wins over the ResizeMode-derived baseline; otherwise keep the
            // chrome defaults. IsMinimizable gates the enabled state.
            CaptionButtonChrome.GetMinimizeChrome(ResizeMode, out Visibility minimizeVisibility, out bool minimizeEnabled);
            if (IsCaptionChromeOverrideExplicit(IsMinimizeButtonVisibleProperty))
            {
                minimizeVisibility = IsMinimizeButtonVisible;
                minimizeEnabled = minimizeVisibility is Visibility.Visible;
            }
            if (!IsMinimizable)
            {
                minimizeEnabled = false;
            }
            _minimizeButton.Visibility = minimizeVisibility;
            _minimizeButton.IsEnabled = minimizeEnabled;

            // Maximize / restore: the pair shares one slot; the visible member tracks WindowState.
            CaptionButtonChrome.GetMaximizeRestoreChrome(
                ResizeMode,
                WindowState,
                out Visibility maxVis,
                out Visibility restVis,
                out bool maxEn,
                out bool restEn);
            if (IsCaptionChromeOverrideExplicit(IsMaximizeButtonVisibleProperty))
            {
                ApplyMaximizeRestoreVisibilityOverride(IsMaximizeButtonVisible, out maxVis, out restVis);
                bool explicitlyVisible = IsMaximizeButtonVisible is Visibility.Visible;
                maxEn = explicitlyVisible && WindowState is not WindowState.Maximized;
                restEn = explicitlyVisible && WindowState is WindowState.Maximized;
            }
            if (!IsMaximizable)
            {
                maxEn = false;
                restEn = false;
            }
            _maximizeButton.Visibility = maxVis;
            _restoreButton.Visibility = restVis;
            _maximizeButton.IsEnabled = maxEn;
            _restoreButton.IsEnabled = restEn;

            // Close: always present from the chrome baseline; an explicit override or IsClosable=false
            // can hide or disable it.
            CaptionButtonChrome.GetCloseChrome(out Visibility closeVisibility, out bool closeEnabled);
            if (IsCaptionChromeOverrideExplicit(IsCloseButtonVisibleProperty))
            {
                closeVisibility = IsCloseButtonVisible;
                closeEnabled = closeVisibility is Visibility.Visible;
            }
            if (!IsClosable)
            {
                closeEnabled = false;
            }
            _closeButton.Visibility = closeVisibility;
            _closeButton.IsEnabled = closeEnabled;

            UpdateCaptionButtonSlots(minimizeVisibility, maxVis, restVis, closeVisibility);
        }

        /// <summary>
        /// Re-assigns the caption buttons' grid columns so collapsed buttons leave no gap. Close is
        /// always anchored to the rightmost slot; the maximize/restore pair and minimize fill the
        /// remaining slots from the right.
        /// </summary>
        /// <param name="minimizeVisibility">The visibility of the minimize button.</param>
        /// <param name="maximizeVisibility">The visibility of the maximize button.</param>
        /// <param name="restoreVisibility">The visibility of the restore button.</param>
        /// <param name="closeVisibility">The visibility of the close button.</param>
        private void UpdateCaptionButtonSlots(
            Visibility minimizeVisibility,
            Visibility maximizeVisibility,
            Visibility restoreVisibility,
            Visibility closeVisibility)
        {
            bool maximizeOccupiesSlot = maximizeVisibility is not Visibility.Collapsed || restoreVisibility is not Visibility.Collapsed;
            bool minimizeOccupiesSlot = minimizeVisibility is not Visibility.Collapsed;
            bool closeOccupiesSlot = closeVisibility is not Visibility.Collapsed;

            Grid.SetColumn(_closeButton, 2);
            int nextSlot = 2;
            if (closeOccupiesSlot)
            {
                nextSlot = 1;
            }

            int maximizeSlot = maximizeOccupiesSlot ? nextSlot : 1;
            Grid.SetColumn(_maximizeButton, maximizeSlot);
            Grid.SetColumn(_restoreButton, maximizeSlot);
            if (maximizeOccupiesSlot)
            {
                nextSlot--;
            }

            if (minimizeOccupiesSlot)
            {
                Grid.SetColumn(_minimizeButton, Math.Max(0, nextSlot));
            }
            else
            {
                Grid.SetColumn(_minimizeButton, 0);
            }
        }

        /// <summary>
        /// Translates an explicit <see cref="IsMaximizeButtonVisible"/> value into the concrete
        /// maximize / restore visibilities for the current window state (only one of the pair is ever
        /// shown).
        /// </summary>
        /// <param name="visibility">The explicit visibility value to apply.</param>
        /// <param name="maximizeVisibility">The resulting visibility of the maximize button.</param>
        /// <param name="restoreVisibility">The resulting visibility of the restore button.</param>
        private void ApplyMaximizeRestoreVisibilityOverride(Visibility visibility, out Visibility maximizeVisibility, out Visibility restoreVisibility)
        {
            if (visibility is Visibility.Visible)
            {
                maximizeVisibility = WindowState is WindowState.Maximized ? Visibility.Collapsed : Visibility.Visible;
                restoreVisibility = WindowState is WindowState.Maximized ? Visibility.Visible : Visibility.Collapsed;
                return;
            }
            if (visibility is Visibility.Hidden)
            {
                maximizeVisibility = WindowState is WindowState.Maximized ? Visibility.Collapsed : Visibility.Hidden;
                restoreVisibility = WindowState is WindowState.Maximized ? Visibility.Hidden : Visibility.Collapsed;
                return;
            }
            maximizeVisibility = Visibility.Collapsed;
            restoreVisibility = Visibility.Collapsed;
        }

        /// <summary>
        /// Returns <see langword="true"/> when the caption-chrome override property has been explicitly assigned
        /// (via code, XAML local value, style, binding, etc.) rather than left at its declared default.
        /// </summary>
        /// <param name="dp">The dependency property to check.</param>
        /// <returns><see langword="true"/> if the property has been explicitly assigned; otherwise, <see langword="false"/>.</returns>
        private bool IsCaptionChromeOverrideExplicit(DependencyProperty dp)
        {
            ValueSource source = DependencyPropertyHelper.GetValueSource(this, dp);
            return source.BaseValueSource is not BaseValueSource.Default and not BaseValueSource.Inherited;
        }

        #endregion Caption-button reflow

        #region WndProc and native hit-testing

        /// <summary>
        /// Win32 message hook. Handles the non-client messages required to drive the custom caption
        /// (hit-testing, snap-layout hover, move suppression, monitor clamp, direct maximize/restore).
        /// All other messages are left to WPF / <see cref="WindowChrome"/>.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="msg">The message identifier.</param>
        /// <param name="wParam">The message parameter.</param>
        /// <param name="lParam">The message parameter.</param>
        /// <param name="handled">Indicates whether the message was handled.</param>
        /// <returns>The result of the message processing.</returns>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == NativeConstants.WM_NCHITTEST)
            {
                // WindowChrome routes the whole title bar through WM_NCHITTEST. We return
                // HTCAPTION for drag regions, HTMAXBUTTON for Windows 11 snap-layout hover,
                // and 0 for WPF-controlled buttons or interactive custom title-bar content.
                int result = HitTestTitleBar(lParam);
                if (result == NativeConstants.HTMAXBUTTON)
                {
                    SetSnapHover(WindowState is WindowState.Maximized ? _restoreButton : _maximizeButton);
                }
                else
                {
                    ClearSnapHover();
                }
                if (result is not 0)
                {
                    handled = true;
                    return new IntPtr(result);
                }
            }
            else if (msg == NativeConstants.WM_NCMOUSELEAVE)
            {
                ClearSnapHover();
            }
            else if (msg == NativeConstants.WM_SYSCOMMAND && (wParam.ToInt64() & 0xFFF0L) == NativeConstants.SC_MOVE && !IsMoveable)
            {
                handled = true;
            }
            else if (msg == NativeConstants.WM_GETMINMAXINFO)
            {
                HandleGetMinMaxInfo(hwnd, lParam, ref handled);
            }
            else if (msg == NativeConstants.WM_NCLBUTTONUP && wParam.ToInt32() == NativeConstants.HTMAXBUTTON)
            {
                HandleMaxButtonClick(ref handled);
            }
            return IntPtr.Zero;
        }

        /// <summary>
        /// Clamps the maximized rectangle to the nearest monitor's work area in response to
        /// <c>WM_GETMINMAXINFO</c>: positions/sizes to the work area, applies the auto-hide-taskbar
        /// shift when the work area covers the whole monitor, and enforces the window's
        /// Min/Max width/height on the native resize track (DPI-scaled). Marks the message handled so
        /// the clamped values are honoured.
        /// </summary>
        /// <param name="hwnd">The window handle.</param>
        /// <param name="lParam">The message parameter.</param>
        /// <param name="handled">Indicates whether the message was handled.</param>
        private void HandleGetMinMaxInfo(IntPtr hwnd, IntPtr lParam, ref bool handled)
        {
            IntPtr monitor = NativeMethods.MonitorFromWindow(hwnd, NativeConstants.MONITOR_DEFAULTTONEAREST);
            if (monitor == IntPtr.Zero)
            {
                return;
            }

            MONITORINFO monitorInfo = new() { cbSize = Marshal.SizeOf<MONITORINFO>() };
            if (!NativeMethods.GetMonitorInfo(monitor, ref monitorInfo))
            {
                return;
            }

            RECT rcWork = monitorInfo.rcWork;
            RECT rcMonitor = monitorInfo.rcMonitor;
            MINMAXINFO mmi = Marshal.PtrToStructure<MINMAXINFO>(lParam);
            mmi.ptMaxPosition.X = rcWork.Left - rcMonitor.Left;
            mmi.ptMaxPosition.Y = rcWork.Top - rcMonitor.Top;
            mmi.ptMaxSize.X = rcWork.Width;
            mmi.ptMaxSize.Y = rcWork.Height;

            bool workAreaCoversMonitor =
                rcWork.Left == rcMonitor.Left &&
                rcWork.Top == rcMonitor.Top &&
                rcWork.Right == rcMonitor.Right &&
                rcWork.Bottom == rcMonitor.Bottom;
            if (workAreaCoversMonitor && NativeMethods.GetAutoHideTaskbarEdge(monitor) is uint autoHideEdge)
            {
                NativeMethods.ApplyAutoHideTaskbarShift(ref mmi, autoHideEdge);
            }

            double dpiX = 1.0, dpiY = 1.0;
            if (_hwndSource?.CompositionTarget is not null)
            {
                Matrix transform = _hwndSource.CompositionTarget.TransformToDevice;
                dpiX = transform.M11;
                dpiY = transform.M22;
            }

            // Respect MaxWidth/MaxHeight if set on the window.
            if (!double.IsPositiveInfinity(MaxWidth) || !double.IsPositiveInfinity(MaxHeight))
            {
                if (!double.IsPositiveInfinity(MaxWidth))
                {
                    int maxWidthPx = (int)(MaxWidth * dpiX);
                    if (maxWidthPx < mmi.ptMaxSize.X)
                    {
                        mmi.ptMaxSize.X = maxWidthPx;
                    }
                    mmi.ptMaxTrackSize.X = maxWidthPx;
                }
                if (!double.IsPositiveInfinity(MaxHeight))
                {
                    int maxHeightPx = (int)(MaxHeight * dpiY);
                    if (maxHeightPx < mmi.ptMaxSize.Y)
                    {
                        mmi.ptMaxSize.Y = maxHeightPx;
                    }
                    mmi.ptMaxTrackSize.Y = maxHeightPx;
                }
            }

            // Enforce MinWidth/MinHeight on native resize track (handled=true bypasses WPF defaults).
            if (MinWidth > 0)
            {
                int minWidthPx = (int)Math.Ceiling(MinWidth * dpiX);
                if (minWidthPx > mmi.ptMinTrackSize.X)
                {
                    mmi.ptMinTrackSize.X = minWidthPx;
                }
            }
            if (MinHeight > 0)
            {
                int minHeightPx = (int)Math.Ceiling(MinHeight * dpiY);
                if (minHeightPx > mmi.ptMinTrackSize.Y)
                {
                    mmi.ptMinTrackSize.Y = minHeightPx;
                }
            }

            Marshal.StructureToPtr(mmi, lParam, fDeleteOld: false);
            handled = true;
        }

        /// <summary>
        /// Handles a click released over the snap-layout max/restore hit area
        /// (<c>WM_NCLBUTTONUP</c> with <c>HTMAXBUTTON</c>) by toggling the window state through the
        /// same direct path as the command handlers and refreshing the caption buttons.
        /// </summary>
        /// <param name="handled">Indicates whether the click was handled.</param>
        private void HandleMaxButtonClick(ref bool handled)
        {
            ClearSnapHover();
            if (WindowState is WindowState.Maximized)
            {
                if (_restoreButton?.Visibility is Visibility.Visible && _restoreButton.IsEnabled)
                {
                    handled = true;
                    RestoreWindowDirect();
                }
            }
            else if (_maximizeButton?.Visibility is Visibility.Visible && _maximizeButton.IsEnabled)
            {
                handled = true;
                MaximizeWindowDirect();
            }
        }

        /// <summary>
        /// Maps a screen-space <c>WM_NCHITTEST</c> point to a hit-test result for the custom caption.
        /// Resolution order: the top resize band wins first; then the Windows 11 snap-layout flyout
        /// over the max/restore button (<c>HTMAXBUTTON</c>); minimize and close fall through to 0 so
        /// the WPF buttons fire; the remaining title-bar area returns <c>HTCAPTION</c> for dragging
        /// unless it is over interactive content or the window is not moveable.
        /// </summary>
        /// <param name="lParam">The message parameter containing the screen-space point.</param>
        private int HitTestTitleBar(IntPtr lParam)
        {
            long lParamValue = lParam.ToInt64();
            int x = unchecked((short)(lParamValue & 0xFFFF));
            int y = unchecked((short)((lParamValue >> 16) & 0xFFFF));
            Point point = PointFromScreen(new(x, y));

            if (TryGetTopResizeHit(point, out int resizeHit))
            {
                return resizeHit;
            }

            if (point.Y < 0 || point.Y > TitleBarHeight)
            {
                return 0;
            }

            // HTMAXBUTTON is what triggers the Windows 11 Snap Layout flyout on hover.
            // Returning it unconditionally surfaces the flyout even when the user has disabled
            // it in Settings (HKCU\...\Explorer\Advanced\EnableSnapAssistFlyout=0), or on
            // windows whose IsMaximizable=false, or on Windows 10 where the flyout doesn't
            // exist at all. In those cases, fall through to 0 so WPF input routing handles
            // the click as a normal button press.
            bool shouldExposeSnapFlyout = SnapLayoutHelper.IsSnapLayoutEnabled()
                && IsMaximizable
                && OsVersionHelper.IsWindows11;
            if (_maximizeButton?.Visibility is Visibility.Visible &&
                _maximizeButton.IsEnabled &&
                IsOverElement(_maximizeButton, point))
            {
                return shouldExposeSnapFlyout ? NativeConstants.HTMAXBUTTON : 0;
            }
            if (_restoreButton?.Visibility is Visibility.Visible &&
                _restoreButton.IsEnabled &&
                IsOverElement(_restoreButton, point))
            {
                return shouldExposeSnapFlyout ? NativeConstants.HTMAXBUTTON : 0;
            }

            // Minimize and close: return 0 so hit falls through to client area; WPF Button + Command fire.
            if ((_minimizeButton?.Visibility is Visibility.Visible &&
                IsOverElement(_minimizeButton, point)) ||
                (_closeButton?.Visibility is Visibility.Visible &&
                IsOverElement(_closeButton, point)))
            {
                return 0;
            }

            // If a custom-content child marked with IsHitTestVisibleInChrome=True is under the
            // cursor (e.g. a search TextBox or ToggleSwitch in the TitleBar content area), return
            // HTCLIENT so Windows passes the click to WPF rather than treating it as a drag.
            //
            // Fast path: when there is no custom TitleBar slot and content does not extend into the
            // caption band, nothing interactive can sit under the cursor here, so short-circuit
            // (&&) skips the InputHitTest tree-walk that WM_NCHITTEST would otherwise run on every
            // mouse move.
            bool overInteractiveContent = (TitleBar is not null || ExtendsContentIntoTitleBar) && IsOverInteractiveContent(point);
            return !overInteractiveContent && IsMoveable ? NativeConstants.HTCAPTION : 0;
        }

        /// <summary>
        /// Resolves whether <paramref name="point"/> falls in the top resize band and, if so, which
        /// resize hit (<c>HTTOP</c>, <c>HTTOPLEFT</c>, or <c>HTTOPRIGHT</c>) applies. The band is
        /// suppressed when the window is maximized or not resizable.
        /// </summary>
        /// <param name="point">The point to test, in window coordinates.</param>
        /// <param name="hit">The resulting resize hit, if any.</param>
        /// <returns><see langword="true"/> if the point falls in the top resize band; otherwise, <see langword="false"/>.</returns>
        private bool TryGetTopResizeHit(Point point, out int hit)
        {
            hit = 0;
            if (WindowState is WindowState.Maximized ||
                ResizeMode is ResizeMode.NoResize or ResizeMode.CanMinimize)
            {
                return false;
            }

            Thickness resizeBorder = _windowChrome.ResizeBorderThickness;
            if (resizeBorder.Top <= 0.0 || point.Y < 0.0 || point.Y > resizeBorder.Top)
            {
                return false;
            }

            double leftCornerWidth = Math.Max(resizeBorder.Left, resizeBorder.Top);
            double rightCornerWidth = Math.Max(resizeBorder.Right, resizeBorder.Top);
            hit = point.X <= leftCornerWidth
                ? NativeConstants.HTTOPLEFT
                : point.X >= ActualWidth - rightCornerWidth
                    ? NativeConstants.HTTOPRIGHT
                    : NativeConstants.HTTOP;

            return true;
        }

        /// <summary>
        /// Applies a synthetic hover visual to the given snap-layout button. NC mouse messages bypass
        /// WPF input routing, so the PointerOver state is driven manually via resource references so
        /// it tracks theme/accent changes.
        /// </summary>
        /// <param name="button">The snap-layout button to apply the hover visual to.</param>
        private void SetSnapHover(System.Windows.Controls.Button? button)
        {
            if (_snapHoveredButton == button)
            {
                return;
            }

            ClearSnapHover();
            if ((button?.IsEnabled) is true)
            {
                // Use resource references (not a TryFindResource snapshot) so the snap-hover colors
                // track theme/accent/high-contrast changes and mirror the WindowButtonStyle
                // PointerOver visual state (subtle fill). ClearSnapHover restores the template/style
                // defaults via ClearValue.
                button.SetResourceReference(BackgroundProperty, "SubtleFillColorSecondaryBrush");
                button.SetResourceReference(ForegroundProperty, "TextFillColorPrimaryBrush");
                _snapHoveredButton = button;
            }
        }

        /// <summary>
        /// Clears the synthetic snap-layout hover visual, restoring the button's template/style
        /// defaults via <see cref="DependencyObject.ClearValue(DependencyProperty)"/>.
        /// </summary>
        private void ClearSnapHover()
        {
            if (_snapHoveredButton is not null)
            {
                _snapHoveredButton.ClearValue(BackgroundProperty);
                _snapHoveredButton.ClearValue(ForegroundProperty);
                _snapHoveredButton = null;
            }
        }

        /// <summary>
        /// Returns <see langword="true"/> when <paramref name="windowPoint"/> (window-space) falls within the
        /// rendered bounds of <paramref name="element"/>.
        /// </summary>
        /// <param name="element">The element to test.</param>
        /// <param name="windowPoint">The point to test, in window coordinates.</param>
        /// <returns><see langword="true"/> if the point falls within the element's bounds; otherwise, <see langword="false"/>.</returns>
        private bool IsOverElement(UIElement element, Point windowPoint)
        {
            if (element is null || element.Visibility is not Visibility.Visible)
            {
                return false;
            }
            Point topLeft = element.TranslatePoint(new Point(0, 0), this);
            Size size = element.RenderSize;
            Rect rect = new(topLeft, size);
            return rect.Contains(windowPoint);
        }

        /// <summary>
        /// Returns <see langword="true"/> when the element under <paramref name="windowPoint"/> (or any of its
        /// visual ancestors) has <see cref="WindowChrome.IsHitTestVisibleInChromeProperty"/> set to
        /// <see langword="true"/>.  Used by <see cref="HitTestTitleBar"/> to let clicks on interactive controls
        /// inside the title bar (e.g. a search TextBox or ToggleSwitch) fall through to WPF instead
        /// of being swallowed as caption-area drag gestures.
        /// </summary>
        /// <param name="windowPoint">The point to test, in window coordinates.</param>
        private bool IsOverInteractiveContent(Point windowPoint)
        {
            DependencyObject? hit = InputHitTest(windowPoint) as DependencyObject;
            while (hit is not null)
            {
                if (hit is IInputElement element && WindowChrome.GetIsHitTestVisibleInChrome(element))
                {
                    return true;
                }

                // ContentElement (e.g. Run, Hyperlink) is not a Visual; VisualTreeHelper.GetParent
                // would throw InvalidOperationException. Walk content/logical tree until we reach
                // a Visual, then continue up the visual tree.
                hit = hit switch
                {
                    Visual or System.Windows.Media.Media3D.Visual3D => VisualTreeHelper.GetParent(hit),
                    FrameworkContentElement fce => fce.Parent ?? LogicalTreeHelper.GetParent(fce),
                    ContentElement ce => ContentOperations.GetParent(ce) ?? LogicalTreeHelper.GetParent(ce),
                    _ => LogicalTreeHelper.GetParent(hit),
                };
            }
            return false;
        }

        #endregion WndProc and native hit-testing

        #region Command handlers

        private void OnCanResizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            bool allowedByResizeMode =
                ResizeMode is ResizeMode.CanResize or
                ResizeMode.CanResizeWithGrip;
            bool allowedByExplicitDp =
                IsCaptionChromeOverrideExplicit(IsMaximizeButtonVisibleProperty) &&
                IsMaximizeButtonVisible is Visibility.Visible;
            e.CanExecute = (allowedByResizeMode || allowedByExplicitDp) && IsMaximizable;
        }

        private void OnCanMinimizeWindow(object sender, CanExecuteRoutedEventArgs e)
        {
            bool allowedByResizeMode = ResizeMode is not ResizeMode.NoResize;
            bool allowedByExplicitDp =
                IsCaptionChromeOverrideExplicit(IsMinimizeButtonVisibleProperty) &&
                IsMinimizeButtonVisible is Visibility.Visible;
            e.CanExecute = (allowedByResizeMode || allowedByExplicitDp) && IsMinimizable;
        }

        private void OnCloseWindow(object sender, ExecutedRoutedEventArgs e)
        {
            SystemCommands.CloseWindow(this);
        }

        // Note: Maximize/Minimize/Restore are driven by setting WindowState directly
        // rather than via SystemCommands.*Window, which post WM_SYSCOMMAND. DefWindowProc
        // gates SC_MINIMIZE on WS_SYSMENU + WS_MINIMIZEBOX (and SC_MAXIMIZE on
        // WS_MAXIMIZEBOX); those bits are intentionally stripped by
        // NativeMethods.HideAllWindowButtons so the native caption does not paint over the
        // custom chrome, and they are also stripped by WPF whenever ResizeMode is
        // ResizeMode.NoResize (the XAML baseline for every PSADT fluent dialog). If we
        // routed through WM_SYSCOMMAND the messages would be silently dropped and the
        // caption buttons would appear clickable but do nothing. Assigning WindowState
        // uses ShowWindow under the hood, which honours the requested state regardless of
        // sysmenu/style gating and keeps the custom caption authoritative.
        //
        // Belt-and-braces: we also call NativeMethods.{Minimize/Maximize/Restore}WindowNative
        // after the WPF assignment. These perform a direct ShowWindow() call on the HWND.
        // ShowWindow() is not gated by window styles, modal dispatcher state, Topmost, or
        // ShowInTaskbar, so the caption button remains functional even in niche scenarios
        // where WPF's WindowStateProperty change handler's internal ShowWindow might not
        // reach the native window (for example if _hwndSource is transiently unavailable
        // mid-activation, or if a third-party WndProc hook mutates WM_SIZE/WM_WINDOWPOSCHANGING
        // replies). When the native window is already in the requested state, the helpers
        // short-circuit via IsIconic/IsZoomed so there is no double-transition.
        private void OnMaximizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            MaximizeWindowDirect();
        }

        private void OnMinimizeWindow(object sender, ExecutedRoutedEventArgs e)
        {
            WindowState = WindowState.Minimized;
            if (_handle != IntPtr.Zero)
            {
                _ = NativeMethods.MinimizeWindowNative(_handle);
            }
        }

        private void OnRestoreWindow(object sender, ExecutedRoutedEventArgs e)
        {
            RestoreWindowDirect();
        }

        /// <summary>
        /// Maximizes the window through the direct <see cref="Window.WindowState"/> path (plus a
        /// native <c>ShowWindow</c> belt-and-braces call) and refreshes the caption buttons.
        /// </summary>
        private void MaximizeWindowDirect()
        {
            WindowState = WindowState.Maximized;
            UpdateCaptionButtons();
            if (_handle != IntPtr.Zero)
            {
                _ = NativeMethods.MaximizeWindowNative(_handle);
            }
        }

        /// <summary>
        /// Restores the window through the direct <see cref="Window.WindowState"/> path (plus a
        /// native <c>ShowWindow</c> belt-and-braces call) and refreshes the caption buttons.
        /// </summary>
        private void RestoreWindowDirect()
        {
            WindowState = WindowState.Normal;
            UpdateCaptionButtons();
            if (_handle != IntPtr.Zero)
            {
                _ = NativeMethods.RestoreWindowNative(_handle);
            }
        }

        #endregion Command handlers

        #region Fields

        /// <summary>
        /// The <see cref="WindowChrome"/> that collapses the native non-client frame. Created in the
        /// constructor and mutated in place by the shell-metric helpers.
        /// </summary>
        private readonly WindowChrome _windowChrome;

        /// <summary>
        /// The native window handle, realised in <see cref="OnSourceInitialized(EventArgs)"/>;
        /// <see cref="IntPtr.Zero"/> before realisation.
        /// </summary>
        private IntPtr _handle;

        /// <summary>The minimize caption button template part, or <see langword="null"/> if absent.</summary>
        private System.Windows.Controls.Button? _minimizeButton;

        /// <summary>The maximize caption button template part, or <see langword="null"/> if absent.</summary>
        private System.Windows.Controls.Button? _maximizeButton;

        /// <summary>The restore caption button template part, or <see langword="null"/> if absent.</summary>
        private System.Windows.Controls.Button? _restoreButton;

        /// <summary>The close caption button template part, or <see langword="null"/> if absent.</summary>
        private System.Windows.Controls.Button? _closeButton;

        /// <summary>
        /// The WPF-owned <see cref="HwndSource"/> for the realised window, used for the message hook,
        /// DPI transform, and redirection-surface color. Released (not disposed) in
        /// <see cref="OnClosed(EventArgs)"/>.
        /// </summary>
        private HwndSource? _hwndSource;

        /// <summary>
        /// The caption button currently showing the synthetic snap-layout hover visual, or
        /// <see langword="null"/> when none is hovered.
        /// </summary>
        private System.Windows.Controls.Button? _snapHoveredButton;

        /// <summary>
        /// Re-entrancy guard for <see cref="FillClientAreaForSizeToContent"/>: forcing the root visual
        /// to re-arrange can itself raise <see cref="FrameworkElement.SizeChanged"/>, so the fill must
        /// not recurse into itself.
        /// </summary>
        private bool _isFillingClientArea;

        #endregion Fields
    }
}
