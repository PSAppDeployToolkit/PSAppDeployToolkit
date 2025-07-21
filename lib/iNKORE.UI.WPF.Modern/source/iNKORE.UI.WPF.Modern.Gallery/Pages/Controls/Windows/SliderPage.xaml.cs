using System.Windows.Controls;
using System.Windows;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class SliderPage
    {
        public SliderPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void NumberBox_ValueChanged(iNKORE.UI.WPF.Modern.Controls.NumberBox sender, iNKORE.UI.WPF.Modern.Controls.NumberBoxValueChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void SnapsToRadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var selected = (SnapsToRadioButtons.SelectedItem as RadioButton)?.Content?.ToString();

            if (selected == "Ticks")
                    Slider3.IsSnapToTickEnabled = true;
            else
                    Slider3.IsSnapToTickEnabled = false;

            UpdateExampleCode();
        }

        private string SnapsToValue => (SnapsToRadioButtons.SelectedItem as RadioButton)?.Content?.ToString() ?? "StepValues";

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
        }

        public string Example1Xaml => $@"
<Slider x:Name=""Slider1"" AutomationProperties.Name=""simple slider"" Width=""{200}"" />
";

        public string Example2Xaml => $@"
<Slider x:Name=""Slider2"" ui:ControlHelper.Header=""Control header"" Maximum=""{MaximumValue?.Value ?? 1000}"" 
    TickFrequency=""{StepFrequencyValue?.Value ?? 10}"" SmallChange=""{SmallChangeValue?.Value ?? 10}"" Value=""{Slider2?.Value ?? 800}"" Width=""{200}"" Minimum=""{MinimumValue?.Value ?? 500}"" />
";

        public string Example3Xaml => $@"
<Slider x:Name=""Slider3""
    AutomationProperties.Name=""Slider with ticks""
    TickFrequency=""20"" 
    TickPlacement=""Both"" 
    SnapsTo=""{SnapsToValue}"" />
";

        public string Example4Xaml => $@"
<Slider x:Name=""Slider4"" AutomationProperties.Name=""vertical slider"" Width=""{100}"" Orientation=""Vertical"" 
    TickFrequency=""10"" TickPlacement=""Both"" Maximum=""50"" Minimum=""-50"" />
";

        public string Example5Xaml => $@"
<Slider x:Name=""slider"" Orientation=""{slider.Orientation}""
    ui:ControlHelper.Header=""Control header""
    AutoToolTipPlacement=""{slider.AutoToolTipPlacement}"" TickPlacement=""{slider.TickPlacement}""
    IsSelectionRangeEnabled=""{slider.IsSelectionRangeEnabled}"" IsDirectionReversed=""{slider.IsDirectionReversed}""
    IsMoveToPointEnabled=""{slider.IsMoveToPointEnabled}"" IsSnapToTickEnabled=""{slider.IsSnapToTickEnabled}""
    Maximum=""100"" TickFrequency=""10"" TickPlacement=""Both"">
</Slider>
";

        #endregion
    }
}
