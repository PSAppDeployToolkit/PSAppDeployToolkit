using System.Collections;
using PSADT.UserInterface;

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
            AppsToClose = options["AppsToClose"] as Services.AppProcessInfo[];
            CountdownDuration = options["CountdownDuration"] as TimeSpan?;
            DeferralsRemaining = options["DeferralsRemaining"] as int?;
            DeferralDeadline = options["DeferralDeadline"] as DateTime?;
            CloseAppsMessageText = options["CloseAppsMessageText"] as string;
            AlternativeCloseAppsMessageText = options["AlternativeCloseAppsMessageText"] as string;
            AutomaticStartCountdownText = options["AutomaticStartCountdownText"] as string;
            CustomMessageText = options["CustomMessageText"] as string;
            DeferralsRemainingText = options["DeferralsRemainingText"] as string;
            DeferralDeadlineText = options["DeferralDeadlineText"] as string;
            DeferButtonText = options["DeferButtonText"] as string;
            ContinueButtonText = options["ContinueButtonText"] as string;
            AlternativeContinueButtonText = options["AlternativeContinueButtonText"] as string;
            ProcessEvaluationService = options["ProcessEvaluationService"] as Services.IProcessEvaluationService;
        }

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

        /// <summary>
        /// The text to be displayed when prompting the user to close applications.
        /// </summary>
        public readonly string? CloseAppsMessageText;

        /// <summary>
        /// The alternative text to be displayed when prompting the user to close applications.
        /// </summary>
        public readonly string? AlternativeCloseAppsMessageText;

        /// <summary>
        /// The text to be displayed when showing the countdown before automatic start.
        /// </summary>
        public readonly string? AutomaticStartCountdownText;

        /// <summary>
        /// The custom message text to be displayed in the dialog.
        /// </summary>
        public readonly string? CustomMessageText;

        /// <summary>
        /// The text to be displayed when showing the number of deferrals remaining.
        /// </summary>
        public readonly string? DeferralsRemainingText;

        /// <summary>
        /// The text to be displayed when showing the deferral deadline.
        /// </summary>
        public readonly string? DeferralDeadlineText;

        /// <summary>
        /// The text for the defer button.
        /// </summary>
        public readonly string? DeferButtonText;

        /// <summary>
        /// The text for the continue button.
        /// </summary>
        public readonly string? ContinueButtonText;

        /// <summary>
        /// The alternative text for the continue button.
        /// </summary>
        public readonly string? AlternativeContinueButtonText;

        /// <summary>
        /// The service used to evaluate running processes.
        /// </summary>
        public readonly Services.IProcessEvaluationService? ProcessEvaluationService;
    }
}
