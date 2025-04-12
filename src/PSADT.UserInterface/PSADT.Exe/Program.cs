using PSADT.UserInterface.Services;

namespace PSADT.UserInterface
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
            string appTitle = "Adobe Reader CS 2025 x64 EN";
            string subtitle = "Bisto Systems Ltd - App Install";
            string? appIconImage = null;
            //string? dialogAccentColor = "";
            string? dialogAccentColor = "#FFFFB900"; // Yellow
            DialogPosition dialogPosition = DialogPosition.BottomRight;
            // DialogPosition dialogPosition = DialogPosition.Center;
            bool dialogTopMost = true;
            bool dialogAllowMove = false;


            AppProcessInfo[] appsToClose =
            {
                new("excel", "Microsoft Office Excel", null, null, null),
                // new("notepad", "Microsoft Notepad", null, null, null),
                // new("cmd", "Command Prompt", null, null, null),
                new("chrome", "Google Chrome", null, null, null),
                new("firefox", null, null, null, null),
                // new("msedge", "Microsoft Edge", null, null, null),
                // new("explorer", null, null, null, null),
                new("spotify", null, null, null, null),
                new("code", "Visual Studio Code", null, null, null),
                new("taskmgr", "Task Manager", null, null, null),
                new("regedit", "Registry Editor", null, null, null),
                new("powerpnt", "Microsoft Office PowerPoint", null, null, null),
                new("winword", "Microsoft Office Word", null, null, null),
                new("outlook", "Microsoft Office Outlook", null, null, null),
                new("onenote", "Microsoft Office OneNote", null, null, null),
                new("skype", "Skype", null, null, null),
                new("slack", "Slack", null, null, null),
                new("zoom", "Zoom", null, null, null),
                new("webex", "WebEx", null, null, null),
                new("acrobat", "Adobe Acrobat Reader", null, null, null),
                new("photoshop", "Adobe Photoshop", null, null, null),
            };

            TimeSpan? dialogExpiryDuration = TimeSpan.FromSeconds(90);

            TimeSpan? countdownDuration = TimeSpan.FromSeconds(90);

            string? closeAppsMessageText = "Please save your work before continuing as the following applications will be closed automatically.";
            string? alternativeCloseAppsMessageText = "Please select \'Install\' to continue with the installation. If you have any deferrals remaining, you may also choose to delay the installation.";
            string? customMessageText = "This is a custom message that can be added using the -CustomText parameter on Show-ADTInstallationWelcome (also now available on Show-ADTInstallationRestartPrompt).";

            int? deferralsRemaining = null;
            DateTime deferralDeadline = DateTime.Parse("2025-04-20T13:00:00");
            // DateTime? deferralDeadline = null;
            string? deferralsRemainingText = "Remaining Deferrals";
            string? deferralDeadlineText = "Deferral Deadline";
            string? automaticStartCountdownText = "Automatic Start Countdown";
            string? deferButtonText = "_Defer";
            string? continueButtonText = "_Close Apps & Install";
            string? alternativeContinueButtonText = "_Install";
            string? progressMessageText = "Performing pre-flight checks ...";
            string? progressMessageDetailText = "Testing your system to ensure the installation can proceed, please wait ...";

            TimeSpan restartCountdownDuration = TimeSpan.FromSeconds(80);
            TimeSpan restartCountdownNoMinimizeDuration = TimeSpan.FromSeconds(70);

            string countdownAutomaticRestartText = "Automatic Restart Countdown";
            string restartMessageText = "Your computer needs to be restarted. Please save your work before continuing.";
            string countdownRestartMessageText = "Your computer needs to be restarted. Please save your work before continuing, then click Restart Now.  You can restart your computer now or wait for the countdown to complete.";
            string dismissButtonText = "_Minimize";
            string restartButtonText = "_Restart Now";

            string customDialogMessageText = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you've not had much sleep and you're clearly cranky, are you sure you want to proceed?";

            string inputBoxMessageText = "Please provide the name of the server you wish to connect to.";
            string inputBoxText = "Type things here";
            string inputBoxButtonLeftText = "_Cancel";
            string inputBoxButtonMiddleText = "_Use Defaults";
            string inputBoxButtonRightText = "_Start";


            string ButtonLeftText = "";
            string ButtonMiddleText = "";
            string ButtonRightText = "_Only one button!";

            // Create ProcessEvaluationService
            var processEvaluationService = new ProcessEvaluationService();

            try
            {
                //Guid moduleGuid = new Guid("8c3c366b-8606-4576-9f2d-4051144f7ca2");
                //String moduleName = "PSAppDeployToolkit";
                //Version moduleVersion = new Version(4, 0, 6);
                //DialogActionResult helpConsoleResult = UnifiedAdtApplication.ShowHelpConsoleDialog(moduleName, moduleGuid, moduleVersion);


                //// #################################################################################

                //// Show Input Dialog for completion
                //string inputResult = UnifiedAdtApplication.ShowInputDialog(
                //    dialogExpiryDuration,
                //    dialogAccentColor,
                //    dialogPosition,
                //    dialogTopMost,
                //    dialogAllowMove,
                //    appTitle,
                //    subtitle,
                //    appIconImage,
                //    inputBoxMessageText,
                //    inputBoxText,
                //    inputBoxButtonLeftText,
                //    inputBoxButtonMiddleText,
                //    inputBoxButtonRightText);

                //Console.WriteLine($"Input Dialog DialogResult: {inputResult}");

                // #################################################################################

                // Show CloseApps Dialog
                var closeAppsResult = UnifiedAdtApplication.ShowCloseAppsDialog(
                    dialogExpiryDuration,
                    dialogAccentColor,
                    dialogPosition,
                    dialogTopMost,
                    dialogAllowMove,
                    appTitle,
                    subtitle,
                    appIconImage,
                    appsToClose,
                    countdownDuration,
                    deferralsRemaining,
                    deferralDeadline,
                    closeAppsMessageText,
                    alternativeCloseAppsMessageText,
                    customMessageText,
                    deferralsRemainingText,
                    deferralDeadlineText,
                    automaticStartCountdownText,
                    deferButtonText,
                    continueButtonText,
                    alternativeContinueButtonText,
                    processEvaluationService
                    ); // Pass the service as optional parameter

                Console.WriteLine($"CloseApps Dialog DialogResult: {closeAppsResult}");

                // #################################################################################

                if (closeAppsResult.Equals("Continue"))
                {
                    // Show Progress Dialog
                    UnifiedAdtApplication.ShowProgressDialog(
                        dialogExpiryDuration,
                        dialogAccentColor,
                        dialogPosition,
                        dialogTopMost,
                        dialogAllowMove,
                        appTitle,
                        subtitle,
                        appIconImage,
                        progressMessageText,
                        progressMessageDetailText);

                    Thread.Sleep(3000); // Simulate some work being done

                    // Simulate a process with progress updates
                    for (int i = 0; i <= 100; i += 10)
                    {
                        // Update progress
                        UnifiedAdtApplication.UpdateProgress($"Installation progress: {i}%", $"Step {i / 10} of 10", i);
                        Thread.Sleep(500);  // Simulate work being done
                    }

                    // Close Progress Dialog
                    UnifiedAdtApplication.CloseCurrentDialog();

                    // #################################################################################

                    // Show Custom Dialog for completion
                    var customResult = UnifiedAdtApplication.ShowCustomDialog(
                            dialogExpiryDuration,
                            dialogAccentColor,
                            dialogPosition,
                            dialogTopMost,
                            dialogAllowMove,
                            appTitle,
                            subtitle,
                            appIconImage,
                            customDialogMessageText,
                            ButtonLeftText,
                            ButtonMiddleText,
                            ButtonRightText);

                    Console.WriteLine($"Custom Dialog DialogResult: {customResult}");
                }
                else
                {
                    Console.WriteLine("Installation deferred or cancelled.");
                }

                // #################################################################################

                // Show Restart Dialog
                var restartResult = UnifiedAdtApplication.ShowRestartDialog(
                    dialogExpiryDuration,
                    dialogAccentColor,
                    dialogPosition,
                    dialogTopMost,
                    dialogAllowMove,
                    appTitle,
                    subtitle,
                    appIconImage,
                    restartCountdownDuration,
                    restartCountdownNoMinimizeDuration,
                    restartMessageText,
                    customMessageText,
                    countdownRestartMessageText,
                    countdownAutomaticRestartText,
                    dismissButtonText,
                    restartButtonText);

                Console.WriteLine($"Restart Dialog DialogResult: {restartResult}");

                if (restartResult.Equals("Restart"))
                {
                    Console.WriteLine("Proceeding with installation after restart.");
                    // Implement actual restart logic here
                }
                else if (restartResult.Equals("Defer"))
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

        }
    }
}
