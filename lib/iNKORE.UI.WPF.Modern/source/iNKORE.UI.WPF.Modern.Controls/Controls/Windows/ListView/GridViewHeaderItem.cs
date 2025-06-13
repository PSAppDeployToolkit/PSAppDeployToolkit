using System.Windows;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class GridViewHeaderItem : ListViewBaseHeaderItem
    {
        static GridViewHeaderItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(GridViewHeaderItem), new FrameworkPropertyMetadata(typeof(GridViewHeaderItem)));
        }

        public GridViewHeaderItem()
        {
        }

        #region Properties

        public static readonly DependencyProperty DividerVisibilityProperty = DependencyProperty.Register("DividerVisibility", typeof(Visibility), typeof(GridViewHeaderItem), new PropertyMetadata(Visibility.Visible));

        public Visibility DividerVisibility
        {
            get { return (Visibility)GetValue(DividerVisibilityProperty); }
            set { SetValue(DividerVisibilityProperty, value); }
        }

        #endregion
    }
}
