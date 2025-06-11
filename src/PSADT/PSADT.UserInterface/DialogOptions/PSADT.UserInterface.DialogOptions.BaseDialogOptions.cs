using System;
using System.Collections;
using System.IO;
using System.Runtime.Serialization;
using PSADT.UserInterface.Dialogs;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for all dialogs.
    /// </summary>
    [DataContract]
    [KnownType(typeof(CloseAppsDialogOptions))]
    [KnownType(typeof(CustomDialogOptions))]
    [KnownType(typeof(InputDialogOptions))]
    [KnownType(typeof(ProgressDialogOptions))]
    [KnownType(typeof(RestartDialogOptions))]
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

            // Test that the specified image paths are valid.
            if (!File.Exists(appIconImage))
            {
                throw new FileNotFoundException("The specified AppIconImage cannot be found", appIconImage);
            }
            // But we can skip performing this one
            // if (!File.Exists(appIconDarkImage))
            // {
            //     throw new FileNotFoundException("The specified AppIconDarkImage cannot be found", appIconDarkImage);
            // }
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
        }

        /// <summary>
        /// The title of the application or process being displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string AppTitle;

        /// <summary>
        /// The subtitle of the dialog, providing additional context or information.
        /// </summary>
        [DataMember]
        public readonly string Subtitle;

        /// <summary>
        /// The image file path for the application icon to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string AppIconImage;

        /// <summary>
        /// The image file path for the application icon (dark mode) to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string AppIconDarkImage;

        /// <summary>
        /// The image file path for the banner to be displayed in the dialog.
        /// </summary>
        [DataMember]
        public readonly string AppBannerImage;

        /// <summary>
        /// The position of the dialog on the screen.
        /// </summary>
        [DataMember]
        public readonly DialogPosition? DialogPosition;

        /// <summary>
        /// Indicates whether the dialog allows the user to move it around the screen.
        /// </summary>
        [DataMember]
        public readonly bool? DialogAllowMove;

        /// <summary>
        /// Indicates whether the dialog should be displayed as a top-most window.
        /// </summary>
        [DataMember]
        public readonly bool DialogTopMost;

        /// <summary>
        /// The accent color for the dialog.
        /// </summary>
        [DataMember]
        public readonly int? FluentAccentColor;

        /// <summary>
        /// The duration for which the dialog will be displayed before it automatically closes.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? DialogExpiryDuration;

        /// <summary>
        /// The interval for which the dialog will persist on the screen.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? DialogPersistInterval;
    }
}
