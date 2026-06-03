#if !NET8_0_OR_GREATER
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Threading
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for CancellationTokenSource.CancelAsync on .NET Framework 4.7.2.
    /// </summary>
    internal static class CancellationTokenSourcePolyfills
    {
        /// <summary>
        /// Communicates a request for cancellation asynchronously.
        /// </summary>
        /// <remarks>This polyfill preserves the public contract aspects that are achievable on .NET
        /// Framework 4.7.2: disposed sources return a faulted task and successful cancellation returns a completed task.
        /// The runtime implementation can queue callback execution more precisely using internal state that is not
        /// available through the public API.</remarks>
        /// <param name="cancellationTokenSource">The cancellation token source to cancel.</param>
        /// <returns>A task that completes after cancellation has been requested.</returns>
        public static Task CancelAsync(this CancellationTokenSource cancellationTokenSource)
        {
            ArgumentNullException.ThrowIfNull(cancellationTokenSource);
            try
            {
                cancellationTokenSource.Cancel();
                return Task.CompletedTask;
            }
            catch (Exception ex) when (ex.Message is not null)
            {
                return Task.FromException(ex);
            }
        }
    }
}
#endif
