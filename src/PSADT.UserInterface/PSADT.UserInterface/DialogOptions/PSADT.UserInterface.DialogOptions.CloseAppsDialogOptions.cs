using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.Serialization;
using PSADT.Module;
using PSADT.ProcessManagement;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    [DataContract]
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
            if (options.ContainsKey("RunningProcessService"))
            {
                if (options["RunningProcessService"] is not RunningProcessService runningProcessService)
                {
                    throw new ArgumentOutOfRangeException("RunningProcessService value is not valid.", (Exception?)null);
                }
                RunningProcessService = runningProcessService;
            }
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
            if (options.ContainsKey("MinimizeWindows"))
            {
                if (options["MinimizeWindows"] is not bool minimiseWindows)
                {
                    throw new ArgumentOutOfRangeException("MinimizeWindows value is not valid.", (Exception?)null);
                }
                MinimizeWindows = minimiseWindows;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Strings = new CloseAppsDialogStrings(strings, deploymentType);
        }

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [DataMember]
        public readonly CloseAppsDialogStrings Strings;

        /// <summary>
        /// The list of applications that should be closed.
        /// </summary>
        [DataMember]
        public readonly RunningProcessService? RunningProcessService;

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
        public readonly bool UnlimitedDeferrals;

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
        /// Indicates whether the dialog should minimize all other windows when it is displayed.
        /// </summary>
        [DataMember]
        public readonly bool MinimizeWindows;

        /// <summary>
        /// The countdown timer used to track the time remaining before the dialog closes automatically.
        /// </summary>
        [DataMember]
        public readonly Stopwatch CountdownStopwatch = new();

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        [DataContract]
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
                Classic = new CloseAppsDialogClassicStrings(classicStrings, deploymentType);
                Fluent = new CloseAppsDialogFluentStrings(fluentStrings, deploymentType);
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
                    if (strings["CountdownClose"] is not string countdownClose || string.IsNullOrWhiteSpace(countdownClose))
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
                    if (strings["DialogMessage"] is not string dialogMessage || string.IsNullOrWhiteSpace(dialogMessage))
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
