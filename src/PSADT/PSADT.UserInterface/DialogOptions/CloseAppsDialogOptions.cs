using System;
using System.Collections;
using System.Globalization;
using System.Runtime.Serialization;
using PSAppDeployToolkit.Foundation;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    [DataContract]
    public sealed record class CloseAppsDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the CloseAppsDialogOptions class using the specified deployment type and
        /// configuration options.
        /// </summary>
        /// <remarks>The options dictionary must include specific keys to fully configure the dialog. If
        /// certain keys are missing, default values will be used for those settings. Ensure that all required options
        /// are provided to achieve the desired dialog configuration.</remarks>
        /// <param name="deploymentType">The type of deployment for the application, which determines the dialog's behavior and appearance.</param>
        /// <param name="options">A dictionary containing configuration options for the dialog, such as titles, images, localization settings,
        /// and behavioral flags. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when the options dictionary is null.</exception>
        public CloseAppsDialogOptions(DeploymentType deploymentType, IDictionary options) : this(
            (string?)(options ?? throw new ArgumentNullException(nameof(options)))["AppTitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppTitle' is missing."),
            (string?)options["Subtitle"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Subtitle' is missing."),
            (string?)options["AppIconImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppIconImage' is missing."),
            (string?)options["AppIconDarkImage"],
            (string?)options["AppBannerImage"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'AppBannerImage' is missing."),
            (string?)options["AppTaskbarIconImage"],
            (bool?)options["DialogTopMost"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'DialogTopMost' is missing."),
            (CultureInfo?)options["Language"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Language' is missing."),
            (int?)options["FluentAccentColor"],
            (int?)options["FluentAccentColorDark"],
            (DialogPosition?)options["DialogPosition"],
            (bool?)options["DialogAllowMove"],
            (bool?)options["DialogAllowMinimize"],
            (TimeSpan?)options["DialogExpiryDuration"],
            (TimeSpan?)options["DialogPersistInterval"],
            new((IDictionary?)options["Strings"] ?? throw new ArgumentNullException(nameof(options), "The specified key 'Strings' is missing."), deploymentType),
            (uint?)options["DeferralsRemaining"],
            (DateTime?)options["DeferralDeadline"],
            (bool?)options["UnlimitedDeferrals"] ?? false,
            (bool?)options["ContinueOnProcessClosure"] ?? false,
            (TimeSpan?)options["CountdownDuration"],
            (bool?)options["ForcedCountdown"] ?? false,
            (bool?)options["HideCloseButton"] ?? false,
            (string?)options["CustomMessageText"])
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
        /// <param name="fluentAccentColorDark">The accent color used for Fluent design elements in the dialog when in dark mode. If <see langword="null"/>, the default dark mode accent color is used.</param>
        /// <param name="dialogPosition">The position of the dialog on the screen. If <see langword="null"/>, the default position is used.</param>
        /// <param name="dialogAllowMove">Indicates whether the dialog can be moved by the user. If <see langword="null"/>, the default behavior is
        /// used.</param>
        /// <param name="dialogAllowMinimize">Indicates whether the dialog exposes a minimize button in its caption area. If <see langword="null"/> or
        /// <see langword="false"/>, the minimize button remains hidden.</param>
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
        private CloseAppsDialogOptions(string appTitle, string subtitle, string appIconImage, string? appIconDarkImage, string appBannerImage, string? appTaskbarIconImage, bool dialogTopMost, CultureInfo language, int? fluentAccentColor, int? fluentAccentColorDark, DialogPosition? dialogPosition, bool? dialogAllowMove, bool? dialogAllowMinimize, TimeSpan? dialogExpiryDuration, TimeSpan? dialogPersistInterval, CloseAppsDialogStrings strings, uint? deferralsRemaining, DateTime? deferralDeadline, bool unlimitedDeferrals, bool continueOnProcessClosure, TimeSpan? countdownDuration, bool forcedCountdown, bool hideCloseButton, string? customMessageText) : base(appTitle, subtitle, appIconImage, appIconDarkImage, appBannerImage, appTaskbarIconImage, dialogTopMost, language, fluentAccentColor, fluentAccentColorDark, dialogPosition, dialogAllowMove, dialogAllowMinimize, dialogExpiryDuration, dialogPersistInterval)
        {
            if (customMessageText is not null)
            {
                ArgumentException.ThrowIfNullOrWhiteSpace(customMessageText);
            }
            ArgumentNullException.ThrowIfNull(strings);
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
        [DataMember]
        public readonly CloseAppsDialogStrings Strings;

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        [DataMember]
        public readonly uint? DeferralsRemaining;

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        [DataMember]
        public readonly DateTime? DeferralDeadline;

        /// <summary>
        /// Indicates whether the system allows an unlimited number of deferrals.
        /// </summary>
        [DataMember]
        public readonly bool UnlimitedDeferrals;

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        [DataMember]
        public readonly bool ContinueOnProcessClosure;

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        [DataMember]
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// Specifies whether the countdown is "forced" or not (affects countdown decisions).
        /// </summary>
        [DataMember]
        public readonly bool ForcedCountdown;

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        [DataMember]
        public readonly bool HideCloseButton;

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [DataMember]
        public readonly string? CustomMessageText;

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        [DataContract]
        public sealed record class CloseAppsDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the CloseAppsDialogStrings class using localized string resources for
            /// different deployment types.
            /// </summary>
            /// <remarks>If the dictionary does not contain entries for the 'Classic' or 'Fluent'
            /// deployment types, the corresponding string resources will not be initialized.</remarks>
            /// <param name="strings">An IDictionary containing localized string resources for the dialog, organized by deployment type.</param>
            /// <param name="deploymentType">The deployment type that determines which set of localized strings to use.</param>
            internal CloseAppsDialogStrings(IDictionary strings, DeploymentType deploymentType) : this(
                new CloseAppsDialogClassicStrings((IDictionary?)(strings ?? throw new ArgumentNullException(nameof(strings)))["Classic"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'Classic' is missing."), deploymentType),
                new CloseAppsDialogFluentStrings((IDictionary?)strings["Fluent"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'Fluent' is missing."), deploymentType))
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
            private CloseAppsDialogStrings(CloseAppsDialogClassicStrings classic, CloseAppsDialogFluentStrings fluent)
            {
                ArgumentNullException.ThrowIfNull(classic);
                ArgumentNullException.ThrowIfNull(fluent);
                Classic = classic;
                Fluent = fluent;
            }

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            [DataMember]
            public readonly CloseAppsDialogClassicStrings Classic;

            /// <summary>
            /// The strings used for the Fluent CloseAppsDialog.
            /// </summary>
            [DataMember]
            public readonly CloseAppsDialogFluentStrings Fluent;

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            [DataContract]
            public sealed record class CloseAppsDialogClassicStrings
            {
                /// <summary>
                /// Initializes a new instance of the CloseAppsDialogClassicStrings class using localized string values
                /// based on the specified deployment type.
                /// </summary>
                /// <remarks>This constructor retrieves localized strings for dialog messages and
                /// button labels according to the provided deployment type. If a required string is not found for the
                /// specified deployment type, a default value is used. All required keys must be present in the
                /// dictionary to ensure correct dialog behavior.</remarks>
                /// <param name="strings">An IDictionary containing localized string values for various dialog messages and button labels. The
                /// dictionary must include entries for each required message key.</param>
                /// <param name="deploymentType">The deployment type that determines which localized string values are selected from the dictionary.</param>
                internal CloseAppsDialogClassicStrings(IDictionary strings, DeploymentType deploymentType) : this(
                    (string?)((IDictionary?)(strings ?? throw new ArgumentNullException(nameof(strings)))["WelcomeMessage"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'WelcomeMessage' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'WelcomeMessage.{deploymentType}' is missing."),
                    (string?)((IDictionary?)strings["CloseAppsMessage"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'CloseAppsMessage' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'CloseAppsMessage.{deploymentType}' is missing."),
                    (string?)((IDictionary?)strings["ExpiryMessage"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ExpiryMessage' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'ExpiryMessage.{deploymentType}' is missing."),
                    (string?)strings["DeferralsRemaining"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DeferralsRemaining' is missing."),
                    (string?)strings["DeferralDeadline"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DeferralDeadline' is missing."),
                    (string?)strings["ExpiryWarning"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ExpiryWarning' is missing."),
                    (string?)((IDictionary?)strings["CountdownDefer"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'CountdownDefer' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'CountdownDefer.{deploymentType}' is missing."),
                    (string?)((IDictionary?)strings["CountdownClose"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'CountdownClose' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), $"The specified key 'CountdownClose.{deploymentType}' is missing."),
                    (string?)strings["ButtonClose"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonClose' is missing."),
                    (string?)strings["ButtonDefer"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonDefer' is missing."),
                    (string?)strings["ButtonContinue"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonContinue' is missing."),
                    (string?)strings["ButtonContinueTooltip"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonContinueTooltip' is missing."))
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
                private CloseAppsDialogClassicStrings(string welcomeMessage, string closeAppsMessage, string expiryMessage, string deferralsRemaining, string deferralDeadline, string expiryWarning, string countdownDefer, string countdownClose, string buttonClose, string buttonDefer, string buttonContinue, string buttonContinueTooltip)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(welcomeMessage);
                    ArgumentException.ThrowIfNullOrWhiteSpace(closeAppsMessage);
                    ArgumentException.ThrowIfNullOrWhiteSpace(expiryMessage);
                    ArgumentException.ThrowIfNullOrWhiteSpace(deferralsRemaining);
                    ArgumentException.ThrowIfNullOrWhiteSpace(deferralDeadline);
                    ArgumentException.ThrowIfNullOrWhiteSpace(expiryWarning);
                    ArgumentException.ThrowIfNullOrWhiteSpace(countdownDefer);
                    ArgumentException.ThrowIfNullOrWhiteSpace(countdownClose);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonClose);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonDefer);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonContinue);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonContinueTooltip);
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
                [DataMember]
                public readonly string WelcomeMessage;

                /// <summary>
                /// Text displayed when prompting to close running programs.
                /// </summary>
                [DataMember]
                public readonly string CloseAppsMessage;

                /// <summary>
                /// Text displayed when a deferral option is available.
                /// </summary>
                [DataMember]
                public readonly string ExpiryMessage;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [DataMember]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [DataMember]
                public readonly string DeferralDeadline;

                /// <summary>
                /// Text displayed after the deferral options.
                /// </summary>
                [DataMember]
                public readonly string ExpiryWarning;

                /// <summary>
                /// The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                /// </summary>
                [DataMember]
                public readonly string CountdownDefer;

                /// <summary>
                /// Text displayed when counting down to automatically closing applications.
                /// </summary>
                [DataMember]
                public readonly string CountdownClose;

                /// <summary>
                /// Text displayed on the close button when prompting to close running programs.
                /// </summary>
                [DataMember]
                public readonly string ButtonClose;

                /// <summary>
                /// Text displayed on the defer button when prompting to close running programs
                /// </summary>
                [DataMember]
                public readonly string ButtonDefer;

                /// <summary>
                /// Text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [DataMember]
                public readonly string ButtonContinue;

                /// <summary>
                /// Tooltip text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [DataMember]
                public readonly string ButtonContinueTooltip;
            }

            /// <summary>
            /// Strings used for the Fluent CloseAppsDialog.
            /// </summary>
            [DataContract]
            public sealed record class CloseAppsDialogFluentStrings
            {
                /// <summary>
                /// Initializes a new instance of the CloseAppsDialogFluentStrings class with localized string values
                /// based on the provided dictionary and deployment type.
                /// </summary>
                /// <remarks>This constructor retrieves localized strings for dialog messages and
                /// button texts based on the specified deployment type. It is important to ensure that the provided
                /// dictionary contains the necessary keys for the expected deployment type to avoid null reference
                /// exceptions.</remarks>
                /// <param name="strings">An IDictionary containing localized string values used for dialog messages and button texts, indexed
                /// by keys relevant to the dialog's context.</param>
                /// <param name="deploymentType">The DeploymentType that specifies the context for which the dialog messages are localized,
                /// influencing the selection of strings from the provided dictionary.</param>
                internal CloseAppsDialogFluentStrings(IDictionary strings, DeploymentType deploymentType) : this(
                    (string?)((IDictionary?)(strings ?? throw new ArgumentNullException(nameof(strings)))["DialogMessage"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DialogMessage' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DialogMessage' is missing."),
                    (string?)((IDictionary?)strings["DialogMessageNoProcesses"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DialogMessageNoProcesses' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DialogMessageNoProcesses' is missing."),
                    (string?)strings["AutomaticStartCountdown"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'AutomaticStartCountdown' is missing."),
                    (string?)strings["DeferralsRemaining"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DeferralsRemaining' is missing."),
                    (string?)strings["DeferralDeadline"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'DeferralDeadline' is missing."),
                    (string?)((IDictionary?)strings["ButtonLeftText"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonLeftText' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonLeftText' is missing."),
                    (string?)strings["ButtonRightText"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonRightText' is missing."),
                    (string?)((IDictionary?)strings["ButtonLeftNoProcessesText"] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonLeftNoProcessesText' is missing."))[deploymentType.ToString()] ?? throw new ArgumentNullException(nameof(strings), "The specified key 'ButtonLeftNoProcessesText' is missing."))
                {
                }

                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogFluentStrings"/> class with the
                /// specified dialog text and button labels.
                /// </summary>
                /// <remarks>This constructor is intended for internal use and should not be called directly by external code.</remarks>
                /// <param name="dialogMessage">The message displayed in the dialog when processes are detected.</param>
                /// <param name="dialogMessageNoProcesses">The message displayed in the dialog when no processes are detected.</param>
                /// <param name="automaticStartCountdown">The text representing the countdown timer for automatic start.</param>
                /// <param name="deferralsRemaining">The text indicating the number of deferrals remaining.</param>
                /// <param name="deferralDeadline">The text representing the deadline for deferrals.</param>
                /// <param name="buttonLeftText">The text displayed on the left button when processes are detected.</param>
                /// <param name="buttonRightText">The text displayed on the right button.</param>
                /// <param name="buttonLeftTextNoProcesses">The text displayed on the left button when no processes are detected.</param>
                private CloseAppsDialogFluentStrings(string dialogMessage, string dialogMessageNoProcesses, string automaticStartCountdown, string deferralsRemaining, string deferralDeadline, string buttonLeftText, string buttonRightText, string buttonLeftTextNoProcesses)
                {
                    ArgumentException.ThrowIfNullOrWhiteSpace(dialogMessage);
                    ArgumentException.ThrowIfNullOrWhiteSpace(dialogMessageNoProcesses);
                    ArgumentException.ThrowIfNullOrWhiteSpace(automaticStartCountdown);
                    ArgumentException.ThrowIfNullOrWhiteSpace(deferralsRemaining);
                    ArgumentException.ThrowIfNullOrWhiteSpace(deferralDeadline);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonLeftText);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonRightText);
                    ArgumentException.ThrowIfNullOrWhiteSpace(buttonLeftTextNoProcesses);
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
                [DataMember]
                public readonly string DialogMessage;

                /// <summary>
                /// This is a message to when there are no running processes available.
                /// </summary>
                [DataMember]
                public readonly string DialogMessageNoProcesses;

                /// <summary>
                /// A string to describe the automatic start countdown.
                /// </summary>
                [DataMember]
                public readonly string AutomaticStartCountdown;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [DataMember]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [DataMember]
                public readonly string DeferralDeadline;

                /// <summary>
                /// This is a phrase used to describe the process of deferring a deploymen
                /// </summary>
                [DataMember]
                public readonly string ButtonLeftText;

                /// <summary>
                /// This is a phrase used to describe the process of closing applications and commencing the deployment.
                /// </summary>
                [DataMember]
                public readonly string ButtonRightText;

                /// <summary>
                /// This is a phrase used to describe the process of commencing the deployment.
                /// </summary>
                [DataMember]
                public readonly string ButtonLeftTextNoProcesses;
            }
        }
    }
}
