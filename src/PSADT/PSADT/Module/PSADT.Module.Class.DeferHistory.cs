using System;

namespace PSADT.Module
{
    public sealed record DeferHistory(int? DeferTimesRemaining, DateTime? DeferDeadline, DateTime? DeferRunIntervalLastTime);
}
