using System;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CustomDialog.
    /// </summary>
    [DataContract]
    public record CustomDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CustomDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CustomDialogOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] as string ?? null!,
            options["Subtitle"] as string ?? null!,
            options["AppIconImage"] as string ?? null!,
            options["AppIconDarkImage"] as string ?? null!,
            options["AppBannerImage"] as string ?? null!,
            options["AppTaskbarIconImage"] as string,
            options["DialogTopMost"] as bool? ?? false,
            options["Language"] as CultureInfo ?? null!,
            options["FluentAccentColor"] as int?,
            options["DialogPosition"] as DialogPosition?,
            options["DialogAllowMove"] as bool?,
            options["DialogExpiryDuration"] as TimeSpan?,
            options["DialogPersistInterval"] as TimeSpan?,
            options["MessageText"] as string ?? null!,
            options["MessageAlignment"] as DialogMessageAlignment?,
            options["ButtonLeftText"] as string,
            options["ButtonMiddleText"] as string,
            options["ButtonRightText"] as string,
            options["Icon"] as DialogSystemIcon?,
            options["MinimizeWindows"] as bool? ?? false)
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
        /// <param name="appTaskbarIconImage">The path to the application's tray icon image used in the dialog. If <see langword="null"/>,
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
        protected CustomDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogSystemIcon? icon, bool minimizeWindows) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
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
        [DataMember]
        public string MessageText { get; private set; }

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public DialogMessageAlignment? MessageAlignment { get; private set; }

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        [DataMember]
        public string? ButtonLeftText { get; private set; }

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        [DataMember]
        public string? ButtonMiddleText { get; private set; }

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        [DataMember]
        public string? ButtonRightText { get; private set; }

        /// <summary>
        /// The icon to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public DialogSystemIcon? Icon { get; private set; }

        /// <summary>
        /// Gets a value indicating whether windows should be minimized.
        /// </summary>
        [DataMember]
        public bool MinimizeWindows { get; private set; }
    }
}
