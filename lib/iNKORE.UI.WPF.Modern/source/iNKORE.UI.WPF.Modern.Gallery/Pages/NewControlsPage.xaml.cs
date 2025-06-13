using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages
{
    /// <summary>
    /// NewControlsPage.xaml 的交互逻辑
    /// </summary>
    public sealed partial class NewControlsPage : ItemsPageBase
    {
        public NewControlsPage()
        {
            InitializeComponent();
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            var menuItem = NavigationRootPage.Current.NavigationView.MenuItems.Cast<NavigationViewItem>().First();
            menuItem.IsSelected = true;
            NavigationRootPage.Current.NavigationView.Header = string.Empty;

            var items = new List<ControlInfoDataItem>();
            foreach (var realm in ControlInfoDataSource.Instance.Realms)
            {
                items.AddRange(realm.Groups.SelectMany(g => g.Items.Where(i => i.BadgeString != null)).OrderBy(i => i.Title).ToList());
            }

            Items = items;  
            DataContext = Items;
        }
    }

    public class ControlsGroupKeyConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value.ToString())
            {
                case "New":
                    return "Recently Added Samples";
                case "Updated":
                    return "Recently Updated Samples";
                case "Preview":
                    return "Preview Samples";
                default:
                    return value;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            switch (value.ToString())
            {
                case "Recently Added Samples":
                    return "New";
                case "Recently Updated Samples":
                    return "Updated";
                case "Preview Samples":
                    return "Preview";
                default:
                    return value;
            }
        }
    }
}
