using System;
using System.Collections;
using System.Globalization;
using PSAppDeployToolkit.SessionManagement;
using System.Text.Json.Serialization;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    public sealed record CloseAppsDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class.
        /// </summary>
        /// <param name="deploymentType"></param>
        /// <param name="options"></param>
        public CloseAppsDialogOptions(DeploymentType deploymentType, Hashtable options) : this(
            (options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] is string appTitle ? appTitle : string.Empty,
            options["Subtitle"] is string subtitle ? subtitle : string.Empty,
            options["AppIconImage"] is string appIconImage ? appIconImage : string.Empty,
            options["AppIconDarkImage"] is string appIconDarkImage ? appIconDarkImage : string.Empty,
            options["AppBannerImage"] is string appBannerImage ? appBannerImage : string.Empty,
            options["AppTaskbarIconImage"] is string appTaskbarIconImage ? appTaskbarIconImage : null,
            options["DialogTopMost"] is bool dialogTopMost && dialogTopMost,
            options["Language"] is CultureInfo language ? language : null!,
            options["FluentAccentColor"] is int fluentAccentColor ? fluentAccentColor : null,
            options["DialogPosition"] is DialogPosition dialogPosition ? dialogPosition : null,
            options["DialogAllowMove"] is bool dialogAllowMove ? dialogAllowMove : null,
            options["DialogExpiryDuration"] is TimeSpan dialogExpiryDuration ? dialogExpiryDuration : null,
            options["DialogPersistInterval"] is TimeSpan dialogPersistInterval ? dialogPersistInterval : null,
            options["Strings"] is Hashtable strings && strings.Count > 0 ? new CloseAppsDialogStrings(strings, deploymentType) : null!,
            options["DeferralsRemaining"] is uint deferralsRemaining ? deferralsRemaining : null,
            options["DeferralDeadline"] is DateTime deferralDeadline ? deferralDeadline : null,
            options["UnlimitedDeferrals"] is bool unlimitedDeferrals && unlimitedDeferrals,
            options["ContinueOnProcessClosure"] is bool continueOnProcessClosure && continueOnProcessClosure,
            options["CountdownDuration"] is TimeSpan countdownDuration ? countdownDuration : null,
            options["ForcedCountdown"] is bool forcedCountdown && forcedCountdown,
            options["HideCloseButton"] is bool hideCloseButton && hideCloseButton,
            options["DialogAllowMinimize"] is bool dialogAllowMinimize && dialogAllowMinimize,
            options["CustomMessageText"] is string customMessageText && !string.IsNullOrWhiteSpace(customMessageText) ? customMessageText : null)
        {
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
        /// <param name="appTaskbarIconImage">The path or URI to the application's tray icon image used in the dialog. If <see langword="null"/>,
        /// the default tray icon is used.</param>
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
        /// <param name="dialogAllowMinimize">A value indicating whether the dialog can be minimized by the user.</param>
        /// <param name="customMessageText">Custom text displayed in the dialog. If <see langword="null"/>, no custom message is shown.</param>
        [JsonConstructor]
        private CloseAppsDialogOptions(string appTitle, string subtitle, string appIconImage, string appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, DialogPosition? dialogPosition, bool? dialogAllowMove, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, CloseAppsDialogStrings strings, uint? deferralsRemaining, DateTime? deferralDeadline, bool unlimitedDeferrals, bool continueOnProcessClosure, TimeSpan? countdownDuration, bool forcedCountdown, bool hideCloseButton, bool dialogAllowMinimize, string? customMessageText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, dialogPosition, dialogAllowMove, dialogExpiryDuration, dialogPersistInterval)
        {
            Strings = strings ?? throw new ArgumentNullException(nameof(strings), "Strings value is null or invalid.");
            DeferralsRemaining = deferralsRemaining;
            DeferralDeadline = deferralDeadline;
            UnlimitedDeferrals = unlimitedDeferrals;
            ContinueOnProcessClosure = continueOnProcessClosure;
            CountdownDuration = countdownDuration;
            ForcedCountdown = forcedCountdown;
            HideCloseButton = hideCloseButton;
            DialogAllowMinimize = dialogAllowMinimize;
            CustomMessageText = customMessageText;
        }

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        public CloseAppsDialogStrings Strings { get; }

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        public uint? DeferralsRemaining { get; }

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        public DateTime? DeferralDeadline { get; }

        /// <summary>
        /// Indicates whether the system allows an unlimited number of deferrals.
        /// </summary>
        public bool UnlimitedDeferrals { get; }

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        public bool ContinueOnProcessClosure { get; }

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        public TimeSpan? CountdownDuration { get; }

        /// <summary>
        /// Specifies whether the countdown is "forced" or not (affects countdown decisions).
        /// </summary>
        public bool ForcedCountdown { get; }

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        public bool HideCloseButton { get; }

        /// <summary>
        /// Indicates whether the dialog allows minimizing.
        /// </summary>
        public bool DialogAllowMinimize { get; }

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        public string? CustomMessageText { get; }

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        public sealed record CloseAppsDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CloseAppsDialogStrings"/> class.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            internal CloseAppsDialogStrings(Hashtable strings, DeploymentType deploymentType) : this(
                strings["Classic"] is Hashtable classicStrings ? new CloseAppsDialogClassicStrings(classicStrings, deploymentType) : null!,
                strings["Fluent"] is Hashtable fluentStrings ? new CloseAppsDialogFluentStrings(fluentStrings, deploymentType) : null!)
            {
            }

            /// <summary>
            /// Initializes a new instance of the <see cref="CloseAppsDialogStrings"/> class with the specified classic
            /// and fluent dialog strings.
            /// </summary>
            /// <param name="classic">The strings used for the classic dialog style. Cannot be <see langword="null"/>.</param>
            /// <param name="fluent">The strings used for the fluent dialog style. Cannot be <see langword="null"/>.</param>
            /// <exception cref="ArgumentNullException">Thrown if <paramref name="classic"/> or <paramref name="fluent"/> is <see
            /// langword="null"/>.</exception>
            [JsonConstructor]
            private CloseAppsDialogStrings(CloseAppsDialogClassicStrings classic, CloseAppsDialogFluentStrings fluent)
            {
                Classic = classic ?? throw new ArgumentNullException(nameof(classic), "Classic strings cannot be null.");
                Fluent = fluent ?? throw new ArgumentNullException(nameof(fluent), "Fluent strings cannot be null.");
            }

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            public CloseAppsDialogClassicStrings Classic { get; }

            /// <summary>
            /// The strings used for the Fluent CloseAppsDialog.
            /// </summary>
            public CloseAppsDialogFluentStrings Fluent { get; }

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
                internal CloseAppsDialogClassicStrings(Hashtable strings, DeploymentType deploymentType) : this(
                    strings["WelcomeMessage"] is Hashtable welcomeMessageTable && welcomeMessageTable[deploymentType.ToString()] is string welcomeMessage ? welcomeMessage : string.Empty,
                    strings["CloseAppsMessage"] is Hashtable closeAppsMessageTable && closeAppsMessageTable[deploymentType.ToString()] is string closeAppsMessage ? closeAppsMessage : string.Empty,
                    strings["ExpiryMessage"] is Hashtable expiryMessageTable && expiryMessageTable[deploymentType.ToString()] is string expiryMessage ? expiryMessage : string.Empty,
                    strings["DeferralsRemaining"] is string deferralsRemaining ? deferralsRemaining : string.Empty,
                    strings["DeferralDeadline"] is string deferralDeadline ? deferralDeadline : string.Empty,
                    strings["ExpiryWarning"] is string expiryWarning ? expiryWarning : string.Empty,
                    strings["CountdownDefer"] is Hashtable countdownDeferTable && countdownDeferTable[deploymentType.ToString()] is string countdownDefer ? countdownDefer : string.Empty,
                    strings["CountdownClose"] is Hashtable countdownCloseTable && countdownCloseTable[deploymentType.ToString()] is string countdownClose ? countdownClose : string.Empty,
                    strings["ButtonClose"] is string buttonClose ? buttonClose : string.Empty,
                    strings["ButtonDefer"] is string buttonDefer ? buttonDefer : string.Empty,
                    strings["ButtonContinue"] is string buttonContinue ? buttonContinue : string.Empty,
                    strings["ButtonContinueTooltip"] is string buttonContinueTooltip ? buttonContinueTooltip : string.Empty)
                {
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
                    if (string.IsNullOrWhiteSpace(welcomeMessage))
                    {
                        throw new ArgumentNullException(nameof(welcomeMessage), "WelcomeMessage value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(closeAppsMessage))
                    {
                        throw new ArgumentNullException(nameof(closeAppsMessage), "CloseAppsMessage value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(expiryMessage))
                    {
                        throw new ArgumentNullException(nameof(expiryMessage), "ExpiryMessage value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(deferralsRemaining))
                    {
                        throw new ArgumentNullException(nameof(deferralsRemaining), "DeferralsRemaining value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(deferralDeadline))
                    {
                        throw new ArgumentNullException(nameof(deferralDeadline), "DeferralDeadline value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(expiryWarning))
                    {
                        throw new ArgumentNullException(nameof(expiryWarning), "ExpiryWarning value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(countdownDefer))
                    {
                        throw new ArgumentNullException(nameof(countdownDefer), "CountdownDefer value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(countdownClose))
                    {
                        throw new ArgumentNullException(nameof(countdownClose), "CountdownClose value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonClose))
                    {
                        throw new ArgumentNullException(nameof(buttonClose), "ButtonClose value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonDefer))
                    {
                        throw new ArgumentNullException(nameof(buttonDefer), "ButtonDefer value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonContinue))
                    {
                        throw new ArgumentNullException(nameof(buttonContinue), "ButtonContinue value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonContinueTooltip))
                    {
                        throw new ArgumentNullException(nameof(buttonContinueTooltip), "ButtonContinueTooltip value is null or invalid.");
                    }

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
                public string WelcomeMessage { get; }

                /// <summary>
                /// Text displayed when prompting to close running programs.
                /// </summary>
                public string CloseAppsMessage { get; }

                /// <summary>
                /// Text displayed when a deferral option is available.
                /// </summary>
                public string ExpiryMessage { get; }

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                public string DeferralsRemaining { get; }

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                public string DeferralDeadline { get; }

                /// <summary>
                /// Text displayed after the deferral options.
                /// </summary>
                public string ExpiryWarning { get; }

                /// <summary>
                /// The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                /// </summary>
                public string CountdownDefer { get; }

                /// <summary>
                /// Text displayed when counting down to automatically closing applications.
                /// </summary>
                public string CountdownClose { get; }

                /// <summary>
                /// Text displayed on the close button when prompting to close running programs.
                /// </summary>
                public string ButtonClose { get; }

                /// <summary>
                /// Text displayed on the defer button when prompting to close running programs
                /// </summary>
                public string ButtonDefer { get; }

                /// <summary>
                /// Text displayed on the continue button when prompting to close running programs.
                /// </summary>
                public string ButtonContinue { get; }

                /// <summary>
                /// Tooltip text displayed on the continue button when prompting to close running programs.
                /// </summary>
                public string ButtonContinueTooltip { get; }
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
                internal CloseAppsDialogFluentStrings(Hashtable strings, DeploymentType deploymentType) : this(
                    strings["DialogMessage"] is Hashtable dialogMessageTable && dialogMessageTable[deploymentType.ToString()] is string dialogMessage ? dialogMessage : string.Empty,
                    strings["DialogMessageNoProcesses"] is Hashtable dialogMessageNoProcessesTable && dialogMessageNoProcessesTable[deploymentType.ToString()] is string dialogMessageNoProcesses ? dialogMessageNoProcesses : string.Empty,
                    strings["AutomaticStartCountdown"] is string automaticStartCountdown ? automaticStartCountdown : string.Empty,
                    strings["DeferralsRemaining"] is string deferralsRemaining ? deferralsRemaining : string.Empty,
                    strings["DeferralDeadline"] is string deferralDeadline ? deferralDeadline : string.Empty,
                    strings["ButtonLeftText"] is Hashtable buttonLeftTextTable && buttonLeftTextTable[deploymentType.ToString()] is string buttonLeftText ? buttonLeftText : string.Empty,
                    strings["ButtonRightText"] is string buttonRightText ? buttonRightText : string.Empty,
                    strings["ButtonLeftNoProcessesText"] is Hashtable buttonLeftNoProcessesTextTable && buttonLeftNoProcessesTextTable[deploymentType.ToString()] is string buttonLeftNoProcessesText ? buttonLeftNoProcessesText : string.Empty)
                {
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
                /// <param name="buttonLeftTextNoProcesses">The text displayed on the left button when no processes are detected.</param>
                [JsonConstructor]
                private CloseAppsDialogFluentStrings(string dialogMessage, string dialogMessageNoProcesses, string automaticStartCountdown, string deferralsRemaining, string deferralDeadline, string buttonLeftText, string buttonRightText, string buttonLeftTextNoProcesses)
                {
                    if (string.IsNullOrWhiteSpace(dialogMessage))
                    {
                        throw new ArgumentNullException(nameof(dialogMessage), "DialogMessage value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(dialogMessageNoProcesses))
                    {
                        throw new ArgumentNullException(nameof(dialogMessageNoProcesses), "DialogMessageNoProcesses value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(automaticStartCountdown))
                    {
                        throw new ArgumentNullException(nameof(automaticStartCountdown), "AutomaticStartCountdown value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(deferralsRemaining))
                    {
                        throw new ArgumentNullException(nameof(deferralsRemaining), "DeferralsRemaining value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(deferralDeadline))
                    {
                        throw new ArgumentNullException(nameof(deferralDeadline), "DeferralDeadline value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonLeftText))
                    {
                        throw new ArgumentNullException(nameof(buttonLeftText), "ButtonLeftText value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonRightText))
                    {
                        throw new ArgumentNullException(nameof(buttonRightText), "ButtonRightText value is null or invalid.");
                    }
                    if (string.IsNullOrWhiteSpace(buttonLeftTextNoProcesses))
                    {
                        throw new ArgumentNullException(nameof(buttonLeftTextNoProcesses), "ButtonLeftNoProcessesText value is null or invalid.");
                    }

                    DialogMessage = dialogMessage;
                    DialogMessageNoProcesses = dialogMessageNoProcesses;
                    AutomaticStartCountdown = automaticStartCountdown;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ButtonLeftText = buttonLeftText;
                    ButtonRightText = buttonRightText;
                    ButtonLeftTextNoProcesses = buttonLeftTextNoProcesses;
                }

                /// <summary>
                /// This is a message to prompt users to save their work.
                /// </summary>
                public string DialogMessage { get; }

                /// <summary>
                /// This is a message to when there are no running processes available.
                /// </summary>
                public string DialogMessageNoProcesses { get; }

                /// <summary>
                /// A string to describe the automatic start countdown.
                /// </summary>
                public string AutomaticStartCountdown { get; }

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                public string DeferralsRemaining { get; }

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                public string DeferralDeadline { get; }

                /// <summary>
                /// This is a phrase used to describe the process of deferring a deploymen
                /// </summary>
                public string ButtonLeftText { get; }

                /// <summary>
                /// This is a phrase used to describe the process of closing applications and commencing the deployment.
                /// </summary>
                public string ButtonRightText { get; }

                /// <summary>
                /// This is a phrase used to describe the process of commencing the deployment.
                /// </summary>
                public string ButtonLeftTextNoProcesses { get; }
            }
        }
    }
}
