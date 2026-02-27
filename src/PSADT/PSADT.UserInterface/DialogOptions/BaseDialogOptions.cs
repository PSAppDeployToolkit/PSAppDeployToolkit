using System;
using System.Collections;
using System.Globalization;
using System.IO;
using System.Runtime.Serialization;
using PSADT.Interop.Extensions;
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
        internal BaseDialogOptions(IDictionary options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] as string ?? null!,
            options["Subtitle"] as string ?? null!,
            options["AppIconImage"] as string ?? null!,
            options["AppIconDarkImage"] as string ?? null!,
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
        private protected BaseDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor = null, DialogPosition? dialogPosition = null, bool? dialogAllowMove = null, TimeSpan? dialogExpiryDuration = null, TimeSpan? dialogPersistInterval = null)
        {
            // Confirm that the language parameter is not null before attempting to access its properties.
            if (language is null)
            {
                throw new ArgumentNullException(nameof(language), "Language value is null or invalid.");
            }

            // Set initial string properties.
            AppTitle = appTitle.ThrowIfNullOrWhiteSpace();
            Subtitle = subtitle.ThrowIfNullOrWhiteSpace();
            AppIconImage = appIconImage.ThrowIfNullOrWhiteSpace();
            AppIconDarkImage = appIconDarkImage.ThrowIfNullOrWhiteSpace();
            AppBannerImage = appBannerImage.ThrowIfNullOrWhiteSpace();

            // Test that the specified image paths are valid.
            if (!(MiscUtilities.GetBase64StringBytes(AppIconImage)?.Length > 0) && !File.Exists(AppIconImage))
            {
                throw new FileNotFoundException($"The specified AppIconImage [{AppIconImage}] cannot be found", AppIconImage);
            }
            if (!(MiscUtilities.GetBase64StringBytes(AppIconDarkImage)?.Length > 0) && !File.Exists(AppIconDarkImage))
            {
                throw new FileNotFoundException($"The specified AppIconDarkImage [{AppIconDarkImage}] cannot be found", AppIconDarkImage);
            }
            if (!(MiscUtilities.GetBase64StringBytes(AppBannerImage)?.Length > 0) && !File.Exists(AppBannerImage))
            {
                throw new FileNotFoundException($"The specified AppBannerImage [{AppBannerImage}] cannot be found", AppBannerImage);
            }

            // AppTaskbarIconImage is optional, so only validate it if it has a value.
            if (!string.IsNullOrWhiteSpace(appTaskbarIconImage))
            {
                if (!(MiscUtilities.GetBase64StringBytes(appTaskbarIconImage!)?.Length > 0) && !File.Exists(appTaskbarIconImage))
                {
                    throw new FileNotFoundException($"The specified AppTaskbarIconImage [{appTaskbarIconImage}] cannot be found", appTaskbarIconImage);
                }
                AppTaskbarIconImage = appTaskbarIconImage;
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
        public readonly string AppIconDarkImage;

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
        public CultureInfo Language => CultureInfo.GetCultureInfo(LanguageName);

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
    }
}
