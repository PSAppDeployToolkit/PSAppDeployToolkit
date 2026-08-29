using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PSADT.UserInterface.DialogOptions;
using PSAppDeployToolkit.Foundation;

namespace PSADT.UserInterface.Tests.TestHelpers
{
    /// <summary>
    /// Builds the smallest dictionary each dialog options type will accept.
    /// </summary>
    /// <remarks>
    /// Every options type is constructed from an <see cref="IDictionary"/> assembled by the PowerShell
    /// module, and the ones deriving from <c>BaseDialogOptions</c> share fifteen keys before they add any
    /// of their own. A test that spelled all of those out per case would be mostly restating the base
    /// type, so it is written once here and each builder adds only what its own type needs.
    /// <para>
    /// Builders return the dictionary rather than the constructed options, so a test can take a valid one
    /// and change or remove the single key it is about to make an assertion on. Every value is a required
    /// one: an optional key left absent here is absent on purpose, so that a test wanting it says so.
    /// </para>
    /// </remarks>
    internal static class SampleOptions
    {
        /// <summary>
        /// The keys every <c>BaseDialogOptions</c> derivative requires.
        /// </summary>
        /// <returns>A new dictionary each call, so callers can mutate it freely.</returns>
        public static Hashtable BaseDialog()
        {
            return new Hashtable
            {
                ["AppTitle"] = "an application",
                ["Subtitle"] = "a subtitle",
                ["AppIconImage"] = TestImages.SampleImage(),
                ["AppBannerImage"] = TestImages.SampleImage(),
                ["DialogTopMost"] = true,
                ["Language"] = CultureInfo.InvariantCulture,
            };
        }

        /// <summary>
        /// The keys <see cref="BalloonTipOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable BalloonTip()
        {
            return new Hashtable
            {
                ["Title"] = "a title",
                ["Text"] = "the balloon text",
                ["Icon"] = BalloonTipIcon.Info,
            };
        }

        /// <summary>
        /// The keys <see cref="NotifyIconOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable NotifyIcon()
        {
            return new Hashtable
            {
                ["AppTitle"] = "an application",
                ["AppIconImage"] = TestImages.SampleImage(),
                ["MessageText"] = "the tooltip text",
            };
        }

        /// <summary>
        /// The keys <see cref="DialogBoxOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable DialogBox()
        {
            return new Hashtable
            {
                ["AppTitle"] = "an application",
                ["MessageText"] = "the message",
                ["DialogButtons"] = DialogBoxButtons.Ok,
                ["DialogDefaultButton"] = DialogBoxDefaultButton.First,
                ["DialogTopMost"] = true,
                ["DialogExpiryDuration"] = TimeSpan.FromMinutes(5),
            };
        }

        /// <summary>
        /// The keys <see cref="HelpConsoleOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable HelpConsole()
        {
            return new Hashtable
            {
                ["ModuleHelpMap"] = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
                {
                    ["a module"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["a function"] = "what it does" },
                },
            };
        }

        /// <summary>
        /// The keys <see cref="ProgressDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable ProgressDialog()
        {
            Hashtable options = BaseDialog();
            options["ProgressMessageText"] = "the progress message";
            options["ProgressDetailMessageText"] = "the detail message";
            return options;
        }

        /// <summary>
        /// The keys <see cref="CustomDialogOptions"/> requires, with one button defined.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable CustomDialog()
        {
            Hashtable options = BaseDialog();
            options["MessageText"] = "the message";
            options["ButtonRightText"] = "OK";
            return options;
        }

        /// <summary>
        /// The keys <see cref="InputDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable InputDialog()
        {
            return CustomDialog();
        }

        /// <summary>
        /// The keys <see cref="ListSelectionDialogOptions"/> requires.
        /// </summary>
        /// <param name="listItems">The items to offer, or none for a default pair.</param>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable ListSelectionDialog(params string[] listItems)
        {
            Hashtable options = CustomDialog();
            options["ListItems"] = listItems.Length > 0 ? listItems : ["alpha", "bravo"];
            options["Strings"] = new Hashtable { ["ListSelectionMessage"] = "choose one" };
            return options;
        }

        /// <summary>
        /// The keys <see cref="CloseAppsDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable CloseAppsDialog()
        {
            Hashtable options = BaseDialog();
            options["Strings"] = CloseAppsStrings();
            return options;
        }

