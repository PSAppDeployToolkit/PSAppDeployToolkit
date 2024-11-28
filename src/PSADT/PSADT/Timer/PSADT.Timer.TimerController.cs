using System;
using System.Threading;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Collections.Concurrent;

namespace PSADT.Timer
{
    /// <summary>
    /// Manages multiple concurrent timers and provides mechanisms for handling timer strategies and callbacks upon expiration.
    /// </summary>
    public class TimerController : IDisposable
    {
        private static readonly ConcurrentBag<CancellationTokenSource> CancellationTokenSourcePool = new ConcurrentBag<CancellationTokenSource>();

        private readonly TimeSpan _originalTimeout;
        private readonly TimerStrategy _timerStrategy;
        private readonly double _jitterPercentage;
        private int _backoffMultiplier = 1;

        private readonly ConcurrentDictionary<Guid, TimerData> _activeTimers;
        private readonly int _maxTimers;
        private Stopwatch _stopwatch;
        private bool _disposed;

        /// <summary>
        /// Event triggered when a timer expires. This can be used to execute custom logic upon timer expiration.
        /// </summary>
        public event Func<Task>? TimerExpiredAsync;

        /// <summary>
        /// Initializes a new instance of the <see cref="TimerController"/> class with a specified timeout, timer strategy, and maximum timers.
        /// </summary>
        /// <param name="timeout">The timeout duration for each timer.</param>
        /// <param name="maxTimers">The maximum number of concurrent timers allowed.</param>
        /// <param name="timerStrategy">The strategy for timer delay (e.g., fixed, exponential backoff, jitter).</param>
        /// <param name="jitterPercentage">The jitter percentage applied to timer delays when using the jitter strategy.</param>
        public TimerController(TimeSpan timeout, int maxTimers = 5, TimerStrategy timerStrategy = TimerStrategy.Fixed, double jitterPercentage = 0.1)
        {
            _originalTimeout = timeout;
            _timerStrategy = timerStrategy;
            _jitterPercentage = jitterPercentage;
            _stopwatch = new Stopwatch();
            _maxTimers = maxTimers;

            _activeTimers = new ConcurrentDictionary<Guid, TimerData>();

            _stopwatch.Start();

            // Attach to the process exit event
            AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
        }

        /// <summary>
        /// Handles process exit by cleaning up and optionally executing any remaining timer actions.
        /// </summary>
        private void OnProcessExit(object? sender, EventArgs e)
        {
            foreach (var timer in _activeTimers.Values)
            {
                // Forcefully execute the expiration action if the timer is still active
                if (!timer.CancellationTokenSource.Token.IsCancellationRequested)
                {
                    timer.ExpirationTask = ExecuteExpirationActionAsync(timer.ExpirationAction);
                }
            }

            DisposeAllTimersAsync().Wait();
        }

        /// <summary>
        /// Starts a new timer with an optional expiration action. Supports multiple timers up to the configured limit.
        /// </summary>
        /// <param name="expirationAction">An optional asynchronous action to execute when the timer expires.</param>
        /// <returns>A <see cref="Task{TResult}"/> that returns the unique ID of the started timer.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the maximum timer limit is reached.</exception>
        public async Task<Guid> StartNewTimerAsync(Func<Task>? expirationAction = null)
        {
            if (_activeTimers.Count >= _maxTimers)
            {
                throw new InvalidOperationException($"Maximum timer limit of [{_maxTimers}] reached.");
            }

            var cancellationTokenSource = RentCancellationTokenSource(GetNextDelay());
            var timerId = Guid.NewGuid();

            var expirationTask = MonitorTimerAsync(timerId, cancellationTokenSource, expirationAction);
            var timerData = new TimerData(expirationTask, cancellationTokenSource);
            _activeTimers.TryAdd(timerId, timerData);

            await expirationTask;

            return timerId;
        }

        /// <summary>
        /// Checks if a specific timer is active.
        /// </summary>
        /// <param name="timerId">The ID of the timer to check.</param>
        /// <returns>True if the timer is active, false otherwise.</returns>
        public bool IsTimerActive(Guid timerId)
        {
            return _activeTimers.ContainsKey(timerId);
        }

