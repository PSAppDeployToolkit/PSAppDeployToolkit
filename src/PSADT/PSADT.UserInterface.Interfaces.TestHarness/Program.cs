using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Management.Automation.Language;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using PSADT.ProcessManagement;
using PSADT.UserInterface.DialogOptions;
using PSADT.UserInterface.DialogResults;
using PSADT.UserInterface.DialogState;
using PSADT.UserInterface.Interfaces;
using PSADT.Utilities;
using PSAppDeployToolkit.Foundation;

namespace PSADT.UserInterface.TestHarness
{
    internal static class Program
    {
        /// <summary>
        /// Initializes the application by setting the unhandled exception handler for the dialog manager.
        /// </summary>
        /// <remarks>This method is automatically called when the module is loaded, ensuring that any
        /// unhandled exceptions are managed appropriately.</remarks>
        [ModuleInitializer]
        internal static void Init()
        {
            AppDomain.CurrentDomain.SetData("PSADT.UserInterface.DialogManager.UnhandledExceptionHandler", static (Exception ex) => { throw new InvalidProgramException("An unhandled WPF exception occurred.", ex); });
        }

        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        private static async Task Main()
        {
            // What dialog style are we running with?
            const DialogStyle dialogStyle = DialogStyle.Fluent; // or DialogStyle.Classic

            // Read PSADT's string table into memory.
            ScriptBlockAst stringsAst = Parser.ParseFile(Path.GetFullPath($@"{AppDomain.CurrentDomain.BaseDirectory}\..\..\..\..\..\PSAppDeployToolkit\ImportsLast.ps1"), out Token[]? tokens, out ParseError[]? errors);
            if (errors.Length > 0)
            {
                throw new InvalidDataException($"Error parsing strings.psd1 file.");
            }

            // Read out the hashtable
            Hashtable stringTable = GetModuleDefaultTable(stringsAst, "Strings");
            Hashtable configTable = GetModuleDefaultTable(stringsAst, "Config");
            Hashtable assetsTable = (Hashtable)configTable["Assets"]!;

            // Set up parameters for testing
            const string appTitle = "Adobe Creative Suite 2.1.45 EN";
            const string subtitle = "EQ Bank Global IT Services - App Installation";
            string appIconImage = (string?)assetsTable["Logo"]!;
            string appIconDarkImage = (string?)assetsTable["Logo"]!;
            string appBannerImage = (string?)assetsTable["Banner"]!;
            const DeploymentType deploymentType = DeploymentType.Install;

            ReadOnlyCollection<ProcessDefinition> appsToClose = new(
            [
                new("remotedesktopmanager", "Remote Desktop Manager"),
                new("chrome", "Google Chrome"),
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
                new("notepad", "Windows Notepad"),
                new("regedit", "Windows Registry Editor"),
                new("taskmgr", "Windows Task Manager")
            ]);

            TimeSpan dialogExpiryDuration = TimeSpan.FromSeconds(580);

            TimeSpan countdownDuration = TimeSpan.FromSeconds(580);

            const string customMessageText = @"Basic URL: [url]https://example.com[/url]
URL with Description: [url=https://example.com]Read the IT Security Policy here[/url].
This is [bold]bold text[/bold] and [italic]italic text[/italic].
Nested tags: [bold]Bold plus [italic]italic inside[/italic], with an [accent]accent[/accent][/bold].
Double nested tags: A cheeky [bold][accent][italic]bold italic accent![/italic][/accent][/bold].";

            const uint deferralsRemaining = 99;
            DateTime deferralDeadline = DateTime.Parse("2026-06-04T13:00:00", CultureInfo.InvariantCulture);

            const string progressMessageText = "Performing [accent]pre-flight checks[/accent]…";
            const string progressDetailMessageText = "Testing your [accent]system to ensure compatibility[/accent]. Please wait…";

            TimeSpan restartCountdownDuration = TimeSpan.FromSeconds(540); // Set this high so we have 9 mins before we accidentally reboot ourselves
            TimeSpan restartCountdownNoMinimizeDuration = TimeSpan.FromSeconds(120); // 2 mins before the user can no longer minimize the restart dialog

            const string customDialogMessageText = "The installation requires you to have an exceptional amount of patience, as well an almost superhuman ability to not lose your temper. Given that you have not had much and seem to be super-cranky, are you sure you want to proceed? [bold]URL Formatting Tests:[/bold] Visit [url]https://psappdeploytoolkit.com[/url] or check our [url=https://github.com/PSAppDeployToolkit/PSAppDeployToolkit]GitHub Repository[/url] for support.";
            const string customDialogButtonLeftText = "LeftButton";
            const string customDialogButtonMiddleText = "MiddleButton";
            const string customDialogButtonRightText = "RightButton";

            const string listDialogMessageText = "Please choose how you’d like to use Adobe Creative Cloud on this device. You can change this later in Preferences.";
            string[] listDialogItems = ["Personal (Individual Plan)", "Team (Creative Cloud for Teams)", "Enterprise (Managed by IT)", "Education (Student / Faculty)", "Shared Device (Lab / Classroom)"];
            const string listDialogButtonLeftText = "OK";
            const string listDialogButtonRightText = "Cancel";

            const string inputDialogMessageText = "Enter the server name e.g. [italic]remotesvr1.psadt.ca[/italic]";
            const string inputDialogTextBox = "YouCompleteMe";
            const string inputDialogButtonLeftText = "Continue";
            const string inputDialogButtonRightText = "Cancel";

            // Set up options for the dialogs
            await using CloseAppsDialogState closeAppsDialogState = new(appsToClose, (_, _, _) => { });
            Hashtable closeAppsDialogOptions = new()
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "DialogTopMost", true },
                { "DialogAllowMove", true },
                { "DialogAllowMinimize", true },
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "CountdownDuration", countdownDuration },
                { "DeferralsRemaining", deferralsRemaining },
                { "DeferralDeadline", deferralDeadline },
                { "CustomMessageText", customMessageText },
                { "Language", CultureInfo.CurrentCulture },
                { "Strings", (Hashtable)stringTable["CloseAppsPrompt"]! },
            };
            ProgressDialogOptions progressDialogOptions = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF00CC6A) }, // Accent Color: Green #00CC6A
                { "DialogAllowMinimize", true },
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
            CustomDialogOptions customDialogOptions = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF0099BC) }, // Accent Color: Cyan #0099BC
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

            CustomDialogOptions customDialog2Options = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFF4A5459) }, // Accent Color: Navy Blue #4A5459
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "ButtonLeftText", customDialogButtonLeftText },
                { "ButtonRightText", customDialogButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MinimizeWindows", false },
                { "Language", CultureInfo.CurrentCulture },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });


            CustomDialogOptions customDialog3Options = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFF7630C) }, // Accent Color: Orange #F7630C
                { "AppTitle", appTitle },
                { "Subtitle", subtitle },
                { "AppIconImage", appIconImage },
                { "AppIconDarkImage", appIconDarkImage },
                { "AppBannerImage", appBannerImage },
                { "MessageText", customDialogMessageText },
                { "ButtonRightText", customDialogButtonRightText },
                { "Icon", DialogSystemIcon.Information },
                { "MinimizeWindows", false },
                { "Language", CultureInfo.CurrentCulture },
                { "MessageAlignment", DialogMessageAlignment.Left }
            });

            ListSelectionDialogOptions listSelectionDialogOptions = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFF600CE) }, // Accent Color: Purple #F600CE
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

            InputDialogOptions inputDialogOptions = new(new Hashtable
            {
                { "DialogExpiryDuration", dialogExpiryDuration },
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFFFB900) }, // Accent Color: Yellow #FFB900
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
                { "FluentAccentColor", ValueTypeConverter.ToInt(0xFFE81123) }, // Accent Color: Red #E81123
                { "DialogTopMost", true },
                { "DialogAllowMove", true },
                { "DialogAllowMinimize", true },
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

            CloseAppsDialogResult closeAppsResult = await DialogManager.ShowCloseAppsDialogAsync(dialogStyle, new CloseAppsDialogOptions(deploymentType, closeAppsDialogOptions), closeAppsDialogState); // Pass the service as optional parameter

            if (closeAppsResult.Equals(CloseAppsDialogResult.Defer))
            {
                return;
            }

            // #################################################################################

            // Show Progress Dialog

            await DialogManager.ShowProgressDialogAsync(dialogStyle, progressDialogOptions);

            Thread.Sleep(3000); // Simulate some work being done

            // Simulate a process with progress updates
            for (int i = 0; i <= 100; i += 10)
            {
                // Update progress
                await DialogManager.UpdateProgressDialogAsync($"Installation progress: {i}%", $"Step {i / 10} of 10", i);
                Thread.Sleep(250);  // Simulate work being done
            }

            // Close Progress Dialog
            await DialogManager.CloseProgressDialogAsync();

            // #################################################################################

            // Show Custom Dialog

            string customResult = await DialogManager.ShowCustomDialogAsync(dialogStyle, customDialogOptions);

            if (customResult == customDialogButtonRightText)
            {
                return;
            }

            // #################################################################################

            // Show Custom2 Dialog

            string custom2Result = await DialogManager.ShowCustomDialogAsync(dialogStyle, customDialog2Options);

            if (customResult == customDialogButtonRightText)
            {
                return;
            }

            // #################################################################################

            // Show Custom3 Dialog

            string custom3Result = await DialogManager.ShowCustomDialogAsync(dialogStyle, customDialog3Options);

            // This dialog only has one button, so we don't need to bother checking the result.

            // #################################################################################

            // Show List Selection Dialog

            ListSelectionDialogResult listSelectionResult = await DialogManager.ShowListSelectionDialogAsync(dialogStyle, listSelectionDialogOptions);

            if (listSelectionResult.Result == listDialogButtonRightText)
            {
                return;
            }

            Console.WriteLine(listSelectionResult.SelectedItem);

            // #################################################################################

            // Show Input Dialog

            InputDialogResult inputResult = await DialogManager.ShowInputDialogAsync(dialogStyle, inputDialogOptions);

            if (inputResult.Result == inputDialogButtonRightText)
            {
                return;
            }

            Console.WriteLine(inputResult.Text);

            // #################################################################################

            // Show Restart Dialog
            _ = await DialogManager.ShowRestartDialogAsync(dialogStyle, new RestartDialogOptions(deploymentType, restartDialogOptions));

            // No need to check the result of the Restart Dialog
        }

        /// <summary>
        /// Retrieves the default hashtable from the requested ImportsLast.ps1 module defaults entry.
        /// </summary>
        /// <param name="importsAst">The parsed ImportsLast.ps1 AST.</param>
        /// <param name="tableName">The module defaults table name.</param>
        /// <returns>The default hashtable for the requested module defaults table.</returns>
        private static Hashtable GetModuleDefaultTable(ScriptBlockAst importsAst, string tableName)
        {
            return (Hashtable)(importsAst.Find(node =>
                node is HashtableAst hashtableAst &&
                IsModuleDefaultHashtable(hashtableAst, tableName),
                true)?.SafeGetValue() ?? throw new InvalidDataException($"Unable to locate the '{tableName}' defaults hashtable in ImportsLast.ps1."));
        }

        /// <summary>
        /// Determines whether the hashtable is the default language/config hashtable in the requested module defaults entry.
        /// </summary>
        /// <param name="hashtableAst">The hashtable AST to inspect.</param>
        /// <param name="tableName">The module defaults table name.</param>
        /// <returns><see langword="true" /> when the hashtable belongs to the requested module defaults entry; otherwise, <see langword="false" />.</returns>
        private static bool IsModuleDefaultHashtable(HashtableAst hashtableAst, string tableName)
        {
            return TryFindAncestor(hashtableAst.Parent, static invoke =>
                invoke.Member.Extent.Text == "new" &&
                invoke.Arguments.Count >= 2 &&
                invoke.Arguments[0].Extent.Text == "[System.String]::Empty",
                out InvokeMemberExpressionAst? defaultEntry) &&
                defaultEntry != null &&
                TryFindAncestor(defaultEntry.Parent, invoke =>
                invoke.Member.Extent.Text == "new" &&
                invoke.Arguments.Count >= 2 &&
                invoke.Arguments[0].Extent.Text == $"'{tableName}'",
                out _);
        }

        /// <summary>
        /// Finds the first ancestor invoke member expression matching the specified predicate.
        /// </summary>
        /// <param name="ast">The AST to start from.</param>
        /// <param name="predicate">The predicate used to match an invoke member expression.</param>
        /// <param name="invokeMemberExpressionAst">The matching invoke member expression, if found.</param>
        /// <returns><see langword="true" /> when a matching ancestor is found; otherwise, <see langword="false" />.</returns>
        private static bool TryFindAncestor(Ast? ast, Func<InvokeMemberExpressionAst, bool> predicate, out InvokeMemberExpressionAst? invokeMemberExpressionAst)
        {
            for (Ast? current = ast; current != null; current = current.Parent)
            {
                if (current is InvokeMemberExpressionAst currentInvokeMemberExpressionAst && predicate(currentInvokeMemberExpressionAst))
                {
                    invokeMemberExpressionAst = currentInvokeMemberExpressionAst;
                    return true;
                }
            }

            invokeMemberExpressionAst = null;
            return false;
        }
    }
}
