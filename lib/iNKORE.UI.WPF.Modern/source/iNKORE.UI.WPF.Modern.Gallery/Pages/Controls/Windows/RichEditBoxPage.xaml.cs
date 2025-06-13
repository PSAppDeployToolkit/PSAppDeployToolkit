using System.Windows;
using System.Windows.Controls;
using iNKORE.UI.WPF.Modern.Controls.Helpers;
using iNKORE.UI.WPF.Modern.Controls;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;


namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class RichEditBoxPage : Page
    {
        public RichEditBoxPage()
        {
            InitializeComponent();
        }

        private void Page_Loaded(object sender, RoutedEventArgs e)
        {
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


        #region Example Code

        public void UpdateExampleCode()
        {
            if (!this.IsLoaded) return;

            Example1.Xaml = Example1Xaml;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<RichTextBox x:Name=""richTextBox""
    ui:ControlHelper.Header=""Control header""
    ui:ControlHelper.PlaceholderText=""Placeholder text""
    SpellCheck.IsEnabled=""True"" />
";

        public string Example2Xaml => $@"
<RichTextBox x:Name=""textBox""
    ui:ControlHelper.Header=""{ControlHelper.GetHeader(textBox)}""
    ui:PlaceholderText=""{ControlHelper.GetPlaceholderText(textBox)}""
    AcceptsReturn=""{textBox.AcceptsReturn}"" IsUndoEnabled=""{textBox.IsUndoEnabled}""
    IsReadOnly=""{textBox.IsReadOnly}"" IsReadOnlyCaretVisible=""{textBox.IsReadOnlyCaretVisible}""
    IsInactiveSelectionHighlightEnabled=""{textBox.IsInactiveSelectionHighlightEnabled}""
    HorizontalScrollBarVisibility=""{textBox.HorizontalScrollBarVisibility}""
    VerticalScrollBarVisibility=""{textBox.VerticalScrollBarVisibility}""
    SelectionOpacity=""{textBox.SelectionOpacity}"" SpellCheck.IsEnabled=""{textBox.SpellCheck.IsEnabled}"" />
";


        #endregion
    }
}
