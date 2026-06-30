using System;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the deferral history for an operation, including the number of remaining deferrals, the deadline for
    /// completion, and the most recent run interval time.
    /// </summary>
    /// <param name="DeferTimesRemaining">The number of times the operation can still be deferred. Specify null if there is no limit.</param>
    /// <param name="DeferDeadline">The deadline by which the operation must be completed. Specify null if there is no deadline.</param>
    /// <param name="DeferRunIntervalLastTime">The date and time when the defer run interval was last executed. Specify null if the operation has not been
    /// run yet.</param>
    public sealed record class DeferHistory(uint? DeferTimesRemaining, DateTime? DeferDeadline, DateTime? DeferRunIntervalLastTime)
    {
        /// <summary>
        /// Gets the number of times the operation can be deferred before it must be completed.
        /// </summary>
        public uint? DeferTimesRemaining { get; } = DeferTimesRemaining;

        /// <summary>
        /// Gets the deadline by which the deferred operation must be completed, if one is set.
        /// </summary>
        public DateTime? DeferDeadline { get; } = DeferDeadline;

        /// <summary>
        /// Gets the date and time when the defer run interval was last recorded, or null if it has not been set.
        /// </summary>
        public DateTime? DeferRunIntervalLastTime { get; } = DeferRunIntervalLastTime;
    }
}
