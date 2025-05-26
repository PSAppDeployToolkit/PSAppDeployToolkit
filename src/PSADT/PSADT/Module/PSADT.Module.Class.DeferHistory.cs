using System;

namespace PSADT.Module
{
    public sealed record DeferHistory(uint? DeferTimesRemaining, DateTime? DeferDeadline, DateTime? DeferRunIntervalLastTime);
}