        /// <summary>
        /// Checks if a specific timer is paused.
        /// </summary>
        /// <param name="timerId">The ID of the timer to check.</param>
        /// <returns>True if the timer is paused, false otherwise.</returns>
        public bool IsTimerPaused(Guid timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerData))
            {
                return timerData.IsPaused;
            }
            else
            {
                throw new InvalidOperationException($"No active timer with ID [{timerId}] found.");
            }
        }

        /// <summary>
        /// Pauses a specific timer.
        /// </summary>
        /// <param name="timerId">The ID of the timer to pause.</param>
        /// <exception cref="InvalidOperationException">Thrown when no active timer with the specified ID is found.</exception>
        public void PauseTimer(Guid timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerData))
            {
                timerData.CancellationTokenSource.Cancel(); // Cancels the current timer, effectively pausing it
                timerData.IsPaused = true;
            }
            else
            {
                throw new InvalidOperationException($"No active timer with ID [{timerId}] found.");
            }
        }

        /// <summary>
        /// Resumes a specific timer.
        /// </summary>
        /// <param name="timerId">The ID of the timer to resume.</param>
        /// <exception cref="InvalidOperationException">Thrown if the timer with the given ID does not exist.</exception>
        public void ResumeTimer(Guid timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerData))
            {
                var remainingTime = GetRemainingTime(timerId);
                var newCancellationTokenSource = RentCancellationTokenSource(remainingTime);
                _activeTimers[timerId] = new TimerData(MonitorTimerAsync(timerId, newCancellationTokenSource, timerData.ExpirationAction), newCancellationTokenSource, timerData.ExpirationAction, false);
            }
            else
            {
                throw new InvalidOperationException($"No active timer with ID [{timerId}] found.");
            }
        }

        /// <summary>
        /// Restarts a specific timer by resetting the timer to the original delay.
        /// </summary>
        /// <param name="timerId">The ID of the timer to restart.</param>
        /// <exception cref="InvalidOperationException">Thrown if the timer with the given ID does not exist.</exception>
        public void RestartTimer(Guid timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerData))
            {
                timerData.CancellationTokenSource.Cancel();
                var newCancellationTokenSource = RentCancellationTokenSource(_originalTimeout);
                _activeTimers[timerId] = new TimerData(MonitorTimerAsync(timerId, newCancellationTokenSource, timerData.ExpirationAction), newCancellationTokenSource, timerData.ExpirationAction, false);
            }
            else
            {
                throw new InvalidOperationException($"No active timer with ID [{timerId}] found.");
            }
        }

        /// <summary>
        /// Gets the elapsed time for a specific timer.
        /// </summary>
        /// <param name="timerId">The unique ID of the timer.</param>
        /// <returns>The elapsed time for the specified timer.</returns>
        public TimeSpan GetElapsedTime(Guid timerId)
        {
            return _originalTimeout - GetRemainingTime(timerId);
        }

        /// <summary>
        /// Monitors a timer and handles its expiration, including executing a custom expiration action if provided.
        /// </summary>
        /// <param name="timerId">The unique ID of the timer.</param>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> associated with the timer.</param>
        /// <param name="expirationAction">An optional asynchronous action to execute when the timer expires.</param>
        private async Task MonitorTimerAsync(Guid timerId, CancellationTokenSource cancellationTokenSource, Func<Task>? expirationAction)
        {
            try
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    TimeSpan delay = GetNextDelay();
                    try
                    {
                        await Task.Delay(delay, cancellationTokenSource.Token);
                    }
                    catch (TaskCanceledException)
                    {
                        return;
                    }

                    await ExecuteExpirationActionAsync(expirationAction);

                    _activeTimers.TryRemove(timerId, out _);
                    ReturnCancellationTokenSource(cancellationTokenSource);
                }
            }
            catch (OperationCanceledException)
            {
                // Timer was canceled, ignore
            }
            finally
            {
                _activeTimers.TryRemove(timerId, out _);
                ReturnCancellationTokenSource(cancellationTokenSource);
            }
        }

        /// <summary>
        /// Executes the expiration action or triggers the TimerExpiredAsync event if no custom action is provided.
        /// </summary>
        /// <param name="expirationAction">The custom expiration action, if provided.</param>
        /// <returns>A task representing the expiration action.</returns>
        private async Task ExecuteExpirationActionAsync(Func<Task>? expirationAction = null)
        {
            if (expirationAction != null)
            {
                await expirationAction();
            }
            else if (TimerExpiredAsync != null)
            {
                await TimerExpiredAsync.Invoke();
            }
        }

        /// <summary>
        /// Gets the remaining time for a specific timer.
        /// </summary>
        /// <param name="timerId">The unique ID of the timer.</param>
        /// <returns>The remaining time for the specified timer as a <see cref="TimeSpan"/>.</returns>
        private TimeSpan GetRemainingTime(Guid timerId)
        {
            if (_activeTimers.TryGetValue(timerId, out var timerData))
            {
                var elapsedTime = _stopwatch.Elapsed; // Time since the timer was created
                var remainingTime = _originalTimeout - elapsedTime;
                return remainingTime < TimeSpan.Zero ? TimeSpan.Zero : remainingTime;
            }

            throw new InvalidOperationException($"No active timer with ID [{timerId}] found.");
        }

        /// <summary>
        /// Gets the next delay interval based on the timer strategy (e.g., fixed, exponential backoff, jitter).
        /// Allows specification of a timerId to use custom strategies per timer.
        /// </summary>
        /// <param name="timerId">Optional unique ID of the timer. If not provided, uses the default strategy.</param>
        /// <returns>The next delay interval as a <see cref="TimeSpan"/>.</returns>
        private TimeSpan GetNextDelay(Guid? timerId = null)
        {
            TimerStrategy strategy = _timerStrategy;

            if (timerId.HasValue && _activeTimers.TryGetValue(timerId.Value, out var timerData))
            {
                // You can extend this to allow custom strategies per timer.
                strategy = _timerStrategy;
            }

            switch (strategy)
            {
                case TimerStrategy.ExponentialBackoff:
                    var backoffDelay = TimeSpan.FromMilliseconds(_originalTimeout.TotalMilliseconds * _backoffMultiplier);
                    _backoffMultiplier *= 2;
                    return backoffDelay;

                case TimerStrategy.Jitter:
                    var jitter = new Random().NextDouble() * _jitterPercentage;
                    var jitteredDelay = _originalTimeout.TotalMilliseconds * (1 + jitter);
                    return TimeSpan.FromMilliseconds(jitteredDelay);

                default:
                    return _originalTimeout;
            }
        }

        /// <summary>
        /// Rents a <see cref="CancellationTokenSource"/> from the pool or creates a new one if none are available.
        /// </summary>
        /// <param name="timeout">The timeout for the <see cref="CancellationTokenSource"/>.</param>
        /// <returns>A <see cref="CancellationTokenSource"/> configured with the specified timeout.</returns>
        private static CancellationTokenSource RentCancellationTokenSource(TimeSpan timeout)
        {
            if (CancellationTokenSourcePool.TryTake(out var cancellationTokenSource))
            {
                cancellationTokenSource.CancelAfter(timeout);
                return cancellationTokenSource;
            }

            return new CancellationTokenSource(timeout);
        }

        /// <summary>
        /// Returns a <see cref="CancellationTokenSource"/> to the pool for reuse.
        /// </summary>
        /// <param name="cancellationTokenSource">The <see cref="CancellationTokenSource"/> to return to the pool.</param>
        private static void ReturnCancellationTokenSource(CancellationTokenSource cancellationTokenSource)
        {
            if (!cancellationTokenSource.IsCancellationRequested)
            {
                cancellationTokenSource.Cancel();
            }
            cancellationTokenSource.Dispose();
            CancellationTokenSourcePool.Add(new CancellationTokenSource());
        }

        /// <summary>
        /// Pauses all active timers.
        /// </summary>
        public void PauseAllTimers()
        {
            foreach (var timer in _activeTimers.Values)
            {
                timer.CancellationTokenSource.Cancel();
                timer.IsPaused = true;
            }
        }

        /// <summary>
        /// Resumes all paused timers.
        /// </summary>
        public void ResumeAllTimers()
        {
            foreach (var timerId in _activeTimers.Keys)
            {
                var timerData = _activeTimers[timerId];
                var cancellationTokenSource = RentCancellationTokenSource(GetRemainingTime(timerId));
                _activeTimers[timerId] = new TimerData(MonitorTimerAsync(timerId, cancellationTokenSource, null), cancellationTokenSource, timerData.ExpirationAction, false);
            }
        }

        /// <summary>
        /// Disposes of the controller, releasing all timers and associated resources.
        /// </summary>
        public void Dispose()
        {
            if (!_disposed)
            {
                PauseAllTimers();
                _disposed = true;
                foreach (var timer in _activeTimers.Values)
                {
                    timer.CancellationTokenSource.Dispose();
                }

                _activeTimers.Clear();
                GC.SuppressFinalize(this);
            }
        }

        /// <summary>
        /// Disposes a specific timer asynchronously.
        /// </summary>
        /// <param name="timerId">The unique ID of the timer to dispose of.</param>
        /// <returns>A task that represents the asynchronous disposal operation.</returns>
        public async Task DisposeTimerAsync(Guid timerId)
        {
            if (_activeTimers.TryRemove(timerId, out var timer))
            {
                timer.CancellationTokenSource.Cancel();
                await timer.ExpirationTask;
                ReturnCancellationTokenSource(timer.CancellationTokenSource);
            }
        }

        /// <summary>
        /// Disposes all timers asynchronously.
        /// </summary>
        /// <returns>A task that represents the asynchronous disposal operation of all timers.</returns>
        public async Task DisposeAllTimersAsync()
        {
            foreach (var timerId in _activeTimers.Keys)
            {
                await DisposeTimerAsync(timerId);
            }
        }
    }
}
