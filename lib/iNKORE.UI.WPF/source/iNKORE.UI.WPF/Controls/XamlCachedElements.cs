using iNKORE.UI.WPF.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Markup;

namespace iNKORE.UI.WPF.Controls
{
    [ContentProperty(nameof(Elements))]
    public class XamlCachedElements : FrameworkElement
    {
        static XamlCachedElements()
        {
            VisibilityProperty.OverrideMetadata(typeof(XamlCachedElements), new FrameworkPropertyMetadata(Visibility.Collapsed));
        }

        public static readonly DependencyProperty ElementsProperty = DependencyProperty.Register(nameof(Elements), typeof(Collection<object>), typeof(XamlCachedElements), new PropertyMetadata(null));
        public Collection<object> Elements
        {
            get { return (Collection<object>)GetValue(ElementsProperty); }
            set { SetValue(ElementsProperty, value); }
        }

        public static readonly DependencyProperty DeatchOnLoadedProperty = DependencyProperty.Register(nameof(DeatchOnLoaded), typeof(bool), typeof(XamlCachedElements), new PropertyMetadata(true));
        public bool DeatchOnLoaded
        {
            get { return (bool)GetValue(DeatchOnLoadedProperty); }
            set { SetValue(DeatchOnLoadedProperty, value); }
        }

        public static readonly DependencyProperty ClearCollectionOnLoadedProperty = DependencyProperty.Register(nameof(ClearCollectionOnLoaded), typeof(bool), typeof(XamlCachedElements), new PropertyMetadata(true));
        public bool ClearCollectionOnLoaded
        {
            get { return (bool)GetValue(ClearCollectionOnLoadedProperty); }
            set { SetValue(ClearCollectionOnLoadedProperty, value); }
        }

        public XamlCachedElements()
        {
            if (Elements == null)
                Elements = new Collection<object>();

            this.Loaded += XamlCachedElements_Loaded_Static;
        }

        private void XamlCachedElements_Loaded_Static(object sender, RoutedEventArgs e)
        {
            if(sender is XamlCachedElements eles)
            {
                if (eles.DeatchOnLoaded)
                {
                    eles.DetachFromLogicalParent();
                }
                if (eles.ClearCollectionOnLoaded)
                {
                    eles.Elements.Clear();
                }
            }
        }
    }
}
