using System;
using System.ComponentModel;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace iNKORE.UI.WPF.Modern.Controls.Helpers
{
    public static class ExpanderAnimationsHelper
    {
        #region ToAnimateControlName

        public static string GetToAnimateControlName(Expander element) =>
            (string)element.GetValue(ToAnimateControlNameProperty);

        public static void SetToAnimateControlName(Expander element, string value) =>
            element.SetValue(ToAnimateControlNameProperty, value);

        public static readonly DependencyProperty ToAnimateControlNameProperty = DependencyProperty.RegisterAttached(
            "ToAnimateControlName",
            typeof(string),
            typeof(ExpanderAnimationsHelper),
            new PropertyMetadata("ExpanderContent"));

        #endregion

        #region ExpandAnimationDuration

        public static TimeSpan GetExpandAnimationDuration(Expander element) =>
            (TimeSpan)element.GetValue(ExpandAnimationDurationProperty);

        public static void SetExpandAnimationDuration(Expander element, TimeSpan value) =>
            element.SetValue(ExpandAnimationDurationProperty, value);

        public static readonly DependencyProperty ExpandAnimationDurationProperty = DependencyProperty.RegisterAttached(
            "ExpandAnimationDuration",
            typeof(TimeSpan),
            typeof(ExpanderAnimationsHelper),
            new PropertyMetadata(TimeSpan.FromMilliseconds(333)));

        #endregion

        #region CollapseAnimationDuration

        public static TimeSpan GetCollapseAnimationDuration(Expander element) =>
            (TimeSpan)element.GetValue(CollapseAnimationDurationProperty);

        public static void SetCollapseAnimationDuration(Expander element, TimeSpan value) =>
            element.SetValue(CollapseAnimationDurationProperty, value);

        public static readonly DependencyProperty CollapseAnimationDurationProperty =
            DependencyProperty.RegisterAttached(
                "CollapseAnimationDuration",
                typeof(TimeSpan),
                typeof(ExpanderAnimationsHelper),
                new PropertyMetadata(TimeSpan.FromMilliseconds(167)));

        #endregion

        #region IsEnabled

        public static bool GetIsEnabled(Expander element) => (bool)element.GetValue(IsEnabledProperty);

        public static void SetIsEnabled(Expander element, bool value) => element.SetValue(IsEnabledProperty, value);

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ExpanderAnimationsHelper),
            new PropertyMetadata(OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            if (d is not Expander expander)
            {
                return;
            }

            if (e.NewValue is not true)
            {
                expander.Expanded -= OnExpanderExpandedOrCollapsed;
                expander.Collapsed -= OnExpanderExpandedOrCollapsed;
                return;
            }

            expander.Expanded += OnExpanderExpandedOrCollapsed;
            expander.Collapsed += OnExpanderExpandedOrCollapsed;

            if (expander.IsLoaded)
            {
                RunExpanderAnimation(expander);
            }
            else
            {
                expander.Loaded += TriggerExpandAnimationOnLoad;
            }

            void TriggerExpandAnimationOnLoad(object sender, RoutedEventArgs routedEventArgs)
            {
                RunExpanderAnimation(expander);
                expander.Loaded -= TriggerExpandAnimationOnLoad;
            }
        }

        private static void OnExpanderExpandedOrCollapsed(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander)
            {
                return;
            }

            RunExpanderAnimation(expander);
        }

        #endregion

        private static void RunExpanderAnimation(Expander expander)
        {
            if (expander.IsExpanded)
            {
                AnimateExpand(expander);
            }
            else
            {
                AnimateCollapse(expander);
            }
        }

        private static void AnimateExpand(Expander expander)
        {
            var toAnimateControl = GetToAnimateControl(expander);
            toAnimateControl.BeginAnimation(UIElement.VisibilityProperty, null);
            UpdateLayout(toAnimateControl);

            if (toAnimateControl.RenderTransform is not TranslateTransform translateTransform)
            {
                toAnimateControl.RenderTransform = translateTransform = new TranslateTransform();
            }

            var (animationProperty, originValue) = GetToAnimatePropertyAndValue(toAnimateControl, expander.ExpandDirection);
            RunTranslationAnimation(GetExpandAnimationDuration(expander), translateTransform, animationProperty, 0, originValue);
        }

        private static void AnimateCollapse(Expander expander)
        {
            var toAnimateControl = GetToAnimateControl(expander);
            var animationDuration = GetCollapseAnimationDuration(expander);

            var visibilityAnimation = new ObjectAnimationUsingKeyFrames
            {
                KeyFrames =
                [
                    new DiscreteObjectKeyFrame
                    {
                        KeyTime = animationDuration,
                        Value = Visibility.Collapsed
                    }
                ]
            };

            UpdateLayout(toAnimateControl);

            if (toAnimateControl.RenderTransform is not TranslateTransform translateTransform)
            {
                toAnimateControl.RenderTransform = translateTransform = new TranslateTransform();
            }

            var (animationProperty, toValue) = GetToAnimatePropertyAndValue(toAnimateControl, expander.ExpandDirection);
            
            toAnimateControl.BeginAnimation(UIElement.VisibilityProperty, visibilityAnimation);
            RunTranslationAnimation(animationDuration, translateTransform, animationProperty,
                toValue);
        }

        private static (DependencyProperty, double) GetToAnimatePropertyAndValue(FrameworkElement toAnimateControl,
            ExpandDirection direction)
        {
            var (toAnimateProp, toResetProp, toValue) = direction switch
            {
                ExpandDirection.Down => (TranslateTransform.YProperty, TranslateTransform.XProperty, -toAnimateControl.ActualHeight),
                ExpandDirection.Up => (TranslateTransform.YProperty, TranslateTransform.XProperty, toAnimateControl.ActualHeight),
                ExpandDirection.Left => (TranslateTransform.XProperty, TranslateTransform.YProperty, toAnimateControl.ActualWidth),
                ExpandDirection.Right => (TranslateTransform.XProperty, TranslateTransform.YProperty, -toAnimateControl.ActualWidth),
            };

            toAnimateControl.RenderTransform.BeginAnimation(toResetProp, null);
            return (toAnimateProp, toValue);
        }

        private static void RunTranslationAnimation(
            TimeSpan animationDuration,
            TranslateTransform translateTransform,
            DependencyProperty toAnimateProperty,
            double targetValue,
            double? fromValue = null)
        {
            var keyFrames = new DoubleKeyFrameCollection
            {
                new SplineDoubleKeyFrame
                {
                    KeySpline = new KeySpline(0, 0, 0, 1),
                    KeyTime = animationDuration,
                    Value = targetValue
                },
            };

            if (fromValue is not null)
            {
                keyFrames.Add(new DiscreteDoubleKeyFrame(fromValue.Value, KeyTime.FromPercent(0)));
            }

            var animation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = keyFrames,
                Duration = animationDuration
            };

            translateTransform.BeginAnimation(toAnimateProperty, animation);
        }

        private static void UpdateLayout(FrameworkElement contentControl)
        {
            contentControl.Measure(new Size(contentControl.MaxWidth, contentControl.MaxHeight));
            contentControl.UpdateLayout();
        }

        private static FrameworkElement GetToAnimateControl(Expander expander) =>
            expander.Template?.FindName(GetToAnimateControlName(expander), expander) as FrameworkElement;
    }
}