using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// IconElementPage.xaml 的交互逻辑
    /// </summary>
    public partial class IconElementPage : Page
    {
        public IconElementPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution = new ControlExampleSubstitution
            {
                Key = "ShowAsMonochrome",
            };
            BindingOperations.SetBinding(Substitution, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = MonochromeButton,
                Path = new PropertyPath("IsChecked"),
            });
            ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>() { Substitution };
            Example1.Substitutions = Substitutions;

            UpdateExampleCode();
        }

        private void MonochromeButton_Click(object sender, RoutedEventArgs e)
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
            Example5.Xaml = Example5Xaml;
            Example7.Xaml = Example7Xaml;
        }

        public string Example1Xaml => $@"
<ui:BitmapIcon x:Name=""SlicesIcon""
    Width=""50"" Height=""50""
    HorizontalAlignment=""Left""
    ShowAsMonochrome=""{SlicesIcon.ShowAsMonochrome}""
    UriSource=""/Assets/slices.png"" />
";

        public string Example2Xaml => $@"
<ikw:SimpleStackPanel  Spacing=""4"" Orientation=""Vertical"" HorizontalAlignment=""Left"">
    <FrameworkElement.Resources>
        <Style TargetType=""ikw:SimpleStackPanel"">
            <Setter Property=""Spacing"" Value=""8""/>
            <Setter Property=""Orientation"" Value=""Horizontal""/>
        </Style>
        <Style TargetType=""Button"" BasedOn=""{{StaticResource {{x:Static ui:ThemeKeys.DefaultButtonStyleKey}}}}"">
            <Setter Property=""HorizontalAlignment"" Value=""Stretch""/>
            <Setter Property=""HorizontalContentAlignment"" Value=""Left""/>
        </Style>
    </FrameworkElement.Resources>
                        
    <!--Segoe Fluent Icons-->
    <TextBlock Margin=""0,0,0,10"" FontSize=""16"" FontWeight=""SemiBold"">
        Segoe Fluent Icons
    </TextBlock>

    <Button>
        <ikw:SimpleStackPanel>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Settings}}"" />
            <TextBlock>
                SegoeFluentIcons | Settings
            </TextBlock>
        </ikw:SimpleStackPanel>
    </Button>
    <Button>
        <ikw:SimpleStackPanel>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.Document}}"" />
            <TextBlock>
                SegoeFluentIcons | Document
            </TextBlock>
        </ikw:SimpleStackPanel>
    </Button>
    <Button>
        <ikw:SimpleStackPanel>
            <ui:FontIcon Icon=""{{x:Static ui:SegoeFluentIcons.SendFill}}"" />
            <TextBlock>
                SegoeFluentIcons | SendFill
            </TextBlock>
        </ikw:SimpleStackPanel>
    </Button>

    <!--Fluent System Icons-->
    <TextBlock Margin=""0,10"" FontSize=""16"" FontWeight=""SemiBold"">
        Fluent System Icons
    </TextBlock>

    <Button>
        <ikw:SimpleStackPanel>
            <ui:FontIcon Icon=""{{x:Static ui:FluentSystemIcons.Home_16_Regular}}"" />
            <TextBlock>
                FluentSystemIcons | Home_16_Regular
            </TextBlock>
        </ikw:SimpleStackPanel>
    </Button>
    <Button>
        <ikw:SimpleStackPanel>
            <ui:FontIcon Icon=""{{x:Static ui:FluentSystemIcons.Accessibility_16_Regular}}"" />
            <TextBlock>
                FluentSystemIcons | Accessibility_16_Regular
            </TextBlock>
        </ikw:SimpleStackPanel>
    </Button>
</ikw:SimpleStackPanel>
";


        public string Example3Xaml => $@"
<Button Margin=""0,0,0,8"">
    <ui:IconAndText Icon=""{{x:Static ui:SegoeFluentIcons.Audio}}"" Content=""Pick a music""/>
</Button>
<Button Padding=""12,12,12,8"">
    <ui:IconAndText Icon=""{{x:Static ui:SegoeFluentIcons.Send}}"" 
        Content=""Send"" IconSize=""24"" Orientation=""Vertical"" Spacing=""8""/>
</Button>
";

        public string Example4Xaml => $@"
<Button Name=""ExampleButton1"">
    <ui:FontIcon FontFamily=""Segoe MDL2 Assets"" Glyph=""&#xE790;"" />
</Button>
";

        public string Example5Xaml => $@"
<Button Name=""ImageExample1"" Width=""100"">
    <ui:ImageIcon Source=""/Assets/slices.png"" />
</Button>
";

        public string Example6Xaml => $@"
<Button Name=""ImageExample2"">
    <ui:ImageIcon Width=""50"" Source=""https://raw.githubusercontent.com/DiemenDesign/LibreICONS/master/svg-color/libre-camera-panorama.svg"" />
</Button>
";

        public string Example7Xaml => $@"
<Button Name=""Example1Button"">
    <ui:PathIcon HorizontalAlignment=""Center"" Data=""F1 M 16,12 20,2L 20,16 1,16"" />
</Button>
";

        #endregion
    }
}
