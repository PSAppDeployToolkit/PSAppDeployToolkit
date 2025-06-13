using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;

namespace SamplesCommon
{
    public partial class LayoutDensitySelector : UserControl
    {
        private ResourceDictionary _compactResources;

        public LayoutDensitySelector()
        {
            InitializeComponent();
        }

        #region TargetElement

        public static readonly DependencyProperty TargetElementProperty =
            DependencyProperty.Register(
                nameof(TargetElement),
                typeof(FrameworkElement),
                typeof(LayoutDensitySelector),
                null);

        public FrameworkElement TargetElement
        {
            get => (FrameworkElement)GetValue(TargetElementProperty);
            set => SetValue(TargetElementProperty, value);
        }

        #endregion

        #region IsCompact

        public static readonly DependencyProperty IsCompactProperty =
            DependencyProperty.Register(
                nameof(IsCompact),
                typeof(bool),
                typeof(LayoutDensitySelector),
                new PropertyMetadata(false, OnIsCompactChanged));

        public bool IsCompact
        {
            get => (bool)GetValue(IsCompactProperty);
            set => SetValue(IsCompactProperty, value);
        }

        private static void OnIsCompactChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            (d as LayoutDensitySelector)?.OnIsCompactChanged(e);
        }

        private void OnIsCompactChanged(DependencyPropertyChangedEventArgs e)
        {
            if (this.IsCompact)
            {
                RadioButton_Compact.IsChecked = true;

                if (_compactResources != null)
                {
                    TargetElement?.Resources.MergedDictionaries.Remove(_compactResources);
                    _compactResources = null;
                }
            }
            else
            {
                RadioButton_Standard.IsChecked = true;

                if (_compactResources == null)
                {
                    _compactResources = new ResourceDictionary { Source = new Uri("/iNKORE.UI.WPF.Modern;component/Themes/DensityStyles/Compact.xaml", UriKind.Relative) };
                    TargetElement?.Resources.MergedDictionaries.Add(_compactResources);
                }
            }
        }


        #endregion

        private void Standard_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsCompact != false)
            {
                this.IsCompact = false;
            }
        }

        private void Compact_Checked(object sender, RoutedEventArgs e)
        {
            if (this.IsCompact != true)
            {
                this.IsCompact = true;
            }
        }
    }
}
