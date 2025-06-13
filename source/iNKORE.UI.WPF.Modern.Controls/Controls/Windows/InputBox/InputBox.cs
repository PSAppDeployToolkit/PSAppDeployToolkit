using System.Threading.Tasks;
using System.Windows;
using static iNKORE.UI.WPF.Modern.Controls.LocalizedDialogCommands;

namespace iNKORE.UI.WPF.Modern.Controls
{
    public static class InputBox
    {
        public static async Task<string> ShowAsync(object title, object prompt, string defaultResponse = "")
        {
            ContentDialog dialog = BuildDialog(title, prompt, defaultResponse);
            ContentDialogResult result = await dialog.ShowAsync();

            if (result != ContentDialogResult.Primary)
            {
                return string.Empty;
            }

            InputBoxContent content = (InputBoxContent)dialog.Content;
            string response = content.responseTextControl.Text;
            return response;
        }

        public static async Task<string> ShowAsync(Window owner, object title, object prompt, string defaultResponse = "")
        {
            ContentDialog dialog = BuildDialog(title, prompt, defaultResponse);
            ContentDialogResult result = await dialog.ShowAsync(owner);

            if (result != ContentDialogResult.Primary)
            {
                return string.Empty;
            }

            InputBoxContent content = (InputBoxContent)dialog.Content;
            string response = content.responseTextControl.Text;
            return response;
        }

        public static async Task<string> ShowAsync(ContentDialogPlacement placement, object title, object prompt, string defaultResponse = "")
        {
            ContentDialog dialog = BuildDialog(title, prompt, defaultResponse);
            ContentDialogResult result = await dialog.ShowAsync(placement);

            if (result != ContentDialogResult.Primary)
            {
                return string.Empty;
            }

            InputBoxContent content = (InputBoxContent)dialog.Content;
            string response = content.responseTextControl.Text;
            return response;
        }

        private static ContentDialog BuildDialog(object title, object prompt, string defaultResponse)
        {
            ContentDialog dialog = new()
            {
                PrimaryButtonText = GetString(DialogBoxCommand.IDOK),
                CloseButtonText = GetString(DialogBoxCommand.IDCANCEL),
                DefaultButton = ContentDialogButton.Primary
            };

            var content = new InputBoxContent();
            dialog.Content = content;

            dialog.Title = title;
            content.promptTextControl.Content = prompt;
            content.responseTextControl.Text = defaultResponse;

            return dialog;
        }
    }
}
