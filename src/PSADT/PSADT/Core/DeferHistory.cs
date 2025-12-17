using System;

namespace PSADT.Core
{
    /// <summary>
    /// Represents the deferral history for an operation, including the number of remaining deferrals, the deadline for
    /// completion, and the most recent run interval time.
    /// </summary>
    public sealed record DeferHistory
    {
        /// <summary>
        /// Initializes a new instance of the DeferHistory class with the specified defer times remaining, deadline, and
        /// last run interval time.
        /// </summary>
        /// <param name="deferTimesRemaining">The number of times the operation can still be deferred. Specify null if there is no limit.</param>
        /// <param name="deferDeadline">The deadline by which the operation must be completed. Specify null if there is no deadline.</param>
        /// <param name="deferRunIntervalLastTime">The most recent time the defer interval was run. Specify null if the operation has not yet run.</param>
        public DeferHistory(uint? deferTimesRemaining, DateTime? deferDeadline, DateTime? deferRunIntervalLastTime)
        {
            DeferTimesRemaining = deferTimesRemaining;
            DeferDeadline = deferDeadline;
            DeferRunIntervalLastTime = deferRunIntervalLastTime;
        }

        /// <summary>
        /// The amount of deferrals remaining for this deployment.
        /// </summary>
        public uint? DeferTimesRemaining { get; }

        /// <summary>
        /// The deadline date and time for deferrals.
        /// </summary>
        public DateTime? DeferDeadline { get; }

        /// <summary>
        /// The last time the defer run interval was executed.
        /// </summary>
        public DateTime? DeferRunIntervalLastTime { get; }
    }
}
