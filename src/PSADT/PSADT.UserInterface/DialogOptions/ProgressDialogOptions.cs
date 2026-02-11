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
    public sealed record ProgressDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialogOptions(Hashtable options) : this(
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
            options["ProgressMessageText"] as string ?? null!,
            options["ProgressDetailMessageText"] as string ?? null!,
            options["ProgressPercentage"] as double?,
            options["MessageAlignment"] as DialogMessageAlignment?)
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
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
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
        private ProgressDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string progressMessageText, string progressDetailMessageText, double? progressPercentage, DialogMessageAlignment? messageAlignment) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            if (string.IsNullOrWhiteSpace(progressMessageText))
            {
                throw new ArgumentNullException(nameof(progressMessageText), "ProgressMessageText value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(progressDetailMessageText))
            {
                throw new ArgumentNullException(nameof(progressDetailMessageText), "ProgressDetailMessageText value is null or invalid.");
            }

            ProgressMessageText = progressMessageText;
            ProgressDetailMessageText = progressDetailMessageText;
            ProgressPercentage = progressPercentage;
            MessageAlignment = messageAlignment;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        [DataMember]
        public string ProgressMessageText { get; private set; }

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        [DataMember]
        public string ProgressDetailMessageText { get; private set; }

        /// <summary>
        /// The percentage value to be displayed on the status bar, if available.
        /// </summary>
        [DataMember]
        public double? ProgressPercentage { get; private set; }

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [DataMember]
        public DialogMessageAlignment? MessageAlignment { get; private set; }
    }
}
