using System;
using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for the CloseAppsDialog.
    /// </summary>
    public sealed class CloseAppsDialogOptions : DialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="CloseAppsDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public CloseAppsDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["CloseAppsMessageText"] is not string closeAppsMessageText || string.IsNullOrWhiteSpace(closeAppsMessageText))
            {
                throw new ArgumentNullException("CloseAppsMessageText cannot be null.", (Exception?)null);
            }
            if (options["AlternativeCloseAppsMessageText"] is not string alternativeCloseAppsMessageText || string.IsNullOrWhiteSpace(alternativeCloseAppsMessageText))
            {
                throw new ArgumentNullException("AlternativeCloseAppsMessageText cannot be null.", (Exception?)null);
            }
            if (options["DeferralsRemainingText"] is not string deferralsRemainingText || string.IsNullOrWhiteSpace(deferralsRemainingText))
            {
                throw new ArgumentNullException("DeferralsRemainingText cannot be null.", (Exception?)null);
            }
            if (options["DeferralDeadlineText"] is not string deferralDeadlineText || string.IsNullOrWhiteSpace(deferralDeadlineText))
            {
                throw new ArgumentNullException("DeferralDeadlineText cannot be null.", (Exception?)null);
            }
            if (options["AutomaticStartCountdownText"] is not string automaticStartCountdownText || string.IsNullOrWhiteSpace(automaticStartCountdownText))
            {
                throw new ArgumentNullException("AutomaticStartCountdownText cannot be null.", (Exception?)null);
            }
            if (options["DeferButtonText"] is not string deferButtonText || string.IsNullOrWhiteSpace(deferButtonText))
            {
                throw new ArgumentNullException("DeferButtonText cannot be null.", (Exception?)null);
            }
            if (options["ContinueButtonText"] is not string continueButtonText || string.IsNullOrWhiteSpace(continueButtonText))
            {
                throw new ArgumentNullException("ContinueButtonText cannot be null.", (Exception?)null);
            }
            if (options["AlternativeContinueButtonText"] is not string alternativeContinueButtonText || string.IsNullOrWhiteSpace(alternativeContinueButtonText))
            {
                throw new ArgumentNullException("AlternativeContinueButtonText cannot be null.", (Exception?)null);
            }
            if (options["DynamicProcessEvaluation"] is not bool dynamicProcessEvaluation)
            {
                throw new ArgumentNullException("dynamicProcessEvaluation cannot be null.", (Exception?)null);
            }

            // The hashtable was correctly defined, so we can assign the values and continue onwards.
            CloseAppsMessageText = closeAppsMessageText;
            AlternativeCloseAppsMessageText = alternativeCloseAppsMessageText;
            DeferralsRemainingText = deferralsRemainingText;
            DeferralDeadlineText = deferralDeadlineText;
            AutomaticStartCountdownText = automaticStartCountdownText;
            DeferButtonText = deferButtonText;
            ContinueButtonText = continueButtonText;
            AlternativeContinueButtonText = alternativeContinueButtonText;
            DynamicProcessEvaluation = dynamicProcessEvaluation;
            AppsToClose = options["AppsToClose"] as Services.AppProcessInfo[];
            CountdownDuration = options["CountdownDuration"] as TimeSpan?;
            DeferralsRemaining = options["DeferralsRemaining"] as int?;
            DeferralDeadline = options["DeferralDeadline"] as DateTime?;
            CustomMessageText = options["CustomMessageText"] is string customMessageText && !string.IsNullOrWhiteSpace(customMessageText) ? customMessageText : null;
        }

        /// <summary>
        /// The text to be displayed when prompting the user to close applications.
        /// </summary>
        public readonly string CloseAppsMessageText;

        /// <summary>
        /// The alternative text to be displayed when prompting the user to close applications.
        /// </summary>
        public readonly string AlternativeCloseAppsMessageText;

        /// <summary>
        /// The text to be displayed when showing the number of deferrals remaining.
        /// </summary>
        public readonly string DeferralsRemainingText;

        /// <summary>
        /// The text to be displayed when showing the deferral deadline.
        /// </summary>
        public readonly string DeferralDeadlineText;

        /// <summary>
        /// The text to be displayed when showing the countdown before automatic start.
        /// </summary>
        public readonly string AutomaticStartCountdownText;

        /// <summary>
        /// The text for the defer button.
        /// </summary>
        public readonly string DeferButtonText;

        /// <summary>
        /// The text for the continue button.
        /// </summary>
        public readonly string ContinueButtonText;

        /// <summary>
        /// The alternative text for the continue button.
        /// </summary>
        public readonly string AlternativeContinueButtonText;

        /// <summary>
        /// The custom message text to be displayed in the dialog.
        /// </summary>
        public readonly string? CustomMessageText;

        /// <summary>
        /// The service used to evaluate running processes.
        /// </summary>
        public readonly bool DynamicProcessEvaluation;

        /// <summary>
        /// The list of applications that should be closed.
        /// </summary>
        public readonly Services.AppProcessInfo[]? AppsToClose;

        /// <summary>
        /// The duration of the countdown before the dialog automatically closes.
        /// </summary>
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The number of deferrals remaining for the user.
        /// </summary>
        public readonly int? DeferralsRemaining;

        /// <summary>
        /// The deadline for deferrals.
        /// </summary>
        public readonly DateTime? DeferralDeadline;
    }
}
