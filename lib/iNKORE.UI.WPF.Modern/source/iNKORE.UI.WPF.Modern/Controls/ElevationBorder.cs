using iNKORE.UI.WPF.Modern.Media.Utils;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class ElevationBorder : ContentControl
    {
        static ElevationBorder()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ElevationBorder), new FrameworkPropertyMetadata(typeof(ElevationBorder)));
            BorderBrushProperty.OverrideMetadata(typeof(ElevationBorder), new FrameworkPropertyMetadata() { PropertyChangedCallback = ElevationRelatedProperty_ValueChanged });
        }

        public static readonly DependencyProperty ElevationColorProperty = DependencyProperty.RegisterAttached(nameof(ElevationColor), typeof(Color?), typeof(ElevationBorder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits) { PropertyChangedCallback = ElevationRelatedProperty_ValueChanged });

        private static void ElevationRelatedProperty_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ElevationBorder)?.RefreshElevationBrush();
        }


        public Color? ElevationColor
        {
            get {  return (Color?)GetValue(ElevationColorProperty); }
            set { SetValue(ElevationColorProperty, value); }
        }

        public static Color? GetElevationColor(DependencyObject d)
        {
            return (Color?)d?.GetValue(ElevationColorProperty);
        }

        public static void SetElevationColor(DependencyObject d, Color? value)
        {
            d.SetValue(ElevationColorProperty, value);
        }

        public static readonly DependencyProperty IsElevationOnTopProperty = DependencyProperty.RegisterAttached(nameof(IsElevationOnTop), typeof(bool?), typeof(ElevationBorder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits) { PropertyChangedCallback = ElevationRelatedProperty_ValueChanged });
        public bool? IsElevationOnTop
        {
            get { return (bool?)GetValue(IsElevationOnTopProperty); }
            set { SetValue(IsElevationOnTopProperty, value); }
        }

        public static bool? GetIsElevationOnTop(DependencyObject d)
        {
            return (bool?)d?.GetValue(IsElevationOnTopProperty);
        }

        public static void SetIsElevationOnTop(DependencyObject d, bool? value)
        {
            d?.SetValue(IsElevationOnTopProperty, value);
        }

        public static readonly DependencyProperty ElevationOpacityProperty = DependencyProperty.RegisterAttached(nameof(ElevationOpacity), typeof(double?), typeof(ElevationBorder), new FrameworkPropertyMetadata(null, FrameworkPropertyMetadataOptions.AffectsRender | FrameworkPropertyMetadataOptions.Inherits) { PropertyChangedCallback = ElevationRelatedProperty_ValueChanged });
        public double? ElevationOpacity
        {
            get { return (double?)GetValue(ElevationOpacityProperty); }
            set { SetValue(ElevationOpacityProperty, value); }
        }

        public static double? GetElevationOpacity(DependencyObject d)
        {
            return (double?)d?.GetValue(ElevationOpacityProperty);
        }

        public static void SetElevationOpacity(DependencyObject d, double? value)
        {
            d?.SetValue(ElevationOpacityProperty, value);
        }


        public ElevationBorder()
        {
            _elevationBrush_Stop1 = new GradientStop(Colors.Transparent, 0);
            _elevationBrush_Stop2 = new GradientStop() { Offset = 1 };

            //BindingOperations.SetBinding(_elevationBrush_Stop2, GradientStop.ColorProperty, new Binding(nameof(ElevationColor)) { Source = this });
            //BindingOperations.SetBinding(_elevationBrush_Stop2, GradientStop.OffsetProperty, new Binding(nameof(ElevationTransitionLength)) { Source = this });

            ElevationBrush = new LinearGradientBrush(new GradientStopCollection()
            {
                _elevationBrush_Stop1,
                _elevationBrush_Stop2,
            })
            {
                MappingMode = BrushMappingMode.Absolute
            };

            this.SizeChanged += ElevationBorder_SizeChanged;

            RefreshElevationBrush();
        }

        private void ElevationBorder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            RefreshElevationBrush();
        }

        private GradientStop _elevationBrush_Stop1;
        private GradientStop _elevationBrush_Stop2;

        public static readonly DependencyProperty CornerRadiusProperty = Border.CornerRadiusProperty.AddOwner(typeof(ElevationBorder));
        public CornerRadius CornerRadius
        {
            get { return (CornerRadius)GetValue(CornerRadiusProperty); }
            set { SetValue(CornerRadiusProperty, value); }
        }

        public static readonly DependencyProperty ElevationLengthProperty = DependencyProperty.Register(nameof(ElevationLength), typeof(double), typeof(ElevationBorder), new PropertyMetadata(1d, ElevationLengthProperty_ValueChanged));

        private static void ElevationLengthProperty_ValueChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as ElevationBorder)?.ElevationLengthProperty_ValueChanged(d, e);
        }
        private void ElevationLengthProperty_ValueChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            RefreshElevationBrush();
        }

        private void RefreshElevationBrush()
        {
            bool elevationOnTop = IsElevationOnTop ?? GetIsElevationOnTop(BorderBrush) ?? false;

            if (elevationOnTop)
            {
                //_elevationBrush_Stop1.Offset = 0;
                //_elevationBrush_Stop2.Offset = 1;

                ElevationBrush.EndPoint = new Point(0.5, 0);
                ElevationBrush.StartPoint = new Point(0.5, ElevationLength + ElevationTransitionLength);
            }
            else
            {
                //_elevationBrush_Stop1.Offset = 1;
                //_elevationBrush_Stop2.Offset = 0;

                ElevationBrush.StartPoint = new Point(0.5, Math.Max(this.ActualHeight - ElevationLength - ElevationTransitionLength, 0));
                ElevationBrush.EndPoint = new Point(0.5, Math.Max(this.ActualHeight - ElevationLength, 0));
            }

            ElevationBrush.Opacity = ElevationOpacity ?? GetElevationOpacity(BorderBrush) ?? 1d;
            _elevationBrush_Stop2.Color = ElevationColor ?? GetElevationColor(BorderBrush) ?? Colors.Transparent;
        }

        public double ElevationLength
        {
            get { return (double)GetValue(ElevationLengthProperty); }
            set { SetValue(ElevationLengthProperty, value); }
        }

        public static readonly DependencyProperty ElevationTransitionLengthProperty = DependencyProperty.Register(nameof(ElevationTransitionLength), typeof(double), typeof(ElevationBorder), new PropertyMetadata(3d));
        public double ElevationTransitionLength
        {
            get { return (double)GetValue(ElevationTransitionLengthProperty); }
            set { SetValue(ElevationTransitionLengthProperty, value); }
        }

        public static readonly DependencyProperty IsElevationEnabledProperty = DependencyProperty.RegisterAttached(nameof(IsElevationEnabled), typeof(bool), typeof(UIElement), new FrameworkPropertyMetadata(true) { Inherits = true });
        public bool IsElevationEnabled
        {
            get { return (bool)GetValue(IsElevationEnabledProperty); }
            set { SetValue(IsElevationEnabledProperty, value); }
        }

        public static bool GetIsElevationEnabled(DependencyObject d)
        {
            return (bool)d.GetValue(IsElevationEnabledProperty);
        }

        public static void SetIsElevationEnabled(DependencyObject d, bool value)
        {
            d.SetValue(IsElevationEnabledProperty, value);
        }

        public static readonly DependencyPropertyKey ElevationBrushPropertyKey = DependencyProperty.RegisterReadOnly(nameof(ElevationBrush), typeof(LinearGradientBrush), typeof(ElevationBorder), new PropertyMetadata(null));
        public static readonly DependencyProperty ElevationBrushProperty = ElevationBrushPropertyKey.DependencyProperty;
        public LinearGradientBrush ElevationBrush
        {
            get { return (LinearGradientBrush)GetValue(ElevationBrushProperty); }
            set { SetValue(ElevationBrushPropertyKey, value); }
        }
    }
}
