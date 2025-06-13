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
<Slider x:Name=""Slider1"" AutomationProperties.Name=""simple slider"" />
<TextBlock Style=""{{StaticResource OutputTextBlockStyle}}"" Text=""{{Binding Value, ElementName=Slider1}}"" />
";

        public string Example2Xaml => $@"
<Slider x:Name=""Slider2""
        ui:ControlHelper.Header=""Control header""
        LargeChange=""100"" SmallChange=""10""
        Maximum=""1000"" Minimum=""500""
        TickFrequency=""10"" Value=""800"" />
<TextBlock Text=""{{Binding Value, ElementName=Slider2, Mode=OneWay}}"" />
";

        public string Example3Xaml => $@"
<Slider x:Name=""Slider3""
        AutomationProperties.Name=""Slider with ticks""
        TickFrequency=""10"" TickPlacement=""Both"" />
<TextBlock Text=""{{Binding Value, ElementName=Slider3, Mode=OneWay}}"" />
";

        public string Example4Xaml => $@"
<Slider x:Name=""Slider4"" Orientation=""Vertical""
        AutomationProperties.Name=""vertical slider""
        Maximum=""50"" Minimum=""-50""
        TickFrequency=""10"" TickPlacement=""Both"" />
<TextBlock Text=""{{Binding Value, ElementName=Slider4, Mode=OneWay}}"" />
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
