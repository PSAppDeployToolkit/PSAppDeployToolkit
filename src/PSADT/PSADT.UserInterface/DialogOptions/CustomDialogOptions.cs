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
        public CustomDialogOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] is string appTitle ? appTitle : string.Empty,
            options["Subtitle"] is string subtitle ? subtitle : string.Empty,
            options["AppIconImage"] is string appIconImage ? appIconImage : string.Empty,
            options["AppIconDarkImage"] is string appIconDarkImage ? appIconDarkImage : string.Empty,
            options["AppBannerImage"] is string appBannerImage ? appBannerImage : string.Empty,
            options["AppTrayIconImage"] is string appTrayIconImage ? appTrayIconImage : null,
            options["DialogTopMost"] is bool dialogTopMost && dialogTopMost,
            options["Language"] is CultureInfo language ? language : null!,
            options["FluentAccentColor"] is int fluentAccentColor ? fluentAccentColor : null,
            options["DialogPosition"] is DialogPosition dialogPosition ? dialogPosition : null,
            options["DialogAllowMove"] is bool dialogAllowMove ? dialogAllowMove : null,
            options["DialogExpiryDuration"] is TimeSpan dialogExpiryDuration ? dialogExpiryDuration : null,
            options["DialogPersistInterval"] is TimeSpan dialogPersistInterval ? dialogPersistInterval : null,
            options["MessageText"] is string messageText ? messageText : string.Empty,
            options["MessageAlignment"] is DialogMessageAlignment messageAlignment ? messageAlignment : null,
            options["ButtonLeftText"] is string buttonLeftText ? buttonLeftText : null,
            options["ButtonMiddleText"] is string buttonMiddleText ? buttonMiddleText : null,
            options["ButtonRightText"] is string buttonRightText ? buttonRightText : null,
            options["Icon"] is DialogSystemIcon icon ? icon : null,
            options["MinimizeWindows"] is bool minimizeWindows && minimizeWindows)
        {
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
        /// <param name="appTrayIconImage">The path to the application's tray icon image used in the dialog. If <see langword="null"/>,
        /// the default tray icon is used.</param>
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
        protected CustomDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTrayIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogSystemIcon? icon, bool minimizeWindows) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTrayIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            if (string.IsNullOrWhiteSpace(messageText))
            {
                throw new ArgumentNullException(nameof(messageText), "MessageText value is null or invalid.");
            }

            // At least one button must be defined.
            if (string.IsNullOrWhiteSpace(buttonLeftText) && string.IsNullOrWhiteSpace(buttonMiddleText) && string.IsNullOrWhiteSpace(buttonRightText))
            {
                throw new ArgumentException("At least one button must be defined.");
            }

            MessageText = messageText;
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
        public string MessageText { get; }

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [JsonProperty]
        public DialogMessageAlignment? MessageAlignment { get; }

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        [JsonProperty]
        public string? ButtonLeftText { get; }

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        [JsonProperty]
        public string? ButtonMiddleText { get; }

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        [JsonProperty]
        public string? ButtonRightText { get; }

        /// <summary>
        /// The icon to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public DialogSystemIcon? Icon { get; }

        /// <summary>
        /// Gets a value indicating whether windows should be minimized.
        /// </summary>
        [JsonProperty]
        public bool MinimizeWindows { get; }
    }
}
