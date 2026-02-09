using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using System.Windows.Forms;
using PSADT.Utilities;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Represents configuration options for displaying a balloon tip notification in the system tray.
    /// </summary>
    /// <remarks>This type encapsulates the required properties for configuring a balloon tip notification,
    /// including the tray title, tray icon, balloon tip title, text, and icon. Use the <see
    /// cref="BalloonTipOptions(Hashtable)"/> constructor to initialize an instance with validated configuration
    /// values.</remarks>
    [DataContract]
    public sealed record BalloonTipOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BalloonTipOptions"/> class using the specified options.
        /// </summary>
        /// <remarks>This constructor validates the provided <paramref name="options"/> to ensure all
        /// required keys are present and contain valid values. If validation succeeds, the corresponding properties are
        /// initialized.</remarks>
        /// <param name="options">A <see cref="Hashtable"/> containing configuration values for the balloon tip. The following keys are
        /// required: <list type="bullet"> <item> <term>TrayTitle</term> <description>A non-empty <see cref="string"/>
        /// representing the title displayed in the system tray.</description> </item> <item> <term>TrayIcon</term>
        /// <description>A non-empty <see cref="string"/> representing the file path to the tray icon image. The file
        /// must exist.</description> </item> <item> <term>BalloonTipTitle</term> <description>A non-empty <see
        /// cref="string"/> representing the title of the balloon tip.</description> </item> <item>
        /// <term>BalloonTipText</term> <description>A non-empty <see cref="string"/> representing the text content of
        /// the balloon tip.</description> </item> <item> <term>BalloonTipIcon</term> <description>A <see
        /// cref="ToolTipIcon"/> value representing the icon displayed in the balloon tip.</description> </item> </list></param>
        /// <exception cref="ArgumentNullException">Thrown if any required key in <paramref name="options"/> is missing, null, or contains an invalid value.</exception>
        /// <exception cref="FileNotFoundException">Thrown if the file specified by the <c>TrayIcon</c> key does not exist.</exception>
        public BalloonTipOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["TrayTitle"] is string trayTitle ? trayTitle : string.Empty,
            options["TrayIcon"] is string trayIcon ? trayIcon : string.Empty,
            options["BalloonTipTitle"] is string balloonTipTitle ? balloonTipTitle : string.Empty,
            options["BalloonTipText"] is string balloonTipText ? balloonTipText : string.Empty,
            options["BalloonTipIcon"] is ToolTipIcon balloonTipIcon ? balloonTipIcon : (ToolTipIcon)(-1),
            options["BalloonTipTime"] is uint balloonTipTime ? balloonTipTime : uint.MaxValue)
        {
        }

        /// <summary>
        /// Primary constructor that validates and initializes all properties.
        /// </summary>
        /// <remarks>This constructor contains all validation logic and is used by both the Hashtable
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
            if (string.IsNullOrWhiteSpace(trayTitle))
            {
                throw new ArgumentNullException(nameof(trayTitle), "TrayTitle value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(trayIcon))
            {
                throw new ArgumentNullException(nameof(trayIcon), "TrayIcon value is null or invalid.");
            }
            if (!(MiscUtilities.GetBase64StringBytes(trayIcon)?.Length > 0) && !File.Exists(trayIcon))
            {
                throw new FileNotFoundException("The specified AppIconImage cannot be found", trayIcon);
            }
            if (string.IsNullOrWhiteSpace(balloonTipTitle))
            {
                throw new ArgumentNullException(nameof(balloonTipTitle), "BalloonTipTitle value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(balloonTipText))
            {
                throw new ArgumentNullException(nameof(balloonTipText), "BalloonTipText value is null or invalid.");
            }
            if ((int)balloonTipIcon == -1)
            {
                throw new ArgumentNullException(nameof(balloonTipIcon), "BalloonTipIcon value is null or invalid.");
            }
            if (balloonTipTime == uint.MaxValue)
            {
                throw new ArgumentNullException(nameof(balloonTipTime), "BalloonTipTime value is null or invalid.");
            }
            TrayTitle = trayTitle;
            TrayIcon = trayIcon;
            BalloonTipTitle = balloonTipTitle;
            BalloonTipText = balloonTipText;
            BalloonTipIcon = balloonTipIcon;
            BalloonTipTime = balloonTipTime;
        }

        /// <summary>
        /// Represents the title displayed on the tray.
        /// </summary>
        [DataMember]
        public string TrayTitle { get; private set; }

        /// <summary>
        /// Represents the file path or identifier for the tray icon used in the application.
        /// </summary>
        [DataMember]
        public string TrayIcon { get; private set; }

        /// <summary>
        /// Gets the title text displayed in the balloon tip of a notification.
        /// </summary>
        [DataMember]
        public string BalloonTipTitle { get; private set; }

        /// <summary>
        /// Gets the text displayed in the balloon tip of a notification.
        /// </summary>
        [DataMember]
        public string BalloonTipText { get; private set; }

        /// <summary>
        /// Gets the icon displayed in the balloon tip associated with the notification.
        /// </summary>
        [DataMember]
        public ToolTipIcon BalloonTipIcon { get; private set; }

        /// <summary>
        /// Gets the duration, in milliseconds, that the balloon tip is displayed.
        /// </summary>
        [DataMember]
        public uint BalloonTipTime { get; private set; }
    }
}
