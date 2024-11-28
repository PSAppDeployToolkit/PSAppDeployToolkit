namespace PSADT.Timer
{
    /// <summary>
    /// Specifies the strategy used to calculate the delay between timer intervals.
    /// </summary>
    public enum TimerStrategy
    {
        /// <summary>
        /// Uses a fixed delay for the timer. The timer interval remains constant across all iterations.
        /// </summary>
        Fixed,

        /// <summary>
        /// Uses an exponential backoff strategy. The timer interval increases exponentially with each iteration, typically doubling the delay after each expiration.
        /// </summary>
        ExponentialBackoff,

        /// <summary>
        /// Uses a jitter strategy. The timer interval is randomized by a certain percentage (jitter), ensuring the delay varies slightly to avoid synchronization patterns or spikes in resource usage.
        /// </summary>
        Jitter
    }
}
