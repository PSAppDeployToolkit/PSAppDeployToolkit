using iNKORE.UI.WPF.Modern.Controls;
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
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// AutomationPropertiesPage.xaml 的交互逻辑
    /// </summary>
    public partial class AutomationPropertiesPage : Page
    {
        public AutomationPropertiesPage()
        {
            InitializeComponent();
        }

        private void FontSizeNumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            // Ensure that if user clears the NumberBox, we don't pass 0 or null as fontsize
            if (sender.Value >= sender.Minimum && FontSizeChangingTextBlock != null)
            {
                FontSizeChangingTextBlock.FontSize = sender.Value;
            }
            else
            {
                // We fell below minimum, so lets restore a correct value
                sender.Value = sender.Minimum;
            }
        }
    }
}
