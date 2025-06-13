using iNKORE.UI.WPF.Modern.Controls;
using iNKORE.UI.WPF.Modern.Gallery.Common;
using iNKORE.UI.WPF.Modern.Gallery.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// ButtonPage.xaml 的交互逻辑
    /// </summary>
    public partial class ButtonPage : Page
    {
        public ButtonPage()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button b)
            {
                string name = b.Name;

                switch (name)
                {
                    case "Button1":
                        Control1Output.Text = "You clicked: " + name;
                        break;
                    case "Button2":
                        Control2Output.Text = "You clicked: " + name;
                        break;
                }
            }
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution = new ControlExampleSubstitution
            {
                Key = "IsEnabled",
                Value = @"IsEnabled=""False"" "
            };
            BindingOperations.SetBinding(Substitution, ControlExampleSubstitution.IsEnabledProperty, new Binding
            {
                Source = DisableButton1,
                Path = new PropertyPath("IsChecked"),
            });
            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution };
            Example1.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        private void DisableButton1_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
        }


        public string Example1Xaml => $@"
<Button x:Name=""Button1"" Click=""Button_Click""
    Content=""Standard XAML button"" IsEnabled=""{Button1.IsEnabled}"" />
";

        public string Example2Xaml => $@"
<Button x:Name=""Button2""
    Width=""50"" Height=""50""
    AutomationProperties.Name=""Pie""
    Click=""Button_Click"">
    <Image AutomationProperties.Name=""Slice"" Source=""/Assets/Slices.png"" />
</Button>
";

        public string Example3Xaml => $@"
<StackPanel>
    <TextBlock
        Margin=""0,0,0,8""
        Text=""The following buttons' content may get clipped if we don't pay careful attention to their layout containers.""
        TextWrapping=""Wrap"" />
    <TextBlock
        Margin=""0,0,0,8""
        Text=""One option to mitigate clipped content is to place Buttons underneath each other, allowing for more space to grow horizontally:""
        TextWrapping=""Wrap"" />
    <Button Margin=""0,0,0,5"" HorizontalAlignment=""Stretch"">
        This is some text that is too long and will get cut off
    </Button>
    <Button HorizontalAlignment=""Stretch"">This is another text that would result in being cut off</Button>

    <TextBlock Margin=""0,8,0,8"" Text=""Another option is to explicitly wrap the Button's content"" />
    <StackPanel HorizontalAlignment=""Center"" Orientation=""Horizontal"">
        <Button MaxWidth=""240"" Margin=""0,0,8,0"">
            <TextBlock Text=""This is some text that is too long and will get cut off"" TextWrapping=""Wrap"" />
        </Button>
        <Button MaxWidth=""240"">
            <TextBlock Text=""This is another text that would result in being cut off"" TextWrapping=""Wrap"" />
        </Button>
    </StackPanel>
</StackPanel>
";

        public string Example4Xaml => $@"
<Button Content=""Accent style button"" Style=""{{DynamicResource {{x:Static ui:ThemeKeys.AccentButtonStyleKey}}}}"" />
";

        #endregion
    }
}
