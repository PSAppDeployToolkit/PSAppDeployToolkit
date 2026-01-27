using System;
using System.Collections;
using System.Globalization;
using System.IO;
using PSADT.UserInterface.Dialogs;
using PSADT.Utilities;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    public abstract record BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOptions"/> class with the specified options.
        /// This accepts a hashtable of parameters to ease construction on the PowerShell side of things.
        /// </summary>
        /// <param name="options"></param>
        internal BaseOptions(Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] is string appTitle ? appTitle : string.Empty,
            options["Subtitle"] is string subtitle ? subtitle : string.Empty,
            options["AppIconImage"] is string appIconImage ? appIconImage : string.Empty,
            options["AppIconDarkImage"] is string appIconDarkImage ? appIconDarkImage : string.Empty,
            options["AppBannerImage"] is string appBannerImage ? appBannerImage : string.Empty,
            options["AppTrayIconImage"] is string appTrayIconImage ? appTrayIconImage : null,
            options["DialogTopMost"] is bool dialogTopMost && dialogTopMost,
            options["Language"] is CultureInfo language ? language : null!,
            options["FluentAccentColor"] is int fluentAccentColor ? fluentAccentColor : null,
            options["DialogPosition"] is DialogPosition dialogPosition ? dialogPosition : null,
            options["DialogAllowMove"] is bool dialogAllowMove ? dialogAllowMove : null,
            options["DialogExpiryDuration"] is TimeSpan dialogExpiryDuration ? dialogExpiryDuration : null,
            options["DialogPersistInterval"] is TimeSpan dialogPersistInterval ? dialogPersistInterval : null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseOptions"/> class with the specified application settings
        /// and dialog configuration.
        /// </summary>
        /// <remarks>This constructor is protected and intended for use by derived classes. It ensures
        /// that essential application settings and dialog configurations are provided.</remarks>
        /// <param name="appTitle">The title of the application. Cannot be null or empty.</param>
        /// <param name="subtitle">The subtitle of the application. Cannot be null or empty.</param>
        /// <param name="appIconImage">The path to the application's icon image. Cannot be null or empty.</param>
        /// <param name="appIconDarkImage">The path to the application's dark mode icon image. Cannot be null or empty.</param>
        /// <param name="appBannerImage">The path to the application's banner image. Cannot be null or empty.</param>
        /// <param name="appTrayIconImage">The path to the application's tray icon image. Can be null or empty.</param>
        /// <param name="dialogTopMost">Indicates whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information representing the language for the dialog. Cannot be null.</param>
        /// <param name="fluentAccentColor">The accent color for Fluent design elements in the dialog. If null, the default accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If null, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If null, the default behavior is used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires. If null, the dialog does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists. If null, the dialog persists indefinitely.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="appTitle"/>, <paramref name="subtitle"/>, <paramref name="appIconImage"/>,
        /// <paramref name="appIconDarkImage"/>, or <paramref name="appBannerImage"/> is null or empty.</exception>
        [JsonConstructor]
        protected BaseOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTrayIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor = null, DialogPosition? dialogPosition = null, bool? dialogAllowMove = null, TimeSpan? dialogExpiryDuration = null, TimeSpan? dialogPersistInterval = null)
        {
            if (string.IsNullOrWhiteSpace(appTitle))
            {
                throw new ArgumentNullException(nameof(appTitle), "AppTitle value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(subtitle))
            {
                throw new ArgumentNullException(nameof(subtitle), "Subtitle value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(appIconImage))
            {
                throw new ArgumentNullException(nameof(appIconImage), "AppIconImage value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(appIconDarkImage))
            {
                throw new ArgumentNullException(nameof(appIconDarkImage), "AppIconDarkImage value is null or invalid.");
            }
            if (string.IsNullOrWhiteSpace(appBannerImage))
            {
                throw new ArgumentNullException(nameof(appBannerImage), "AppBannerImage value is null or invalid.");
            }
            if (language is null)
            {
                throw new ArgumentNullException(nameof(language), "Language value is null or invalid.");
            }

            // Test that the specified image paths are valid.
            if (!(MiscUtilities.GetBase64StringBytes(appIconImage)?.Length > 0) && !File.Exists(appIconImage))
            {
                throw new FileNotFoundException("The specified AppIconImage cannot be found", appIconImage);
            }
            if (!(MiscUtilities.GetBase64StringBytes(appIconDarkImage)?.Length > 0) && !File.Exists(appIconDarkImage))
            {
                throw new FileNotFoundException("The specified AppIconDarkImage cannot be found", appIconDarkImage);
            }
            if (!(MiscUtilities.GetBase64StringBytes(appBannerImage)?.Length > 0) && !File.Exists(appBannerImage))
            {
                throw new FileNotFoundException("The specified AppBannerImage cannot be found", appBannerImage);
            }

            // AppTrayIconImage is optional, so only validate it if it has a value.
            if (!string.IsNullOrWhiteSpace(appTrayIconImage))
            {
                if (!(MiscUtilities.GetBase64StringBytes(appTrayIconImage!)?.Length > 0) && !File.Exists(appTrayIconImage))
                {
                    throw new FileNotFoundException("The specified AppTrayIconImage cannot be found", appTrayIconImage);
                }
                AppTrayIconImage = appTrayIconImage;
            }

            // Set all remaining properties.
            AppTitle = appTitle;
            Subtitle = subtitle;
            AppIconImage = appIconImage;
            AppIconDarkImage = appIconDarkImage;
            AppBannerImage = appBannerImage;
            DialogTopMost = dialogTopMost;
            Language = language;
            FluentAccentColor = fluentAccentColor;
            DialogPosition = dialogPosition;
            DialogAllowMove = dialogAllowMove;
            DialogExpiryDuration = dialogExpiryDuration;
            DialogPersistInterval = dialogPersistInterval;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public string AppTitle { get; }

        /// <summary>
        /// The subtitle of the dialog, providing additional context or information.
        /// </summary>
        [JsonProperty]
        public string Subtitle { get; }

        /// <summary>
        /// The image file path for the application icon to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public string AppIconImage { get; }

        /// <summary>
        /// The image file path for the application icon (dark mode) to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public string AppIconDarkImage { get; }

        /// <summary>
        /// The image file path for the banner to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public string AppBannerImage { get; }

        /// <summary>
        /// Gets the file path or resource identifier for the application's tray icon image.
        /// </summary>
        [JsonProperty]
        public string? AppTrayIconImage { get; }

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [JsonProperty]
        public bool DialogTopMost { get; }

        /// <summary>
        /// Gets the culture information representing the language associated with this instance.
        /// </summary>
        [JsonProperty]
        public CultureInfo Language { get; }

        /// <summary>
        /// The accent color for the dialog.
        /// </summary>
        [JsonProperty]
        public int? FluentAccentColor { get; }

        /// <summary>
        /// The position of the dialog on the screen.
        /// </summary>
        [JsonProperty]
        public DialogPosition? DialogPosition { get; }

        /// <summary>
        /// Indicates whether the dialog allows the user to move it around the screen.
        /// </summary>
        [JsonProperty]
        public bool? DialogAllowMove { get; }

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [JsonProperty]
        public TimeSpan? DialogExpiryDuration { get; }

        /// <summary>
        /// The interval for which the dialog will persist on the screen.
        /// </summary>
        [JsonProperty]
        public TimeSpan? DialogPersistInterval { get; }
    }
}
