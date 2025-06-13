using System.Windows;
using System.Windows.Controls;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class RadioButtonsPage
    {
        public RadioButtonsPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
            UpdateExampleCode();
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<ui:RadioButtons x:Name=""Options"" Header=""Options:"">
    <!--  A RadioButton group.  -->
    <RadioButton x:Name=""Option1RadioButton"" Content=""Option 1"" />
    <RadioButton x:Name=""Option2RadioButton"" Content=""Option 2"" />
    <RadioButton x:Name=""Option3RadioButton"" Content=""Option 3"" />
</ui:RadioButtons>
";

        public string Example2Xaml => $@"
<ui:RadioButtons
    x:Name=""BackgroundRadioButtons""
    Header=""Background""
    MaxColumns=""4"">
    <RadioButton Content=""Green"" />
    <RadioButton Content=""Yellow"" />
    <RadioButton Content=""Blue"" />
    <RadioButton Content=""White"" IsChecked=""True"" />
</ui:RadioButtons>
<ui:RadioButtons
    x:Name=""BorderBrushRadioButtons""
    Header=""BorderBrush""
    MaxColumns=""4""
    SelectedIndex=""1"">
    <sys:String>Green</sys:String>
    <sys:String>Yellow</sys:String>
    <sys:String>Blue</sys:String>
    <sys:String>White</sys:String>
</ui:RadioButtons>
";

        #endregion
    }
}
