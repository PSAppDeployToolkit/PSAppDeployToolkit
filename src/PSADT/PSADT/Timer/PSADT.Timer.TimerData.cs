using System;
using System.Threading;
using System.Threading.Tasks;

namespace PSADT.Timer
{
    /// <summary>
    /// Represents data related to a specific timer, including its expiration task, cancellation control, and optional expiration action.
    /// </summary>
    public class TimerData
    {
        /// <summary>
        /// Gets or sets the task that represents the expiration action of the timer.
        /// </summary>
        public Task ExpirationTask { get; set; }

        /// <summary>
        /// Gets the <see cref="CancellationTokenSource"/> used to control the timer.
        /// </summary>
        public CancellationTokenSource CancellationTokenSource { get; }

        /// <summary>
        /// Gets or sets a value indicating whether the timer is currently paused.
        /// </summary>
        public bool IsPaused { get; set; }

        /// <summary>
        /// Gets the optional expiration action associated with this timer.
        /// </summary>
        public Func<Task>? ExpirationAction { get; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerData"/> class.
        /// </summary>
        /// <param name="expirationTask">The task that represents the expiration action of the timer.</param>
        /// <param name="cancellationTokenSource">The cancellation token source for controlling the timer.</param>
        /// <param name="expirationAction">The optional action to execute when the timer expires.</param>
        /// <param name="isPaused">Indicates whether the timer is paused.</param>
        public TimerData(Task expirationTask, CancellationTokenSource cancellationTokenSource, Func<Task>? expirationAction = null, bool isPaused = false)
        {
            ExpirationTask = expirationTask ?? throw new ArgumentNullException(nameof(expirationTask));
            CancellationTokenSource = cancellationTokenSource ?? throw new ArgumentNullException(nameof(cancellationTokenSource));
            ExpirationAction = expirationAction;
            IsPaused = isPaused;
        }
    }
}
