using System;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    [DataContract]
    public sealed record class ProgressDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the ProgressDialogOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>If a required key is missing from the options dictionary, the corresponding property
        /// is set to its default value. Ensure that the dictionary contains valid entries for all necessary
        /// configuration keys to achieve the desired dialog behavior.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that define the configuration for the progress dialog, such as
        /// application title, subtitle, icon images, dialog properties, and progress message details.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        public ProgressDialogOptions(IDictionary options) : this(
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
            (string?)options["ProgressMessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'ProgressMessageText' is missing."),
            (string?)options["ProgressDetailMessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'ProgressDetailMessageText' is missing."),
            (double?)options["ProgressPercentage"],
            (DialogMessageAlignment?)options["MessageAlignment"])
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class with specified options for
        /// configuring a progress dialog.
        /// </summary>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle displayed below the application title in the dialog.</param>
        /// <param name="appIconImage">The path or identifier for the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path or identifier for the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path or identifier for the banner image displayed in the dialog.</param>
        /// <param name="appTaskbarIconImage">The path or identifier for the application's tray icon image used in the dialog. If <see langword="null"/>,
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
        /// <param name="dialogExpiryDuration">The duration after which the dialog automatically expires. If <see langword="null"/>, the dialog does not
        /// expire automatically.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, the default interval is
        /// used.</param>
        /// <param name="progressMessageText">The main progress message text displayed in the dialog. Cannot be <see langword="null"/>.</param>
        /// <param name="progressDetailMessageText">The detailed progress message text displayed in the dialog. Cannot be <see langword="null"/>.</param>
        /// <param name="progressPercentage">The percentage of progress completed, represented as a value between 0 and 100. If <see langword="null"/>,
        /// progress is not displayed.</param>
        /// <param name="messageAlignment">The alignment of the progress messages within the dialog. If <see langword="null"/>, the default alignment
        /// is used.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="progressMessageText"/> or <paramref name="progressDetailMessageText"/> is <see
        /// langword="null"/>.</exception>
        private ProgressDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string progressMessageText, string progressDetailMessageText, double? progressPercentage, DialogMessageAlignment? messageAlignment) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(progressMessageText);
            ArgumentException.ThrowIfNullOrWhiteSpace(progressDetailMessageText);
            ProgressMessageText = progressMessageText;
            ProgressDetailMessageText = progressDetailMessageText;
            ProgressPercentage = progressPercentage;
            MessageAlignment = messageAlignment;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        [DataMember]
        public readonly string ProgressMessageText;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        [DataMember]
        public readonly string ProgressDetailMessageText;

        /// <summary>
        /// The percentage value to be displayed on the status bar, if available.
        /// </summary>
        [DataMember]
        public readonly double? ProgressPercentage;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public readonly DialogMessageAlignment? MessageAlignment;
    }
}
