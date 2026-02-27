using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using PSADT.Interop.Extensions;
using PSADT.Utilities;

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
        /// default value is used: 'BalloonTipIcon' defaults to an invalid value, and 'BalloonTipTime' defaults to the
        /// maximum value for an unsigned integer.</remarks>
        /// <param name="options">An IDictionary containing configuration values for the balloon tip. Expected keys include 'TrayTitle',
        /// 'TrayIcon', 'BalloonTipTitle', 'BalloonTipText', 'BalloonTipIcon', and 'BalloonTipTime'.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options parameter is null.</exception>
        public BalloonTipOptions(IDictionary options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["TrayTitle"] as string ?? null!,
            options["TrayIcon"] as string ?? null!,
            options["BalloonTipTitle"] as string ?? null!,
            options["BalloonTipText"] as string ?? null!,
            options["BalloonTipIcon"] as ToolTipIcon? ?? (ToolTipIcon)(-1),
            options["BalloonTipTime"] as uint? ?? uint.MaxValue)
        {
        }

        /// <summary>
        /// Primary constructor that validates and initializes all properties.
        /// </summary>
        /// <remarks>This constructor contains all validation logic and is used by both the IDictionary
        /// constructor and JSON deserialization.</remarks>
        /// <param name="trayTitle">The title of the system tray application.</param>
        /// <param name="trayIcon">The path to the system tray icon.</param>
        /// <param name="balloonTipTitle">The title of the balloon tip notification.</param>
        /// <param name="balloonTipText">The text content of the balloon tip notification.</param>
        /// <param name="balloonTipIcon">The icon to display in the balloon tip notification.</param>
        /// <param name="balloonTipTime">The duration, in milliseconds, for which the balloon tip is displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown if any required parameter is null, empty, or whitespace.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the specified tray icon file does not exist.</exception>
        private BalloonTipOptions(string trayTitle, string trayIcon, string balloonTipTitle, string balloonTipText, ToolTipIcon balloonTipIcon, uint balloonTipTime)
        {
            // Set initial string properties.
            TrayTitle = trayTitle.ThrowIfNullOrWhiteSpace();
            TrayIcon = trayIcon.ThrowIfNullOrWhiteSpace();
            BalloonTipTitle = balloonTipTitle.ThrowIfNullOrWhiteSpace();
            BalloonTipText = balloonTipText.ThrowIfNullOrWhiteSpace();

            // Confirm remaining parameters are valid.
            if (!(MiscUtilities.GetBase64StringBytes(TrayIcon)?.Length > 0) && !File.Exists(TrayIcon))
            {
                throw new FileNotFoundException($"The specified AppIconImage [{TrayIcon}] cannot be found", TrayIcon);
            }
            if ((int)balloonTipIcon == -1)
            {
                throw new ArgumentNullException(nameof(balloonTipIcon), "BalloonTipIcon value is null or invalid.");
            }
            if (balloonTipTime == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(balloonTipTime), "BalloonTipTime value is null or invalid.");
            }
            BalloonTipIcon = balloonTipIcon;
            BalloonTipTime = balloonTipTime;
        }

        /// <summary>
        /// Represents the title displayed on the tray.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string TrayTitle;

        /// <summary>
        /// Represents the file path or identifier for the tray icon used in the application.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string TrayIcon;

        /// <summary>
        /// Gets the title text displayed in the balloon tip of a notification.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string BalloonTipTitle;

        /// <summary>
        /// Gets the text displayed in the balloon tip of a notification.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string BalloonTipText;

        /// <summary>
        /// Gets the icon displayed in the balloon tip associated with the notification.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly ToolTipIcon BalloonTipIcon;

        /// <summary>
        /// Gets the duration, in milliseconds, that the balloon tip is displayed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly uint BalloonTipTime;
    }
}
