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
            Example6.Xaml = Example6Xaml;
        }

        public string Example1Xaml => $@"
<ui:BitmapIcon x:Name=""SlicesIcon""
    Width=""50"" Height=""50""
    HorizontalAlignment=""Left""
    ShowAsMonochrome=""{SlicesIcon.ShowAsMonochrome}""
    UriSource=""/Assets/slices.png"" />
";

        public string Example2Xaml => $@"
<Button Name=""ExampleButton1"">
    <ui:FontIcon FontFamily=""Segoe MDL2 Assets"" Glyph=""&#xE790;"" />
</Button>
";

        public string Example3Xaml => $@"
<Button Name=""ImageExample1"" Width=""100"">
    <ui:ImageIcon Source=""/Assets/slices.png"" />
</Button>
";

        public string Example4Xaml => $@"
<Button Name=""ImageExample2"">
    <ui:ImageIcon Width=""50"" Source=""https://raw.githubusercontent.com/DiemenDesign/LibreICONS/master/svg-color/libre-camera-panorama.svg"" />
</Button>
";

        public string Example5Xaml => $@"
<Button Name=""Example1Button"">
    <ui:PathIcon HorizontalAlignment=""Center"" Data=""F1 M 16,12 20,2L 20,16 1,16"" />
</Button>
";

        public string Example6Xaml => $@"
<Button Name=""AcceptButton"">
    <ui:IconAndText Content=""Confirm""
        Icon=""{{x:Static ui:SegoeFluentIcons.Accept}}""/>
</Button>
";

        #endregion
    }
}
