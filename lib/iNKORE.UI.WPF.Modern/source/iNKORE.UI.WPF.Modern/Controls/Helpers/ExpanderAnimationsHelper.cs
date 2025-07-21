using System;
using System.ComponentModel;
using System.Linq;
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
            new PropertyMetadata("ExpanderContent", OnToAnimateControlNameChanged));

        private static void OnToAnimateControlNameChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var handler = d.GetValue(ExpansionHandlerProperty);

            if (handler is not ExpanderExpansionBaseHandler baseHandler)
            {
                return;
            }

            baseHandler.UpdateToAnimateControl(e.NewValue as string);
        }

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

            var expandDirectionDescriptor = DependencyPropertyDescriptor.FromName(
                nameof(Expander.ExpandDirection),
                typeof(Expander),
                typeof(Expander));

            if (e.NewValue is not true)
            {
                expandDirectionDescriptor?.RemoveValueChanged(expander, OnExpandDirectionChanged);
                expander.Expanded -= OnExpanderExpanded;
                expander.Collapsed -= OnExpanderCollapsed;
                expander.ClearValue(ExpansionHandlerProperty);
                return;
            }

            OnExpandDirectionChanged(expander, null);
            expandDirectionDescriptor?.AddValueChanged(expander, OnExpandDirectionChanged);
            expander.Expanded += OnExpanderExpanded;
            expander.Collapsed += OnExpanderCollapsed;
        }

        private static void OnExpanderExpanded(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander)
            {
                return;
            }

            if (GetExpansionHandler(expander) is not { } handler)
            {
                return;
            }

            var animationDuration = GetExpandAnimationDuration(expander);
            handler.Handle(animationDuration);
        }

        private static void OnExpanderCollapsed(object sender, RoutedEventArgs e)
        {
            if (sender is not Expander expander)
            {
                return;
            }

            if (GetExpansionHandler(expander) is not { } handler)
            {
                return;
            }

            var animationDuration = GetCollapseAnimationDuration(expander);
            handler.Handle(animationDuration);
        }

        private static void OnExpandDirectionChanged(object sender, EventArgs e)
        {
            if (sender is not Expander expander)
            {
                return;
            }

            var toAnimateControlName = GetToAnimateControlName(expander);
            ExpanderExpansionBaseHandler expansionHandler = expander.ExpandDirection switch
            {
                ExpandDirection.Up or ExpandDirection.Down => new ExpanderVerticalExpansionHandler(expander,
                    toAnimateControlName),
                _ => new ExpanderHorizontalExpansionHandler(expander, toAnimateControlName)
            };

            expander.SetValue(ExpansionHandlerProperty, expansionHandler);
        }

        #endregion

        public static ExpanderExpansionBaseHandler GetExpansionHandler(Expander element) =>
            (ExpanderExpansionBaseHandler)element.GetValue(ExpansionHandlerProperty);

        public static readonly DependencyProperty ExpansionHandlerProperty = DependencyProperty.RegisterAttached(
            "ExpansionHandler",
            typeof(ExpanderExpansionBaseHandler),
            typeof(ExpanderAnimationsHelper));
    }

    public abstract class ExpanderExpansionBaseHandler
    {
        protected readonly Expander Expander;
        private string _toAnimateTemplateControlName;
        protected FrameworkElement ToAnimateControl;
        protected FrameworkElement ContentControl;

        protected ExpanderExpansionBaseHandler(Expander expander, string toAnimateTemplateControlName)
        {
            Expander = expander;
            _toAnimateTemplateControlName = toAnimateTemplateControlName;

            if (expander.IsLoaded)
            {
                FindAnimationControls();
            }
            else
            {
                expander.Loaded += OnLoaded;
            }
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            FindAnimationControls();
            Expander.Loaded -= OnLoaded;
        }

        private void FindAnimationControls()
        {
            FindToAnimateControl();
            ContentControl = LogicalTreeHelper.GetChildren(Expander).OfType<FrameworkElement>().LastOrDefault();
        }

        private void FindToAnimateControl()
        {
            ToAnimateControl =
                Expander?.Template?.FindName(_toAnimateTemplateControlName, Expander) as FrameworkElement;
        }

        public void UpdateToAnimateControl(string newToAnimateControlName)
        {
            _toAnimateTemplateControlName = newToAnimateControlName;
            FindToAnimateControl();
        }

        protected abstract DependencyProperty GetToAnimateProperty();
        protected abstract double GetAnimationToValue();

        public void Handle(TimeSpan animationDuration)
        {
            if (ToAnimateControl is null || ContentControl is null || Expander is null)
            {
                return;
            }

            var correctionFactor = Expander.ExpandDirection switch
            {
                ExpandDirection.Down or ExpandDirection.Left => -1,
                _ => 1
            };

            if (Expander.IsExpanded)
            {
                AnimateExpand(animationDuration, correctionFactor);
            }
            else
            {
                AnimateCollapse(animationDuration, correctionFactor);
            }
        }

        private void AnimateExpand(TimeSpan animationDuration, int correctionFactor)
        {
            ToAnimateControl.BeginAnimation(UIElement.VisibilityProperty, null);
            ToAnimateControl.Visibility = Visibility.Visible;

            UpdateLayout(ContentControl);

            if (ToAnimateControl.RenderTransform is not TranslateTransform translateTransform)
            {
                ToAnimateControl.RenderTransform = translateTransform = new TranslateTransform();
            }

            var animationProperty = GetToAnimateProperty();
            if (!translateTransform.IsSealed)
            {
                //this will only work before any animation is applied
                translateTransform.SetValue(animationProperty, correctionFactor * GetAnimationToValue());
            }

            RunTranslationAnimation(animationDuration, translateTransform, animationProperty, 0);
        }

        private void AnimateCollapse(TimeSpan animationDuration, int correctionFactor)
        {
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

            UpdateLayout(ContentControl);

            if (ToAnimateControl.RenderTransform is not TranslateTransform translateTransform)
            {
                ToAnimateControl.RenderTransform = translateTransform = new TranslateTransform();
            }

            ToAnimateControl.BeginAnimation(UIElement.VisibilityProperty, visibilityAnimation);
            RunTranslationAnimation(animationDuration, translateTransform, GetToAnimateProperty(),
                correctionFactor * GetAnimationToValue());
        }

        protected static void RunTranslationAnimation(
            TimeSpan animationDuration,
            TranslateTransform translateTransform,
            DependencyProperty toAnimateProperty,
            double targetValue)
        {
            var yAnimation = new DoubleAnimationUsingKeyFrames
            {
                KeyFrames = new DoubleKeyFrameCollection
                {
                    new SplineDoubleKeyFrame
                    {
                        KeySpline = new KeySpline(0, 0, 0, 1),
                        KeyTime = animationDuration,
                        Value = targetValue
                    },
                },

                Duration = animationDuration
            };

            translateTransform.BeginAnimation(toAnimateProperty, yAnimation);
        }

        protected static void UpdateLayout(FrameworkElement contentControl)
        {
            //update content measures
            contentControl.Measure(new Size(contentControl.MaxWidth, contentControl.MaxHeight));
            contentControl.UpdateLayout();
        }
    }

    public sealed class ExpanderHorizontalExpansionHandler(Expander expander, string toAnimateTemplateControlName)
        : ExpanderExpansionBaseHandler(expander, toAnimateTemplateControlName)
    {
        protected override DependencyProperty GetToAnimateProperty() => TranslateTransform.XProperty;
        protected override double GetAnimationToValue() => ContentControl.ActualWidth;
    }

    public sealed class ExpanderVerticalExpansionHandler(Expander expander, string toAnimateTemplateControlName)
        : ExpanderExpansionBaseHandler(expander, toAnimateTemplateControlName)
    {
        protected override DependencyProperty GetToAnimateProperty() => TranslateTransform.YProperty;

        protected override double GetAnimationToValue() => ContentControl.ActualHeight;
    }
}