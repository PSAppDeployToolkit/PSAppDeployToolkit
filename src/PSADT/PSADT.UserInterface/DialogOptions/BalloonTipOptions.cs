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
    /// cref="BalloonTipOptions(IDictionary)"/> constructor to initialize an instance with validated configuration
    /// values.</remarks>
    [DataContract]
    public sealed record BalloonTipOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the BalloonTipOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>Values are retrieved from the provided options dictionary. If a key is missing, a
        /// default value is used: 'Icon' defaults to an invalid value.</remarks>
        /// <param name="options">An IDictionary containing configuration values for the balloon tip. Expected keys include 'TrayTitle',
        /// 'TrayIcon', 'Title', 'Text', and 'Icon'.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options parameter is null.</exception>
        public BalloonTipOptions(IDictionary options) : this(
            (string?)(options ?? throw new ArgumentNullException(nameof(options)))["Title"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Title' is missing."),
            (string?)options["Text"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Text' is missing."),
            (BalloonTipIcon?)options["Icon"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Icon' is missing."))
        {
        }

        /// <summary>
        /// Primary constructor that validates and initializes all properties.
        /// </summary>
        /// <remarks>This constructor contains all validation logic and is used by both the IDictionary
        /// constructor and JSON deserialization.</remarks>
        /// <param name="title">The title of the balloon tip notification.</param>
        /// <param name="text">The text content of the balloon tip notification.</param>
        /// <param name="icon">The icon to display in the balloon tip notification.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null, empty, or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified tray icon file does not exist.</exception>
        private BalloonTipOptions(string title, string text, BalloonTipIcon icon)
        {
            // Set initial string properties.
            ArgumentException.ThrowIfNullOrWhiteSpace(title);
            ArgumentException.ThrowIfNullOrWhiteSpace(text);
            Title = title;
            Text = text;
            Icon = icon;
        }

        /// <summary>
        /// Gets the title text displayed in the balloon tip of a notification.
        /// </summary>
        [DataMember]
        public readonly string Title;

        /// <summary>
        /// Gets the text displayed in the balloon tip of a notification.
        /// </summary>
        [DataMember]
        public readonly string Text;

        /// <summary>
        /// Gets the icon displayed in the balloon tip associated with the notification.
        /// </summary>
        [DataMember]
        public readonly BalloonTipIcon Icon;
    }
}
