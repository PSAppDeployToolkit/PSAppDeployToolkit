using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation.Language;
using System.Threading;
using PSADT.ProcessManagement;
using PSADT.Utilities;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSAppDeployToolkit.Foundation;

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
            DialogStyle dialogStyle = DialogStyle.Fluent; // or DialogStyle.Classic

            // Read PSADT's string table into memory.
            ScriptBlockAst stringsAst = Parser.ParseFile(Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Strings\strings.psd1"), out Token[]? tokens, out ParseError[]? errors);
            if (errors.Length > 0)
            {
                throw new InvalidDataException($"Error parsing strings.psd1 file.");
            }

            // Read out the hashtable
            Hashtable stringTable = (Hashtable)stringsAst.Find(x => x is HashtableAst, false).SafeGetValue();

            // Set up parameters for testing
            string appTitle = "Adobe Creative Suite 2.1.45 EN";
            string subtitle = "EQ Bank Global IT Services - App Install";
            string appIconImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\AppIcon.png");
            string appIconDarkImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\AppIcon.png");
            string appBannerImage = Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\Assets\Banner.Classic.png");
            // var FluentAccentColor = ValueTypeConverter.ToInt(0xFF01C9D9); // Cyan
            DialogPosition dialogPosition = DialogPosition.BottomRight;
            // DialogPosition dialogPosition = DialogPosition.Center;
            bool dialogTopMost = true;
            bool dialogAllowMove = true;
            DeploymentType deploymentType = DeploymentType.Install;

            ReadOnlyCollection<ProcessDefinition> appsToClose = new(
            [
                new("remotedesktopmanager", "Remote Desktop Manager"),
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
            ]);

            TimeSpan dialogExpiryDuration = TimeSpan.FromSeconds(580);

            TimeSpan countdownDuration = TimeSpan.FromSeconds(580);

            string customMessageText = @"Basic URL: [url]https://example.com[/url]
URL with Description: [url=https://example.com]Read the IT Security Policy here[/url].
This is [bold]bold text[/bold] and [italic]italic text[/italic].
Nested tags: [bold]Bold plus [italic]italic inside[/italic], with an [accent]accent[/accent][/bold].
Double nested tags: A cheeky [bold][accent][italic]bold italic accent![/italic][/accent][/bold].";

            uint deferralsRemaining = 3;
            DateTime deferralDeadline = DateTime.Parse("2025-09-20T13:00:00", CultureInfo.InvariantCulture);

            // DateTime? deferralDeadline = null;
            string progressMessageText = "Performing [accent]pre-flight checks[/accent]…";
            string progressDetailMessageText = "Testing your [accent]system to ensure compatibility[/accent]. Please wait…";

            TimeSpan restartCountdownDuration = TimeSpan.FromSeconds(80);
            TimeSpan restartCountdownNoMinimizeDuration = TimeSpan.FromSeconds(70);

            string customDialogMessageText = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you have not had much and seem to be super-cranky, are you sure you want to proceed? [bold]URL Formatting Tests:[/bold] Visit [url]https://psappdeploytoolkit.com[/url] or check our [url=https://github.com/PSAppDeployToolkit/PSAppDeployToolkit]GitHub Repository[/url] for support.";
            string customDialogButtonLeftText = "LeftButton";
            string customDialogButtonMiddleText = "MiddleButton";
            string customDialogButtonRightText = "RightButton";

            string listDialogMessageText = "Please choose how you’d like to use Adobe Creative Cloud on this device. You can change this later in Preferences.";
            string[] listDialogItems = ["Personal (Individual Plan)", "Team (Creative Cloud for Teams)", "Enterprise (Managed by IT)", "Education (Student / Faculty)", "Shared Device (Lab / Classroom)"];
            string listDialogButtonLeftText = "OK";
            string listDialogButtonRightText = "Cancel";

            string inputDialogMessageText = "Enter the server name e.g. [italic]remotesvr1.psadt.ca[/italic]";
            string inputDialogTextBox = "YouCompleteMe";
            string inputDialogButtonLeftText = "Continue";
            string inputDialogButtonRightText = "Cancel";

            // Set up options for the dialogs
            using CloseAppsDialogState closeAppsDialogState = new(appsToClose, (_, _, _) => { });
            Hashtable closeAppsDialogOptions = new()
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
                { "DialogAllowMinimize", true },
                { "CustomMessageText", customMessageText },
                { "Language", CultureInfo.CurrentCulture },
                { "Strings", (Hashtable)stringTable["CloseAppsPrompt"]! },
            };
            ProgressDialogOptions progressDialogOptions = new(new()
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
                { "Language", CultureInfo.CurrentCulture },
                { "AdditionalOption", true }
            });
            CustomDialogOptions customDialogOptions = new(new()
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF00B7C3) }, // Accent Color: Cyan #00B7C3
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "ButtonLeftText", customDialogButtonLeftText },
                { "ButtonMiddleText", customDialogButtonMiddleText },
                { "ButtonRightText", customDialogButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MinimizeWindows", false },
                { "Language", CultureInfo.CurrentCulture },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });

            ListSelectionDialogOptions listSelectionDialogOptions = new(new()
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF00D326) }, // Accent Color: Blue #00D326
                { "DialogPosition", dialogPosition },
                { "DialogTopMost", dialogTopMost },
                { "DialogAllowMove", dialogAllowMove },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", listDialogMessageText },
                { "ButtonLeftText", listDialogButtonLeftText },
                { "ButtonRightText", listDialogButtonRightText },
                { "ListItems", listDialogItems },
                { "InitialSelectedItem", listDialogItems[0] },
                { "Strings", (Hashtable)stringTable["ListSelectionPrompt"]! },
                { "MinimizeWindows", false },
                { "Language", CultureInfo.CurrentCulture },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });

            InputDialogOptions inputDialogOptions = new(new()
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
                { "Language", CultureInfo.CurrentCulture },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });
            Hashtable restartDialogOptions = new()
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
                { "CountdownDuration", restartCountdownDuration },
                { "CountdownNoMinimizeDuration", restartCountdownNoMinimizeDuration },
                // { "CustomMessageText", customMessageText },
                { "Language", CultureInfo.CurrentCulture },
                { "Strings", (Hashtable)stringTable["RestartPrompt"]! },
            };

            // #################################################################################

            // Show CloseApps Dialog

            CloseAppsDialogResult closeAppsResult = DialogManager.ShowCloseAppsDialog(dialogStyle, new CloseAppsDialogOptions(deploymentType, closeAppsDialogOptions), closeAppsDialogState); // Pass the service as optional parameter

            if (closeAppsResult == CloseAppsDialogResult.Defer)
            {
                Environment.Exit(0);
            }

            // #################################################################################

            // Show Progress Dialog

            DialogManager.ShowProgressDialog(dialogStyle, progressDialogOptions);

            Thread.Sleep(3000); // Simulate some work being done

            // Simulate a process with progress updates
            for (int i = 0; i <= 100; i += 10)
            {
                // Update progress
                DialogManager.UpdateProgressDialog($"Installation progress: {i}%", $"Step {i / 10} of 10", i);
                Thread.Sleep(250);  // Simulate work being done
            }

            // Close Progress Dialog
            DialogManager.CloseProgressDialog();

            // #################################################################################

            // Show Custom Dialog

            string customResult = DialogManager.ShowCustomDialog(dialogStyle, customDialogOptions);

            if (customResult == customDialogButtonRightText)
            {
                Environment.Exit(0);
            }

            // #################################################################################

            // Show List Selection Dialog

            ListSelectionDialogResult listSelectionResult = DialogManager.ShowListSelectionDialog(dialogStyle, listSelectionDialogOptions);

            if (listSelectionResult.Result == listDialogButtonRightText)
            {
                Environment.Exit(0);
            }

            Console.WriteLine(listSelectionResult.SelectedItem?.ToString());

            // #################################################################################

            // Show Input Dialog

            InputDialogResult inputResult = DialogManager.ShowInputDialog(dialogStyle, inputDialogOptions);

            if (inputResult.Result == inputDialogButtonRightText)
            {
                Environment.Exit(0);
            }

            Console.WriteLine(inputResult.Text);

            // #################################################################################

            // Show Restart Dialog
            string restartResult = DialogManager.ShowRestartDialog(dialogStyle, new RestartDialogOptions(deploymentType, restartDialogOptions));

            // No need to check the result of the Restart Dialog
        }
    }
}
