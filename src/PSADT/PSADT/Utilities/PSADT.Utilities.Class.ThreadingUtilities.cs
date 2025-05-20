using System.Threading;
using System.Threading.Tasks;

namespace PSADT.Utilities
{
    /// <summary>
    /// Provides utility methods for working with threading and cancellation scenarios.
    /// </summary>
    /// <remarks>This class contains methods designed to simplify common threading tasks, such as waiting for a cancellation signal. It is intended for internal use and is not exposed as part of the public API.</remarks>
    internal static class ThreadingUtilities
    {
        /// <summary>
        /// Waits asynchronously until the specified <see cref="CancellationToken"/> is canceled.
        /// </summary>
        /// <remarks>This method uses an infinite delay that is interrupted only when the provided <paramref name="cancellationToken"/> is canceled. If the token is canceled, the returned task completes successfully. No exceptions are propagated to the caller.</remarks>
        /// <param name="cancellationToken">A <see cref="CancellationToken"/> that signals the request for cancellation.</param>
        /// <returns>A task that completes when the <paramref name="cancellationToken"/> is canceled.</returns>
        internal static async Task WaitForCancellationAsync(CancellationToken cancellationToken)
        {
            try
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, cancellationToken);
            }
            catch (TaskCanceledException)
            {
                return;
            }
        }
    }
}
