using System.Collections;

namespace PSADT.UserInterface.Dialogs
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    public sealed class RestartDialogOptions : DialogOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public RestartDialogOptions(Hashtable options) : base(options)
        {
            RestartMessageText = options["RestartMessageText"] as string;
            CountdownDuration = options["CountdownDuration"] as TimeSpan?;
            CountdownNoMinimizeDuration = options["CountdownNoMinimizeDuration"] as TimeSpan?;
            CountdownRestartMessageText = options["CountdownRestartMessageText"] as string;
            CountdownAutomaticRestartText = options["CountdownAutomaticRestartText"] as string;
            DismissButtonText = options["DismissButtonText"] as string;
            RestartButtonText = options["RestartButtonText"] as string;
            CustomMessageText = options["CustomMessageText"] as string;
        }

        /// <summary>
        /// The text to be displayed in the restart message.
        /// </summary>
        public readonly string? RestartMessageText;

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        public readonly TimeSpan? CountdownNoMinimizeDuration;

        /// <summary>
        /// The text to be displayed in the countdown restart message.
        /// </summary>
        public readonly string? CountdownRestartMessageText;

        /// <summary>
        /// The text to be displayed in the countdown automatic restart message.
        /// </summary>
        public readonly string? CountdownAutomaticRestartText;

        /// <summary>
        /// The text for the dismiss button.
        /// </summary>
        public readonly string? DismissButtonText;

        /// <summary>
        /// The text for the restart button.
        /// </summary>
        public readonly string? RestartButtonText;

        /// <summary>
        /// The text to be displayed in the custom message.
        /// </summary>
        public readonly string? CustomMessageText;
    }
}
