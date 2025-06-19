using System;
using System.Collections;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    public sealed record InputDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public InputDialogOptions(Hashtable options) : base(options)
        {
            // Just set our one and only field.
            if (options.ContainsKey("InitialInputText"))
            {
                if (options["InitialInputText"] is not string initialInputText || string.IsNullOrWhiteSpace(initialInputText))
                {
                    throw new ArgumentOutOfRangeException("InitialInputText value is not valid.", (Exception?)null);
                }
                InitialInputText = initialInputText;
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="InputDialogOptions"/> class with the specified dialog
        /// configuration options.
        /// </summary>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle text displayed in the dialog.</param>
        /// <param name="appIconImage">The path to the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path to the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path to the banner image displayed in the dialog.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="fluentAccentColor">The accent color used for Fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, persistence is disabled.</param>
        /// <param name="messageText">The main message text displayed in the dialog.</param>
        /// <param name="messageAlignment">The alignment of the message text within the dialog. If <see langword="null"/>, the default alignment is
        /// used.</param>
        /// <param name="buttonLeftText">The text displayed on the left button in the dialog. If <see langword="null"/>, the button is not displayed.</param>
        /// <param name="buttonMiddleText">The text displayed on the middle button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="buttonRightText">The text displayed on the right button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">A value indicating whether all other windows should be minimized when the dialog is displayed.</param>
        /// <param name="initialInputText">The initial text displayed in the input field of the dialog. If <see langword="null"/>, the input field is
        /// empty.</param>
        [JsonConstructor]
        private InputDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, DialogPosition? dialogPosition, bool? dialogAllowMove, bool dialogTopMost, int? fluentAccentColor, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogSystemIcon? icon, bool minimizeWindows, string? initialInputText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, dialogPosition, dialogAllowMove, dialogTopMost, fluentAccentColor, dialogExpiryDuration, dialogPersistInterval, messageText, messageAlignment, buttonLeftText, buttonMiddleText, buttonRightText, icon, minimizeWindows)
        {
            InitialInputText = initialInputText;
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        [JsonProperty]
        public readonly string? InitialInputText;
    }
}
