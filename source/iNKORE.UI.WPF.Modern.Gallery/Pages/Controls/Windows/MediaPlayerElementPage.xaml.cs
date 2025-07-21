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
    /// MediaPlayerElementPage.xaml 的交互逻辑
    /// </summary>
    public partial class MediaPlayerElementPage : Page
    {
        public MediaPlayerElementPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:MediaPlayerElement AreTransportControlsEnabled=""True"" AutoPlay=""False"" Tag=""Assets/SampleMedia/ladybug.wmv"" 
    Source=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource RelativeToAbsoluteConverter}}}}"" />
";

        public string Example2Xaml => $@"
<ui:MediaPlayerElement AutoPlay=""True"" Tag=""Assets/SampleMedia/fishes.wmv"" 
    Source=""{{Binding Tag, RelativeSource={{RelativeSource Self}}, Converter={{StaticResource RelativeToAbsoluteConverter}}}}"" />
";

        #endregion

    }
}
