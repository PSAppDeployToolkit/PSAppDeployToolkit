using System.Windows;
using iNKORE.UI.WPF.Modern.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ProgressBarPage
    {
        public ProgressBarPage()
        {
            InitializeComponent();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
        }

        public string Example1Xaml => $@"
<ui:ProgressBar x:Name=""ProgressBar1""
    IsIndeterminate=""{ProgressBar1.IsIndeterminate}""
    ShowError=""{ProgressBar1.ShowError}""
    ShowPaused=""{ProgressBar1.ShowPaused}"" />
";

        public string Example2Xaml => $@"
<ProgressBar x:Name=""ProgressBar2"" Value=""{ProgressValue.Value}"" />
";

        public string Example3Xaml => $@"
<ui:ProgressRing x:Name=""ProgressRing1"" IsActive=""{ProgressToggle.IsOn}"" />
";

        #endregion

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void RadioButton_Click(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        private void ProgressValue_ValueChanged(NumberBox sender, NumberBoxValueChangedEventArgs args)
        {
            UpdateExampleCode();
        }

        private void ProgressToggle_Toggled(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }
    }
}
