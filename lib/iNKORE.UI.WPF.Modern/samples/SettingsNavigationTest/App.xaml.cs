using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace SettingsNavigationTest
{
    /// <summary>
    /// App.xaml 的交互逻辑
    /// </summary>
    public partial class App : Application
    {
    }
    internal static class VisualTreeExtensions
    {
        public static T TryFindParent<T>(this DependencyObject child) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(child);
            while (parent != null)
            {
                if (parent is T parentType) return parentType;
                parent = VisualTreeHelper.GetParent(parent);
            }

            return null;
        }
    }
}
