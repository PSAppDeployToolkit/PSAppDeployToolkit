using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using PSADT.ProcessManagement;
using PSADT.UserInterface;
using PSADT.UserInterface.DialogOptions;
using PSADT.WindowManagement;

namespace PSADT.ClientServer.Server.Tests.TestHelpers
{
    /// <summary>
    /// Builds one valid instance of each options type a payload carries.
    /// </summary>
    /// <remarks>
    /// The payload tests are about the payloads. What they need from an options type is an instance that
    /// is valid, and a second one that differs, so that a payload built around each can be compared - so
    /// building them is done here once rather than in each of the seven files that needs one.
    /// <para>
    /// The dialog options are built from a dictionary because that is the only public way in: they are
    /// constructed from what PowerShell hands over, and the constructor taking the values one by one is
    /// private. Every key those constructors insist on is present.
    /// </para>
    /// <para>
    /// Nothing here reaches the file system. The images are real ones, because the options decode them and
    /// refuse anything that will not load, but they are carried inline: the same validation accepts a
    /// base64 image as readily as a path, so the tests need no file on disk and no fixture to keep beside
    /// them.
    /// </para>
    /// </remarks>
    internal static class SampleOptions
    {
        /// <summary>
        /// A one-pixel transparent image, for the options that insist on one they can decode.
        /// </summary>
        private const string SampleImage = "iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVR42mP8z8BQDwAEhQGAhKmMIQAAAABJRU5ErkJggg==";

        /// <summary>
        /// Builds window information options.
        /// </summary>
        /// <param name="windowTitleRegex">The title pattern, varied to tell two instances apart, or nothing at all
        /// to match every window.</param>
        /// <returns>The options.</returns>
        internal static WindowInfoOptions WindowInfo(string? windowTitleRegex = "^Untitled")
        {
            return new(windowTitleRegex, windowHandleFilter: null, parentProcessFilter: null, parentProcessIdFilter: null, parentProcessMainWindowHandleFilter: null);
        }

        /// <summary>
        /// Builds send keys options.
        /// </summary>
        /// <param name="keys">The keys to send, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static SendKeysOptions SendKeys(string keys = "^s")
        {
            return new(0x1234, keys);
        }

        /// <summary>
        /// Builds shell execute options.
        /// </summary>
        /// <param name="filePath">The file to run, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static UserShellExecuteOptions ShellExecute(string filePath = @"C:\Windows\System32\cmd.exe")
        {
            return new(filePath);
        }

        /// <summary>
        /// Builds balloon tip options.
        /// </summary>
        /// <param name="text">The balloon text, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static BalloonTipOptions BalloonTip(string text = "the balloon text")
        {
            return new(new Hashtable
            {
                ["Title"] = "a title",
                ["Text"] = text,
                ["Icon"] = BalloonTipIcon.Info,
            });
        }

        /// <summary>
        /// Builds notification icon options.
        /// </summary>
        /// <param name="messageText">The tooltip text, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static NotifyIconOptions NotifyIcon(string messageText = "the tooltip text")
        {
            return new(new Hashtable
            {
                ["AppTitle"] = "an application",
                ["AppIconImage"] = SampleImage,
                ["AppTaskbarIconImage"] = null,
                ["MessageText"] = messageText,
            });
        }

        /// <summary>
        /// Builds progress dialog options.
        /// </summary>
        /// <param name="progressMessageText">The progress message, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static ProgressDialogOptions ProgressDialog(string progressMessageText = "the progress message")
        {
            return new(new Hashtable
            {
                ["AppTitle"] = "an application",
                ["Subtitle"] = "a subtitle",
                ["AppIconImage"] = SampleImage,
                ["AppBannerImage"] = SampleImage,
                ["DialogTopMost"] = true,
                ["Language"] = CultureInfo.InvariantCulture,
                ["ProgressMessageText"] = progressMessageText,
                ["ProgressDetailMessageText"] = "the detail message",
            });
        }

        /// <summary>
        /// Builds list selection dialog options, which are the ones holding a list.
        /// </summary>
        /// <param name="listItems">The items to choose between, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static ListSelectionDialogOptions ListSelectionDialog(params string[] listItems)
        {
            return new(new Hashtable
            {
                ["AppTitle"] = "an application",
                ["Subtitle"] = "a subtitle",
                ["AppIconImage"] = SampleImage,
                ["AppBannerImage"] = SampleImage,
                ["DialogTopMost"] = true,
                ["Language"] = CultureInfo.InvariantCulture,
                ["MessageText"] = "the message",
                ["ButtonRightText"] = "OK",
                ["ListItems"] = listItems.Length > 0 ? listItems : ["alpha", "bravo"],
                ["Strings"] = new Hashtable { ["ListSelectionMessage"] = "choose one" },
            });
        }

        /// <summary>
        /// Builds help console options, which are the ones holding a dictionary of dictionaries.
        /// </summary>
        /// <param name="description">The help text, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static HelpConsoleOptions HelpConsole(string description = "what it does")
        {
            return new(new Hashtable
            {
                ["ModuleHelpMap"] = new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
                {
                    ["a module"] = new Dictionary<string, string>(StringComparer.Ordinal) { ["a function"] = description },
                },
            });
        }

        /// <summary>
        /// Builds dialog box options, as one concrete kind of dialog options a modal dialog payload carries.
        /// </summary>
        /// <param name="messageText">The message, varied to tell two instances apart.</param>
        /// <returns>The options.</returns>
        internal static DialogBoxOptions DialogBox(string messageText = "the message")
        {
            return new(new Hashtable
            {
                ["AppTitle"] = "an application",
                ["MessageText"] = messageText,
                ["DialogButtons"] = DialogBoxButtons.Ok,
                ["DialogDefaultButton"] = DialogBoxDefaultButton.First,
                ["DialogTopMost"] = true,
                ["DialogExpiryDuration"] = TimeSpan.FromMinutes(5),
            });
        }
    }
}
