﻿using System;
using System.Collections;
using System.IO;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Represents configuration options for displaying a balloon tip notification in the system tray.
    /// </summary>
    /// <remarks>This type encapsulates the required properties for configuring a balloon tip notification,
    /// including the tray title, tray icon, balloon tip title, text, and icon. Use the <see
    /// cref="BalloonTipOptions(Hashtable)"/> constructor to initialize an instance with validated configuration
    /// values.</remarks>
    public sealed record BalloonTipOptions
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
        public BalloonTipOptions(Hashtable options)
        {
            // Nothing here is allowed to be null.
            if (options["TrayTitle"] is not string trayTitle || string.IsNullOrWhiteSpace(trayTitle))
            {
                throw new ArgumentNullException("TrayTitle value is null or invalid.", (Exception?)null);
            }
            if (options["TrayIcon"] is not string trayIcon || string.IsNullOrWhiteSpace(trayIcon))
            {
                throw new ArgumentNullException("TrayIcon value is null or invalid.", (Exception?)null);
            }
            if (options["BalloonTipTitle"] is not string balloonTipTitle || string.IsNullOrWhiteSpace(balloonTipTitle))
            {
                throw new ArgumentNullException("BalloonTipTitle value is null or invalid.", (Exception?)null);
            }
            if (options["BalloonTipText"] is not string balloonTipText || string.IsNullOrWhiteSpace(balloonTipText))
            {
                throw new ArgumentNullException("BalloonTipText value is null or invalid.", (Exception?)null);
            }
            if (options["BalloonTipIcon"] is not ToolTipIcon balloonTipIcon)
            {
                throw new ArgumentNullException("BalloonTipIcon value is null or invalid.", (Exception?)null);
            }
            if (options["BalloonTipTime"] is not uint balloonTipTime)
            {
                throw new ArgumentNullException("BalloonTipTime value is null or invalid.", (Exception?)null);
            }

            // Test that the specified image paths are valid.
            if (!File.Exists(trayIcon))
            {
                throw new FileNotFoundException("The specified AppIconImage cannot be found", trayIcon);
            }

            // The hashtable was correctly defined, assign the remaining values.
            TrayTitle = trayTitle;
            TrayIcon = trayIcon;
            BalloonTipTitle = balloonTipTitle;
            BalloonTipText = balloonTipText;
            BalloonTipIcon = balloonTipIcon;
            BalloonTipTime = balloonTipTime;
        }

        /// <summary>
        /// Represents configuration options for displaying a balloon tip notification in the system tray.
        /// </summary>
        /// <remarks>This class encapsulates the settings required to display a balloon tip, including
        /// titles, text, icons, and the duration for which the balloon tip is displayed. The constructor is private and
        /// intended for deserialization purposes using the <see cref="JsonConstructorAttribute"/>.</remarks>
        /// <param name="trayTitle">The title of the system tray application. Cannot be <see langword="null"/>.</param>
        /// <param name="trayIcon">The path to the system tray icon. Cannot be <see langword="null"/>.</param>
        /// <param name="balloonTipTitle">The title of the balloon tip notification. Cannot be <see langword="null"/>.</param>
        /// <param name="balloonTipText">The text content of the balloon tip notification. Cannot be <see langword="null"/>.</param>
        /// <param name="balloonTipIcon">The icon to display in the balloon tip notification.</param>
        /// <param name="balloonTipTime">The duration, in milliseconds, for which the balloon tip is displayed.</param>
        /// <exception cref="ArgumentNullException">Thrown if any of the following parameters are <see langword="null"/>: <paramref name="trayTitle"/>,
        /// <paramref name="trayIcon"/>, <paramref name="balloonTipTitle"/>, or <paramref name="balloonTipText"/>.</exception>
        [JsonConstructor]
        private BalloonTipOptions(string trayTitle, string trayIcon, string balloonTipTitle, string balloonTipText, ToolTipIcon balloonTipIcon, uint balloonTipTime)
        {
            TrayTitle = trayTitle ?? throw new ArgumentNullException(nameof(trayTitle));
            TrayIcon = trayIcon ?? throw new ArgumentNullException(nameof(trayIcon));
            BalloonTipTitle = balloonTipTitle ?? throw new ArgumentNullException(nameof(balloonTipTitle));
            BalloonTipText = balloonTipText ?? throw new ArgumentNullException(nameof(balloonTipText));
            BalloonTipIcon = balloonTipIcon;
            BalloonTipTime = balloonTipTime;
        }

        /// <summary>
        /// Represents the title displayed on the tray.
        /// </summary>
        [JsonProperty]
        public readonly string TrayTitle;

        /// <summary>
        /// Represents the file path or identifier for the tray icon used in the application.
        /// </summary>
        [JsonProperty]
        public readonly string TrayIcon;

        /// <summary>
        /// Gets the title text displayed in the balloon tip of a notification.
        /// </summary>
        [JsonProperty]
        public readonly string BalloonTipTitle;

        /// <summary>
        /// Gets the text displayed in the balloon tip of a notification.
        /// </summary>
        [JsonProperty]
        public readonly string BalloonTipText;

        /// <summary>
        /// Gets the icon displayed in the balloon tip associated with the notification.
        /// </summary>
        [JsonProperty]
        public readonly ToolTipIcon BalloonTipIcon;

        /// <summary>
        /// Gets the duration, in milliseconds, that the balloon tip is displayed.
        /// </summary>
        [JsonProperty]
        public readonly uint BalloonTipTime;
    }
}
