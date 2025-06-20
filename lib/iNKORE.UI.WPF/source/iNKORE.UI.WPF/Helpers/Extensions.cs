using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows;
using System.Windows.Media;

namespace iNKORE.UI.WPF.Helpers
{
    public static class Extensions
    {
        public const string XmlNamespace = "http://schemas.inkore.net/lib/ui/wpf";

        /// <summary>
        /// Attach this property to a VirtualizingStackPanel as well as its containing ListView
        /// to enable per-pixel scrolling. Uses ScrollUnitProperty (.NET Framework 4.5 and later)
        /// when available, otherwise uses the private VirtualizingPanel.IsPixelBased property
        /// through reflexion. Code is inspired by https://stackoverflow.com/a/17431815/111461
        /// </summary>
        public static readonly DependencyProperty SmoothScrollingProperty =
            DependencyProperty.RegisterAttached("SmoothScrolling", typeof(bool), typeof(Extensions),
                                                new UIPropertyMetadata(false, SmoothScrollingChanged));

        public static bool GetSmoothScrolling(DependencyObject o)
            => (bool)o.GetValue(SmoothScrollingProperty);

        public static void SetSmoothScrolling(DependencyObject o, bool val)
            => o.SetValue(SmoothScrollingProperty, val);

        private static void SmoothScrollingChanged(DependencyObject o, DependencyPropertyChangedEventArgs e)
        {
            if (o is VirtualizingPanel || o is ItemsControl)
            {
                var enum_type = typeof(Window).Assembly.GetType("System.Windows.Controls.ScrollUnit");
                var field_flags = BindingFlags.Public | BindingFlags.Static | BindingFlags.FlattenHierarchy;
                var field = typeof(VirtualizingPanel).GetField("ScrollUnitProperty", field_flags);
                if (enum_type != null && field?.GetValue(null) is DependencyProperty dp)
                {
                    o.SetValue(dp, Enum.Parse(enum_type, (bool)e.NewValue ? "Pixel" : "Item"));
                }
                else if (o is VirtualizingPanel)
                {
                    var prop_flags = BindingFlags.NonPublic | BindingFlags.Instance;
                    var prop = o.GetType().GetProperty("IsPixelBased", prop_flags);
                    prop?.SetValue(o, (bool)e.NewValue, null);
                }
            }
        }

#if NETFRAMEWORK

    public static void Deconstruct<TKey, TValue>(
        this KeyValuePair<TKey, TValue> target,
        out TKey key,
        out TValue value)
    {
        key = target.Key;
        value = target.Value;
    }

#endif
        public static GeneralTransform SafeTransformToVisual(this Visual self, Visual visual)
        {
            if (self.FindCommonVisualAncestor(visual) != null)
            {
                return self.TransformToVisual(visual);
            }
            return Transform.Identity;
        }

        public static object GetProperty(this object item, string name) => item.GetType()?.GetProperty(name)?.GetValue(item, null);

        public static void SetProperty(this object item, string name, object value) => item.GetType()?.GetProperty(name)?.SetValue(item, value);

    }
}