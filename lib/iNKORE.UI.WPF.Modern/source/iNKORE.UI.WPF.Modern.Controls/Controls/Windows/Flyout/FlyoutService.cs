using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public static class FlyoutService
    {
        public static readonly DependencyProperty FlyoutProperty =
            DependencyProperty.RegisterAttached(
                "Flyout",
                typeof(FlyoutBase),
                typeof(FlyoutService),
                new PropertyMetadata(OnFlyoutChanged));

        public static FlyoutBase GetFlyout(Button button)
        {
            return (FlyoutBase)button.GetValue(FlyoutProperty);
        }

        public static void SetFlyout(Button button, FlyoutBase value)
        {
            button.SetValue(FlyoutProperty, value);
        }

        private static void OnFlyoutChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var button = (Button)d;

            if (e.OldValue is FlyoutBase oldFlyout)
            {
                button.Click -= OnButtonClick;
                button.MouseRightButtonUp -= Button_MouseRightButtonUp;
            }

            if (e.NewValue is FlyoutBase newFlyout)
            {
                button.Click += OnButtonClick;
                button.MouseRightButtonUp += Button_MouseRightButtonUp;
                //button.MouseRightButtonDown += Button_MouseRightButtonDown;
                //button.MouseLeftButtonDown += Button_MouseLeftButtonDown;
                //button.MouseLeftButtonUp += Button_MouseLeftButtonUp;
            }
        }

        //static Dictionary<Button, DispatcherTimer> longPressTimers = new Dictionary<Button, DispatcherTimer>();

        //private static void Button_MouseLeftButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    throw new System.NotImplementedException();
        //}

        //private static void Button_MouseLeftButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    throw new System.NotImplementedException();
        //}

        //private static void Button_MouseRightButtonDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    throw new System.NotImplementedException();
        //}

        private static void Button_MouseRightButtonUp(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            ButtonFlyoutOpening(sender, FlyoutOpeningMode.RightMouseButtonUp);
        }

        //private static void OnButtonLongPressStarted(object sender, System.Windows.Input.MouseButtonEventArgs e)
        //{
        //    var button = (Button)sender;
        //    var openingMode = GetFlyoutOpeningMode(button);
        //    bool longPressStart = false;

        //    if (openingMode.HasFlag(FlyoutOpeningMode.LeftButtonLongPress) && e.ChangedButton == System.Windows.Input.MouseButton.Left)
        //        longPressStart = true;
        //    else if (openingMode.HasFlag(FlyoutOpeningMode.RightButtonLongPress) && e.ChangedButton == System.Windows.Input.MouseButton.Right)
        //        longPressStart = true;

        //    if (longPressStart)
        //    {
        //        if (!longPressTimers.ContainsKey(button))
        //        {
        //            DispatcherTimer timer = new DispatcherTimer();
        //            timer.Interval = TimeSpan.FromMilliseconds(GetFlyoutOpeningLongPressMilliseconds(button));
        //            timer.Tag = button;
        //            timer.Tick += Timer_Tick;
        //            longPressTimers.Add(button, timer);
        //        }
        //    }
        //}

        //private static void Timer_Tick(object sender, EventArgs e)
        //{
        //    DispatcherTimer timer = sender as DispatcherTimer;
        //    Button button = timer?.Tag as Button;

        //    if (button != null)
        //    {
        //        ButtonFlyoutOpening(button);
        //        timer.Stop();
        //        timer.Tick -= Timer_Tick;
        //        longPressTimers.Remove(button);
        //    }
        //}

        private static void OnButtonClick(object sender, RoutedEventArgs e)
        {
            ButtonFlyoutOpening(sender, FlyoutOpeningMode.Click);
        }

        private static void ButtonFlyoutOpening(object sender, FlyoutOpeningMode requested)
        {
            var button = (Button)sender;
            var open = GetFlyoutOpeningMode(button);
            if (open.HasFlag(requested))
            {
                ButtonFlyoutOpening(button);
            }

        }

        private static void ButtonFlyoutOpening(Button button)
        {
            var open = GetFlyoutOpeningMode(button);
            var flyout = GetFlyout(button);
            if (flyout != null)
            {
                flyout.ShowAt(button);
            }

        }


        public static readonly DependencyProperty FlyoutOpeningModeProperty =
            DependencyProperty.RegisterAttached(
                "FlyoutOpeningMode",
                typeof(FlyoutOpeningMode),
                typeof(FlyoutService),
                new PropertyMetadata(FlyoutOpeningMode.Click));

        public static FlyoutOpeningMode GetFlyoutOpeningMode(Button button)
        {
            return (FlyoutOpeningMode)button.GetValue(FlyoutOpeningModeProperty);
        }

        public static void SetFlyoutOpeningMode(Button button, FlyoutOpeningMode value)
        {
            button.SetValue(FlyoutOpeningModeProperty, value);
        }

        //public static readonly DependencyProperty FlyoutOpeningLongPressMillisecondsProperty =
        //    DependencyProperty.RegisterAttached(
        //        "FlyoutOpeningLongPressMilliseconds",
        //        typeof(int),
        //        typeof(FlyoutService),
        //        new PropertyMetadata(FlyoutOpeningMode.Click));

        //public static int GetFlyoutOpeningLongPressMilliseconds(Button button)
        //{
        //    return (int)button.GetValue(FlyoutOpeningLongPressMillisecondsProperty);
        //}

        //public static void SetFlyoutOpeningLongPressMilliseconds(Button button, int value)
        //{
        //    button.SetValue(FlyoutOpeningLongPressMillisecondsProperty, value);
        //}

    }

    public enum FlyoutOpeningMode
    {
        None = 0,
        Click = 1,
        RightMouseButtonUp = 2,
        //LeftButtonLongPress = 4,
        //RightButtonLongPress = 8
    }

}
