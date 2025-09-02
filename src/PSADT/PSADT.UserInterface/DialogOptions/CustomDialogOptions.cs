using System;
using System.Collections;
using System.Globalization;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CustomDialog.
    /// </summary>
    public record CustomDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["MessageText"] is not string messageText || string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException("MessageText value is null or invalid.", (Exception?)null);
            }
            if (options["MinimizeWindows"] is not bool minimizeWindows)
            {
                throw new ArgumentNullException("MinimizeWindows value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("MessageAlignment"))
            {
                if (options["MessageAlignment"] is not DialogMessageAlignment messageAlignment)
                {
                    throw new ArgumentOutOfRangeException("MessageAlignment value is not valid.", (Exception?)null);
                }
                MessageAlignment = messageAlignment;
            }
            if (options.ContainsKey("ButtonLeftText"))
            {
                if (options["ButtonLeftText"] is not string buttonLeftText || string.IsNullOrWhiteSpace(buttonLeftText))
                {
                    throw new ArgumentOutOfRangeException("ButtonLeftText value is not valid.", (Exception?)null);
                }
                ButtonLeftText = buttonLeftText;
            }
            if (options.ContainsKey("ButtonMiddleText"))
            {
                if (options["ButtonMiddleText"] is not string buttonMiddleText || string.IsNullOrWhiteSpace(buttonMiddleText))
                {
                    throw new ArgumentOutOfRangeException("ButtonMiddleText value is not valid.", (Exception?)null);
                }
                ButtonMiddleText = buttonMiddleText;
            }
            if (options.ContainsKey("ButtonRightText"))
            {
                if (options["ButtonRightText"] is not string buttonRightText || string.IsNullOrWhiteSpace(buttonRightText))
                {
                    throw new ArgumentOutOfRangeException("ButtonRightText value is not valid.", (Exception?)null);
                }
                ButtonRightText = buttonRightText;
            }
            if (options.ContainsKey("Icon"))
            {
                if (options["Icon"] is not DialogSystemIcon icon)
                {
                    throw new ArgumentOutOfRangeException("Icon value is not valid.", (Exception?)null);
                }
                Icon = icon;
            }

            // The hashtable was correctly defined, assign the remaining values.
            MessageText = messageText;
            MinimizeWindows = minimizeWindows;

            // At least one button must be defined before we finish.
            if (string.IsNullOrWhiteSpace(ButtonLeftText) && string.IsNullOrWhiteSpace(ButtonMiddleText) && string.IsNullOrWhiteSpace(ButtonRightText))
            {
                throw new ArgumentNullException("At least one button must be defined.", (Exception?)null);
            }
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class with the specified dialog
        /// configuration options.
        /// </summary>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle displayed in the dialog, providing additional context.</param>
        /// <param name="appIconImage">The path to the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path to the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path to the banner image displayed in the dialog.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information used for localizing the dialog.</param>
        /// <param name="fluentAccentColor">The accent color used for fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, the default interval is
        /// used.</param>
        /// <param name="messageText">The main message text displayed in the dialog. Cannot be <see langword="null"/> or empty.</param>
        /// <param name="messageAlignment">The alignment of the message text within the dialog. If <see langword="null"/>, the default alignment is
        /// used.</param>
        /// <param name="buttonLeftText">The text displayed on the left button in the dialog. If <see langword="null"/>, the button is not displayed.</param>
        /// <param name="buttonMiddleText">The text displayed on the middle button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="buttonRightText">The text displayed on the right button in the dialog. If <see langword="null"/>, the button is not
        /// displayed.</param>
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">Indicates whether all other windows should be minimized when the dialog is displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageText"/> is <see langword="null"/> or empty.</exception>
        [JsonConstructor]
        protected CustomDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogSystemIcon? icon, bool minimizeWindows) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            MessageText = messageText ?? throw new ArgumentNullException(nameof(messageText), "MessageText cannot be null or empty.");
            MessageAlignment = messageAlignment;
            ButtonLeftText = buttonLeftText;
            ButtonMiddleText = buttonMiddleText;
            ButtonRightText = buttonRightText;
            Icon = icon;
            MinimizeWindows = minimizeWindows;
        }

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string MessageText;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly DialogMessageAlignment? MessageAlignment;

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string? ButtonLeftText;

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string? ButtonMiddleText;

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string? ButtonRightText;

        /// <summary>
        /// The icon to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly DialogSystemIcon? Icon;

        /// <summary>
        /// Gets a value indicating whether windows should be minimized.
        /// </summary>
        [JsonProperty]
        public readonly bool MinimizeWindows;
    }
}
