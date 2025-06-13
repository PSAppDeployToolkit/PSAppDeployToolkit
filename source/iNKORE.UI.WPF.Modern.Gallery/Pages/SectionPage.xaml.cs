using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.DataModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
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
    /// SectionPage.xaml 的交互逻辑
    /// </summary>
    public partial class SectionPage : ItemsPageBase
    {
        public SectionPage()
        {
            InitializeComponent();
        }

        public static SectionPage Create(ControlInfoDataGroup group)
        {
            var page = new SectionPage();
            if (group != null) page.LoadData(group);
            return page;
        }

        public ControlInfoDataGroup Group { get; private set; }

        public void LoadData(ControlInfoDataGroup group)
        {
            if (group == null) throw new ArgumentNullException("group");
            var menuItem = NavigationRootPage.Current.NavigationView.MenuItems.Cast<NavigationViewItemBase>().Single(i => i.DataContext == group);
            menuItem.IsSelected = true;
            NavigationRootPage.Current.NavigationView.Header = menuItem.Content;

            Group = group;
            Items = group?.Items?.OrderBy(i => i.Title).ToList();
            DataContext = Items;
        }

        protected override void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
        }
    }
}
