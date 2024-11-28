using System;

namespace PSADT.Logging.Models
{
    /// <summary>
    /// Implements an exponential backoff strategy for retrying operations.
    /// </summary>
    internal class ExponentialBackoff
    {
        private readonly TimeSpan _initialDelay;
        private readonly TimeSpan _maxDelay;
        private readonly double _factor;
        private int _attempt;

        /// <summary>
        /// Initializes a new instance of the ExponentialBackoff class.
        /// </summary>
        /// <param name="initialDelay">The initial delay between retry attempts.</param>
        /// <param name="maxDelay">The maximum delay between retry attempts.</param>
        /// <param name="factor">The factor by which to increase the delay on each attempt.</param>
        public ExponentialBackoff(TimeSpan initialDelay, TimeSpan maxDelay, double factor)
        {
            _initialDelay = initialDelay;
            _maxDelay = maxDelay;
            _factor = factor;
        }

        /// <summary>
        /// Calculates the next delay based on the current attempt.
        /// </summary>
        /// <returns>The TimeSpan representing the next delay.</returns>
        public TimeSpan NextDelay()
        {
            var delay = TimeSpan.FromTicks((long)(_initialDelay.Ticks * Math.Pow(_factor, _attempt)));
            _attempt++;
            return delay > _maxDelay ? _maxDelay : delay;
        }

        /// <summary>
        /// Resets the backoff attempt counter.
        /// </summary>
        public void Reset()
        {
            _attempt = 0;
        }
    }
}
