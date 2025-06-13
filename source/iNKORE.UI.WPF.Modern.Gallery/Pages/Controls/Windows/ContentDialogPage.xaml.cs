using iNKORE.UI.WPF.Modern.Controls;
using System.Windows;
using System.Windows.Controls;
using Page = iNKORE.UI.WPF.Modern.Controls.Page;

namespace iNKORE.UI.WPF.Modern.Gallery.Pages.Controls.Windows
{
    public partial class ContentDialogPage : Page
    {
        public ContentDialogPage()
        {
            InitializeComponent();

            UpdateExampleCode();
        }

        private async void ShowDialog_Click(object sender, RoutedEventArgs e)
        {
            ContentDialog dialog = new ContentDialog();
            dialog.Title = "Save your work?";
            dialog.PrimaryButtonText = "Save";
            dialog.SecondaryButtonText = "Don't Save";
            dialog.CloseButtonText = "Cancel";
            dialog.DefaultButton = ContentDialogButton.Primary;
            dialog.Content = new ContentDialogContent();

            var result = await dialog.ShowAsync();

            if (result == ContentDialogResult.Primary)
            {
                DialogResult.Text = "User saved their work";
            }
            else if (result == ContentDialogResult.Secondary)
            {
                DialogResult.Text = "User did not save their work";
            }
            else
            {
                DialogResult.Text = "User cancelled the dialog";
            }
        }

        #region Example Code

        public void UpdateExampleCode()
        {
            Example1.Xaml = Example1Xaml;
            Example1.CSharp = Example1CS;
            Example2.Xaml = Example2Xaml;
        }

        public string Example1Xaml => $@"
<Button x:Name=""ShowDialog""
    Content=""Show dialog"" Click=""ShowDialog_Click""/>
<TextBlock x:Name=""DialogResult"" Style=""{{StaticResource OutputTextBlockStyle}}"" />
";

        public string Example1CS => $@"
private async void ShowDialog_Click(object sender, RoutedEventArgs e)
{{
    ContentDialog dialog = new ContentDialog();
    dialog.Title = ""Save your work?"";
    dialog.PrimaryButtonText = ""Save"";
    dialog.SecondaryButtonText = ""Don't Save"";
    dialog.CloseButtonText = ""Cancel"";
    dialog.DefaultButton = ContentDialogButton.Primary;
    dialog.Content = new ContentDialogContent();

    var result = await dialog.ShowAsync();

    if (result == ContentDialogResult.Primary)
    {{
        DialogResult.Text = ""User saved their work"";
    }}
    else if (result == ContentDialogResult.Secondary)
    {{
        DialogResult.Text = ""User did not save their work"";
    }}
    else
    {{
        DialogResult.Text = ""User cancelled the dialog"";
    }}
}}
";

        public string Example2Xaml => TestContentDialog.CodeXaml;

        #endregion

    }
}
