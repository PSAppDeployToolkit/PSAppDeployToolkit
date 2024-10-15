using PSADT.UserInterface;
using PSADT.UserInterface.Utilities;

namespace PSADT.Exe
{
    internal static class Program
    {
        public static void Main(string[] args)
        {

            // Set up parameters for testing
            string appTitle = "Microsoft Office 365 1.2 x64 EN";
            string subtitle = "Lockheed Martin - App Install";
            string appIconImage = "";
            string bannerImageLight = "";
            string bannerImageDark = "";
            string closeAppMessage = "";
            int defersRemaining = 5;
            var appsToClose = new List<AppProcessInfo>
            {
                new() {ProcessName= "spotify", ProcessDescription="Spotify"},
                new() {ProcessName= "excel", ProcessDescription="Microsoft Office Excel"},
                new() {ProcessName= "cmd", ProcessDescription="Command Prompt"},
                new() {ProcessName= "chrome", ProcessDescription="Google Chrome"},
                new() {ProcessName = "firefox", ProcessDescription = "Mozilla Firefox"},
                new() {ProcessName = "msedge", ProcessDescription = "Microsoft Edge"},
                new() {ProcessName = "teams", ProcessDescription = "Microsoft Teams"},
                new() {ProcessName = "explorer", ProcessDescription = "Windows Explorer"},
                new() {ProcessName = "code", ProcessDescription = "Visual Studio Code"}

            };
            string buttonLeftText = "Defer";
            string buttonRightText = @"Close Apps & Install";

            string progressMessage = "Performing pre-flight checks ...";
            string progressMessageDetail = "Testing your system to ensure the installation can proceed, please wait ...";
            Boolean topMost = true;

            // Create app instance
            AdtApplication app = new();

            // -- Progress Dialog
            // app.ShowProgressDialog(appTitle, subtitle, topMost, appIconImage, bannerImageLight, bannerImageDark, progressMessage, progressMessageDetail);

            // app.CloseCurrentDialog();

            // -- Welcome Dialog
            //// Wait 5 seconds and close the dialog, then wait 3 seconds
            //Thread.Sleep(5000);
            //app.CloseCurrentDialog();


            app.ShowWelcomeDialog(appTitle, subtitle, topMost, defersRemaining, appsToClose, appIconImage, bannerImageLight, bannerImageDark, closeAppMessage, buttonLeftText, buttonRightText);

            //Thread.Sleep(10000);
            //app.CloseCurrentDialog();

            //// -- Progress Dialog
            //app.ShowProgressDialog(appTitle, null, progress_message, progress_message_detail);

            //// Wait 5 seconds
            //Thread.Sleep(3000);
            //// - Close Progress Dialog
            //app.CloseCurrentDialog();
            //Thread.Sleep(3000);

            //// Wait 3 seconds and display Welcome Dialog again
            //app.ShowWelcomeDialog("2nd dialog!", null, appsToClose);
        }
    }
}
