using System;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the InputDialog.
    /// </summary>
    [DataContract]
    public sealed record class InputDialogOptions : CustomDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the InputDialogOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>If a required key is missing from the options dictionary, the corresponding property
        /// will be set to its default value. Ensure that the dictionary contains valid entries for all necessary
        /// configuration keys to achieve the desired dialog behavior.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that define the configuration settings for the input dialog.
        /// Expected keys include application title, subtitle, icon images, dialog properties, message settings, and
        /// button text. Each key should correspond to the appropriate value type required by the dialog.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options parameter is null.</exception>
        public InputDialogOptions(IDictionary options) : this(
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
            (bool?)options["MinimizeWindows"] ?? false,
            (string?)options["InitialInputText"],
            (bool?)options["SecureInput"] ?? false)
        {
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
        /// <param name="appTaskbarIconImage">The path to the application's tray icon image used in the dialog. If <see langword="null"/>,
        /// the default tray icon is used.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information used for localizing the dialog.</param>
        /// <param name="fluentAccentColor">The accent color used for Fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="fluentAccentColorDark">The accent color used for Fluent design elements in the dialog when in dark mode. If <see langword="null"/>, the default dark accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogAllowMinimize">Indicates whether the dialog exposes a minimize button in its caption area. If <see langword="null"/> or
        /// <see langword="false"/>, the minimize button remains hidden.</param>
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
        /// <param name="defaultButton">Indicates which button is the default button in the dialog. If <see langword="null"/>, no default button is set.</param>
        /// <param name="icon">The system icon displayed in the dialog. If <see langword="null"/>, no icon is displayed.</param>
        /// <param name="minimizeWindows">A value indicating whether all other windows should be minimized when the dialog is displayed.</param>
        /// <param name="initialInputText">The initial text displayed in the input field of the dialog. If <see langword="null"/>, the input field is
        /// empty.</param>
        /// <param name="secureInput">A value indicating whether the input should be masked (for passwords or sensitive data).</param>
        private InputDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string messageText, DialogMessageAlignment? messageAlignment, string? buttonLeftText, string? buttonMiddleText, string? buttonRightText, DialogDefaultButton? defaultButton, DialogSystemIcon? icon, bool minimizeWindows, string? initialInputText, bool secureInput) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval, messageText, messageAlignment, buttonLeftText, buttonMiddleText, buttonRightText, defaultButton, icon, minimizeWindows)
        {
            if (initialInputText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(initialInputText);
            }
            InitialInputText = initialInputText;
            SecureInput = secureInput;
        }

        /// <summary>
        /// The initial text to be displayed in the input field.
        /// </summary>
        [DataMember]
        public readonly string? InitialInputText;

        /// <summary>
        /// Indicates whether the input should be masked (for passwords or sensitive data).
        /// </summary>
        [DataMember]
        public readonly bool SecureInput;
    }
}
