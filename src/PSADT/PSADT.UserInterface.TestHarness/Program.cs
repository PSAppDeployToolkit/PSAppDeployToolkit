using System;
using System.Collections;
using System.IO;
using System.Management.Automation.Language;
using System.Threading;
using PSADT.Module;
using PSADT.ProcessManagement;
using PSADT.Utilities;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.DialogState;

namespace PSADT.UserInterface.TestHarness
{
    internal static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main()
        {
            // What dialog style are we running with?
            var dialogStyle = DialogStyle.Fluent; // or DialogStyle.Classic

            // Read PSADT's string table into memory.
            var stringsAst = Parser.ParseFile(Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Strings\strings.psd1"), out var tokens, out var errors);
            if (errors.Length > 0)
            {
                throw new InvalidDataException($"Error parsing strings.psd1 file.");
            }

            // Read out the hashtable
            var stringTable = (Hashtable)stringsAst.Find(x => x is HashtableAst, false).SafeGetValue();

            // Set up parameters for testing
            string appTitle = "Adobe Creative Suite 2.1.45 EN";
            string subtitle = "EQ Bank Global IT Services - App Install";
            string appIconImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\AppIcon.png");
            string appIconDarkImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\AppIconDark.png");
            string appBannerImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\Banner.Classic.png");
            // var FluentAccentColor = ValueTypeConverter.ToInt(0xFF01C9D9); // Cyan
            DialogPosition dialogPosition = DialogPosition.BottomRight;
            // DialogPosition dialogPosition = DialogPosition.Center;
            bool dialogTopMost = true;
            bool dialogAllowMove = true;
            DeploymentType deploymentType = DeploymentType.Install;

            ProcessDefinition[] appsToClose =
            {
                new("chrome", "Google Chrome"),
                // new("msedge", "Microsoft Edge", null, null, null),
                new("firefox", "Mozilla FireFox"),
                new("notepad++", "NotePad++"),
                new("spotify", "Spotify"),
                new("acrobat", "Adobe Acrobat Reader"),
                new("photoshop", "Adobe Photoshop"),
                new("code", "Microsoft Visual Studio Code"),
                new("excel", "Microsoft Office Excel"),
                new("onenote", "Microsoft Office OneNote"),
                new("outlook", "Microsoft Office Outlook"),
                new("powerpnt", "Microsoft Office PowerPoint"),
                new("winword", "Microsoft Office Word"),
                // new("cmd", "Windows Command Prompt"),
                new("notepad", "Windows Notepad"),
                new("regedit", "Windows Registry Editor"),
                new("taskmgr", "Windows Task Manager")
            };

            TimeSpan dialogExpiryDuration = TimeSpan.FromSeconds(580);

            TimeSpan countdownDuration = TimeSpan.FromSeconds(580);

            string customMessageText = "Oh yeah. You can do [italic]italics[/italic] now. And [bold]bold text strings[/bold]. And [accent]accent colored text strings![/accent] This is a custom message that can be added using the [bold]-CustomText[/bold] parameter on [bold]Show-ADTInstallationWelcome[/bold] and [bold]Show-ADTInstallationRestartPrompt[/bold].";

            uint deferralsRemaining = 3;
            DateTime deferralDeadline = DateTime.Parse("2025-06-20T13:00:00");

            // DateTime? deferralDeadline = null;
            string progressMessageText = "Performing [accent]pre-flight checks[/accent]…";
            string progressDetailMessageText = "Testing your [accent]system to ensure compatibility[/accent]. Please wait…";

            TimeSpan restartCountdownDuration = TimeSpan.FromSeconds(80);
            TimeSpan restartCountdownNoMinimizeDuration = TimeSpan.FromSeconds(70);

            string customDialogMessageText = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you have not had much and seem to be super-cranky, are you sure you want to proceed? And don't forget to visit this website [url]https://psappdeploytoolkit.com[/url]";

            string inputDialogMessageText = "Enter the server name e.g. [italic]remotesvr1.psadt.ca[/italic]";
            string inputDialogTextBox = "YouCompleteMe";
            string inputDialogButtonLeftText = "Continue";
            string inputDialogButtonRightText = "Cancel";

            string ButtonLeftText = "LeftButton";
            string ButtonMiddleText = "MiddleButton";
            string ButtonRightText = "RightButton";

            // Set up options for the dialogs
            var closeAppsDialogState = new CloseAppsDialogState(appsToClose, null);
            var closeAppsDialogOptions = new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                //{ "FluentAccentColor", ValueTypeConverter.ToInt(0xFF107C10) }, // Accent Color: Green #107C10
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "CountdownDuration", countdownDuration },
                { "DeferralsRemaining", deferralsRemaining },
                { "DeferralDeadline", deferralDeadline },
                { "CustomMessageText", customMessageText },
                { "Strings", (Hashtable)stringTable["CloseAppsPrompt"]! },
            };
            var progressDialogOptions = new ProgressDialogOptions(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFFFB900) }, // Accent Color: Yellow #FFB900
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "ProgressMessageText", progressMessageText },
                { "ProgressDetailMessageText", progressDetailMessageText },
                { "AdditionalOption", true }
            });
            var customDialogOptions = new CustomDialogOptions(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF01C9D9) }, // Accent Color: Cyan #00B7C3
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "ButtonLeftText", ButtonLeftText },
                { "ButtonMiddleText", ButtonMiddleText },
                { "ButtonRightText", ButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MinimizeWindows", false },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });

            var inputDialogOptions = new InputDialogOptions(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFE81123) }, // Accent Color: Red #E81123
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", inputDialogMessageText },
                { "InitialInputText", inputDialogTextBox },
                { "ButtonLeftText", inputDialogButtonLeftText },
                { "ButtonRightText", inputDialogButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MinimizeWindows", false },
                { "MessageAlignment", DialogMessageAlignment.Left }
            })
            {
            };
            var restartDialogOptions = new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFE3008C) }, // Accent Color: Purple #E3008C
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                // { "CountdownDuration", restartCountdownDuration },
                // { "CountdownNoMinimizeDuration", restartCountdownNoMinimizeDuration },
                // { "CustomMessageText", customMessageText },
                { "Strings", (Hashtable)stringTable["RestartPrompt"]! },
            };

            try
            {
                var closeAppsResult = DialogManager.ShowCloseAppsDialog(dialogStyle, new CloseAppsDialogOptions(deploymentType, closeAppsDialogOptions), closeAppsDialogState); // Pass the service as optional parameter

                Console.WriteLine($"CloseApps Dialog DialogResult: {closeAppsResult}");

                // #################################################################################

                // Show CloseApps Dialog

                if (closeAppsResult != CloseAppsDialogResult.Defer)
                {
                    // Show Progress Dialog
                    DialogManager.ShowProgressDialog(dialogStyle, progressDialogOptions);

                    Thread.Sleep(3000); // Simulate some work being done

                    // Simulate a process with progress updates
                    for (int i = 0; i <= 100; i += 10)
                    {
                        // Update progress
                        DialogManager.UpdateProgressDialog($"Installation progress: {i}%", $"Step {i / 10} of 10", i);
                        Thread.Sleep(500);  // Simulate work being done
                    }

                    // Close Progress Dialog
                    DialogManager.CloseProgressDialog();

                    // #################################################################################

                    // Show Custom Dialog

                    var customResult = DialogManager.ShowCustomDialog(dialogStyle, customDialogOptions);

                    Console.WriteLine($"Custom Dialog DialogResult: {customResult}");

                    // #################################################################################

                    // Show Input Dialog

                    var inputResult = DialogManager.ShowInputDialog(dialogStyle, inputDialogOptions);

                    // #################################################################################

                    // Show Restart Dialog
                }
                else
                {
                    Console.WriteLine("Installation deferred or cancelled.");
                }

                var restartResult = DialogManager.ShowRestartDialog(dialogStyle, new RestartDialogOptions(deploymentType, restartDialogOptions));

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
        }
    }
}
