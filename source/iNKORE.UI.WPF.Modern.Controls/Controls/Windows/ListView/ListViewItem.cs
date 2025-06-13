using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Input;
using iNKORE.UI.WPF.Modern.Controls.Primitives;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public class ListViewItem : ListViewBaseItem
    {
        static ListViewItem()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(ListViewItem), new FrameworkPropertyMetadata(typeof(ListViewItem)));
        }
    }
}
