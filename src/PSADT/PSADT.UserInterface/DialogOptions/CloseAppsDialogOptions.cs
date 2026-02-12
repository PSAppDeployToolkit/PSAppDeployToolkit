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
    public sealed record CloseAppsDialogOptions : BaseDialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class.
        /// </summary>
        /// <param name="deploymentType"></param>
        /// <param name="options"></param>
        public CloseAppsDialogOptions(DeploymentType deploymentType, Hashtable options) : this(
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
            options["DialogPersistInterval"] as TimeSpan?,
            options["Strings"] as Hashtable is { Count: > 0 } strings ? new CloseAppsDialogStrings(strings, deploymentType) : null!,
            options["DeferralsRemaining"] as uint?,
            options["DeferralDeadline"] as DateTime?,
            options["UnlimitedDeferrals"] as bool? ?? false,
            options["ContinueOnProcessClosure"] as bool? ?? false,
            options["CountdownDuration"] as TimeSpan?,
            options["ForcedCountdown"] as bool? ?? false,
            options["HideCloseButton"] as bool? ?? false,
            options["DialogAllowMinimize"] as bool? ?? false,
            options["CustomMessageText"] as string is { Length: > 0 } customMessageText ? customMessageText : null)
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
            CustomMessageText = !string.IsNullOrWhiteSpace(customMessageText) ? customMessageText : null;
        }


        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly CloseAppsDialogStrings Strings;

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly uint? DeferralsRemaining;

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly DateTime? DeferralDeadline;

        /// <summary>
        /// Indicates whether the system allows an unlimited number of deferrals.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool UnlimitedDeferrals;

        /// <summary>
        /// Indicates whether the continue button should be implied when all processes have closed.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool ContinueOnProcessClosure;

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// Specifies whether the countdown is "forced" or not (affects countdown decisions).
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool ForcedCountdown;

        /// <summary>
        /// Indicates whether the close button should be hidden.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool HideCloseButton;

        /// <summary>
        /// Indicates whether the dialog allows minimizing.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly bool DialogAllowMinimize;

        /// <summary>
        /// Represents a custom message text that can be optionally provided.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
        [DataMember]
        public readonly string? CustomMessageText;

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1034:Nested types should not be visible", Justification = "The nesting in this case is alright.")]
        [DataContract]
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
            private CloseAppsDialogStrings(CloseAppsDialogClassicStrings classic, CloseAppsDialogFluentStrings fluent)
            {
                Classic = classic ?? throw new ArgumentNullException(nameof(classic), "Classic strings cannot be null.");
                Fluent = fluent ?? throw new ArgumentNullException(nameof(fluent), "Fluent strings cannot be null.");
            }

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
            [DataMember]
            public readonly CloseAppsDialogClassicStrings Classic;

            /// <summary>
            /// The strings used for the Fluent CloseAppsDialog.
            /// </summary>
            [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
            [DataMember]
            public readonly CloseAppsDialogFluentStrings Fluent;

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            [DataContract]
            public sealed record CloseAppsDialogClassicStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogClassicStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                internal CloseAppsDialogClassicStrings(Hashtable strings, DeploymentType deploymentType) : this(
                    ((Hashtable?)strings["WelcomeMessage"])?[deploymentType.ToString()] as string ?? null!,
                    ((Hashtable?)strings["CloseAppsMessage"])?[deploymentType.ToString()] as string ?? null!,
                    ((Hashtable?)strings["ExpiryMessage"])?[deploymentType.ToString()] as string ?? null!,
                    strings["DeferralsRemaining"] as string ?? null!,
                    strings["DeferralDeadline"] as string ?? null!,
                    strings["ExpiryWarning"] as string ?? null!,
                    ((Hashtable?)strings["CountdownDefer"])?[deploymentType.ToString()] as string ?? null!,
                    ((Hashtable?)strings["CountdownClose"])?[deploymentType.ToString()] as string ?? null!,
                    strings["ButtonClose"] as string ?? null!,
                    strings["ButtonDefer"] as string ?? null!,
                    strings["ButtonContinue"] as string ?? null!,
                    strings["ButtonContinueTooltip"] as string ?? null!)
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
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string WelcomeMessage;

                /// <summary>
                /// Text displayed when prompting to close running programs.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string CloseAppsMessage;

                /// <summary>
                /// Text displayed when a deferral option is available.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ExpiryMessage;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DeferralDeadline;

                /// <summary>
                /// Text displayed after the deferral options.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ExpiryWarning;

                /// <summary>
                /// The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string CountdownDefer;

                /// <summary>
                /// Text displayed when counting down to automatically closing applications.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string CountdownClose;

                /// <summary>
                /// Text displayed on the close button when prompting to close running programs.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonClose;

                /// <summary>
                /// Text displayed on the defer button when prompting to close running programs
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonDefer;

                /// <summary>
                /// Text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonContinue;

                /// <summary>
                /// Tooltip text displayed on the continue button when prompting to close running programs.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonContinueTooltip;
            }

            /// <summary>
            /// Strings used for the Fluent CloseAppsDialog.
            /// </summary>
            [DataContract]
            public sealed record CloseAppsDialogFluentStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogFluentStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                internal CloseAppsDialogFluentStrings(Hashtable strings, DeploymentType deploymentType) : this(
                    ((Hashtable?)strings["DialogMessage"])?[deploymentType.ToString()] as string ?? null!,
                    ((Hashtable?)strings["DialogMessageNoProcesses"])?[deploymentType.ToString()] as string ?? null!,
                    strings["AutomaticStartCountdown"] as string ?? null!,
                    strings["DeferralsRemaining"] as string ?? null!,
                    strings["DeferralDeadline"] as string ?? null!,
                    ((Hashtable?)strings["ButtonLeftText"])?[deploymentType.ToString()] as string ?? null!,
                    strings["ButtonRightText"] as string ?? null!,
                    ((Hashtable?)strings["ButtonLeftNoProcessesText"])?[deploymentType.ToString()] as string ?? null!)
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
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DialogMessage;

                /// <summary>
                /// This is a message to when there are no running processes available.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DialogMessageNoProcesses;

                /// <summary>
                /// A string to describe the automatic start countdown.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string AutomaticStartCountdown;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string DeferralDeadline;

                /// <summary>
                /// This is a phrase used to describe the process of deferring a deploymen
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonLeftText;

                /// <summary>
                /// This is a phrase used to describe the process of closing applications and commencing the deployment.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonRightText;

                /// <summary>
                /// This is a phrase used to describe the process of commencing the deployment.
                /// </summary>
                [System.Diagnostics.CodeAnalysis.SuppressMessage("Design", "CA1051:Do not declare visible instance fields", Justification = "This needs to be a field for the DataContractSerializer.")]
                [DataMember]
                public readonly string ButtonLeftTextNoProcesses;
            }
        }
    }
}
