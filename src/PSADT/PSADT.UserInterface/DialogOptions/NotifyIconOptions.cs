using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Represents configuration options for displaying a balloon tip notification in the system tray.
    /// </summary>
    /// <remarks>This type encapsulates the required properties for configuring a balloon tip notification,
    /// including the tray title, tray icon, balloon tip title, text, and icon. Use the <see
    /// cref="NotifyIconOptions(IDictionary)"/> constructor to initialize an instance with validated configuration
    /// values.</remarks>
    [DataContract]
    public sealed record class NotifyIconOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the NotifyIconOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>Values are retrieved from the provided options dictionary. If a key is missing, a
        /// default value is used: 'BalloonTipIcon' defaults to an invalid value.</remarks>
        /// <param name="options">An IDictionary containing configuration values for the balloon tip. Expected keys include 'Text',
        /// 'Icon', 'BalloonTipTitle', 'BalloonTipText', and 'BalloonTipIcon'.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options parameter is null.</exception>
        public NotifyIconOptions(IDictionary options) : this(
            (string?)options["AppTitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppTitle' is missing."),
            (string?)options["AppIconImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppIconImage' is missing."),
            (string?)options["AppTaskbarIconImage"],
            (string?)options["MessageText"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'MessageText' is missing."))
        {
        }

        /// <summary>
        /// Primary constructor that validates and initializes all properties.
        /// </summary>
        /// <remarks>This constructor contains all validation logic and is used by both the IDictionary
        /// constructor and JSON deserialization.</remarks>
        /// <param name="appTitle">The title of the balloon tip notification.</param>
        /// <param name="appIconImage">The path to the system tray icon.</param>
        /// <param name="appTaskbarIconImage">The path to the taskbar icon (optional).</param>
        /// <param name="messageText">The title of the system tray application.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null, empty, or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified tray icon file does not exist.</exception>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "This is a false positive, we're directly consuming the ValueTask.")]
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD002:Avoid problematic synchronous waits", Justification = "Synchronous wait is necessary for constructor initialization.")]
        private NotifyIconOptions(string appTitle, string appIconImage, string? appTaskbarIconImage, string messageText)
        {
            // Set initial string properties.
            ArgumentException.ThrowIfNullOrWhiteSpace(appTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(messageText);
            ArgumentException.ThrowIfNullOrWhiteSpace(appIconImage);
            AppIconImage = BaseDialogOptions.ThrowIfImageIsInvalidAsync(appIconImage, nameof(AppIconImage)).ConfigureAwait(false).GetAwaiter().GetResult();
            MessageText = messageText;
            AppTitle = appTitle;

            // Optional taskbar icon validation.
            if (appTaskbarIconImage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(appTaskbarIconImage);
                AppTaskbarIconImage = BaseDialogOptions.ThrowIfImageIsInvalidAsync(appTaskbarIconImage, nameof(AppTaskbarIconImage)).ConfigureAwait(false).GetAwaiter().GetResult();
            }
        }

        /// <summary>
        /// Represents the title displayed on a toast notification..
        /// </summary>
        [DataMember]
        public readonly string AppTitle;

        /// <summary>
        /// Represents the file path or identifier for the tray icon used in the application.
        /// </summary>
        [DataMember]
        public readonly string AppIconImage;

        /// <summary>
        /// Represents the file path or identifier for the tray icon used in the application.
        /// </summary>
        [DataMember]
        public readonly string? AppTaskbarIconImage;

        /// <summary>
        /// Represents the title displayed on the tray.
        /// </summary>
        [DataMember]
        public readonly string MessageText;
    }
}
