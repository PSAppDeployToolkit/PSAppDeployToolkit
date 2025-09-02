using System;
using System.Collections;
using System.Globalization;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the ProgressDialog.
    /// </summary>
    public sealed record ProgressDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ProgressDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public ProgressDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["ProgressMessageText"] is not string progressMessageText || string.IsNullOrWhiteSpace(progressMessageText))
            {
                throw new ArgumentNullException("ProgressMessageText value is null or invalid.", (Exception?)null);
            }
            if (options["ProgressDetailMessageText"] is not string progressDetailMessageText || string.IsNullOrWhiteSpace(progressDetailMessageText))
            {
                throw new ArgumentNullException("ProgressDetailMessageText value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("ProgressPercentage"))
            {
                if (options["ProgressPercentage"] is not double progressPercentage)
                {
                    throw new ArgumentOutOfRangeException("ProgressPercentage value is not valid.", (Exception?)null);
                }
                ProgressPercentage = progressPercentage;
            }
            if (options.ContainsKey("MessageAlignment"))
            {
                if (options["MessageAlignment"] is not DialogMessageAlignment messageAlignment)
                {
                    throw new ArgumentOutOfRangeException("MessageAlignment value is not valid.", (Exception?)null);
                }
                MessageAlignment = messageAlignment;
            }

            // The hashtable was correctly defined, assign the remaining values.
            ProgressMessageText = progressMessageText;
            ProgressDetailMessageText = progressDetailMessageText;
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
        [JsonConstructor]
        private ProgressDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, string progressMessageText, string progressDetailMessageText, double? progressPercentage, DialogMessageAlignment? messageAlignment) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            ProgressMessageText = progressMessageText ?? throw new ArgumentNullException(nameof(progressMessageText));
            ProgressDetailMessageText = progressDetailMessageText ?? throw new ArgumentNullException(nameof(progressDetailMessageText));
            ProgressPercentage = progressPercentage;
            MessageAlignment = messageAlignment;
        }

        /// <summary>
        /// The message to be displayed in the progress dialog, indicating the current status or action being performed.
        /// </summary>
        [JsonProperty]
        public readonly string ProgressMessageText;

        /// <summary>
        /// The detailed message to be displayed in the progress dialog, providing more context or information about the current action.
        /// </summary>
        [JsonProperty]
        public readonly string ProgressDetailMessageText;

        /// <summary>
        /// The percentage value to be displayed on the status bar, if available.
        /// </summary>
        [JsonProperty]
        public readonly double? ProgressPercentage;

        /// <summary>
        /// The alignment of the message text in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly DialogMessageAlignment? MessageAlignment;
    }
}
