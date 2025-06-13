using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// TimePickerPage.xaml 的交互逻辑
    /// </summary>
    public partial class TimePickerPage : Page
    {
        public TimePickerPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<ui:TimePicker />
";

        #endregion

    }
}
