using iNKORE.UI.WPF.Modern.Controls;
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
    /// CanvasPage.xaml 的交互逻辑
    /// </summary>
    public partial class CanvasPage : Page
    {
        private ObservableCollection<ControlExampleSubstitution> Substitutions = new ObservableCollection<ControlExampleSubstitution>();

        public CanvasPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            ControlExampleSubstitution Substitution1 = new ControlExampleSubstitution
            {
                Key = "Left",
            };
            BindingOperations.SetBinding(Substitution1, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = TopSlider,
                Path = new PropertyPath("Value"),
            });
            ControlExampleSubstitution Substitution2 = new ControlExampleSubstitution
            {
                Key = "Top",
            };
            BindingOperations.SetBinding(Substitution2, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = LeftSlider,
                Path = new PropertyPath("Value"),
            });
            ControlExampleSubstitution Substitution3 = new ControlExampleSubstitution
            {
                Key = "Z",
            };
            BindingOperations.SetBinding(Substitution3, ControlExampleSubstitution.ValueProperty, new Binding
            {
                Source = ZSlider,
                Path = new PropertyPath("Value"),
            });
            Example1.Substitutions = new ObservableCollection<ControlExampleSubstitution> { Substitution1, Substitution2, Substitution3 };

            UpdateExampleCode();
        }

        private void TopSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void LeftSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        private void ZSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Canvas
    x:Name=""Control1""
    Width=""140""
    Height=""140""
    VerticalAlignment=""Top""
    Background=""Gray"">
    <Canvas.Resources>
        <Style TargetType=""Rectangle"">
            <Setter Property=""Height"" Value=""40"" />
            <Setter Property=""Width"" Value=""40"" />
        </Style>
    </Canvas.Resources>
    <Rectangle
        Canvas.Left=""{LeftSlider.Value}""
        Canvas.Top=""{TopSlider.Value}""
        Canvas.ZIndex=""{ZSlider.Value}""
        Fill=""Red"" />
    <Rectangle
        Canvas.Left=""20""
        Canvas.Top=""20""
        Canvas.ZIndex=""1""
        Fill=""Blue"" />
    <Rectangle
        Canvas.Left=""40""
        Canvas.Top=""40""
        Canvas.ZIndex=""2""
        Fill=""Green"" />
    <Rectangle
        Canvas.Left=""60""
        Canvas.Top=""60""
        Canvas.ZIndex=""3""
        Fill=""Yellow"" />
</Canvas>
";

        #endregion
    }
}
