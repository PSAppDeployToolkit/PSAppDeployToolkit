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

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    /// <summary>
    /// Interaction logic for MenuPage.xaml
    /// </summary>
    public partial class MenuPage
    {
        public MenuPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        #endregion

        public string Example1Xaml => $@"
<Menu>
    <MenuItem Header=""File"">
        <MenuItem Header=""New"" />
        <MenuItem Header=""Open..."" />
        <MenuItem Header=""Save"" />
        <MenuItem Header=""Exit"" />
    </MenuItem>

    <MenuItem Header=""Edit"">
        <MenuItem Header=""Undo"" />
        <MenuItem Header=""Cut"" />
        <MenuItem Header=""Copy"" />
        <MenuItem Header=""Paste"" />
    </MenuItem>

    <MenuItem Header=""Help"">
        <MenuItem Header=""About"" />
    </MenuItem>
</Menu>
";

        public string Example2Xaml => $@"
<Menu>
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
<Menu>
    <MenuItem Header=""File"">
        <MenuItem Header=""New"">
            <MenuItem Header=""Plain Text Document"" />
            <MenuItem Header=""Rich Text Document"" />
            <MenuItem Header=""Other Formats..."" />
        </MenuItem>
        <MenuItem Header=""Open..."" />
        <MenuItem Header=""Save"" />
        <Separator />
        <MenuItem Header=""Exit"" />
    </MenuItem>

    <MenuItem Header=""Edit"">
        <MenuItem Header=""Undo"" />
        <MenuItem Header=""Cut"" />
        <MenuItem Header=""Copy"" />
        <MenuItem Header=""Paste"" />
    </MenuItem>

    <MenuItem Header=""View"">
        <MenuItem Header=""Output"" />
        <Separator />
        <ui:RadioMenuItem GroupName=""OrientationGroup"" Header=""Landscape"" />
        <ui:RadioMenuItem
            GroupName=""OrientationGroup""
            Header=""Portrait""
            IsChecked=""True"" />
        <Separator />
        <ui:RadioMenuItem GroupName=""SizeGroup"" Header=""Small icons"" />
        <ui:RadioMenuItem
            GroupName=""SizeGroup""
            Header=""Medium icons""
            IsChecked=""True"" />
        <ui:RadioMenuItem GroupName=""SizeGroup"" Header=""Large icons"" />
    </MenuItem>

    <MenuItem Header=""Help"">
        <MenuItem Header=""About"" />
    </MenuItem>
</Menu>
";
    }
}
