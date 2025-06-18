using System;

namespace PSADT.Module
{
    /// <summary>
    /// Represents the history and state of deferred operations, including remaining deferrals, deadlines, and the last
    /// interval execution time.
    /// </summary>
    /// <remarks>This record is used to track the state of deferred operations, such as how many deferrals
    /// remain, the deadline for completing the deferrals,  and the last time the operation was executed within the
    /// defined interval. It is immutable and designed for scenarios where deferred execution  needs to be monitored or
    /// managed.</remarks>
    /// <param name="DeferTimesRemaining">The number of deferrals remaining. A value of <see langword="null"/> indicates that the deferral count is not
    /// applicable or unlimited.</param>
    /// <param name="DeferDeadline">The deadline by which all deferrals must be completed. A value of <see langword="null"/> indicates that no
    /// deadline is set.</param>
    /// <param name="DeferRunIntervalLastTime">The timestamp of the last execution within the defined interval. A value of <see langword="null"/> indicates
    /// that no execution has occurred yet.</param>
    public sealed record DeferHistory(uint? DeferTimesRemaining, DateTime? DeferDeadline, DateTime? DeferRunIntervalLastTime);
}
