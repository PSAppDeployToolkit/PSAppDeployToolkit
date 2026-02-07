using System;
using System.Collections;
using System.Globalization;
using PSAppDeployToolkit.Foundation;
using System.Text.Json.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    public sealed record RestartDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class.
        /// </summary>
        /// <param name="deploymentType"></param>
        /// <param name="options"></param>
        public RestartDialogOptions(DeploymentType deploymentType, Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] is string appTitle ? appTitle : string.Empty,
            options["Subtitle"] is string subtitle ? subtitle : string.Empty,
            options["AppIconImage"] is string appIconImage ? appIconImage : string.Empty,
            options["AppIconDarkImage"] is string appIconDarkImage ? appIconDarkImage : string.Empty,
            options["AppBannerImage"] is string appBannerImage ? appBannerImage : string.Empty,
            options["AppTaskbarIconImage"] is string appTaskbarIconImage ? appTaskbarIconImage : null,
            options["DialogTopMost"] is bool dialogTopMost && dialogTopMost,
            options["Language"] is CultureInfo language ? language : null!,
            options["FluentAccentColor"] is int fluentAccentColor ? fluentAccentColor : null,
            options["DialogPosition"] is DialogPosition dialogPosition ? dialogPosition : null,
            options["DialogAllowMove"] is bool dialogAllowMove ? dialogAllowMove : null,
            options["DialogExpiryDuration"] is TimeSpan dialogExpiryDuration ? dialogExpiryDuration : null,
            options["DialogPersistInterval"] is TimeSpan dialogPersistInterval ? dialogPersistInterval : null,
            options["Strings"] is Hashtable strings && strings.Count > 0 ? new RestartDialogStrings(strings, deploymentType) : null!,
            options["CountdownDuration"] is TimeSpan countdownDuration ? countdownDuration : null,
            options["CountdownNoMinimizeDuration"] is TimeSpan countdownNoMinimizeDuration ? countdownNoMinimizeDuration : null,
            options["CustomMessageText"] is string customMessageText ? customMessageText : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class with the specified configuration
        /// options.
        /// </summary>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle displayed in the dialog, providing additional context.</param>
        /// <param name="appIconImage">The path or URI to the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path or URI to the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path or URI to the banner image displayed in the dialog.</param>
        /// <param name="appTaskbarIconImage">The path or URI to the application's tray icon image used in the dialog. If <see langword="null"/>, no tray icon is used.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information used for localizing the dialog.</param>
        /// <param name="fluentAccentColor">The accent color used for Fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, the default interval is
        /// used.</param>
        /// <param name="strings">The localized strings used for dialog text and labels. Cannot be <see langword="null"/>.</param>
        /// <param name="countdownDuration">The duration of the countdown timer displayed in the dialog. If <see langword="null"/>, no countdown timer
        /// is displayed.</param>
        /// <param name="countdownNoMinimizeDuration">The duration during which the countdown timer cannot be minimized. If <see langword="null"/>, the default
        /// behavior is used.</param>
        /// <param name="customMessageText">Custom text displayed in the dialog. If <see langword="null"/>, no custom message is displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strings"/> is <see langword="null"/>.</exception>
        [JsonConstructor]
        private RestartDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, RestartDialogStrings strings, TimeSpan? countdownDuration, TimeSpan? countdownNoMinimizeDuration, string? customMessageText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            Strings = strings ?? throw new ArgumentNullException(nameof(strings), "Strings value is null or invalid.");
            CountdownDuration = countdownDuration;
            CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
            CustomMessageText = customMessageText;
        }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        public RestartDialogStrings Strings { get; }

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        public TimeSpan? CountdownDuration { get; }

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        public TimeSpan? CountdownNoMinimizeDuration { get; }

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        public string? CustomMessageText { get; }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        public sealed record RestartDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RestartDialogStrings"/> class with the specified strings.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            internal RestartDialogStrings(Hashtable strings, DeploymentType deploymentType) : this(
                strings["Title"] is string title ? title : string.Empty,
                strings["Message"] is Hashtable messageTable && messageTable[deploymentType.ToString()] is string message ? message : string.Empty,
                strings["MessageTime"] is string messageTime ? messageTime : string.Empty,
                strings["MessageRestart"] is string messageRestart ? messageRestart : string.Empty,
                strings["TimeRemaining"] is string timeRemaining ? timeRemaining : string.Empty,
                strings["ButtonRestartNow"] is string buttonRestartNow ? buttonRestartNow : string.Empty,
                strings["ButtonRestartLater"] is string buttonRestartLater ? buttonRestartLater : string.Empty)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="RestartDialogStrings"/> class with the specified dialog
            /// strings.
            /// </summary>
            /// <remarks>This constructor is marked as private and is intended for use with JSON
            /// deserialization. It ensures that all required dialog strings are provided and valid.</remarks>
            /// <param name="title">The title of the restart dialog. Cannot be <see langword="null"/>.</param>
            /// <param name="message">The main message displayed in the restart dialog. Cannot be <see langword="null"/>.</param>
            /// <param name="messageTime">The message indicating the time remaining before the restart. Cannot be <see langword="null"/>.</param>
            /// <param name="messageRestart">The message displayed when the restart is imminent. Cannot be <see langword="null"/>.</param>
            /// <param name="timeRemaining">The string representing the remaining time before the restart. Cannot be <see langword="null"/>.</param>
            /// <param name="buttonRestartNow">The label for the "Restart Now" button. Cannot be <see langword="null"/>.</param>
            /// <param name="buttonRestartLater">The label for the "Restart Later" button. Cannot be <see langword="null"/>.</param>
            /// <exception cref="ArgumentNullException">Thrown if any of the parameters are <see langword="null"/>.</exception>
            [JsonConstructor]
            private RestartDialogStrings(string title, string message, string messageTime, string messageRestart, string timeRemaining, string buttonRestartNow, string buttonRestartLater)
            {
                if (string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentNullException(nameof(title), "Title value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(message))
                {
                    throw new ArgumentNullException(nameof(message), "Message value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(messageTime))
                {
                    throw new ArgumentNullException(nameof(messageTime), "MessageTime value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(messageRestart))
                {
                    throw new ArgumentNullException(nameof(messageRestart), "MessageRestart value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(timeRemaining))
                {
                    throw new ArgumentNullException(nameof(timeRemaining), "TimeRemaining value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(buttonRestartNow))
                {
                    throw new ArgumentNullException(nameof(buttonRestartNow), "ButtonRestartNow value is null or invalid.");
                }
                if (string.IsNullOrWhiteSpace(buttonRestartLater))
                {
                    throw new ArgumentNullException(nameof(buttonRestartLater), "ButtonRestartLater value is null or invalid.");
                }

                Title = title;
                Message = message;
                MessageTime = messageTime;
                MessageRestart = messageRestart;
                TimeRemaining = timeRemaining;
                ButtonRestartNow = buttonRestartNow;
                ButtonRestartLater = buttonRestartLater;
            }

            /// <summary>
            /// Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
            /// </summary>
            public string Title { get; }

            /// <summary>
            /// Text displayed when the device requires a restart.
            /// </summary>
            public string Message { get; }

            /// <summary>
            /// Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
            /// </summary>
            public string MessageTime { get; }

            /// <summary>
            /// Text displayed when indicating when the device will be restarted.
            /// </summary>
            public string MessageRestart { get; }

            /// <summary>
            /// Text displayed to indicate the amount of time remaining until a restart will occur.
            /// </summary>
            public string TimeRemaining { get; }

            /// <summary>
            /// Button text for when wanting to restart the device now.
            /// </summary>
            public string ButtonRestartNow { get; }

            /// <summary>
            /// Button text for allowing the user to restart later.
            /// </summary>
            public string ButtonRestartLater { get; }
        }
    }
}
