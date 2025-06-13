using iNKORE.UI.WPF.Modern.Controls;
using System;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class NumberBoxPage
    {
        public NumberBoxPage()
        {
            InitializeComponent();
            FormattedNumberBox.NumberFormatter = new CustomNumberFormatter();
        }

        private class CustomNumberFormatter : INumberBoxNumberFormatter
        {
            public string FormatDouble(double value)
            {
                return value.ToString("F");
            }

            public double? ParseDouble(string text)
            {
                if (double.TryParse(text, out double result))
                {
                    return Math.Round(result * 4, MidpointRounding.AwayFromZero) / 4;
                }
                return null;
            }
        }

        private void PopupHorizonalOffset_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            NumberBox1.Resources["NumberBoxPopupHorizonalOffset"] = args.NewValue;
            UpdateExampleCode();
        }

        private void PopupVerticalOffset_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            NumberBox1.Resources["NumberBoxPopupVerticalOffset"] = args.NewValue;
            UpdateExampleCode();
        }

        private void Page_Loaded(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void RadioButtons_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void NumberBox_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateExampleCode();
        }

        private void TextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, System.Windows.RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void CornerRadiusSlider_ValueChanged(object sender, System.Windows.RoutedPropertyChangedEventArgs<double> e)
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
        }

        public string Example1Xaml => $@"
<ui:NumberBox AcceptsExpression=""True""
    Header=""Enter an expression:""
    PlaceholderText=""1 + 2^2"" Value=""NaN"" />
";

        public string Example2Xaml => $@"
<ui:NumberBox x:Name=""NumberBoxSpinButtonPlacementExample""
    Header=""Enter an integer:"" Value=""10"" 
    SpinButtonPlacementMode=""{NumberBoxSpinButtonPlacementExample.SpinButtonPlacementMode}""
    SmallChange=""10"" LargeChange=""100"" />
";

        public string Example3Xaml => $@"
<ui:NumberBox x:Name=""FormattedNumberBox""
    Header=""Enter a dollar amount:""
    PlaceholderText=""0.00"" Value=""NaN"" />
";

        public string Example4Xaml => $@"
<ui:NumberBox x:Name=""NumberBox1""
    Header=""{NumberBox1.Header}""
    Maximum=""{NumberBox1.Maximum}"" Minimum=""{NumberBox1.Minimum}""
    PlaceholderText=""{NumberBox1.PlaceholderText}""
    SpinButtonPlacementMode=""{NumberBox1.SpinButtonPlacementMode}"" 
    Description=""{NumberBox1.Description}""
    ValidationMode=""{NumberBox1.ValidationMode}""
    IsWrapEnabled=""{NumberBox1.IsWrapEnabled}""
    AcceptsExpression=""{NumberBox1.AcceptsExpression}""
    CornerRadius=""{NumberBox1.CornerRadius}""
</ui:NumberBox>
";

        #endregion
    }
}
