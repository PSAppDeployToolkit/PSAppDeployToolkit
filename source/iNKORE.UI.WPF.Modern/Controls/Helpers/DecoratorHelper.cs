using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Controls.Helpers
{
    public static class DecoratorHelper
    {
        #region Child

        public static readonly DependencyProperty ChildProperty =
            DependencyProperty.RegisterAttached(
                "Child",
                typeof(UIElement),
                typeof(DecoratorHelper),
                new PropertyMetadata(default(UIElement), OnChildChanged));

        public static UIElement GetChild(Decorator border)
        {
            return (UIElement)border.GetValue(ChildProperty);
        }

        public static void SetChild(Decorator border, UIElement value)
        {
            border.SetValue(ChildProperty, value);
        }

        private static void OnChildChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            ((Decorator)d).Child = (UIElement)e.NewValue;
        }

        #endregion
    }
}
