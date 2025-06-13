using iNKORE.UI.WPF.Controls;
using iNKORE.UI.WPF.Modern.Common.IconKeys;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class IconAndText : ContentControl
    {
        static IconAndText()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(IconAndText), new FrameworkPropertyMetadata(typeof(IconAndText)));
        }

        #region Properties

        public static readonly DependencyProperty IconProperty = FontIcon.IconProperty.AddOwner(typeof(IconAndText));
        public FontIconData? Icon
        {
            get { return (FontIconData?)GetValue(IconProperty); }
            set { SetValue(IconProperty, value); }
        }

        public static readonly DependencyProperty SpacingProperty = SimpleStackPanel.SpacingProperty.AddOwner(typeof(IconAndText), new PropertyMetadata(6d));
        public double Spacing
        {
            get { return (double)GetValue(SpacingProperty); }
            set { SetValue(SpacingProperty, value); }
        }

        public static readonly DependencyProperty OrientationProperty = SimpleStackPanel.OrientationProperty.AddOwner(typeof(IconAndText), new PropertyMetadata(Orientation.Horizontal));
        public Orientation Orientation
        {
            get { return (Orientation)GetValue(OrientationProperty); }
            set { SetValue(OrientationProperty, value); }
        }

        public static readonly DependencyProperty IconSizeProperty = DependencyProperty.Register(nameof(IconSize), typeof(double), typeof(IconAndText), new PropertyMetadata(16d));
        public double IconSize
        {
            get { return (double)GetValue(IconSizeProperty); }
            set { SetValue(IconSizeProperty, value); }
        }


        #endregion
    }
}
