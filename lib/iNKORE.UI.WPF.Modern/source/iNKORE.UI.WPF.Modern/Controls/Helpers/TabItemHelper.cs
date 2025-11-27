using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Common;
using iNKORE.UI.WPF.Modern.Helpers;
using System;
using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Markup;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Controls.Helpers
{
    public sealed class TabItemHeaderConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is string headerString && !string.IsNullOrWhiteSpace(headerString))
            {
                return headerString;
            }
            
            return null;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    
    /// <summary>
    /// TabViewItem Properties
    /// </summary>
    public static class TabItemHelper
    {
        private static readonly ResourceAccessor ResourceAccessor = new(typeof(TabItemHelper));
        
        #region IsEnabled

        public static bool GetIsEnabled(TabItem element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(TabItem element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(TabItemHelper),
            new PropertyMetadata(OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var item = (TabItem)d;
            if ((bool)e.NewValue)
            {
                item.Loaded += OnLoaded;
                item.SizeChanged += OnSizeChanged;
            }
            else
            {
                item.Loaded -= OnLoaded;
                item.SizeChanged -= OnSizeChanged;
                BindingOperations.ClearBinding(item,FrameworkElement.ToolTipProperty);
            }
        }

        #endregion

        #region Icon

        /// <summary>
        /// Sets the value for the Icon to be displayed within the tab.
        /// </summary>
        /// <param name="tabItem">The element from which to read the property value.</param>
        /// <returns>The Icon to be displayed within the tab.</returns>
        public static object GetIcon(TabItem tabItem)
        {
            return tabItem.GetValue(IconProperty);
        }

        /// <summary>
        /// Gets the value for the Icon to be displayed within the tab.
        /// </summary>
        /// <param name="tabItem">The element from which to read the property value.</param>
        /// <param name="value">The Icon to be displayed within the tab.</param>
        public static void SetIcon(TabItem tabItem, object value)
        {
            tabItem.SetValue(IconProperty, value);
        }

        /// <summary>
        /// Identifies the Icon dependency property.
        /// </summary>
        public static readonly DependencyProperty IconProperty =
            DependencyProperty.RegisterAttached(
                "Icon",
                typeof(object),
                typeof(TabItemHelper));

        #endregion

        #region TabGeometry

        public static object GetTabGeometry(TabItem tabItem)
        {
            return tabItem.GetValue(TabGeometryProperty);
        }

        private static void SetTabGeometry(TabItem tabItem, object value)
        {
            tabItem.SetValue(TabGeometryProperty, value);
        }

        public static readonly DependencyProperty TabGeometryProperty =
            DependencyProperty.RegisterAttached(
                "TabGeometry",
                typeof(Geometry),
                typeof(TabItemHelper));

        #endregion

        #region IsAddTabButtonVisible

        public static readonly DependencyProperty IsAddTabButtonVisibleProperty = DependencyProperty.RegisterAttached(
            "IsAddTabButtonVisible",




            typeof(bool),
            typeof(TabItemHelper),
            new PropertyMetadata(false));

        public static bool GetIsAddTabButtonVisible(TabItem element)
        {
            return (bool)element.GetValue(IsAddTabButtonVisibleProperty);
        }

        #endregion

        #region CloseTabButtonCommand

        internal static readonly DependencyProperty CloseTabButtonCommandProperty = DependencyProperty.RegisterAttached(
            "CloseTabButtonCommand",
            typeof(ICommand),
            typeof(TabItemHelper),
            null);

        internal static ICommand GetCloseTabButtonCommand(TabItem element)
        {
            return (ICommand)element.GetValue(CloseTabButtonCommandProperty);
        }

        internal static void SetCloseTabButtonCommand(TabItem tabItem, ICommand value)
        {
            tabItem.SetValue(CloseTabButtonCommandProperty, value);
        }

        #endregion

        #region CloseButtonOverlayMode

        public static readonly DependencyProperty CloseButtonOverlayModeProperty = DependencyProperty.RegisterAttached(
            "CloseButtonOverlayMode",
            typeof(TabViewCloseButtonOverlayMode),
            typeof(TabItemHelper),
            null);

        public static TabViewCloseButtonOverlayMode GetCloseButtonOverlayMode(TabControl element)
        {
            return (TabViewCloseButtonOverlayMode)element.GetValue(CloseButtonOverlayModeProperty);
        }

        #endregion

        #region IsClosable

        /// <summary>
        /// Identifies the IsClosable dependency property that indicates whether the tab shows a close button. true if the tab shows a close button; otherwise, false. The default is true.
        /// </summary>
        public static readonly DependencyProperty IsClosableProperty = DependencyProperty.RegisterAttached(
            "IsClosable",
            typeof(bool),
            typeof(TabItemHelper),
            new PropertyMetadata(true));

        public static bool GetIsClosable(TabItem element)
        {
            return (bool)element.GetValue(IsClosableProperty);
        }

        public static void SetIsClosable(TabItem element, bool value)
        {
            element.SetValue(IsClosableProperty, value);
        }

        #endregion


        #region CloseRequestedEvent

        public static readonly RoutedEvent CloseRequestedEvent = EventManager.RegisterRoutedEvent(
            "CloseRequested",
            RoutingStrategy.Bubble,
            typeof(EventHandler<TabViewTabCloseRequestedEventArgs>),
            typeof(TabItemHelper));

        public static void AddCloseRequestedHandler(TabItem tabItem, EventHandler<TabViewTabCloseRequestedEventArgs> handler)
        {
            tabItem.AddHandler(CloseRequestedEvent, handler);
        }

        public static void RemoveCloseRequestedHandler(TabItem tabItem, EventHandler<TabViewTabCloseRequestedEventArgs> handler)
        {
            tabItem.RemoveHandler(CloseRequestedEvent, handler);
        }


        #endregion


        private static void OnLoaded(object sender, RoutedEventArgs e)
        {
            TabItem TabItem = sender as TabItem;
            UpdateTabGeometry(TabItem);
            UpdateHeaderTooltip(TabItem);
            UpdateCloseButtonTooltip(TabItem);
            UpdateCloseButtonEvents(TabItem);

            TabControl TabControl = TabItem.FindAscendant<TabControl>();

            if (TabControl != null)
            {
                TabItem.SetBinding(CloseButtonOverlayModeProperty, new Binding
                {
                    Source = TabControl,
                    Mode = BindingMode.OneWay,
                    Path = new PropertyPath(TabControlHelper.CloseButtonOverlayModeProperty)
                });
            }
        }

        private static void UpdateHeaderTooltip(TabItem TabItem)
        {
            if (TabItem.ToolTip is null && TabItem.GetTemplateChild<FrameworkElement>("TabContainer") is { } headerContainer)
            {
                headerContainer.SetBinding(
                    FrameworkElement.ToolTipProperty,
                    new Binding
                    {
                        Path = new PropertyPath(HeaderedContentControl.HeaderProperty),
                        RelativeSource = new RelativeSource(RelativeSourceMode.TemplatedParent),
                        Mode = BindingMode.OneWay,
                        Converter = TabItem.TryFindResource("TabItemHeaderConverter") as IValueConverter
                    });
            }
        }

        private static readonly RoutedCommand CloseTabButtonCommand = new RoutedCommand()
        {
            InputGestures = { new KeyGesture(Key.F4, ModifierKeys.Control) }
        };

        private static void UpdateCloseButtonEvents(TabItem item)
        {
            TabControl tabControl = item.FindAscendant<TabControl>();

            void ExecutedCustomCommand(object sender, ExecutedRoutedEventArgs e)
            {
                var eargs = new TabViewTabCloseRequestedEventArgs(TabControlHelper.TabCloseRequestedEvent, item.Content, item);
                tabControl.RaiseEvent(eargs);

                // According to WinUI 3 behavior, the TabView's CloseRequested will be fired first,
                // then the TabItem's CloseRequested will be fired after that.
                // Since WinUI 3 does not have a 'routed' event for TabItem CloseRequested, we may apply
                // the same logic here, but adopting a handled check for TabItem CloseRequested event.
                // If this is inappropriate, feel free to propose a change.
                if (!eargs.Handled)
                {
                    item.RaiseEvent(new TabViewTabCloseRequestedEventArgs(CloseRequestedEvent, item.Content, item));
                }

                e.Handled = true;
            }

            void CanExecuteCustomCommand(object sender, CanExecuteRoutedEventArgs e)
            {
                if (tabControl != null)
                {
                    e.CanExecute = true;
                }
                else
                {
                    e.CanExecute = false;
                }
                e.Handled = true;
            }

            CommandBinding closeTabButtonCommandBinding = new CommandBinding(CloseTabButtonCommand, ExecutedCustomCommand, CanExecuteCustomCommand);
            item.CommandBindings.Add(closeTabButtonCommandBinding);
            SetCloseTabButtonCommand(item, CloseTabButtonCommand);

            // Cleanup previous bindings
            foreach (var binding in item.CommandBindings)
            {
                if (binding is CommandBinding cmb && cmb.Command == CloseTabButtonCommand
                    && cmb != closeTabButtonCommandBinding)
                {
                    item.CommandBindings.Remove(cmb);
                    break;
                }
            }
        }

        private static void UpdateCloseButtonTooltip(TabItem item)
        {
            if (item?.GetTemplateChild<FrameworkElement>("CloseButton") is not { } closeButton)
            {
                return;
            }

            closeButton.ToolTip =
                ResourceAccessor.GetLocalizedStringResource(ResourceAccessor.SR_TabViewCloseButtonTooltipWithKA);
        }

        private static void OnSizeChanged(object sender, SizeChangedEventArgs e)
        {
            UpdateTabGeometry(sender as TabItem);
        }

        private static void UpdateTabGeometry(TabItem tabItem)
        {
            try
            {
                var scaleFactor = 1.5;
#if NET462_OR_NEWER
                scaleFactor = VisualTreeHelper.GetDpi(tabItem).DpiScaleX;
#else
                HwndSource hwnd = (HwndSource)PresentationSource.FromVisual(tabItem);
                Matrix transformToDevice = hwnd.CompositionTarget.TransformToDevice;
                scaleFactor = transformToDevice.M11;
#endif
                var height = tabItem.ActualHeight;
                var popupRadius = ControlHelper.GetCornerRadius(tabItem);
                var leftCorner = popupRadius.TopLeft;
                var rightCorner = popupRadius.TopRight;

                // Assumes 4px curving-out corners, which are hardcoded in the markup
                //var data = $"F1 M0,{height - 1f / scaleFactor}  a 4,4 0 0 0 4,-4  L 4,{leftCorner}  a {leftCorner},{leftCorner} 0 0 1 {leftCorner},-{leftCorner}  l {tabItem.ActualWidth - (leftCorner + rightCorner + 1.0f / scaleFactor)},0  a {rightCorner},{rightCorner} 0 0 1 {rightCorner},{rightCorner}  l 0,{height - (4 + rightCorner + 1.0f / scaleFactor)}  a 4,4 0 0 0 4,4 Z";
                var data = $"F1 M0,{Math.Round(height - 1f / scaleFactor)}  a 4,4 0 0 0 4,-4  L 4,{leftCorner}  a {leftCorner},{leftCorner} 0 0 1 {leftCorner},-{leftCorner}  l {Math.Round(tabItem.ActualWidth - (leftCorner + rightCorner + 1.0f / scaleFactor))},0  a {rightCorner},{rightCorner} 0 0 1 {rightCorner},{rightCorner}  l 0,{Math.Round(height - (4 + rightCorner + 1.0f / scaleFactor))}  a 4,4 0 0 0 4,4 Z";

                var geometry = Geometry.Parse(data);

                SetTabGeometry(tabItem, geometry);
            }
            catch { }
        }
    }
}
