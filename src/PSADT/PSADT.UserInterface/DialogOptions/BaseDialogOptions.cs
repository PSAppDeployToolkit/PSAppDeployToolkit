using System;
using System.Collections;
using System.Globalization;
using System.IO;
using PSADT.UserInterface.Dialogs;
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
        public BaseOptions(Hashtable options)
        {
            // Nothing here is allowed to be null.
            if (options["AppTitle"] is not string appTitle || string.IsNullOrWhiteSpace(appTitle))
            {
                throw new ArgumentNullException("AppTitle value is null or invalid.", (Exception?)null);
            }
            if (options["Subtitle"] is not string subTitle || string.IsNullOrWhiteSpace(subTitle))
            {
                throw new ArgumentNullException("Subtitle value is null or invalid.", (Exception?)null);
            }
            if (options["AppIconImage"] is not string appIconImage || string.IsNullOrWhiteSpace(appIconImage))
            {
                throw new ArgumentNullException("AppIconImage value is null or invalid.", (Exception?)null);
            }
            if (options["AppIconDarkImage"] is not string appIconDarkImage || string.IsNullOrWhiteSpace(appIconDarkImage))
            {
                throw new ArgumentNullException("AppIconDarkImage value is null or invalid.", (Exception?)null);
            }
            if (options["AppBannerImage"] is not string appBannerImage || string.IsNullOrWhiteSpace(appBannerImage))
            {
                throw new ArgumentNullException("AppBannerImage value is null or invalid.", (Exception?)null);
            }
            if (options["DialogTopMost"] is not bool dialogTopMost)
            {
                throw new ArgumentNullException("DialogTopMost value is null or invalid.", (Exception?)null);
            }
            if (options["Language"] is not CultureInfo language)
            {
                throw new ArgumentNullException("Language value is null or invalid.", (Exception?)null);
            }

            // Test that the specified image paths are valid.
            if (!File.Exists(appIconImage))
            {
                throw new FileNotFoundException("The specified AppIconImage cannot be found", appIconImage);
            }
            if (!File.Exists(appIconDarkImage))
            {
                throw new FileNotFoundException("The specified AppIconDarkImage cannot be found", appIconDarkImage);
            }
            if (!File.Exists(appBannerImage))
            {
                throw new FileNotFoundException("The specified AppBannerImage cannot be found", appBannerImage);
            }

            // Test and set optional values.
            if (options.ContainsKey("DialogAllowMove"))
            {
                if (options["DialogAllowMove"] is not bool dialogAllowMove)
                {
                    throw new ArgumentOutOfRangeException("DialogAllowMove value is not valid.", (Exception?)null);
                }
                DialogAllowMove = dialogAllowMove;
            }
            if (options.ContainsKey("DialogExpiryDuration"))
            {
                if (options["DialogExpiryDuration"] is not TimeSpan dialogExpiryDuration)
                {
                    throw new ArgumentOutOfRangeException("DialogExpiryDuration value is not valid.", (Exception?)null);
                }
                DialogExpiryDuration = dialogExpiryDuration;
            }
            if (options.ContainsKey("DialogPersistInterval"))
            {
                if (options["DialogPersistInterval"] is not TimeSpan dialogPersistInterval)
                {
                    throw new ArgumentOutOfRangeException("DialogPersistInterval value is not valid.", (Exception?)null);
                }
                DialogPersistInterval = dialogPersistInterval;
            }
            if (options.ContainsKey("FluentAccentColor"))
            {
                if (options["FluentAccentColor"] is not int fluentAccentColor)
                {
                    throw new ArgumentOutOfRangeException("FluentAccentColor value is not valid.", (Exception?)null);
                }
                FluentAccentColor = fluentAccentColor;
            }
            if (options.ContainsKey("DialogPosition"))
            {
                if (options["DialogPosition"] is not DialogPosition dialogPosition)
                {
                    throw new ArgumentOutOfRangeException("DialogPosition value is not valid.", (Exception?)null);
                }
                DialogPosition = dialogPosition;
            }

            // The hashtable was correctly defined, assign the remaining values.
            AppTitle = appTitle;
            Subtitle = subTitle;
            AppIconImage = appIconImage;
            AppIconDarkImage = appIconDarkImage;
            AppBannerImage = appBannerImage;
            DialogTopMost = dialogTopMost;
            Language = language;
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
        protected BaseOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor = null, DialogPosition? dialogPosition = null, bool? dialogAllowMove = null, TimeSpan? dialogExpiryDuration = null, TimeSpan? dialogPersistInterval = null)
        {
            AppTitle = appTitle ?? throw new ArgumentNullException(nameof(appTitle), "AppTitle cannot be null or empty.");
            Subtitle = subtitle ?? throw new ArgumentNullException(nameof(subtitle), "Subtitle cannot be null or empty.");
            AppIconImage = appIconImage ?? throw new ArgumentNullException(nameof(appIconImage), "AppIconImage cannot be null or empty.");
            AppIconDarkImage = appIconDarkImage ?? throw new ArgumentNullException(nameof(appIconDarkImage), "AppIconDarkImage cannot be null or empty.");
            AppBannerImage = appBannerImage ?? throw new ArgumentNullException(nameof(appBannerImage), "AppBannerImage cannot be null or empty.");
            Language = language ?? throw new ArgumentNullException(nameof(language), "Language cannot be null.");
            DialogTopMost = dialogTopMost;
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
        public readonly string AppTitle;

        /// <summary>
        /// The subtitle of the dialog, providing additional context or information.
        /// </summary>
        [JsonProperty]
        public readonly string Subtitle;

        /// <summary>
        /// The image file path for the application icon to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string AppIconImage;

        /// <summary>
        /// The image file path for the application icon (dark mode) to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string AppIconDarkImage;

        /// <summary>
        /// The image file path for the banner to be displayed in the dialog.
        /// </summary>
        [JsonProperty]
        public readonly string AppBannerImage;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [JsonProperty]
        public readonly bool DialogTopMost;

        /// <summary>
        /// Gets the culture information representing the language associated with this instance.
        /// </summary>
        [JsonProperty]
        public readonly CultureInfo Language;

        /// <summary>
        /// The accent color for the dialog.
        /// </summary>
        [JsonProperty]
        public readonly int? FluentAccentColor;

        /// <summary>
        /// The position of the dialog on the screen.
        /// </summary>
        [JsonProperty]
        public readonly DialogPosition? DialogPosition;

        /// <summary>
        /// Indicates whether the dialog allows the user to move it around the screen.
        /// </summary>
        [JsonProperty]
        public readonly bool? DialogAllowMove;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [JsonProperty]
        public readonly TimeSpan? DialogExpiryDuration;

        /// <summary>
        /// The interval for which the dialog will persist on the screen.
        /// </summary>
        [JsonProperty]
        public readonly TimeSpan? DialogPersistInterval;
    }
}
