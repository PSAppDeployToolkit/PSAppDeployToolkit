using PSADT.UserInterface;
using PSADT.UserInterface.Services;

namespace PSADT.Exe
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        public static void Main(string[] args)
        {
            if (args is null)
            {
                throw new ArgumentNullException(nameof(args));
            }

            // Set up parameters for testing
            const string appTitle = "Microsoft Office 365 1.2 x64 EN";
            const string subtitle = "MyCompanyName Technology Ltd - App Install";
            const bool topMost = true;
            const string? appIconImage = null;
            const string? bannerImageLight = null;
            const string? bannerImageDark = null;

            var appsToClose = new List<AppProcessInfo>
            {
                new("excel", "Microsoft Office Excel", null, null, null),
                new("cmd", "Command Prompt", null, null, null),
                new("chrome", "Google Chrome", null, null, null),
                new("firefox", null, null, null, null),
                new("msedge", "Microsoft Edge", null, null, null),
                new("explorer", null, null, null, null),
                new("spotify", null, null, null, null),
                new("code", "Visual Studio Code", null, null, null),
                new("taskmgr", "Task Manager", null, null, null),
                new("regedit", "Registry Editor", null, null, null),
            };

            const string closeAppMessage = "Please save your work before continuing. The following applications will be closed automatically.";
            const string altCloseAppMessage = "Please select \"Install\" to continue with the installation. If you have any \"Defers\" remaining, you may also choose to delay the installation.";
            const int defersRemaining = 5;
            const string deferRemainText = "remain";
            const string deferButtonText = "Defer";
            const string continueButtonText = "Close Apps & Install";
            const string altContinueButtonText = "Install";

            const string progressMessage = "Performing pre-flight checks ...";
            const string progressMessageDetail = "Testing your system to ensure the installation can proceed, please wait ...";

            const double restartCountdownMins = 5;
            const string restartMessage = "The installation will begin in 5 minutes. You can restart your computer now or wait for the countdown to complete.";
            const string dismissButtonText = "Dismiss";
            const string restartButtonText = "Restart Now";

            const string customMessage = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you've not had much sleep and you're clearly cranky, are you sure you want to proceed? ";
            const string button1Text = "No thanks";
            const string button2Text = "";
            const string button3Text = "Bring it!";

            // Create ProcessEvaluationService
            var processEvaluationService = new ProcessEvaluationService();

            try
            {
                // Show Welcome Dialog
                string welcomeResult = UnifiedAdtApplication.ShowWelcomeDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    defersRemaining,
                    appsToClose,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    closeAppMessage,
                    altCloseAppMessage,
                    deferRemainText,
                    deferButtonText,
                    continueButtonText,
                    altContinueButtonText,
                    processEvaluationService); // Pass the service as optional parameter

                Console.WriteLine($"Welcome Dialog Result: {welcomeResult}");

                if (welcomeResult.Equals("Continue", StringComparison.OrdinalIgnoreCase))
                {
                    // Show Progress Dialog
                    UnifiedAdtApplication.ShowProgressDialog(
                        appTitle,
                        subtitle,
                        topMost,
                        appIconImage,
                        bannerImageLight,
                        bannerImageDark,
                        progressMessage,
                        progressMessageDetail);

                    // Simulate a process with progress updates
                    for (int i = 0; i <= 100; i += 10)
                    {
                        // Update progress
                        UnifiedAdtApplication.UpdateProgress(i, $"Installation progress: {i}%", $"Step {i / 10} of 10");
                        Thread.Sleep(1000);  // Simulate work being done
                    }

                    // Close Progress Dialog
                    UnifiedAdtApplication.CloseCurrentDialog();

                    // Show Custom Dialog for completion
                    string customResult = UnifiedAdtApplication.ShowCustomDialog(
                        appTitle,
                        subtitle,
                        topMost,
                        appIconImage,
                        bannerImageLight,
                        bannerImageDark,
                        customMessage,
                        button1Text,
                        button2Text,
                        button3Text);

                    Console.WriteLine($"Custom Dialog Result: {customResult}");
                }
                else
                {
                    Console.WriteLine("Installation deferred or cancelled.");
                }

                // Show Restart Dialog
                string restartResult = UnifiedAdtApplication.ShowRestartDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    restartCountdownMins,
                    restartMessage,
                    dismissButtonText,
                    restartButtonText);

                Console.WriteLine($"Restart Dialog Result: {restartResult}");

                if (restartResult.Equals("Restart", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Proceeding with installation after restart.");
                    // Implement actual restart logic here
                }
                else if (restartResult.Equals("Defer", StringComparison.OrdinalIgnoreCase))
                {
                    Console.WriteLine("Installation deferred by the user.");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred: {ex.Message}");
            }
            finally
            {
                // Dispose the UnifiedAdtApplication when done
                UnifiedAdtApplication.Dispose();
            }

            // Example of re-instantiating after disposal if needed
            /*
            try
            {
                // Attempt to use UnifiedAdtApplication after disposal
                string newWelcomeResult = UnifiedAdtApplication.ShowWelcomeDialog(
                    appTitle,
                    subtitle,
                    topMost,
                    defersRemaining,
                    appsToClose,
                    appIconImage,
                    bannerImageLight,
                    bannerImageDark,
                    closeAppMessage,
                    deferRemainText,
                    deferButtonText,
                    continueButtonText,
                    processEvaluationService);

                Console.WriteLine($"New Welcome Dialog Result: {newWelcomeResult}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"An error occurred after disposal: {ex.Message}");
            }
            */
        }
    }
}