        /// <summary>
        /// The nested 'Strings' dictionary <see cref="CloseAppsDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable CloseAppsStrings()
        {
            return new Hashtable
            {
                ["Classic"] = new Hashtable
                {
                    ["WelcomeMessage"] = PerDeploymentType("welcome"),
                    ["CloseAppsMessage"] = PerDeploymentType("close these"),
                    ["ExpiryMessage"] = PerDeploymentType("this expires"),
                    ["DeferralsRemaining"] = "deferrals remaining",
                    ["DeferralDeadline"] = "deferral deadline",
                    ["ExpiryWarning"] = "expiry warning",
                    ["CountdownDefer"] = PerDeploymentType("counting down to defer"),
                    ["CountdownClose"] = PerDeploymentType("counting down to close"),
                    ["ButtonClose"] = "Close",
                    ["ButtonDefer"] = "Defer",
                    ["ButtonContinue"] = "Continue",
                    ["ButtonContinueTooltip"] = "continue tooltip",
                },
                ["Fluent"] = new Hashtable
                {
                    ["DialogMessage"] = PerDeploymentType("dialog message"),
                    ["DialogMessageNoProcesses"] = PerDeploymentType("nothing is running"),
                    ["AutomaticStartCountdown"] = "automatic start countdown",
                    ["DeferralsRemaining"] = "deferrals remaining",
                    ["DeferralDeadline"] = "deferral deadline",
                    ["ButtonLeftText"] = PerDeploymentType("left"),
                    ["ButtonRightText"] = "right",
                    ["ButtonLeftNoProcessesText"] = PerDeploymentType("left with nothing running"),
                },
            };
        }

        /// <summary>
        /// The keys <see cref="RestartDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable RestartDialog()
        {
            Hashtable options = BaseDialog();
            options["Strings"] = RestartStrings();
            return options;
        }

        /// <summary>
        /// The nested 'Strings' dictionary <see cref="RestartDialogOptions"/> requires.
        /// </summary>
        /// <returns>A new dictionary each call.</returns>
        public static Hashtable RestartStrings()
        {
            return new Hashtable
            {
                ["Title"] = "a title",
                ["Message"] = PerDeploymentType("a message"),
                ["MessageTime"] = "message time",
                ["MessageRestart"] = "message restart",
                ["TimeRemaining"] = "time remaining",
                ["ButtonRestartNow"] = "Restart Now",
                ["ButtonRestartLater"] = "Restart Later",
                ["ButtonCancel"] = "Cancel",
            };
        }

        /// <summary>
        /// Walks into a nested table within a fixture, so a test can damage one string in it.
        /// </summary>
        /// <remarks>
        /// Written as a helper rather than an inline cast because the two target frameworks disagree
        /// about <see cref="Hashtable"/>'s indexer: .NET annotates it as returning a nullable, .NET
        /// Framework does not, so a null-forgiving operator is required on one and reported as
        /// unnecessary on the other. Going through a type test and a throw satisfies both.
        /// </remarks>
        /// <param name="table">The fixture to walk into.</param>
        /// <param name="keys">The nested keys to follow, outermost first.</param>
        /// <returns>The nested table.</returns>
        /// <exception cref="InvalidOperationException">Thrown if a key is absent or does not hold a table.</exception>
        public static Hashtable Nested(Hashtable table, params string[] keys)
        {
            ArgumentNullException.ThrowIfNull(table);
            ArgumentNullException.ThrowIfNull(keys);
            Hashtable current = table;
            foreach (string key in keys)
            {
                current = current[key] as Hashtable ?? throw new InvalidOperationException($"The fixture holds no nested table at '{key}'.");
            }
            return current;
        }

        /// <summary>
        /// Wraps a string as the per-deployment-type dictionary the strings tables use for the values
        /// that are worded differently for an install, an uninstall and a repair.
        /// </summary>
        /// <param name="text">The text to give every deployment type, suffixed with which one it is so a
        /// test can tell whether the right branch was read.</param>
        /// <returns>A dictionary keyed by <see cref="DeploymentType"/> name.</returns>
        public static Hashtable PerDeploymentType(string text)
        {
            Hashtable table = [];
            foreach (DeploymentType deploymentType in EnumValues.Declared<DeploymentType>())
            {
                table[deploymentType.ToString()] = $"{text} ({deploymentType})";
            }
            return table;
        }
    }
}
