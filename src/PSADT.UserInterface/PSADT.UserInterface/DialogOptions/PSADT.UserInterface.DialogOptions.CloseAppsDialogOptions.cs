using System;
using System.Collections;
using PSADT.UserInterface.Dialogs;
using PSADT.UserInterface.Services;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    public sealed class CloseAppsDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CloseAppsDialogOptions(Hashtable options, DeploymentType deploymentType) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["Strings"] is not Hashtable strings || strings.Count == 0)
            {
                throw new ArgumentNullException("Strings table value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("AppsToClose"))
            {
                if (options["AppsToClose"] is not Services.AppProcessInfo[] appsToClose)
                {
                    throw new ArgumentOutOfRangeException("AppsToClose value is not valid.", (Exception?)null);
                }
                AppsToClose = appsToClose;
            }
            if (options.ContainsKey("DeferralsRemaining"))
            {
                if (options["DeferralsRemaining"] is not int deferralsRemaining)
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
            if (options.ContainsKey("CustomMessageText"))
            {
                if (options["CustomMessageText"] is not string customMessageText || string.IsNullOrWhiteSpace(customMessageText))
                {
                    throw new ArgumentOutOfRangeException("CustomMessageText value is not valid.", (Exception?)null);
                }
                CustomMessageText = customMessageText;
            }
            if (options.ContainsKey("CountdownDuration"))
            {
                if (options["CountdownDuration"] is not TimeSpan countdownDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownDuration value is not valid.", (Exception?)null);
                }
                CountdownDuration = countdownDuration;
            }

            // The hashtable was correctly defined, assign the remaining values.
            Strings = new CloseAppsDialogStrings(strings, deploymentType);
        }

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        public readonly CloseAppsDialogStrings Strings;

        /// <summary>
        /// The list of applications that should be closed.
        /// </summary>
        public readonly AppProcessInfo[] AppsToClose = [];

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        public readonly int? DeferralsRemaining;

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        public readonly DateTime? DeferralDeadline;

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The custom message text to be displayed in the dialog.
        /// </summary>
        public readonly string? CustomMessageText;

        /// <summary>
        /// The strings used for the CloseAppsDialog.
        /// </summary>
        public sealed class CloseAppsDialogStrings
        {
            /// <summary>
            /// Initializes a new instance of the <see cref="CloseAppsDialogStrings"/> class.
            /// </summary>
            /// <param name="strings"></param>
            /// <param name="deploymentType"></param>
            /// <exception cref="ArgumentNullException"></exception>
            public CloseAppsDialogStrings(Hashtable strings, DeploymentType deploymentType)
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
            public readonly CloseAppsDialogClassicStrings Classic;

            /// <summary>
            /// The strings used for the Fluent CloseAppsDialog.
            /// </summary>
            public readonly CloseAppsDialogFluentStrings Fluent;

            /// <summary>
            /// The strings used for the classic CloseAppsDialog.
            /// </summary>
            public sealed class CloseAppsDialogClassicStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogClassicStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                public CloseAppsDialogClassicStrings(Hashtable strings, DeploymentType deploymentType)
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
                public readonly string WelcomeMessage;

                /// <summary>
                /// Text displayed when prompting to close running programs.
                /// </summary>
                public readonly string CloseAppsMessage;

                /// <summary>
                /// Text displayed when a deferral option is available.
                /// </summary>
                public readonly string ExpiryMessage;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                public readonly string DeferralDeadline;

                /// <summary>
                /// Text displayed after the deferral options.
                /// </summary>
                public readonly string ExpiryWarning;

                /// <summary>
                /// The countdown message displayed at the Welcome Screen to indicate when the deployment will continue if no response from user.
                /// </summary>
                public readonly string CountdownDefer;

                /// <summary>
                /// Text displayed when counting down to automatically closing applications.
                /// </summary>
                public readonly string CountdownClose;

                /// <summary>
                /// Text displayed on the close button when prompting to close running programs.
                /// </summary>
                public readonly string ButtonClose;

                /// <summary>
                /// Text displayed on the defer button when prompting to close running programs
                /// </summary>
                public readonly string ButtonDefer;

                /// <summary>
                /// Text displayed on the continue button when prompting to close running programs.
                /// </summary>
                public readonly string ButtonContinue;

                /// <summary>
                /// Tooltip text displayed on the continue button when prompting to close running programs.
                /// </summary>
                public readonly string ButtonContinueTooltip;
            }

            /// <summary>
            /// Strings used for the Fluent CloseAppsDialog.
            /// </summary>
            public sealed class CloseAppsDialogFluentStrings
            {
                /// <summary>
                /// Initializes a new instance of the <see cref="CloseAppsDialogFluentStrings"/> class.
                /// </summary>
                /// <param name="strings"></param>
                /// <param name="deploymentType"></param>
                /// <exception cref="ArgumentNullException"></exception>
                public CloseAppsDialogFluentStrings(Hashtable strings, DeploymentType deploymentType)
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
                    if (strings["ButtonLeftText"] is not string buttonLeftText || string.IsNullOrWhiteSpace(buttonLeftText))
                    {
                        throw new ArgumentNullException("ButtonLeftText value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonRightText"] is not Hashtable buttonRightTextTable || buttonRightTextTable[deploymentType.ToString()] is not string buttonRightText || string.IsNullOrWhiteSpace(buttonRightText))
                    {
                        throw new ArgumentNullException("ButtonRightText value is null or invalid.", (Exception?)null);
                    }
                    if (strings["ButtonRightNoProcessesText"] is not Hashtable buttonRightNoProcessesTextTable || buttonRightNoProcessesTextTable[deploymentType.ToString()] is not string buttonRightNoProcessesText || string.IsNullOrWhiteSpace(buttonRightNoProcessesText))
                    {
                        throw new ArgumentNullException("ButtonRightNoProcessesText value is null or invalid.", (Exception?)null);
                    }

                    // The hashtable was correctly defined, assign the remaining values.
                    DialogMessage = dialogMessage;
                    DialogMessageNoProcesses = dialogMessageNoProcesses;
                    AutomaticStartCountdown = automaticStartCountdown;
                    DeferralsRemaining = deferralsRemaining;
                    DeferralDeadline = deferralDeadline;
                    ButtonLeftText = buttonLeftText;
                    ButtonRightText = buttonRightText;
                    ButtonRightTextNoProcesses = buttonRightNoProcessesText;
                }

                /// <summary>
                /// This is a message to prompt users to save their work.
                /// </summary>
                public readonly string DialogMessage;

                /// <summary>
                /// This is a message to when there are no running processes available.
                /// </summary>
                public readonly string DialogMessageNoProcesses;

                /// <summary>
                /// A string to describe the automatic start countdown.
                /// </summary>
                public readonly string AutomaticStartCountdown;

                /// <summary>
                /// Text displayed when there are a specific number of deferrals remaining.
                /// </summary>
                public readonly string DeferralsRemaining;

                /// <summary>
                /// Text displayed when there is a specific deferral deadline.
                /// </summary>
                public readonly string DeferralDeadline;

                /// <summary>
                /// This is a phrase used to describe the process of deferring a deploymen
                /// </summary>
                public readonly string ButtonLeftText;

                /// <summary>
                /// This is a phrase used to describe the process of closing applications and commencing the deployment.
                /// </summary>
                public readonly string ButtonRightText;

                /// <summary>
                /// This is a phrase used to describe the process of commencing the deployment.
                /// </summary>
                public readonly string ButtonRightTextNoProcesses;
            }
        }
    }
}
