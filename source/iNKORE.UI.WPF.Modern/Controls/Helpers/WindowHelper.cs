using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers;
using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Shell;
using System.Linq;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Controls.Helpers
{
    public static class WindowHelper
    {
        //private const string DefaultWindowStyleKey = "DefaultWindowStyle";
        private const string TheWindowStyleKey = "TheWindowStyle";
        //private const string AcrylicWindowStyleKey = "AcrylicWindowStyle";
        //private const string MicaWindowStyleKey = "MicaWindowStyle";
        //private const string SnapWindowStyleKey = "SnapWindowStyle";

        #region UseModernWindowStyle

        public static readonly DependencyProperty UseModernWindowStyleProperty =
            DependencyProperty.RegisterAttached(
                "UseModernWindowStyle",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(OnUseModernWindowStyleChanged));

        public static bool GetUseModernWindowStyle(Window window)
        {
            return (bool)window.GetValue(UseModernWindowStyleProperty);
        }

        public static void SetUseModernWindowStyle(Window window, bool value)
        {
            window.SetValue(UseModernWindowStyleProperty, value);
        }

        private static void OnUseModernWindowStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            bool newValue = (bool)e.NewValue;

            if (DesignerProperties.GetIsInDesignMode(d))
            {
                if (d is Control control)
                {
                    if (newValue)
                    {
                        if (control.TryFindResource(TheWindowStyleKey) is Style style)
                        {
                            var dStyle = new Style();

                            foreach (Setter setter in style.Setters)
                            {
                                if (setter.Property == Control.BackgroundProperty ||
                                    setter.Property == Control.ForegroundProperty)
                                {
                                    dStyle.Setters.Add(setter);
                                }
                            }

                            control.Style = dStyle;
                        }
                    }
                    else
                    {
                        control.ClearValue(FrameworkElement.StyleProperty);
                    }
                }
            }
            else
            {
                var window = (Window)d;
                SetWindowStyle(window);
            }

        }

        #endregion

        #region UseAeroBackdrop

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property")]
        public static readonly DependencyProperty UseAeroBackdropProperty =
            DependencyProperty.RegisterAttached(
                "UseAeroBackdrop",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(OnUseAeroBackdropChanged));

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property")]
        public static bool GetUseAeroBackdrop(Window window)
        {
            return (bool)window.GetValue(UseAeroBackdropProperty);
        }

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property")]
        public static void SetUseAeroBackdrop(Window window, bool value)
        {
            window.SetValue(UseAeroBackdropProperty, value);
        }

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property")]
        private static void OnUseAeroBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (OSVersionHelper.OSVersion < new Version(6, 0) || new Version(6, 2, 8824) < OSVersionHelper.OSVersion)
            {
                return;
            }

            if (d is Window window)
            {
                SetWindowStyle(window);
            }
        }

        #endregion

        #region UseAcrylicBackdrop

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property", true)]
        public static readonly DependencyProperty UseAcrylicBackdropProperty =
            DependencyProperty.RegisterAttached(
                "UseAcrylicBackdrop",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(OnUseAcrylicBackdropChanged));

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property", true)]
        public static bool GetUseAcrylicBackdrop(Window window)
        {
            return (bool)window.GetValue(UseAcrylicBackdropProperty);
        }

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property", true)]
        public static void SetUseAcrylicBackdrop(Window window, bool value)
        {
            window.SetValue(UseAcrylicBackdropProperty, value);
        }

        [Obsolete("This property is no longer maintained, please use SystemBackdropType property", true)]
        private static void OnUseAcrylicBackdropChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //if (!AcrylicHelper.IsSupported())
            //{
            //    return;
            //}

            //if (d is Window window)
            //{
            //    var handler = new RoutedEventHandler(async (sender, e) =>
            //    {
            //        await Task.Delay(1);
            //        AcrylicHelper.Apply(window);
            //    });

            //    SetWindowStyle(window);

            //    if ((bool)e.NewValue)
            //    {
            //        AcrylicHelper.Apply(window);

            //        if (!window.IsLoaded)
            //        {
            //            window.Loaded += (sender, e) => AcrylicHelper.Apply(window);
            //        }

            //        if (AcrylicHelper.IsAcrylicSupported())
            //        {
            //            ThemeManager.RemoveActualThemeChangedHandler(window, handler);
            //            ThemeManager.AddActualThemeChangedHandler(window, handler);
            //        }
            //    }
            //    else
            //    {
            //        AcrylicHelper.Remove(window);
            //        ThemeManager.RemoveActualThemeChangedHandler(window, handler);
            //    }
            //}
        }

        #endregion

        #region SystemBackdropType

        public static readonly DependencyProperty SystemBackdropTypeProperty =
            DependencyProperty.RegisterAttached(
                "SystemBackdropType",
                typeof(BackdropType),
                typeof(WindowHelper),
                new PropertyMetadata(OnSystemBackdropTypeChanged));

        public static BackdropType GetSystemBackdropType(Window window)
        {
            return (BackdropType)window.GetValue(SystemBackdropTypeProperty);
        }

        public static void SetSystemBackdropType(Window window, BackdropType value)
        {
            window.SetValue(SystemBackdropTypeProperty, value);
        }

        private static void OnSystemBackdropTypeChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            //if (!((BackdropType)e.NewValue).IsSupported())
            //{
            //    return;
            //}

            if (d is Window window)
            {
                var newBackdrop = GetSystemBackdropType(window);

                if (e.OldValue is BackdropType oldBackdrop &&
                    (oldBackdrop.GetActualBackdropType() == BackdropType.Acrylic10 || newBackdrop.GetActualBackdropType() == BackdropType.Acrylic10
                        && oldBackdrop.GetActualBackdropType() != newBackdrop.GetActualBackdropType()))
                {
                    BackdropHelper.Remove(window);
                }

                SetWindowStyle(window);
                BackdropHelper.Apply(window, GetSystemBackdropType(window));
                UpdateWindowChrome(window);
            }
        }

        #endregion

        #region Acrylic10Color

        public static readonly DependencyProperty Acrylic10ColorProperty =
            DependencyProperty.RegisterAttached(
                "Acrylic10Color",
                typeof(Color),
                typeof(WindowHelper),
                new PropertyMetadata(Colors.Transparent, OnAcrylic10ColorChanged));

        public static Color? GetAcrylic10Color(Window window)
        {
            return (Color)window.GetValue(Acrylic10ColorProperty);
        }

        public static void SetAcrylic10Color(Window window, Color value)
        {
            window.SetValue(Acrylic10ColorProperty, value);
        }

        private static void OnAcrylic10ColorChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            OnSystemBackdropTypeChanged(d, e);
        }

        #endregion

        #region CornerStyle

        public static readonly DependencyProperty CornerStyleProperty =
            DependencyProperty.RegisterAttached(
                "CornerStyle",
                typeof(WindowCornerStyle),
                typeof(WindowHelper),
                new PropertyMetadata(WindowCornerStyle.Default, OnCornerStyleChanged));

        private static void OnCornerStyleChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                CornerHelper.SetWindowCorners(window, (WindowCornerStyle)e.NewValue);
                UpdateShouldDisplayManualBorder(window);

            }
        }

        public static WindowCornerStyle GetCornerStyle(Window window)
        {
            return (WindowCornerStyle)window.GetValue(CornerStyleProperty);
        }

        public static void SetCornerStyle(Window window, WindowCornerStyle value)
        {
            window.SetValue(CornerStyleProperty, value);
        }


        #endregion

        #region ApplyBackground

        public static readonly DependencyProperty ApplyBackgroundProperty =
            DependencyProperty.RegisterAttached(
                "ApplyBackground",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(true));

        public static bool GetApplyBackground(Window window)
        {
            return (bool)window.GetValue(ApplyBackgroundProperty);
        }

        public static void SetApplyBackground(Window window, bool value)
        {
            window.SetValue(ApplyBackgroundProperty, value);
        }


        #endregion

        #region ApplyNoise

        public static readonly DependencyProperty ApplyNoiseProperty =
            DependencyProperty.RegisterAttached(
                "ApplyNoise",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(false));

        public static bool GetApplyNoise(Window window)
        {
            return (bool)window.GetValue(ApplyNoiseProperty);
        }

        public static void SetApplyNoise(Window window, bool value)
        {
            window.SetValue(ApplyNoiseProperty, value);
        }


        #endregion

        #region ShouldDisplayManualBorder

        public static readonly DependencyPropertyKey ShouldDisplayManualBorderPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ShouldDisplayManualBorder",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(false));

        public static readonly DependencyProperty ShouldDisplayManualBorderProperty = ShouldDisplayManualBorderPropertyKey.DependencyProperty;

        public static bool GetShouldDisplayManualBorder(Window window)
        {
            return (bool)window.GetValue(ShouldDisplayManualBorderProperty);
        }

        private static void SetShouldDisplayManualBorder(Window window, bool value)
        {
            window.SetValue(ShouldDisplayManualBorderPropertyKey, value);
        }

        public static void UpdateShouldDisplayManualBorder(Window window)
        {
            if (window == null)
            {
                return;
            }

            var isOsBorderPresent = OSVersionHelper.IsWindows11OrGreater;

            var newValue = !isOsBorderPresent;
            SetShouldDisplayManualBorder(window, newValue);
        }

        #endregion


        #region FixMaximizedWindow

        public static readonly DependencyProperty FixMaximizedWindowProperty =
            DependencyProperty.RegisterAttached(
                "FixMaximizedWindow",
                typeof(bool),
                typeof(WindowHelper),
                new PropertyMetadata(false, OnFixMaximizedWindowChanged));

        public static bool GetFixMaximizedWindow(Window window)
        {
            return (bool)window.GetValue(FixMaximizedWindowProperty);
        }

        public static void SetFixMaximizedWindow(Window window, bool value)
        {
            window.SetValue(FixMaximizedWindowProperty, value);
        }

        private static void OnFixMaximizedWindowChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is Window window)
            {
                if ((bool)e.NewValue)
                {
                    MaximizedWindowFixer.SetMaximizedWindowFixer(window, new MaximizedWindowFixer());
                }
                else
                {
                    window.ClearValue(MaximizedWindowFixer.MaximizedWindowFixerProperty);
                }
            }
        }

        #endregion

        public static void SetWindowStyle(Window window)
        {
            bool isModern = DependencyPropertyHelper.GetValueSource(window, UseModernWindowStyleProperty).BaseValueSource != BaseValueSource.Default && GetUseModernWindowStyle(window);
       
            //var backdrop = GetSystemBackdropType(window);

            //bool isUseMica = new BackdropType[] { BackdropType.Mica, BackdropType.Tabbed, BackdropType.Acrylic11 }.Contains(backdrop); //DependencyPropertyHelper.GetValueSource(window, SystemBackdropTypeProperty).BaseValueSource != BaseValueSource.Default;
            //bool isUseAcrylic10 = backdrop == BackdropType.Acrylic10; // DependencyPropertyHelper.GetValueSource(window, UseAcrylicBackdropProperty).BaseValueSource != BaseValueSource.Default && GetUseAcrylicBackdrop(window);
            // bool isUseAero = DependencyPropertyHelper.GetValueSource(window, UseAeroBackdropProperty).BaseValueSource != BaseValueSource.Default && GetUseAeroBackdrop(window);

            //bool isSetMica = false;
            //bool isSetAcrylic10 = false;
            //bool isSetAero = false;

            void ApplyDarkMode()
            {
                var theme = ThemeManager.GetActualTheme(window);

                bool IsDark(ElementTheme theme)
                {
                    return theme == ElementTheme.Default
                        ? ThemeManager.Current.ActualApplicationTheme == ApplicationTheme.Dark
                        : theme == ElementTheme.Dark;
                }

                try
                {
                    if (IsDark(theme))
                    {
                        window.ApplyDarkMode();
                    }
                    else
                    {
                        window.RemoveDarkMode();
                    }
                }
                catch { }
            }

            var handler = new RoutedEventHandler((sender, e) => ApplyDarkMode());

            WindowResizeModeDescriptor.RemoveValueChanged(window, OnWindowResizeModeDescriptorValueChanged);
            ThemeManager.RemoveActualThemeChangedHandler(window, handler);

            if (isModern)
            {
                ApplyDarkMode();

                void onLoaded(object sender, RoutedEventArgs e)
                {
                    // This is needed to fix the issue with the window not being loaded correctly
                    WindowChrome.SetWindowChrome(window, (WindowChrome.GetWindowChrome(window)?.Clone() as WindowChrome) ?? WindowChrome.GetWindowChrome(window));
                    
                    window.RemoveTitleBar();
                }


                if (window.IsLoaded)
                {
                    onLoaded(null, null);
                }
                else
                {
                    
                    window.Loaded -= onLoaded;
                    window.Loaded += onLoaded;
                }

                ThemeManager.AddActualThemeChangedHandler(window, handler);

                WindowResizeModeDescriptor.AddValueChanged(window, OnWindowResizeModeDescriptorValueChanged);

                window.SetResourceReference(FrameworkElement.StyleProperty, TheWindowStyleKey);

                //if (isUseMica)
                //{
                //    if (type.IsSupported())
                //    {
                //        isSetMica = true;
                //        //window.SetResourceReference(FrameworkElement.StyleProperty, MicaWindowStyleKey);
                //    }
                //}

                //if (!isSetMica && isUseAcrylic10)
                //{
                //    if (AcrylicHelper.IsAcrylicSupported())
                //    {
                //        //isSetAcrylic10 = true;
                //        //window.SetResourceReference(FrameworkElement.StyleProperty, AcrylicWindowStyleKey);
                //    }
                //    else if (AcrylicHelper.IsSupported())
                //    {
                //        //isSetAcrylic10 = true;
                //        //window.SetResourceReference(FrameworkElement.StyleProperty, AeroWindowStyleKey);
                //    }
                //}

                //if (!isSetMica && !isSetAcrylic10 && isUseAero)
                //{
                //    if (new Version(6, 0) <= OSVersionHelper.OSVersion && OSVersionHelper.OSVersion < new Version(6, 2, 8824))
                //    {
                //        isSetAero = true;
                //        window.SetResourceReference(FrameworkElement.StyleProperty, AeroWindowStyleKey);
                //    }
                //}

                //if (!isSetMica && !isSetAcrylic && !isSetAero)
                //{
                //    if (OSVersionHelper.IsWindows11OrGreater)
                //    {
                //        window.SetResourceReference(FrameworkElement.StyleProperty, SnapWindowStyleKey);
                //    }
                //    else
                //    {
                //        window.SetResourceReference(FrameworkElement.StyleProperty, DefaultWindowStyleKey);
                //    }
                //}
            }
            else
            {
                window.ClearValue(FrameworkElement.StyleProperty);
                window.RemoveDarkMode();
            }

            UpdateWindowChrome(window);
            UpdateShouldDisplayManualBorder(window);
        }


        #region Chrome Management

        static DependencyPropertyDescriptor WindowResizeModeDescriptor = DependencyPropertyDescriptor.FromProperty(Window.ResizeModeProperty, typeof(Window));

        private static void OnWindowResizeModeDescriptorValueChanged(object sender, EventArgs e)
        {
            if (sender is Window win)
            {
                UpdateWindowChrome(win);
            }
        }


        public static WindowChrome UpdateWindowChrome(this Window window)
        {
            if (window == null)
            {
                return null;
            }

            var chrome = WindowChrome.GetWindowChrome(window);

            if (GetUseModernWindowStyle(window))
            {
                if (chrome == null)
                {
                    chrome = new WindowChrome() 
                    {
                        CornerRadius = new CornerRadius(0),
                        NonClientFrameEdges = NonClientFrameEdges.None,
                        UseAeroCaptionButtons = false
                    };
                }
                // -----------------------------
                // Resize border thickness
                // -----------------------------
                
                var isResizable = true;
                switch (window.ResizeMode)
                {
                    case ResizeMode.NoResize:
                    case ResizeMode.CanMinimize:
                        isResizable = false;
                        break;
                    case ResizeMode.CanResize:
                    case ResizeMode.CanResizeWithGrip:
                        isResizable = true;
                        break;
                }

                var resizeBorderThickness = isResizable ? new Thickness(4) : new Thickness(0);

                if (chrome.ResizeBorderThickness != resizeBorderThickness)
                    chrome.ResizeBorderThickness = resizeBorderThickness;

                // -----------------------------
                // Caption height
                // -----------------------------

                var captionHeight = TitleBar.GetHeight(window);

                if (chrome.CaptionHeight != captionHeight)
                    chrome.CaptionHeight = captionHeight;


                // -----------------------------
                // Glass frame thickness
                // -----------------------------

                var glassFrameThickness = new Thickness(-1);
                switch (GetSystemBackdropType(window).GetActualBackdropType())
                {
                    case BackdropType.None:
                        glassFrameThickness = new Thickness(-1);
                        break;
                    case BackdropType.Acrylic10:
                        glassFrameThickness = new Thickness(0, 1, 0, 0);
                        break;
                    case BackdropType.Mica:
                    case BackdropType.Tabbed:
                    case BackdropType.Acrylic11:
                        glassFrameThickness = new Thickness(-1);
                        break;
                }

                if (chrome.GlassFrameThickness != glassFrameThickness)
                    chrome.GlassFrameThickness = glassFrameThickness;


                // Final

                WindowChrome.SetWindowChrome(window, chrome);
            }

            return chrome;
        }


        #endregion
    }
}