using System;
using System.Collections;
using System.Globalization;
using PSADT.Module;
using PSADT.UserInterface.Dialogs;
using Newtonsoft.Json;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    public sealed record CloseAppsDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CloseAppsDialogOptions(DeploymentType deploymentType, Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["Strings"] is not Hashtable strings || strings.Count == 0)
            {
                throw new ArgumentNullException("Strings table value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("DeferralsRemaining"))
            {
                if (options["DeferralsRemaining"] is not uint deferralsRemaining)
                {
                    throw new ArgumentOutOfRangeException("DeferralsRemaining value is not valid.", (Exception?)null);
                }
                DeferralsRemaining = deferralsRemaining;
            }
            if (options.ContainsKey("DeferralDeadline"))
            {
                if (options["DeferralDeadline"] is not DateTime deferralDeadline)
                {
                    throw new ArgumentOutOfRangeException("DeferralDeadline value is not valid.", (Exception?)null);
                }
                DeferralDeadline = deferralDeadline;
            }
            if (options.ContainsKey("UnlimitedDeferrals"))
            {
                if (options["UnlimitedDeferrals"] is not bool unlimitedDeferrals)
                {
                    throw new ArgumentOutOfRangeException("UnlimitedDeferrals value is not valid.", (Exception?)null);
                }
                UnlimitedDeferrals = unlimitedDeferrals;
            }
            if (options.ContainsKey("ContinueOnProcessClosure"))
            {
                if (options["ContinueOnProcessClosure"] is not bool continueOnProcessClosure)
                {
                    throw new ArgumentOutOfRangeException("ContinueOnProcessClosure value is not valid.", (Exception?)null);
                }
                ContinueOnProcessClosure = continueOnProcessClosure;
            }
            if (options.ContainsKey("CountdownDuration"))
            {
                if (options["CountdownDuration"] is not TimeSpan countdownDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownDuration value is not valid.", (Exception?)null);
                }
                CountdownDuration = countdownDuration;
            }
            if (options.ContainsKey("ForcedCountdown"))
            {
                if (options["ForcedCountdown"] is not bool forcedCountdown)
                {
                    throw new ArgumentOutOfRangeException("ForcedCountdown value is not valid.", (Exception?)null);
                }
                ForcedCountdown = forcedCountdown;
            }
            if (options.ContainsKey("HideCloseButton"))
            {
                if (options["HideCloseButton"] is not bool hideCloseButton)
                {
                    throw new ArgumentOutOfRangeException("HideCloseButton value is not valid.", (Exception?)null);
                }
                HideCloseButton = hideCloseButton;
            }
            if (options.ContainsKey("CustomMessageText"))
            {
                if (options["CustomMessageText"] is not string customMessageText || string.IsNullOrWhiteSpace(customMessageText))
                {
                    throw new ArgumentOutOfRangeException("CustomMessageText value is not valid.", (Exception?)null);
                }
                CustomMessageText = customMessageText;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Strings = new(strings, deploymentType);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class with the specified dialog
        /// configuration options.
        /// </summary>
        /// <remarks>This constructor is primarily used for deserialization purposes and allows
        /// customization of various dialog properties, including appearance, behavior, and timing settings. It is
        /// marked as private to restrict direct instantiation.</remarks>
        /// <param name="appTitle">The title of the application displayed in the dialog.</param>
        /// <param name="subtitle">The subtitle text displayed below the application title in the dialog.</param>
        /// <param name="appIconImage">The path or URI to the application's icon image used in the dialog.</param>
        /// <param name="appIconDarkImage">The path or URI to the application's dark mode icon image used in the dialog.</param>
        /// <param name="appBannerImage">The path or URI to the banner image displayed in the dialog.</param>
        /// <param name="dialogTopMost">A value indicating whether the dialog should always appear on top of other windows.</param>
        /// <param name="language">The culture information used for localizing the dialog.</param>
        /// <param name="fluentAccentColor">The accent color used for Fluent design elements in the dialog. If <see langword="null"/>, the default
        /// accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogExpiryDuration">The duration after which the dialog expires and closes automatically. If <see langword="null"/>, the dialog
        /// does not expire.</param>
        /// <param name="dialogPersistInterval">The interval at which the dialog persists its state. If <see langword="null"/>, persistence is disabled.</param>
        /// <param name="strings">An object containing localized strings used in the dialog.</param>
        /// <param name="deferralsRemaining">The number of deferrals remaining for the dialog. If <see langword="null"/>, deferrals are not tracked.</param>
        /// <param name="deferralDeadline">The deadline by which all deferrals must be completed. If <see langword="null"/>, no deadline is enforced.</param>
        /// <param name="unlimitedDeferrals">A value indicating whether the dialog allows unlimited deferrals.</param>
        /// <param name="continueOnProcessClosure">A value indicating whether the dialog should continue to display even if the associated process is closed.</param>
        /// <param name="countdownDuration">The duration of the countdown timer displayed in the dialog. If <see langword="null"/>, no countdown is
        /// displayed.</param>
        /// <param name="forcedCountdown">A value indicating whether the countdown timer is mandatory and cannot be skipped.</param>
        /// <param name="hideCloseButton">A value indicating whether the close button is hidden in the dialog.</param>
        /// <param name="customMessageText">Custom text displayed in the dialog. If <see langword="null"/>, no custom message is shown.</param>
        [JsonConstructor]
        private CloseAppsDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, CloseAppsDialogStrings strings, uint? deferralsRemaining, DateTime? deferralDeadline, bool unlimitedDeferrals, bool continueOnProcessClosure, TimeSpan? countdownDuration, bool forcedCountdown, bool hideCloseButton, string? customMessageText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            // Assign the values.
            Strings = strings;
            DeferralsRemaining = deferralsRemaining;
            DeferralDeadline = deferralDeadline;
            UnlimitedDeferrals = unlimitedDeferrals;
            ContinueOnProcessClosure = continueOnProcessClosure;
            CountdownDuration = countdownDuration;
            ForcedCountdown = forcedCountdown;
            HideCloseButton = hideCloseButton;
            CustomMessageText = customMessageText;
        }

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [JsonProperty]
        public readonly CloseAppsDialogStrings Strings;

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        [JsonProperty]
        public readonly uint? DeferralsRemaining;

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        [JsonProperty]
        public readonly DateTime? DeferralDeadline;

        /// <summary>
        /// Indicates whether the system allows an unlimited number of deferrals.
        /// </summary>
        [JsonProperty]
        public readonly bool UnlimitedDeferrals;

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        [JsonProperty]
        public readonly bool ContinueOnProcessClosure;

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        [JsonProperty]
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// Specifies whether the countdown is "forced" or not (affects countdown decisions).
        /// </summary>
        [JsonProperty]
        public readonly bool ForcedCountdown;

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        [JsonProperty]
        public readonly bool HideCloseButton;

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [JsonProperty]
        public readonly string? CustomMessageText;

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        public sealed record CloseAppsDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CloseAppsDialogStrings"/> class.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            internal CloseAppsDialogStrings(Hashtable strings, DeploymentType deploymentType)
            {
                // Nothing here is allowed to be null.
                if (strings["Classic"] is not Hashtable classicStrings)
                {
                    throw new ArgumentNullException("Classic string table value is null or invalid.", (Exception?)null);
                }
                if (strings["Fluent"] is not Hashtable fluentStrings)
                {
                    throw new ArgumentNullException("Fluent string table value is null or invalid.", (Exception?)null);
                }

                // The hashtable was correctly defined, assign the remaining values.
                Classic = new(classicStrings, deploymentType);
                Fluent = new(fluentStrings, deploymentType);
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CloseAppsDialogStrings"/> class with the specified classic
            /// and fluent dialog strings.
            /// </summary>
            /// <param name="classicStrings">The strings used for the classic dialog style. Cannot be <see langword="null"/>.</param>
            /// <param name="fluentStrings">The strings used for the fluent dialog style. Cannot be <see langword="null"/>.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="classicStrings"/> or <paramref name="fluentStrings"/> is <see
            /// langword="null"/>.</exception>
            [JsonConstructor]
            private CloseAppsDialogStrings(CloseAppsDialogClassicStrings classic, CloseAppsDialogFluentStrings fluent)
            {
                // Assign the values.
                Classic = classic ?? throw new ArgumentNullException(nameof(classic), "Classic strings cannot be null.");
                Fluent = fluent ?? throw new ArgumentNullException(nameof(fluent), "Fluent strings cannot be null.");
            }

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            [JsonProperty]
            public readonly CloseAppsDialogClassicStrings Classic;

            /// <summary>
            /// The strings used for the Fluent CloseAppsDialog.
            /// </summary>
            [JsonProperty]
            public readonly CloseAppsDialogFluentStrings Fluent;

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            public sealed record CloseAppsDialogClassicStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogClassicStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                internal CloseAppsDialogClassicStrings(Hashtable strings, DeploymentType deploymentType)
                {
                    // Nothing here is allowed to be null.
                    if (strings["WelcomeMessage"] is not Hashtable welcomeMessageTable || welcomeMessageTable[deploymentType.ToString()] is not string welcomeMessage || string.IsNullOrWhiteSpace(welcomeMessage))
                    {
                        throw new ArgumentNullException("WelcomeMessage value is null or invalid.", (Exception?)null);
                    }
                    if (strings["CloseAppsMessage"] is not Hashtable closeAppsMessageTable || closeAppsMessageTable[deploymentType.ToString()] is not string closeAppsMessage || string.IsNullOrWhiteSpace(closeAppsMessage))
                    {
                        throw new ArgumentNullException("CloseAppsMessage value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ExpiryMessage"] is not Hashtable expiryMessageTable || expiryMessageTable[deploymentType.ToString()] is not string expiryMessage || string.IsNullOrWhiteSpace(expiryMessage))
                    {
                        throw new ArgumentNullException("ExpiryMessage value is null or invalid.", (Exception?)null);
                    }
                    if (strings["DeferralsRemaining"] is not string deferralsRemaining || string.IsNullOrWhiteSpace(deferralsRemaining))
                    {
                        throw new ArgumentNullException("DeferralsRemaining value is null or invalid.", (Exception?)null);
                    }
                    if (strings["DeferralDeadline"] is not string deferralDeadline || string.IsNullOrWhiteSpace(deferralDeadline))
                    {
                        throw new ArgumentNullException("DeferralDeadline value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ExpiryWarning"] is not string expiryWarning || string.IsNullOrWhiteSpace(expiryWarning))
                    {
                        throw new ArgumentNullException("ExpiryWarning value is null or invalid.", (Exception?)null);
                    }
                    if (strings["CountdownDefer"] is not Hashtable countdownDeferTable || countdownDeferTable[deploymentType.ToString()] is not string countdownDefer || string.IsNullOrWhiteSpace(countdownDefer))
                    {
                        throw new ArgumentNullException("CountdownDefer value is null or invalid.", (Exception?)null);
                    }
                    if (strings["CountdownClose"] is not Hashtable countdownCloseTable || countdownCloseTable[deploymentType.ToString()] is not string countdownClose || string.IsNullOrWhiteSpace(countdownClose))
                    {
                        throw new ArgumentNullException("CountdownClose value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonClose"] is not string buttonClose || string.IsNullOrWhiteSpace(buttonClose))
                    {
                        throw new ArgumentNullException("ButtonClose value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonDefer"] is not string buttonDefer || string.IsNullOrWhiteSpace(buttonDefer))
                    {
                        throw new ArgumentNullException("ButtonDefer value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonContinue"] is not string buttonContinue || string.IsNullOrWhiteSpace(buttonContinue))
                    {
                        throw new ArgumentNullException("ButtonContinue value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonContinueTooltip"] is not string buttonContinueTooltip || string.IsNullOrWhiteSpace(buttonContinueTooltip))
                    {
                        throw new ArgumentNullException("ButtonContinueTooltip value is null or invalid.", (Exception?)null);
                    }

                    // The hashtable was correctly defined, assign the remaining values.
                    WelcomeMessage = welcomeMessage;
                    CloseAppsMessage = closeAppsMessage;
                    ExpiryMessage = expiryMessage;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ExpiryWarning = expiryWarning;
                    CountdownDefer = countdownDefer;
                    CountdownClose = countdownClose;
                    ButtonClose = buttonClose;
                    ButtonDefer = buttonDefer;
                    ButtonContinue = buttonContinue;
                    ButtonContinueTooltip = buttonContinueTooltip;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogClassicStrings"/> class with the
                /// specified dialog strings.
                /// </summary>
                /// <remarks>This constructor is intended for use with JSON deserialization. It
                /// allows the initialization of all dialog-related strings used in the classic close apps
                /// dialog.</remarks>
                /// <param name="welcomeMessage">The welcome message displayed at the start of the dialog.</param>
                /// <param name="closeAppsMessage">The message prompting the user to close applications.</param>
                /// <param name="expiryMessage">The message indicating the expiration of the dialog or action.</param>
                /// <param name="deferralsRemaining">The message showing the number of deferrals remaining.</param>
                /// <param name="deferralDeadline">The message indicating the deadline for deferrals.</param>
                /// <param name="expiryWarning">The warning message displayed when the expiration is imminent.</param>
                /// <param name="countdownDefer">The message displayed during the countdown for deferring the action.</param>
                /// <param name="countdownClose">The message displayed during the countdown for closing applications.</param>
                /// <param name="buttonClose">The label for the button used to close applications.</param>
                /// <param name="buttonDefer">The label for the button used to defer the action.</param>
                /// <param name="buttonContinue">The label for the button used to continue the process.</param>
                /// <param name="buttonContinueTooltip">The tooltip text for the continue button, providing additional context or instructions.</param>
                [JsonConstructor]
                private CloseAppsDialogClassicStrings(string welcomeMessage, string closeAppsMessage, string expiryMessage, string deferralsRemaining, string deferralDeadline, string expiryWarning, string countdownDefer, string countdownClose, string buttonClose, string buttonDefer, string buttonContinue, string buttonContinueTooltip)
                {
                    // Assign the values.
                    WelcomeMessage = welcomeMessage;
                    CloseAppsMessage = closeAppsMessage;
                    ExpiryMessage = expiryMessage;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ExpiryWarning = expiryWarning;
                    CountdownDefer = countdownDefer;
                    CountdownClose = countdownClose;
                    ButtonClose = buttonClose;
                    ButtonDefer = buttonDefer;
                    ButtonContinue = buttonContinue;
                    ButtonContinueTooltip = buttonContinueTooltip;
                }

                /// <summary>
                /// Text displayed when only the deferral dialog is to be displayed and there are no applications to close
                /// </summary>
                [JsonProperty]
                public readonly string WelcomeMessage;

                /// <summary>
                /// Text displayed when prompting to close running programs.
                /// </summary>
                [JsonProperty]
                public readonly string CloseAppsMessage;

                /// <summary>
                /// Text displayed when a deferral option is available.
                /// </summary>
                [JsonProperty]
                public readonly string ExpiryMessage;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [JsonProperty]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [JsonProperty]
                public readonly string DeferralDeadline;

                /// <summary>
                /// Text displayed after the deferral options.
                /// </summary>
                [JsonProperty]
                public readonly string ExpiryWarning;

                /// <summary>
                /// The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                /// </summary>
                [JsonProperty]
                public readonly string CountdownDefer;

                /// <summary>
                /// Text displayed when counting down to automatically closing applications.
                /// </summary>
                [JsonProperty]
                public readonly string CountdownClose;

                /// <summary>
                /// Text displayed on the close button when prompting to close running programs.
                /// </summary>
                [JsonProperty]
                public readonly string ButtonClose;

                /// <summary>
                /// Text displayed on the defer button when prompting to close running programs
                /// </summary>
                [JsonProperty]
                public readonly string ButtonDefer;

                /// <summary>
                /// Text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [JsonProperty]
                public readonly string ButtonContinue;

                /// <summary>
                /// Tooltip text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [JsonProperty]
                public readonly string ButtonContinueTooltip;
            }

            /// <summary>
            /// Strings used for the Fluent CloseAppsDialog.
            /// </summary>
            public sealed record CloseAppsDialogFluentStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogFluentStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                internal CloseAppsDialogFluentStrings(Hashtable strings, DeploymentType deploymentType)
                {
                    // Nothing here is allowed to be null.
                    if (strings["DialogMessage"] is not Hashtable dialogMessageTable || dialogMessageTable[deploymentType.ToString()] is not string dialogMessage || string.IsNullOrWhiteSpace(dialogMessage))
                    {
                        throw new ArgumentNullException("DialogMessage value is null or invalid.", (Exception?)null);
                    }
                    if (strings["DialogMessageNoProcesses"] is not Hashtable dialogMessageNoProcessesTable || dialogMessageNoProcessesTable[deploymentType.ToString()] is not string dialogMessageNoProcesses || string.IsNullOrWhiteSpace(dialogMessageNoProcesses))
                    {
                        throw new ArgumentNullException("DialogMessageNoProcesses value is null or invalid.", (Exception?)null);
                    }
                    if (strings["AutomaticStartCountdown"] is not string automaticStartCountdown || string.IsNullOrWhiteSpace(automaticStartCountdown))
                    {
                        throw new ArgumentNullException("AutomaticStartCountdown value is null or invalid.", (Exception?)null);
                    }
                    if (strings["DeferralsRemaining"] is not string deferralsRemaining || string.IsNullOrWhiteSpace(deferralsRemaining))
                    {
                        throw new ArgumentNullException("DeferralsRemaining value is null or invalid.", (Exception?)null);
                    }
                    if (strings["DeferralDeadline"] is not string deferralDeadline || string.IsNullOrWhiteSpace(deferralDeadline))
                    {
                        throw new ArgumentNullException("DeferralDeadline value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonLeftText"] is not Hashtable buttonLeftTextTable || buttonLeftTextTable[deploymentType.ToString()] is not string buttonLeftText || string.IsNullOrWhiteSpace(buttonLeftText))
                    {
                        throw new ArgumentNullException("ButtonLeftText value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonRightText"] is not string buttonRightText || string.IsNullOrWhiteSpace(buttonRightText))
                    {
                        throw new ArgumentNullException("ButtonRightText value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonLeftNoProcessesText"] is not Hashtable buttonLeftNoProcessesTextTable || buttonLeftNoProcessesTextTable[deploymentType.ToString()] is not string buttonLeftNoProcessesText || string.IsNullOrWhiteSpace(buttonLeftNoProcessesText))
                    {
                        throw new ArgumentNullException("ButtonLeftNoProcessesText value is null or invalid.", (Exception?)null);
                    }

                    // The hashtable was correctly defined, assign the remaining values.
                    DialogMessage = dialogMessage;
                    DialogMessageNoProcesses = dialogMessageNoProcesses;
                    AutomaticStartCountdown = automaticStartCountdown;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ButtonLeftText = buttonLeftText;
                    ButtonRightText = buttonRightText;
                    ButtonLeftTextNoProcesses = buttonLeftNoProcessesText;
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogFluentStrings"/> class with the
                /// specified dialog text and button labels.
                /// </summary>
                /// <remarks>This constructor is marked with the <see
                /// cref="JsonConstructorAttribute"/> to enable deserialization of the object from JSON. It is intended
                /// for internal use and should not be called directly by external code.</remarks>
                /// <param name="dialogMessage">The message displayed in the dialog when processes are detected.</param>
                /// <param name="dialogMessageNoProcesses">The message displayed in the dialog when no processes are detected.</param>
                /// <param name="automaticStartCountdown">The text representing the countdown timer for automatic start.</param>
                /// <param name="deferralsRemaining">The text indicating the number of deferrals remaining.</param>
                /// <param name="deferralDeadline">The text representing the deadline for deferrals.</param>
                /// <param name="buttonLeftText">The text displayed on the left button when processes are detected.</param>
                /// <param name="buttonRightText">The text displayed on the right button.</param>
                /// <param name="buttonLeftNoProcessesText">The text displayed on the left button when no processes are detected.</param>
                [JsonConstructor]
                private CloseAppsDialogFluentStrings(string dialogMessage, string dialogMessageNoProcesses, string automaticStartCountdown, string deferralsRemaining, string deferralDeadline, string buttonLeftText, string buttonRightText, string buttonLeftNoProcessesText)
                {
                    // Assign the values.
                    DialogMessage = dialogMessage;
                    DialogMessageNoProcesses = dialogMessageNoProcesses;
                    AutomaticStartCountdown = automaticStartCountdown;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ButtonLeftText = buttonLeftText;
                    ButtonRightText = buttonRightText;
                    ButtonLeftTextNoProcesses = buttonLeftNoProcessesText;
                }

                /// <summary>
                /// This is a message to prompt users to save their work.
                /// </summary>
                [JsonProperty]
                public readonly string DialogMessage;

                /// <summary>
                /// This is a message to when there are no running processes available.
                /// </summary>
                [JsonProperty]
                public readonly string DialogMessageNoProcesses;

                /// <summary>
                /// A string to describe the automatic start countdown.
                /// </summary>
                [JsonProperty]
                public readonly string AutomaticStartCountdown;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [JsonProperty]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [JsonProperty]
                public readonly string DeferralDeadline;

                /// <summary>
                /// This is a phrase used to describe the process of deferring a deploymen
                /// </summary>
                [JsonProperty]
                public readonly string ButtonLeftText;

                /// <summary>
                /// This is a phrase used to describe the process of closing applications and commencing the deployment.
                /// </summary>
                [JsonProperty]
                public readonly string ButtonRightText;

                /// <summary>
                /// This is a phrase used to describe the process of commencing the deployment.
                /// </summary>
                [JsonProperty]
                public readonly string ButtonLeftTextNoProcesses;
            }
        }
    }
}
