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

namespace iNKORE.UI.WPF.Modern.Gallery.Samples
{
    /// <summary>
    /// SampleStandardSizingPage.xaml 的交互逻辑
    /// </summary>
    public partial class SampleStandardSizingPage : Page
    {
        public TextBox FirstName => firstName;
        public TextBox LastName => lastName;
        public PasswordBox Password => password;
        public PasswordBox ConfirmPassword => confirmPassword;
        public DatePicker ChosenDate => chosenDate;

        public SampleStandardSizingPage()
        {
            InitializeComponent();
        }

        public void CopyState(SampleCompactSizingPage page)
        {
            FirstName.Text = page.FirstName.Text;
            LastName.Text = page.LastName.Text;
            Password.Password = page.Password.Password;
            ConfirmPassword.Password = page.ConfirmPassword.Password;
            ChosenDate.SelectedDate = page.ChosenDate.SelectedDate;
        }

        public static string CodeXaml => $@"
<ui:Page
    x:Class=""iNKORE.UI.WPF.Modern.Gallery.Samples.SampleStandardSizingPage""
    xmlns=""http://schemas.microsoft.com/winfx/2006/xaml/presentation""
    xmlns:x=""http://schemas.microsoft.com/winfx/2006/xaml""
    xmlns:d=""http://schemas.microsoft.com/expression/blend/2008""
    xmlns:local=""clr-namespace:iNKORE.UI.WPF.Modern.Gallery.Samples""
    xmlns:mc=""http://schemas.openxmlformats.org/markup-compatibility/2006""
    xmlns:ikw=""http://schemas.inkore.net/lib/ui/wpf""
    xmlns:ui=""http://schemas.inkore.net/lib/ui/wpf/modern"" 
    Title=""SampleStandardSizingPage""
    d:DesignHeight=""450""
    d:DesignWidth=""800""
    mc:Ignorable=""d"">
    <Grid Background=""{{DynamicResource ApplicationPageBackgroundThemeBrush}}"">
        <ikw:SimpleStackPanel x:Name=""CompactPanel"" Spacing=""16"">
            <TextBlock
                x:Name=""HeaderBlock""
                FontSize=""18""
                Text=""Standard Size"" />
            <TextBox x:Name=""firstName"" ui:ControlHelper.Header=""First Name:"" />
            <TextBox x:Name=""lastName"" ui:ControlHelper.Header=""Last Name:"" />
            <PasswordBox x:Name=""password"" ui:ControlHelper.Header=""Password:"" />
            <PasswordBox x:Name=""confirmPassword"" ui:ControlHelper.Header=""Confirm Password:"" />
            <DatePicker x:Name=""chosenDate"" ui:ControlHelper.Header=""Pick a date"" />
        </ikw:SimpleStackPanel>
    </Grid>
</ui:Page>
";
    }
}
