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
    [KnownType(typeof(InputDialogOptions))]
    [KnownType(typeof(ListSelectionDialogOptions))]
    public record CustomDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the CustomDialogOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>If a required key is missing from the options dictionary, a default value is used for
        /// that option. Ensure that the dictionary contains valid entries for all necessary configuration keys to
        /// achieve the desired dialog behavior.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that define the dialog's configuration, such as titles, images,
        /// language, button text, and behavior settings. Keys must match the expected option names; values should be of
        /// the appropriate type for each option.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        public CustomDialogOptions(IDictionary options) : this(
            (string?)(options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppTitle' is missing."),
            (string?)options["Subtitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Subtitle' is missing."),
            (string?)options["AppIconImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppIconImage' is missing."),
            (string?)options["AppIconDarkImage"],
            (string?)options["AppBannerImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppBannerImage' is missing."),
            (string?)options["AppTaskbarIconImage"],
            (bool?)options["DialogTopMost"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogTopMost' is missing."),
            (CultureInfo?)options["Language"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Language' is missing."),
            (int?)options["FluentAccentColor"],
            (int?)options["FluentAccentColorDark"],
            (DialogPosition?)options["DialogPosition"],
            (bool?)options["DialogAllowMove"],
            (bool?)options["DialogAllowMinimize"],
            (TimeSpan?)options["DialogExpiryDuration"],
            (TimeSpan?)options["DialogPersistInterval"],
            (string?)options["MessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'MessageText' is missing."),
            (DialogMessageAlignment?)options["MessageAlignment"],
            (string?)options["ButtonLeftText"],
            (string?)options["ButtonMiddleText"],
            (string?)options["ButtonRightText"],
            (DialogDefaultButton?)options["DefaultButton"],
            (DialogSystemIcon?)options["Icon"],
            (bool?)options["MinimizeWindows"] ?? false)
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
        /// <param name="fluentAccentColorDark">The accent color used for fluent design elements in the dialog when in dark mode. If <see langword="null"/>, the default dark accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogAllowMinimize">Indicates whether the dialog exposes a minimize button in its caption area. If <see langword="null"/> or
        /// <see langword="false"/>, the minimize button remains hidden.</param>
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
        /// <param name="defaultButton">Indicates which button is the default button in the dialog. If <see langword="null"/>, no default button is set.</param>
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">Indicates whether all other windows should be minimized when the dialog is displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="messageText"/> is <see langword="null"/> or empty.</exception>
        private protected CustomDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogDefaultButton? defaultButton, DialogSystemIcon? icon, bool minimizeWindows) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval)
        {
            // At least one button must be defined.
            if (buttonLeftText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonLeftText);
            }
            if (buttonMiddleText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonMiddleText);
            }
            if (buttonRightText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonRightText);
            }
            if (buttonLeftText is null && buttonMiddleText is null && buttonRightText is null)
            {
                throw new NotSupportedException("At least one button must be defined.");
            }

            // Assign remaining properties.
            ArgumentException.ThrowIfNullOrWhiteSpace(messageText);
            MessageText = messageText;
            MessageAlignment = messageAlignment;
            ButtonLeftText = buttonLeftText;
            ButtonMiddleText = buttonMiddleText;
            ButtonRightText = buttonRightText;
            DefaultButton = defaultButton;
            Icon = icon;
            MinimizeWindows = minimizeWindows;
        }

        /// <summary>
        /// The custom message to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string MessageText;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogMessageAlignment? MessageAlignment;

        /// <summary>
        /// The text for the left button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonLeftText;

        /// <summary>
        /// The text for the middle button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonMiddleText;

        /// <summary>
        /// The text for the right button in the dialog.
        /// </summary>
        [DataMember]
        public readonly string? ButtonRightText;

        /// <summary>
        /// Indicates which button is the default button in the dialog, if any. If <see langword="null"/>, no default button is set.
        /// </summary>
        [DataMember]
        public readonly DialogDefaultButton? DefaultButton;

        /// <summary>
        /// The icon to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogSystemIcon? Icon;

        /// <summary>
        /// Gets a value indicating whether windows should be minimized.
        /// </summary>
        [DataMember]
        public readonly bool MinimizeWindows;
    }
}
