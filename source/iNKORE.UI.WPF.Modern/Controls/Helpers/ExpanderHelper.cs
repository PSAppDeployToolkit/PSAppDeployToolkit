using iNKORE.UI.WPF.Helpers;
using iNKORE.UI.WPF.Modern.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Controls.Helpers
{
    public class ExpanderHelper
    {
        #region IsEnabled

        public static bool GetIsEnabled(Expander element)
        {
            return (bool)element.GetValue(IsEnabledProperty);
        }

        public static void SetIsEnabled(Expander element, bool value)
        {
            element.SetValue(IsEnabledProperty, value);
        }

        public static readonly DependencyProperty IsEnabledProperty = DependencyProperty.RegisterAttached(
            "IsEnabled",
            typeof(bool),
            typeof(ExpanderHelper),
            new PropertyMetadata(OnIsEnabledChanged));

        private static void OnIsEnabledChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var element = (Expander)d;
            if (element.IsLoaded)
            {
                AddSizeChanged();
            }
            else
            {
                element.Loaded += (sender, e) => AddSizeChanged();
            }

            void AddSizeChanged()
            {
                if (element.FindDescendantByName("ExpanderContent") is FrameworkElement expanderContent)
                {
                    if ((bool)e.NewValue)
                    {
                        expanderContent.SizeChanged += OnContentSizeChanged;
                    }
                    else
                    {
                        expanderContent.SizeChanged -= OnContentSizeChanged;
                    }
                }
                else if (element.Content is FrameworkElement content)
                {
                    if ((bool)e.NewValue)
                    {
                        content.SizeChanged += OnContentSizeChanged;
                    }
                    else
                    {
                        content.SizeChanged -= OnContentSizeChanged;
                    }
                }
            }
        }

        #endregion

        #region ContentWidth

        public static double GetContentWidth(Expander element)
        {
            return (double)element.GetValue(ContentWidthProperty);
        }

        private static void SetContentWidth(Expander element, double value)
        {
            element.SetValue(ContentWidthPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ContentWidthPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ContentWidth",
                typeof(double),
                typeof(ExpanderHelper),
                null);

        public static readonly DependencyProperty ContentWidthProperty =
            ContentWidthPropertyKey.DependencyProperty;

        #endregion

        #region NegativeContentWidth

        public static double GetNegativeContentWidth(Expander element)
        {
            return (double)element.GetValue(NegativeContentWidthProperty);
        }

        private static void SetNegativeContentWidth(Expander element, double value)
        {
            element.SetValue(NegativeContentWidthPropertyKey, value);
        }

        private static readonly DependencyPropertyKey NegativeContentWidthPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "NegativeContentWidth",
                typeof(double),
                typeof(ExpanderHelper),
                null);

        public static readonly DependencyProperty NegativeContentWidthProperty =
            NegativeContentWidthPropertyKey.DependencyProperty;

        #endregion

        #region ContentHeight

        public static double GetContentHeight(Expander element)
        {
            return (double)element.GetValue(ContentHeightProperty);
        }

        private static void SetContentHeight(Expander element, double value)
        {
            element.SetValue(ContentHeightPropertyKey, value);
        }

        private static readonly DependencyPropertyKey ContentHeightPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "ContentHeight",
                typeof(double),
                typeof(ExpanderHelper),
                null);

        public static readonly DependencyProperty ContentHeightProperty =
            ContentHeightPropertyKey.DependencyProperty;

        #endregion

        #region NegativeContentHeight

        public static double GetNegativeContentHeight(Expander element)
        {
            return (double)element.GetValue(NegativeContentHeightProperty);
        }

        private static void SetNegativeContentHeight(Expander element, double value)
        {
            element.SetValue(NegativeContentHeightPropertyKey, value);
        }

        private static readonly DependencyPropertyKey NegativeContentHeightPropertyKey =
            DependencyProperty.RegisterAttachedReadOnly(
                "NegativeContentHeight",
                typeof(double),
                typeof(ExpanderHelper),
                null);

        public static readonly DependencyProperty NegativeContentHeightProperty =
            NegativeContentHeightPropertyKey.DependencyProperty;

        #endregion

        private static void OnContentSizeChanged(object sender, SizeChangedEventArgs args)
        {
            if (((FrameworkElement)sender).FindAscendant<Expander>() is Expander expander)
            {
                var width = args.NewSize.Width;
                SetContentWidth(expander, width);
                SetNegativeContentWidth(expander, -1 * width);

                var height = args.NewSize.Height;
                SetContentHeight(expander, height);
                SetNegativeContentHeight(expander, -1 * height);
            }
        }
    }
}
