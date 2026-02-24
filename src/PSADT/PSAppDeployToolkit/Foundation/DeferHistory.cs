using System;

namespace PSAppDeployToolkit.Foundation
{
    /// <summary>
    /// Represents the deferral history for an operation, including the number of remaining deferrals, the deadline for
    /// completion, and the most recent run interval time.
    /// </summary>
    /// <param name="DeferTimesRemaining">The number of times the operation can still be deferred. Specify null if there is no limit.</param>
    /// <param name="DeferDeadline">The deadline by which the operation must be completed. Specify null if there is no deadline.</param>
    /// <param name="DeferRunIntervalLastTime">The most recent time the defer interval was run. Specify null if the operation has not yet run.</param>
    public sealed record DeferHistory(uint? DeferTimesRemaining, DateTime? DeferDeadline, DateTime? DeferRunIntervalLastTime);
}
