using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls.Primitives;
using iNKORE.UI.WPF.Modern.Helpers.Styles;
using iNKORE.UI.WPF.Modern.Gallery.Samples;
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
    /// SystemBackdropsPage.xaml 的交互逻辑
    /// </summary>
    public partial class SystemBackdropsPage : Page
    {
        public SystemBackdropsPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

         private void createBuiltInWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new SampleSystemBackdropsWindow(
                SampleSystemBackdropsWindow.BackdropPickerMode.Full);
            win.Show();
        }

        private void createMicaWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new SampleSystemBackdropsWindow(
                SampleSystemBackdropsWindow.BackdropPickerMode.MicaOnly);
            win.Show();
        }

        private void createAcrylicWindow_Click(object sender, RoutedEventArgs e)
        {
            var win = new SampleSystemBackdropsWindow(
                SampleSystemBackdropsWindow.BackdropPickerMode.AcrylicOnly);
            win.Show();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.CSharp = Example1CS;
            Example2.CSharp = Example2CS;
            Example3.CSharp = Example3CS;
        }

        public string Example1CS => $@"
bool TrySetMicaBackdrop(bool useMicaAlt)
{{
    if (OSVersionHelper.IsWindows11OrGreater)
    {{
        var backdropType = useMicaAlt ? BackdropType.Tabbed : BackdropType.Mica;
        WindowHelper.SetSystemBackdropType(this, backdropType);

        return true; // Succeeded.
    }}

    return false; // Mica is not supported on this system.
}}

bool TrySetDesktopAcrylicBackdrop()
{{
    if (OSVersionHelper.IsWindows11OrGreater)
    {{
        WindowHelper.SetSystemBackdropType(this, BackdropType.Acrylic);

        return true; // Succeeded.
    }}

    return false; // DesktopAcrylic is not supported on this system.
}}";

        public string Example2CS => $@"
var newWindow = new SampleSystemBackdropsWindow();
WindowHelper.SetSystemBackdropType(newWindow, BackdropType.Mica);
newWindow.Show();
";

        public string Example3CS => $@"
var newWindow = new SampleSystemBackdropsWindow();
WindowHelper.SetSystemBackdropType(newWindow,BackdropType.Acrylic);
newWindow.Show();
";

        #endregion
    }
}
