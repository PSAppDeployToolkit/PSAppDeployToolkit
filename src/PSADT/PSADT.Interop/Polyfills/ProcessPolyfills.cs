#if !NET5_0_OR_GREATER
using System.Threading;
using System.Threading.Tasks;

#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Diagnostics
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for Process.WaitForExitAsync on .NET Framework 4.7.2.
    /// </summary>
    internal static class ProcessPolyfills
    {
        /// <summary>
        /// Instructs the process component to wait for the associated process to exit, or for cancellation.
        /// </summary>
        /// <param name="process">The process to wait on.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel waiting for process exit.</param>
        /// <returns>A task that will complete when the process exits or cancellation is requested.</returns>
        public static async Task WaitForExitAsync(this Process process, CancellationToken cancellationToken = default)
        {
            // Validate provided input and process state.
            ArgumentNullException.ThrowIfNull(process);
            if (!process.HasExited)
            {
                cancellationToken.ThrowIfCancellationRequested();
            }

            // Enable raising events to allow waiting for process exit.
            try
            {
                process.EnableRaisingEvents = true;
            }
            catch (InvalidOperationException) when (process.HasExited)
            {
                return;
            }

            // Set up a TaskCompletionSource to represent the asynchronous wait operation.
            TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            void OnExited(object? __, EventArgs ___)
            {
                _ = tcs.TrySetResult(null);
            }
            EventHandler handler = OnExited;
            process.Exited += handler;
            try
            {
                if (!process.HasExited)
                {
                    using CancellationTokenRegistration ctr = cancellationToken.CanBeCanceled ? cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken)) : default;
                    _ = await tcs.Task.ConfigureAwait(false);
                }
            }
            finally
            {
                process.Exited -= handler;
            }
        }
    }
}
#endif
