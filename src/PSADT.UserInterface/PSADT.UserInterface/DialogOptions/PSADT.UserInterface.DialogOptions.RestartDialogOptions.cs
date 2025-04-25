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
                throw new ArgumentNullException("RestartMessageText value is null or invalid.", (Exception?)null);
            }
            if (options["DismissButtonText"] is not string dismissButtonText || string.IsNullOrWhiteSpace(dismissButtonText))
            {
                throw new ArgumentNullException("DismissButtonText value is null or invalid.", (Exception?)null);
            }
            if (options["RestartButtonText"] is not string restartButtonText || string.IsNullOrWhiteSpace(restartButtonText))
            {
                throw new ArgumentNullException("RestartButtonText value is null or invalid.", (Exception?)null);
            }
            if (options["CountdownRestartMessageText"] is not string countdownRestartMessageText || string.IsNullOrWhiteSpace(countdownRestartMessageText))
            {
                throw new ArgumentNullException("CountdownRestartMessageText value is null or invalid.", (Exception?)null);
            }
            if (options["CountdownAutomaticRestartText"] is not string countdownAutomaticRestartText || string.IsNullOrWhiteSpace(countdownAutomaticRestartText))
            {
                throw new ArgumentNullException("CountdownAutomaticRestartText value is null or invalid.", (Exception?)null);
            }

            // Test and set optional values.
            if (options.ContainsKey("CountdownDuration"))
            {
                if (options["CountdownDuration"] is not TimeSpan countdownDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownDuration value is not valid.", (Exception?)null);
                }
                CountdownDuration = countdownDuration;
            }
            if (options.ContainsKey("CountdownNoMinimizeDuration"))
            {
                if (options["CountdownNoMinimizeDuration"] is not TimeSpan countdownNoMinimizeDuration)
                {
                    throw new ArgumentOutOfRangeException("CountdownNoMinimizeDuration value is not valid.", (Exception?)null);
                }
                CountdownNoMinimizeDuration = countdownNoMinimizeDuration;
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
            RestartMessageText = restartMessageText;
            DismissButtonText = dismissButtonText;
            RestartButtonText = restartButtonText;
            CountdownRestartMessageText = countdownRestartMessageText;
            CountdownAutomaticRestartText = countdownAutomaticRestartText;
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
