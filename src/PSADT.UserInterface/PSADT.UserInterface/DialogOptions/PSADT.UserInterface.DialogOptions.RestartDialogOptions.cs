using System;
using System.Collections;

namespace PSADT.UserInterface.DialogOptions
{
    /// <summary>
    /// Options for the RestartDialog.
    /// </summary>
    public sealed class RestartDialogOptions : BaseOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="RestartDialogOptions"/> class.
        /// </summary>
        /// <param name="options"></param>
        public RestartDialogOptions(Hashtable options) : base(options)
        {
            // Nothing here is allowed to be null.
            if (options["RestartMessageText"] is not string restartMessageText || string.IsNullOrWhiteSpace(restartMessageText))
            {
                throw new ArgumentNullException("RestartMessageText cannot be null.", (Exception?)null);
            }
            if (options["DismissButtonText"] is not string dismissButtonText || string.IsNullOrWhiteSpace(dismissButtonText))
            {
                throw new ArgumentNullException("DismissButtonText cannot be null.", (Exception?)null);
            }
            if (options["RestartButtonText"] is not string restartButtonText || string.IsNullOrWhiteSpace(restartButtonText))
            {
                throw new ArgumentNullException("RestartButtonText cannot be null.", (Exception?)null);
            }
            if (options["CountdownRestartMessageText"] is not string countdownRestartMessageText || string.IsNullOrWhiteSpace(countdownRestartMessageText))
            {
                throw new ArgumentNullException("CountdownRestartMessageText cannot be null.", (Exception?)null);
            }
            if (options["CountdownAutomaticRestartText"] is not string countdownAutomaticRestartText || string.IsNullOrWhiteSpace(countdownAutomaticRestartText))
            {
                throw new ArgumentNullException("CountdownAutomaticRestartText cannot be null.", (Exception?)null);
            }

            // The hashtable was correctly defined, so we can assign the values and continue onwards.
            RestartMessageText = restartMessageText;
            DismissButtonText = dismissButtonText;
            RestartButtonText = restartButtonText;
            CountdownRestartMessageText = countdownRestartMessageText;
            CountdownAutomaticRestartText = countdownAutomaticRestartText;
            CountdownDuration = options["CountdownDuration"] as TimeSpan?;
            CountdownNoMinimizeDuration = options["CountdownNoMinimizeDuration"] as TimeSpan?;
            CustomMessageText = options["CustomMessageText"] is string customMessageText && !string.IsNullOrWhiteSpace(customMessageText) ? customMessageText : null;
        }

        /// <summary>
        /// The text to be displayed in the restart message.
        /// </summary>
        public readonly string RestartMessageText;

        /// <summary>
        /// The text for the dismiss button.
        /// </summary>
        public readonly string DismissButtonText;

        /// <summary>
        /// The text for the restart button.
        /// </summary>
        public readonly string RestartButtonText;

        /// <summary>
        /// The text to be displayed in the countdown restart message.
        /// </summary>
        public readonly string CountdownRestartMessageText;

        /// <summary>
        /// The text to be displayed in the countdown automatic restart message.
        /// </summary>
        public readonly string CountdownAutomaticRestartText;

        /// <summary>
        /// The duration for which the countdown will be displayed.
        /// </summary>
        public readonly TimeSpan? CountdownDuration;

        /// <summary>
        /// The duration for which the countdown will be displayed without minimizing the dialog.
        /// </summary>
        public readonly TimeSpan? CountdownNoMinimizeDuration;

        /// <summary>
        /// The text to be displayed in the custom message.
        /// </summary>
        public readonly string? CustomMessageText;
    }
}
