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
    /// RadialGradientBrushPage.xaml 的交互逻辑
    /// </summary>
    public partial class RadialGradientBrushPage : Page
    {
        public RadialGradientBrushPage()
        {
            InitializeComponent();
            Loaded += OnPageLoaded;
        }

        private void OnPageLoaded(object sender, RoutedEventArgs e)
        {
            MappingModeComboBox.SelectionChanged += OnMappingModeChanged;
            SpreadMethodComboBox.SelectionChanged += OnSpreadMethodChanged;
            InitializeSliders();
            UpdateExampleCode();
        }

        private void OnSpreadMethodChanged(object sender, SelectionChangedEventArgs e)
        {
            RadialGradientBrushExample.SpreadMethod = (GradientSpreadMethod)Enum.Parse(typeof(GradientSpreadMethod), SpreadMethodComboBox.SelectedValue.ToString());
            UpdateExampleCode();
        }

        private void OnMappingModeChanged(object sender, SelectionChangedEventArgs e)
        {
            RadialGradientBrushExample.MappingMode = (BrushMappingMode)Enum.Parse(typeof(BrushMappingMode), MappingModeComboBox.SelectedValue.ToString());
            InitializeSliders();

            UpdateExampleCode();
        }

        private void InitializeSliders()
        {
            var rectSize = Rect.RenderSize;
            if (RadialGradientBrushExample.MappingMode == BrushMappingMode.Absolute)
            {
                CenterXSlider.Maximum = RadiusXSlider.Maximum = OriginXSlider.Maximum = rectSize.Width;
                CenterYSlider.Maximum = RadiusYSlider.Maximum = OriginYSlider.Maximum = rectSize.Width;
                CenterXSlider.Value = RadiusXSlider.Value = OriginXSlider.Value = rectSize.Width / 2;
                CenterYSlider.Value = RadiusYSlider.Value = OriginYSlider.Value = rectSize.Width / 2;
                CenterXSlider.TickFrequency = RadiusXSlider.TickFrequency = OriginXSlider.TickFrequency = rectSize.Width / 50;
                CenterYSlider.TickFrequency = RadiusYSlider.TickFrequency = OriginYSlider.TickFrequency = rectSize.Height / 50;
            }
            else
            {
                CenterXSlider.Maximum = RadiusXSlider.Maximum = OriginXSlider.Maximum = 1.0;
                CenterYSlider.Maximum = RadiusYSlider.Maximum = OriginYSlider.Maximum = 1.0;
                CenterXSlider.Value = RadiusXSlider.Value = OriginXSlider.Value = 0.5;
                CenterYSlider.Value = RadiusYSlider.Value = OriginYSlider.Value = 0.5;
                CenterXSlider.TickFrequency = RadiusXSlider.TickFrequency = OriginXSlider.TickFrequency = 0.02;
                CenterYSlider.TickFrequency = RadiusYSlider.TickFrequency = OriginYSlider.TickFrequency = 0.02;
            }
        }

        private void OnSliderValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            RadialGradientBrushExample.Center = new Point(CenterXSlider.Value, CenterYSlider.Value);
            RadialGradientBrushExample.RadiusX = RadiusXSlider.Value;
            RadialGradientBrushExample.RadiusY = RadiusYSlider.Value;
            RadialGradientBrushExample.GradientOrigin = new Point(OriginXSlider.Value, OriginYSlider.Value);

            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
        }

        public string Example1Xaml => $@"
<Rectangle x:Name=""Rect"">
    <Rectangle.Fill>
        <RadialGradientBrush x:Name=""RadialGradientBrushExample"" SpreadMethod=""{RadialGradientBrushExample.SpreadMethod}""
            RadiusX=""{RadialGradientBrushExample.RadiusX}"" RadiusY=""{RadialGradientBrushExample.RadiusY}"" GradientOrigin=""{RadialGradientBrushExample.GradientOrigin.X}, {RadialGradientBrushExample.GradientOrigin.Y}"" 
            MappingMode=""{RadialGradientBrushExample.MappingMode}"" Center=""{RadialGradientBrushExample.Center.X}, {RadialGradientBrushExample.Center.Y}"">
            <GradientStop Offset=""0.0"" Color=""Yellow""/>
            <GradientStop Offset=""1"" Color=""Blue""/>
        </RadialGradientBrush>
    </Rectangle.Fill>
</Rectangle>
";

        #endregion

    }
}
