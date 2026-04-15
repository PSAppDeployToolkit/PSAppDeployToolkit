#if !NET6_0_OR_GREATER
#pragma warning disable IDE0130 // Namespace does not match folder structure
namespace System.Threading.Tasks
#pragma warning restore IDE0130 // Namespace does not match folder structure
{
    /// <summary>
    /// Polyfills for Task.WaitAsync on .NET Framework 4.7.2.
    /// </summary>
    internal static class TaskPolyfills
    {
        /// <summary>
        /// Asynchronously waits for the task to complete or for cancellation to be requested.
        /// </summary>
        /// <param name="task">The task to wait for.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that completes when the original task completes or cancellation is requested.</returns>
        public static Task WaitAsync(this Task task, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(task);
            return !task.IsCompleted && cancellationToken.CanBeCanceled ? WaitAsyncCore(task, cancellationToken) : task;
        }

        /// <summary>
        /// Asynchronously waits for the task to complete or for cancellation to be requested.
        /// </summary>
        /// <typeparam name="TResult">The task result type.</typeparam>
        /// <param name="task">The task to wait for.</param>
        /// <param name="cancellationToken">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that completes when the original task completes or cancellation is requested.</returns>
        public static Task<TResult> WaitAsync<TResult>(this Task<TResult> task, CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(task);
            return !task.IsCompleted && cancellationToken.CanBeCanceled ? WaitAsyncCore(task, cancellationToken) : task;
        }

        /// <summary>
        /// Asynchronously waits for the specified task to complete or for the cancellation token to be triggered.
        /// </summary>
        /// <remarks>If the cancellation token is triggered before the specified task completes, the
        /// returned task will be canceled.</remarks>
        /// <param name="task">The task to wait for completion.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation.</returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "This WaitAsync polyfill must await the caller-provided task to preserve BCL semantics outside a JoinableTaskFactory context.")]
        private static async Task WaitAsyncCore(Task task, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration ctr = cancellationToken.Register(() =>
            {
                _ = tcs.TrySetCanceled(cancellationToken);
            });
            Task completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
            await completed.ConfigureAwait(false);
        }

        /// <summary>
        /// Waits for the specified task to complete or for the cancellation token to be canceled, and returns the
        /// result of the task.
        /// </summary>
        /// <remarks>If the cancellation token is canceled before the task completes, the returned task
        /// will be canceled. If the task completes before cancellation, its result is returned.</remarks>
        /// <typeparam name="TResult">The type of the result produced by the task.</typeparam>
        /// <param name="task">The task to wait for completion.</param>
        /// <param name="cancellationToken">A cancellation token that can be used to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous wait operation. The result contains the value produced by the
        /// specified task.</returns>
        [Diagnostics.CodeAnalysis.SuppressMessage("Usage", "VSTHRD003:Avoid awaiting foreign Tasks", Justification = "This WaitAsync polyfill must await the caller-provided task to preserve BCL semantics outside a JoinableTaskFactory context.")]
        private static async Task<TResult> WaitAsyncCore<TResult>(Task<TResult> task, CancellationToken cancellationToken)
        {
            TaskCompletionSource<object?> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
            using CancellationTokenRegistration ctr = cancellationToken.Register(() =>
            {
                _ = tcs.TrySetCanceled(cancellationToken);
            });
            Task completed = await Task.WhenAny(task, tcs.Task).ConfigureAwait(false);
            if (ReferenceEquals(completed, task))
            {
                return await task.ConfigureAwait(false);
            }
            await completed.ConfigureAwait(false);
            return await task.ConfigureAwait(false);
        }
    }
}
#endif
