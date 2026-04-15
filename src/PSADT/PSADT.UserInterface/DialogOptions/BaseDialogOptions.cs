using System;
using System.Collections;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using PSADT.UserInterface.Utilities;
using PSADT.Utilities;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    [KnownType(typeof(CloseAppsDialogOptions))]
    [KnownType(typeof(CustomDialogOptions))]
    [KnownType(typeof(DialogBoxOptions))]
    [KnownType(typeof(HelpConsoleOptions))]
    [KnownType(typeof(InputDialogOptions))]
    [KnownType(typeof(ListSelectionDialogOptions))]
    [KnownType(typeof(ProgressDialogOptions))]
    [KnownType(typeof(RestartDialogOptions))]
    [KnownType(typeof(BalloonTipOptions))]
    public abstract record BaseDialogOptions : IDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the BaseDialogOptions class using the specified configuration options.
        /// </summary>
        /// <remarks>If a required key is missing from the options dictionary, a default value is used for
        /// that setting. Ensure that the dictionary contains valid entries for all necessary dialog options to achieve
        /// the desired configuration.</remarks>
        /// <param name="options">A dictionary containing key-value pairs that define dialog configuration settings, such as application
        /// title, subtitle, icon images, language, and display options. Keys must match the expected option names.</param>
        /// <exception cref="ArgumentNullException">Thrown if the options dictionary is null.</exception>
        private protected BaseDialogOptions(IDictionary options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] as string ?? null!,
            options["Subtitle"] as string ?? null!,
            options["AppIconImage"] as string ?? null!,
            options["AppIconDarkImage"] as string,
            options["AppBannerImage"] as string ?? null!,
            options["AppTaskbarIconImage"] as string,
            options["DialogTopMost"] as bool? ?? false,
            options["Language"] as CultureInfo ?? null!,
            options["FluentAccentColor"] as int?,
            options["DialogPosition"] as DialogPosition?,
            options["DialogAllowMove"] as bool?,
            options["DialogExpiryDuration"] as TimeSpan?,
            options["DialogPersistInterval"] as TimeSpan?)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="BaseDialogOptions"/> class with the specified application settings
        /// and dialog configuration.
        /// </summary>
        /// <remarks>This constructor is protected and intended for use by derived classes. It ensures
        /// that essential application settings and dialog configurations are provided.</remarks>
        /// <param name="appTitle">The title of the application. Cannot be null or empty.</param>
        /// <param name="subtitle">The subtitle of the application. Cannot be null or empty.</param>
        /// <param name="appIconImage">The path to the application's icon image. Cannot be null or empty.</param>
        /// <param name="appIconDarkImage">The path to the application's dark mode icon image. Cannot be null or empty.</param>
        /// <param name="appBannerImage">The path to the application's banner image. Cannot be null or empty.</param>
        /// <param name="appTaskbarIconImage">The path to the application's tray icon image. Can be null or empty.</param>
        /// <param name="dialogTopMost">Indicates whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information representing the language for the dialog. Cannot be null.</param>
        /// <param name="fluentAccentColor">The accent color for Fluent design elements in the dialog. If null, the default accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If null, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If null, the default behavior is used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires. If null, the dialog does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists. If null, the dialog persists indefinitely.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="appTitle"/>, <paramref name="subtitle"/>, <paramref name="appIconImage"/>,
        /// <paramref name="appIconDarkImage"/>, or <paramref name="appBannerImage"/> is null or empty.</exception>
        private protected BaseDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor = null, DialogPosition? dialogPosition = null, bool? dialogAllowMove = null, TimeSpan? dialogExpiryDuration = null, TimeSpan? dialogPersistInterval = null)
        {
            // Set initial string properties.
            ArgumentNullException.ThrowIfNull(language);
            ArgumentException.ThrowIfNullOrWhiteSpace(appTitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(subtitle);
            ArgumentException.ThrowIfNullOrWhiteSpace(appIconImage);
            ArgumentException.ThrowIfNullOrWhiteSpace(appBannerImage);
            AppTitle = appTitle;
            Subtitle = subtitle;
            AppIconImage = ThrowIfImageIsInvalid(appIconImage, nameof(AppIconImage));
            AppBannerImage = ThrowIfImageIsInvalid(appBannerImage, nameof(AppBannerImage));

            // AppTaskbarIconImage is optional, so only validate it if it has a value.
            if (appIconDarkImage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(appIconDarkImage);
                AppIconDarkImage = ThrowIfImageIsInvalid(appIconDarkImage, nameof(AppIconDarkImage));
            }
            if (appTaskbarIconImage is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(appTaskbarIconImage);
                AppTaskbarIconImage = ThrowIfImageIsInvalid(appTaskbarIconImage, nameof(AppTaskbarIconImage));
            }

            // Set all remaining properties.
            DialogTopMost = dialogTopMost;
            LanguageName = language.Name;
            FluentAccentColor = fluentAccentColor;
            DialogPosition = dialogPosition;
            DialogAllowMove = dialogAllowMove;
            DialogExpiryDuration = dialogExpiryDuration;
            DialogPersistInterval = dialogPersistInterval;
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string AppTitle;

        /// <summary>
        /// The subtitle of the dialog, providing additional context or information.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string Subtitle;

        /// <summary>
        /// The image file path for the application icon to be displayed in the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string AppIconImage;

        /// <summary>
        /// The image file path for the application icon (dark mode) to be displayed in the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? AppIconDarkImage;

        /// <summary>
        /// The image file path for the banner to be displayed in the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string AppBannerImage;

        /// <summary>
        /// Gets the file path or resource identifier for the application's tray icon image.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? AppTaskbarIconImage;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool DialogTopMost;

        /// <summary>
        /// Gets the culture information representing the language associated with this instance.
        /// </summary>
        [IgnoreDataMember]
        public CultureInfo Language => new(LanguageName);

        /// <summary>
        /// The accent color for the dialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly int? FluentAccentColor;

        /// <summary>
        /// The position of the dialog on the screen.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DialogPosition? DialogPosition;

        /// <summary>
        /// Indicates whether the dialog allows the user to move it around the screen.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool? DialogAllowMove;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly TimeSpan? DialogExpiryDuration;

        /// <summary>
        /// The interval for which the dialog will persist on the screen.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly TimeSpan? DialogPersistInterval;

        /// <summary>
        /// Gets the culture name string for serialization.
        /// </summary>
        [DataMember]
        private readonly string LanguageName;

        /// <summary>
        /// Validates the specified image input and returns the image string if it represents a valid image file or
        /// base64-encoded image.
        /// </summary>
        /// <remarks>This method supports validation of both icon and bitmap formats, and accepts either a
        /// file path or a base64 string. The image is considered valid if it can be loaded as an icon or bitmap without
        /// error.</remarks>
        /// <param name="image">The path to the image file or a base64 string representing the image to validate. Must refer to a valid
        /// image file or a valid base64-encoded image.</param>
        /// <param name="identifier">The name of the image parameter, used in exception messages to identify the source of errors.</param>
        /// <returns>The original image string if the image is valid.</returns>
        /// <exception cref="FileNotFoundException">Thrown if the specified image file does not exist.</exception>
        /// <exception cref="ArgumentException">Thrown if the specified image is not a valid image format.</exception>
        [StackTraceHidden]
        internal static string ThrowIfImageIsInvalid(string image, string identifier)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(image); ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
            try
            {
                using Stream stream = MiscUtilities.GetBase64StringBytes(image) is not byte[] bytes ? new FileStream(image, FileMode.Open, FileAccess.Read, FileShare.Read) : new MemoryStream(bytes, false);
                try
                {
                    if (!DrawingUtilities.IsStreamAnIcon(stream))
                    {
                        using Bitmap bmp = new(stream, true);
                        _ = bmp.Size;
                    }
                    else
                    {
                        using Icon icon = new(stream, 256, 256);
                        _ = icon.Size;
                    }
                }
                catch (OutOfMemoryException ex)
                {
                    throw new FileFormatException(new Uri(image), "The specified file is invalid or otherwise corrupted.", ex);
                }
                return image;
            }
            catch (Exception ex)
            {
                throw new BadImageFormatException($"The specified [{identifier}] is not a valid image format", identifier, ex);
            }
        }
    }
}
