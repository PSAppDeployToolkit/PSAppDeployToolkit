using System;
using System.Collections;
using System.Globalization;
using PSADT.Core;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    public sealed record RestartDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class.
        /// </summary>
        /// <param name="deploymentType"></param>
        /// <param name="options"></param>
        public RestartDialogOptions(DeploymentType deploymentType, Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["Strings"] is not Hashtable strings || strings.Count == 0)
            {
                throw new ArgumentNullException("Strings value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("CountdownDuration"))
            {
                if (options["CountdownDuration"] is not TimeSpan countdownDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownDuration value is not valid.", (Exception?)null);
                }
                CountdownDuration = countdownDuration;
            }
            if (options.ContainsKey("CountdownNoMinimizeDuration"))
            {
                if (options["CountdownNoMinimizeDuration"] is not TimeSpan countdownNoMinimizeDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownNoMinimizeDuration value is not valid.", (Exception?)null);
                }
                CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
            }
            if (options.ContainsKey("CustomMessageText"))
            {
                if (options["CustomMessageText"] is not string customMessageText || string.IsNullOrWhiteSpace(customMessageText))
                {
                    throw new ArgumentOutOfRangeException("CustomMessageText value is not valid.", (Exception?)null);
                }
                CustomMessageText = customMessageText;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Strings = new(strings, deploymentType);
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
        private RestartDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, RestartDialogStrings strings, TimeSpan? countdownDuration, TimeSpan? countdownNoMinimizeDuration, string? customMessageText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            // Nothing here is allowed to be null.
            Strings = strings ?? throw new ArgumentNullException(nameof(strings), "Strings value is null or invalid.");
            CountdownDuration = countdownDuration;
            CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
            CustomMessageText = customMessageText;
        }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        [JsonProperty]
        public RestartDialogStrings Strings { get; }

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        [JsonProperty]
        public TimeSpan? CountdownDuration { get; }

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        [JsonProperty]
        public TimeSpan? CountdownNoMinimizeDuration { get; }

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [JsonProperty]
        public string? CustomMessageText { get; }

        /// <summary>
        /// The strings used for the RestartDialog.
        /// </summary>
        public sealed record RestartDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="RestartDialogStrings"/> class with the specified strings.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            internal RestartDialogStrings(Hashtable strings, DeploymentType deploymentType)
            {
                // Nothing here is allowed to be null.
                if (strings["Title"] is not string title || string.IsNullOrWhiteSpace(title))
                {
                    throw new ArgumentNullException("Title value is null or invalid.", (Exception?)null);
                }
                if (strings["Message"] is not Hashtable messageTable || messageTable[deploymentType.ToString()] is not string message || string.IsNullOrWhiteSpace(message))
                {
                    throw new ArgumentNullException("Message value is null or invalid.", (Exception?)null);
                }
                if (strings["MessageTime"] is not string messageTime || string.IsNullOrWhiteSpace(messageTime))
                {
                    throw new ArgumentNullException("MessageTime value is null or invalid.", (Exception?)null);
                }
                if (strings["MessageRestart"] is not string messageRestart || string.IsNullOrWhiteSpace(messageRestart))
                {
                    throw new ArgumentNullException("MessageRestart value is null or invalid.", (Exception?)null);
                }
                if (strings["TimeRemaining"] is not string timeRemaining || string.IsNullOrWhiteSpace(timeRemaining))
                {
                    throw new ArgumentNullException("TimeRemaining value is null or invalid.", (Exception?)null);
                }
                if (strings["ButtonRestartNow"] is not string buttonRestartNow || string.IsNullOrWhiteSpace(buttonRestartNow))
                {
                    throw new ArgumentNullException("ButtonRestartNow value is null or invalid.", (Exception?)null);
                }
                if (strings["ButtonRestartLater"] is not string buttonRestartLater || string.IsNullOrWhiteSpace(buttonRestartLater))
                {
                    throw new ArgumentNullException("ButtonRestartLater value is null or invalid.", (Exception?)null);
                }

                // The hashtable was correctly defined, assign the remaining values.
                Title = title;
                Message = message;
                MessageTime = messageTime;
                MessageRestart = messageRestart;
                TimeRemaining = timeRemaining;
                ButtonRestartNow = buttonRestartNow;
                ButtonRestartLater = buttonRestartLater;
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
                Title = title ?? throw new ArgumentNullException(nameof(title), "Title value is null or invalid.");
                Message = message ?? throw new ArgumentNullException(nameof(message), "Message value is null or invalid.");
                MessageTime = messageTime ?? throw new ArgumentNullException(nameof(messageTime), "MessageTime value is null or invalid.");
                MessageRestart = messageRestart ?? throw new ArgumentNullException(nameof(messageRestart), "MessageRestart value is null or invalid.");
                TimeRemaining = timeRemaining ?? throw new ArgumentNullException(nameof(timeRemaining), "TimeRemaining value is null or invalid.");
                ButtonRestartNow = buttonRestartNow ?? throw new ArgumentNullException(nameof(buttonRestartNow), "ButtonRestartNow value is null or invalid.");
                ButtonRestartLater = buttonRestartLater ?? throw new ArgumentNullException(nameof(buttonRestartLater), "ButtonRestartLater value is null or invalid.");
            }

            /// <summary>
            /// Text displayed in the title of the restart prompt which helps the script identify whether there is already a restart prompt being displayed and not to duplicate it.
            /// </summary>
            [JsonProperty]
            public string Title { get; }

            /// <summary>
            /// Text displayed when the device requires a restart.
            /// </summary>
            [JsonProperty]
            public string Message { get; }

            /// <summary>
            /// Text displayed as a prefix to the time remaining, indicating that users should save their work, etc.
            /// </summary>
            [JsonProperty]
            public string MessageTime { get; }

            /// <summary>
            /// Text displayed when indicating when the device will be restarted.
            /// </summary>
            [JsonProperty]
            public string MessageRestart { get; }

            /// <summary>
            /// Text displayed to indicate the amount of time remaining until a restart will occur.
            /// </summary>
            [JsonProperty]
            public string TimeRemaining { get; }

            /// <summary>
            /// Button text for when wanting to restart the device now.
            /// </summary>
            [JsonProperty]
            public string ButtonRestartNow { get; }

            /// <summary>
            /// Button text for allowing the user to restart later.
            /// </summary>
            [JsonProperty]
            public string ButtonRestartLater { get; }
        }
    }
}
