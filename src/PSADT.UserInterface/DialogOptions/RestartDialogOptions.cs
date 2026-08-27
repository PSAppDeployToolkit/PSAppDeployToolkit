using System;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using PSAppDeployToolkit.Foundation;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    [DataContract]
    public sealed record class RestartDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the RestartDialogOptions class using the specified deployment type and a
        /// dictionary of configuration options.
        /// </summary>
        /// <remarks>The options dictionary must contain the required keys for proper dialog
        /// configuration. Missing or incorrectly typed values may result in runtime exceptions. Ensure that all
        /// necessary options, such as 'AppTitle', 'Subtitle', and other dialog settings, are provided and of the
        /// expected type.</remarks>
        /// <param name="deploymentType">The deployment type that determines the context in which the restart dialog is presented.</param>
        /// <param name="options">A dictionary containing configuration options for the dialog, such as titles, images, language, and behavior
        /// settings. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        public RestartDialogOptions(DeploymentType deploymentType, IDictionary options) : this(
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
            new((IDictionary?)options["Strings"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Strings' is missing."), deploymentType),
            (TimeSpan?)options["CountdownDuration"],
            (TimeSpan?)options["CountdownNoMinimizeDuration"],
            (string?)options["ShutdownReasonText"],
            (string?)options["CustomMessageText"],
            (bool?)options["DialogAllowCancel"])
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
        /// <param name="fluentAccentColorDark">The accent color used for Fluent design elements in the dialog when in dark mode. If <see langword="null"/>, the default dark accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogAllowMinimize">Indicates whether the dialog exposes a minimize button in its caption area. If <see langword="null"/> or
        /// <see langword="false"/>, the minimize button remains hidden.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, the default interval is
        /// used.</param>
        /// <param name="strings">The localized strings used for dialog text and labels. Cannot be <see langword="null"/>.</param>
        /// <param name="countdownDuration">The duration of the countdown timer displayed in the dialog. If <see langword="null"/>, no countdown timer
        /// is displayed.</param>
        /// <param name="countdownNoMinimizeDuration">The duration during which the countdown timer cannot be minimized. If <see langword="null"/>, the default
        /// behavior is used.</param>
        /// <param name="shutdownReasonText">Represents the reason for shutdown, which can be optionally provided to give users more context about why a restart is necessary. If provided, this text can be displayed in the dialog to inform users about the specific reason for the restart, such as "System updates require a restart" or "A critical error occurred that requires a restart". If <see langword="null"/>, no specific shutdown reason is displayed.</param>
        /// <param name="customMessageText">Custom text displayed in the dialog. If <see langword="null"/>, no custom message is displayed.</param>
        /// <param name="allowCancel">Indicates whether the dialog displays a Cancel button that closes the prompt without restarting. If <see langword="null"/> or <see langword="false"/>, no Cancel button is shown.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="strings"/> is <see langword="null"/>.</exception>
        private RestartDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, RestartDialogStrings strings, TimeSpan? countdownDuration, TimeSpan? countdownNoMinimizeDuration, string? shutdownReasonText, string? customMessageText, bool? allowCancel) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval)
        {
            if (customMessageText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(customMessageText);
            }
            ArgumentNullException.ThrowIfNull(strings);
            Strings = strings;
            CountdownDuration = countdownDuration;
            CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
            ShutdownReasonText = shutdownReasonText;
            CustomMessageText = customMessageText;
            AllowCancel = allowCancel ?? false;
        }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [DataMember]
        public readonly RestartDialogStrings Strings;

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? CountdownNoMinimizeDuration;

        /// <summary>
        /// Represents the reason for shutdown, which can be optionally provided to give users more context about why a restart is necessary. If provided, this text can be displayed in the dialog to inform users about the specific reason for the restart, such as "System updates require a restart" or "A critical error occurred that requires a restart". If <see langword="null"/>, no specific shutdown reason is displayed.
        /// </summary>
        [DataMember]
        public readonly string? ShutdownReasonText;

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [DataMember]
        public readonly string? CustomMessageText;

        /// <summary>
        /// Indicates whether the dialog displays a Cancel button that closes the prompt without restarting.
        /// </summary>
        [DataMember]
        public readonly bool AllowCancel;

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [DataContract]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        public sealed record class RestartDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the RestartDialogStrings class using the specified string resources and
            /// deployment type.
            /// </summary>
            /// <remarks>This constructor selects the appropriate localized message for the dialog
            /// based on the provided deployment type. All required string resources must be present in the dictionary
            /// to ensure correct dialog display.</remarks>
            /// <param name="strings">An IDictionary containing the string resources required for the restart dialog, such as titles,
            /// messages, and button labels. Keys must match the expected resource names.</param>
            /// <param name="deploymentType">The deployment type that determines which localized message string to use in the dialog.</param>
            internal RestartDialogStrings(IDictionary strings, DeploymentType deploymentType) : this(
                (string?)(strings ?? throw new ArgumentNullException(nameof(strings)))["Title"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'Title' is missing."),
                (string?)((IDictionary?)strings["Message"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'Message' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'Message.{deploymentType}' is missing."),
                (string?)strings["MessageTime"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'MessageTime' is missing."),
                (string?)strings["MessageRestart"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'MessageRestart' is missing."),
                (string?)strings["TimeRemaining"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'TimeRemaining' is missing."),
                (string?)strings["ButtonRestartNow"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonRestartNow' is missing."),
                (string?)strings["ButtonRestartLater"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonRestartLater' is missing."),
                (string?)strings["ButtonCancel"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonCancel' is missing."))
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
            /// <param name="buttonCancel">The label for the "Cancel" button. Cannot be <see langword="null"/>.</param>
            /// <exception cref="ArgumentNullException">Thrown if any of the parameters are <see langword="null"/>.</exception>
            private RestartDialogStrings(string title, string message, string messageTime, string messageRestart, string timeRemaining, string buttonRestartNow, string buttonRestartLater, string buttonCancel)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(title);
                ArgumentException.ThrowIfNullOrWhiteSpace(message);
                ArgumentException.ThrowIfNullOrWhiteSpace(messageTime);
                ArgumentException.ThrowIfNullOrWhiteSpace(messageRestart);
                ArgumentException.ThrowIfNullOrWhiteSpace(timeRemaining);
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonRestartNow);
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonRestartLater);
                ArgumentException.ThrowIfNullOrWhiteSpace(buttonCancel);
                Title = title;
                Message = message;
                MessageTime = messageTime;
                MessageRestart = messageRestart;
                TimeRemaining = timeRemaining;
                ButtonRestartNow = buttonRestartNow;
                ButtonRestartLater = buttonRestartLater;
                ButtonCancel = buttonCancel;
            }

            /// <summary>
            /// Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
            /// </summary>
            [DataMember]
            public readonly string Title;

            /// <summary>
            /// Text displayed when the device requires a restart.
            /// </summary>
            [DataMember]
            public readonly string Message;

            /// <summary>
            /// Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
            /// </summary>
            [DataMember]
            public readonly string MessageTime;

            /// <summary>
            /// Text displayed when indicating when the device will be restarted.
            /// </summary>
            [DataMember]
            public readonly string MessageRestart;

            /// <summary>
            /// Text displayed to indicate the amount of time remaining until a restart will occur.
            /// </summary>
            [DataMember]
            public readonly string TimeRemaining;

            /// <summary>
            /// Button text for when wanting to restart the device now.
            /// </summary>
            [DataMember]
            public readonly string ButtonRestartNow;

            /// <summary>
            /// Button text for allowing the user to restart later.
            /// </summary>
            [DataMember]
            public readonly string ButtonRestartLater;

            /// <summary>
            /// Button text for cancelling the pending restart.
            /// </summary>
            [DataMember]
            public readonly string ButtonCancel;
        }
    }
}
