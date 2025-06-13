using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class TextBoxPage
    {
        public TextBoxPage()
        {
            InitializeComponent();
            UpdateExampleCode();
        }

        private void CheckBox_Click(object sender, RoutedEventArgs e)
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


        private void ClearClipboard(object sender, RoutedEventArgs e)
        {
            Clipboard.Clear();
        }

        private void OptionsExpander_Expanded(object sender, RoutedEventArgs e)
        {
            OptionsExpander.Header = "Hide options";
        }

        private void OptionsExpander_Collapsed(object sender, RoutedEventArgs e)
        {
            OptionsExpander.Header = "Show options";
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsInitialized) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
            Example3.Xaml = Example3Xaml;
            Example4.Xaml = Example4Xaml;
            Example5.Xaml = Example5Xaml;
        }

        public string Example1Xaml => $@"
<TextBox AutomationProperties.Name=""simple TextBox"" />
";

        public string Example2Xaml => $@"
<TextBox ui:ControlHelper.Header=""Enter your name:"" ui:ControlHelper.PlaceholderText=""Name"" />
";

        public string Example3Xaml => $@"
<TextBox AutomationProperties.Name=""customized TextBox""
    FontFamily=""Arial"" FontSize=""24""
    FontStyle=""Italic"" IsReadOnly=""True""
    Foreground=""CornflowerBlue""
    Text=""I am super excited to be here!"" />
";

        public string Example4Xaml => $@"
<TextBox AcceptsReturn=""True""
    AutomationProperties.Name=""multi-line TextBox""
    SelectionBrush=""Green""
    SpellCheck.IsEnabled=""True""
    TextWrapping=""Wrap"" />
";

        public string Example5Xaml => $@"
<TextBox x:Name=""textBox"" AcceptsReturn=""{textBox.AcceptsReturn}""
    IsReadOnly=""{textBox.IsReadOnly}"" IsReadOnlyCaretVisible=""{textBox.IsReadOnlyCaretVisible}""
    IsUndoEnabled=""{textBox.IsUndoEnabled}"" IsInactiveSelectionHighlightEnabled=""{textBox.IsInactiveSelectionHighlightEnabled}""
    ui:ControlHelper.Header=""{ControlHelper.GetHeader(textBox)}""
    ui:ControlHelper.PlaceholderText=""{ControlHelper.GetPlaceholderText(textBox)}"" 
    TextWrapping=""{textBox.TextWrapping}"" SelectionOpacity=""{textBox.SelectionOpacity}""
    HorizontalScrollBarVisibility=""{textBox.HorizontalScrollBarVisibility}"" VerticalScrollBarVisibility=""{textBox.VerticalScrollBarVisibility}"" />
";

        #endregion

    }
}
