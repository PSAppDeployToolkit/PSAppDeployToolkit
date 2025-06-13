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
    /// MenuBarPage.xaml 的交互逻辑
    /// </summary>
    public partial class MenuBarPage : Page
    {
        public MenuBarPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void OnElementClicked(object sender, RoutedEventArgs e)
        {
            var selectedFlyoutItem = sender as MenuItem;
            string exampleNumber = selectedFlyoutItem.Name.Substring(0, 1);
            if (exampleNumber == "o")
            {
                SelectedOptionText.Text = "You clicked: " + (sender as MenuItem).Header;
            }
            else if (exampleNumber == "t")
            {
                SelectedOptionText1.Text = "You clicked: " + (sender as MenuItem).Header;
            }
            else if (exampleNumber == "z")
            {
                SelectedOptionText2.Text = "You clicked: " + (sender as MenuItem).Header;
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<Menu x:Name=""Menu1"">
    <MenuItem Header=""File"">
        <MenuItem
            x:Name=""o1""
            Click=""OnElementClicked""
            Header=""New"" />
        <MenuItem
            x:Name=""o2""
            Click=""OnElementClicked""
            Header=""Open"" />
        <MenuItem
            x:Name=""o3""
            Click=""OnElementClicked""
            Header=""Save"" />
        <MenuItem
            x:Name=""o4""
            Click=""OnElementClicked""
            Header=""Exit"" />
    </MenuItem>

    <MenuItem Header=""Edit"">
        <MenuItem
            x:Name=""o5""
            Click=""OnElementClicked""
            Header=""Undo"" />
        <MenuItem
            x:Name=""o6""
            Click=""OnElementClicked""
            Header=""Cut"" />
        <MenuItem
            x:Name=""o7""
            Click=""OnElementClicked""
            Header=""Copy"" />
        <MenuItem
            x:Name=""o8""
            Click=""OnElementClicked""
            Header=""Paste"" />
    </MenuItem>

    <MenuItem Header=""Help"">
        <MenuItem
            x:Name=""o9""
            Click=""OnElementClicked""
            Header=""About"" />
    </MenuItem>
</Menu>
";

        public string Example2Xaml => $@"
<Menu x:Name=""Menu2"">
    <MenuItem Header=""File"">
        <MenuItem Header=""New"" InputGestureText=""Ctrl+N"" />
        <MenuItem Header=""Open..."" InputGestureText=""Ctrl+O"" />
        <MenuItem Header=""Save"" InputGestureText=""Ctrl+S"" />
        <MenuItem Header=""Exit"" InputGestureText=""Ctrl+E"" />
    </MenuItem>

    <MenuItem Header=""Edit"">
        <MenuItem Header=""Undo"" InputGestureText=""Ctrl+Z"" />
        <MenuItem Header=""Cut"" InputGestureText=""Ctrl+X"" />
        <MenuItem Header=""Copy"" InputGestureText=""Ctrl+C"" />
        <MenuItem Header=""Paste"" InputGestureText=""Ctrl+V"" />
    </MenuItem>

    <MenuItem Header=""Help"">
        <MenuItem Header=""About"" InputGestureText=""Ctrl+I"" />
    </MenuItem>
</Menu>
";

        public string Example3Xaml => $@"
<Menu x:Name=""Menu3"">
    <MenuItem Header=""File"">
        <MenuItem Header=""New"">
            <MenuItem
                x:Name=""z1""
                Click=""OnElementClicked""
                Header=""Plain Header Document"" />
            <MenuItem
                x:Name=""z2""
                Click=""OnElementClicked""
                Header=""Rich Header Document"" />
            <MenuItem
                x:Name=""z3""
                Click=""OnElementClicked""
                Header=""Other Formats"" />
        </MenuItem>
        <MenuItem
            x:Name=""z4""
            Click=""OnElementClicked""
            Header=""Open"" />
        <MenuItem
            x:Name=""z5""
            Click=""OnElementClicked""
            Header=""Save"" />
        <Separator />
        <MenuItem
            x:Name=""z6""
            Click=""OnElementClicked""
            Header=""Exit"" />
    </MenuItem>

    <MenuItem Header=""Edit"">
        <MenuItem
            x:Name=""z7""
            Click=""OnElementClicked""
            Header=""Undo"" />
        <MenuItem
            x:Name=""z8""
            Click=""OnElementClicked""
            Header=""Cut"" />
        <MenuItem
            x:Name=""z9""
            Click=""OnElementClicked""
            Header=""Copy"" />
        <MenuItem
            x:Name=""z11""
            Click=""OnElementClicked""
            Header=""Paste"" />
    </MenuItem>

    <MenuItem Header=""View"">
        <MenuItem
            x:Name=""z12""
            Click=""OnElementClicked""
            Header=""Output"" />
        <Separator />
        <ui:RadioMenuItem
            x:Name=""z13""
            Click=""OnElementClicked""
            GroupName=""OrientationGroup""
            Header=""Landscape"" />
        <ui:RadioMenuItem
            x:Name=""z14""
            Click=""OnElementClicked""
            GroupName=""OrientationGroup""
            Header=""Portrait""
            IsChecked=""True"" />
        <Separator />
        <ui:RadioMenuItem
            x:Name=""z15""
            Click=""OnElementClicked""
            GroupName=""SizeGroup""
            Header=""Small icons"" />
        <ui:RadioMenuItem
            x:Name=""z16""
            Click=""OnElementClicked""
            GroupName=""SizeGroup""
            Header=""Medium icons""
            IsChecked=""True"" />
        <ui:RadioMenuItem
            x:Name=""z17""
            Click=""OnElementClicked""
            GroupName=""SizeGroup""
            Header=""Large icons"" />
    </MenuItem>

    <MenuItem Header=""Help"">
        <MenuItem
            x:Name=""z18""
            Click=""OnElementClicked""
            Header=""About"" />
    </MenuItem>
</Menu>
";

        #endregion

    }
}
