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
            string appTitle = "Super Street Fighter 2 Turbo XL";
            string subtitle = "CapComNom Entertainment Ltd - App Install";
            string appIconImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\AppIcon.png");
            string appBannerImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\Banner.Classic.png");
            // var FluentAccentColor = ValueTypeConverter.ToInt(0xFF01C9D9); // Cyan
            DialogPosition dialogPosition = DialogPosition.BottomRight;
            //DialogPosition dialogPosition = DialogPosition.Center;
            bool dialogTopMost = true;
            bool dialogAllowMove = false;
            DeploymentType deploymentType = DeploymentType.Install;


            ProcessDefinition[] appsToClose =
            {
                new("excel", "Microsoft Office Excel"),
                // new("notepad", "Microsoft Notepad"),
                // new("cmd", "Command Prompt"),
                new("chrome", "Google Chrome"),
                new("firefox"),
                // new("msedge", "Microsoft Edge", null, null, null),
                // new("explorer", null, null, null, null),
                new("spotify"),
                new("code", "Visual Studio Code"),
                new("taskmgr", "Task Manager"),
                new("regedit", "Registry Editor"),
                new("powerpnt", "Microsoft Office PowerPoint"),
                new("winword", "Microsoft Office Word"),
                new("outlook", "Microsoft Office Outlook"),
                new("onenote", "Microsoft Office OneNote"),
                new("skype", "Skype"),
                new("slack", "Slack"),
                new("zoom", "Zoom"),
                new("webex", "WebEx"),
                new("acrobat", "Adobe Acrobat Reader"),
                new("photoshop", "Adobe Photoshop"),
            };

            TimeSpan dialogExpiryDuration = TimeSpan.FromSeconds(580);

            TimeSpan countdownDuration = TimeSpan.FromSeconds(580);

            string customMessageText = "Oh yeah. You can do *italics* now. And **bold text strings**. And 'accent colored text strings!' This is a custom message that can be added using the *-CustomText* parameter on *Show-ADTInstallationWelcome* and *Show-ADTInstallationRestartPrompt*.";

            uint deferralsRemaining = 3;
            DateTime deferralDeadline = DateTime.Parse("2025-06-20T13:00:00");

            // DateTime? deferralDeadline = null;
            string progressMessageText = "Performing pre-flight checks…";
            string progressDetailMessageText = "Testing your system to ensure compatibility, please wait…";

            TimeSpan restartCountdownDuration = TimeSpan.FromSeconds(80);
            TimeSpan restartCountdownNoMinimizeDuration = TimeSpan.FromSeconds(70);

            string customDialogMessageText = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you have not had much and seem to be super-cranky, are you sure you want to proceed?";

            string initialInputText = "You can replace me opr leave me blank";

            string ButtonLeftText = "LeftButton";
            string ButtonMiddleText = "MiddleButton";
            string ButtonRightText = "RightButton";

            // Set up options for the dialogs
            var closeAppsDialogState = new CloseAppsDialogState(appsToClose, null);
            var closeAppsDialogOptions = new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                // { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF01C9D9) }, // Accent Color:  Cyan
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
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
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF01C9D9) }, // Accent Color:  Cyan
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppBannerImage", appBannerImage },
                { "ProgressMessageText", progressMessageText },
                { "ProgressDetailMessageText", progressDetailMessageText },
                { "AdditionalOption", true }
            });
            var customDialogOptions = new CustomDialogOptions(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFD00063) }, // Accent Color:  Cyan
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "ButtonLeftText", ButtonLeftText },
                { "ButtonMiddleText", ButtonMiddleText },
                { "ButtonRightText", ButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });

            var inputDialogOptions = new InputDialogOptions(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFFF00FF) }, // Accent Color:  Cyan
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "InitialInputText", initialInputText },
                { "ButtonLeftText", ButtonLeftText },
                { "ButtonRightText", ButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MessageAlignment", DialogMessageAlignment.Left }
            })
            {
                
            };
            var restartDialogOptions = new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFFF6B00) }, // Accent Color:  Orange
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppBannerImage", appBannerImage },
                { "CountdownDuration", restartCountdownDuration },
                { "CountdownNoMinimizeDuration", restartCountdownNoMinimizeDuration },
                { "CustomMessageText", customMessageText },
                { "Strings", (Hashtable)stringTable["RestartPrompt"]! },
            };

            try
            {
                // Show CloseApps Dialog
                var closeAppsResult = DialogManager.ShowCloseAppsDialog(dialogStyle, new CloseAppsDialogOptions(deploymentType, closeAppsDialogOptions), closeAppsDialogState); // Pass the service as optional parameter

                Console.WriteLine($"CloseApps Dialog DialogResult: {closeAppsResult}");

                // #################################################################################

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

                    // Show Custom Dialog for completion
                    var inputResult = DialogManager.ShowInputDialog(dialogStyle, inputDialogOptions);


                // #################################################################################

                // Show Custom Dialog for completion
                var customResult = DialogManager.ShowCustomDialog(dialogStyle, customDialogOptions);

                    Console.WriteLine($"Custom Dialog DialogResult: {customResult}");
                }
                else
                {
                    Console.WriteLine("Installation deferred or cancelled.");
                }

                // #################################################################################

                // Show Restart Dialog
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
