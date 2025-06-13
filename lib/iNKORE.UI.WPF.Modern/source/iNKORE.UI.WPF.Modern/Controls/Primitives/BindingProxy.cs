using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls.Primitives
{
    public class BindingProxy : Freezable
    {
        #region Value

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
                nameof(Value),
                typeof(object),
                typeof(BindingProxy));

        public object Value
        {
            get => GetValue(ValueProperty);
            set => SetValue(ValueProperty, value);
        }

        #endregion

        protected override Freezable CreateInstanceCore()
        {
            return new BindingProxy();
        }
    }
}
