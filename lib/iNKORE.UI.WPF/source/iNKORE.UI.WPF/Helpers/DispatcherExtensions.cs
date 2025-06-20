// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System;
using System.ComponentModel;                 // EditorBrowsableAttribute, BrowsableAttribute
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Threading;

namespace iNKORE.UI.WPF.Helpers
{
    /// <summary>
    /// Helpers for executing code in a <see cref="Dispatcher"/>.
    /// </summary>
    public static class DispatcherExtensions
    {
#if !NETCOREAPP
        /// <summary>
        ///     Executes the specified delegate asynchronously 
        ///     on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action)
        {
            return dispatcher.BeginInvoke(action);
        }

        /// <summary>
        ///     Executes the specified delegate asynchronously 
        ///     on the thread that the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <returns>
        ///     An IAsyncResult object that represents the result of the
        ///     BeginInvoke operation.
        /// </returns>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static DispatcherOperation BeginInvoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
        {
            return dispatcher.BeginInvoke(action, priority);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action)
        {
            dispatcher.Invoke(action);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, DispatcherPriority priority)
        {
            dispatcher.Invoke(action, priority);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="timeout">
        ///     The maximum amount of time to wait for the operation to complete.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, TimeSpan timeout)
        {
            dispatcher.Invoke(action, timeout);
        }

        /// <summary>
        ///     Executes the specified delegate synchronously on the thread that
        ///     the Dispatcher was created on.
        /// </summary>
        /// <param name="dispatcher">
        ///     Dispatcher that executes the specified method
        /// </param>
        /// <param name="action">
        ///     A delegate to a no argument no return type method
        /// </param>
        /// <param name="timeout">
        ///     The maximum amount of time to wait for the operation to complete.
        /// </param>
        /// <param name="priority">
        ///     The priority that determines in what order the specified method
        ///     is invoked relative to the other pending methods in the Dispatcher.
        /// </param>
        /// <remarks>
        ///     This method is now part of the Dispatcher class.
        /// </remarks>
        [Browsable(false), EditorBrowsable(EditorBrowsableState.Never)]
        public static void Invoke(this Dispatcher dispatcher, Action action, TimeSpan timeout, DispatcherPriority priority)
        {
            dispatcher.Invoke(action, timeout, priority);
        }
#endif

        /// <summary>
        /// Invokes a given function on the target <see cref="Dispatcher"/> and returns a
        /// <see cref="Task"/> that completes when the invocation of the function is completed.
        /// </summary>
        /// <param name="dispatcher">The target <see cref="Dispatcher"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Action"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="function"/> is over.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task EnqueueAsync(this Dispatcher dispatcher, Action function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            // Run the function directly when we have thread access.
            // Also reuse Task.CompletedTask in case of success,
            // to skip an unnecessary heap allocation for every invocation.
            if (dispatcher.CheckAccess())
            {
                try
                {
                    function();
#if !NET452
                    return Task.CompletedTask;
#else
                    return new Task(() => { }, default, (TaskCreationOptions)16384);
#endif
                }
                catch (Exception e)
                {
#if !NET452
                    return Task.FromException(e);
#else
                    return new Task(() => throw e);
#endif
                }
            }

            static Task TryEnqueueAsync(Dispatcher dispatcher, Action function, DispatcherPriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<object>();

                _ = dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        function();

                        taskCompletionSource.SetResult(null);
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, priority);

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="Dispatcher"/> and returns a
        /// <see cref="Task{TResult}"/> that completes when the invocation of the function is completed.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="function"/> to relay through the returned <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="dispatcher">The target <see cref="Dispatcher"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that completes when the invocation of <paramref name="function"/> is over.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> EnqueueAsync<T>(this Dispatcher dispatcher, Func<T> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher.CheckAccess())
            {
                try
                {
                    return Task.FromResult(function());
                }
                catch (Exception e)
                {
#if !NET452
                    return Task.FromException<T>(e);
#else
                    return new Task<T>(() => throw e);
#endif
                }
            }

            static Task<T> TryEnqueueAsync(Dispatcher dispatcher, Func<T> function, DispatcherPriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<T>();

                _ = dispatcher.InvokeAsync(() =>
                {
                    try
                    {
                        taskCompletionSource.SetResult(function());
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, priority);

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="Dispatcher"/> and returns a
        /// <see cref="Task"/> that acts as a proxy for the one returned by the given function.
        /// </summary>
        /// <param name="dispatcher">The target <see cref="Dispatcher"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task"/> that acts as a proxy for the one returned by <paramref name="function"/>.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task EnqueueAsync(this Dispatcher dispatcher, Func<Task> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            // If we have thread access, we can retrieve the task directly.
            // We don't use ConfigureAwait(false) in this case, in order
            // to let the caller continue its execution on the same thread
            // after awaiting the task returned by this function.
            if (dispatcher.CheckAccess())
            {
                try
                {
                    if (function() is Task awaitableResult)
                    {
                        return awaitableResult;
                    }
#if !NET452
                    return Task.FromException(new InvalidOperationException("The Task returned by function cannot be null."));
#else
                    return new Task(() => throw new InvalidOperationException("The Task returned by function cannot be null."));
#endif
                }
                catch (Exception e)
                {
#if !NET452
                    return Task.FromException(e);
#else
                    return new Task(() => throw e);
#endif
                }
            }

            static Task TryEnqueueAsync(Dispatcher dispatcher, Func<Task> function, DispatcherPriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<object>();

                _ = dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        if (function() is Task awaitableResult)
                        {
                            await awaitableResult.ConfigureAwait(false);

                            taskCompletionSource.SetResult(null);
                        }
                        else
                        {
                            taskCompletionSource.SetException(GetEnqueueException("The Task returned by function cannot be null."));
                        }
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, priority);

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Invokes a given function on the target <see cref="Dispatcher"/> and returns a
        /// <see cref="Task{TResult}"/> that acts as a proxy for the one returned by the given function.
        /// </summary>
        /// <typeparam name="T">The return type of <paramref name="function"/> to relay through the returned <see cref="Task{TResult}"/>.</typeparam>
        /// <param name="dispatcher">The target <see cref="Dispatcher"/> to invoke the code on.</param>
        /// <param name="function">The <see cref="Func{TResult}"/> to invoke.</param>
        /// <param name="priority">The priority level for the function to invoke.</param>
        /// <returns>A <see cref="Task{TResult}"/> that relays the one returned by <paramref name="function"/>.</returns>
        /// <remarks>If the current thread has access to <paramref name="dispatcher"/>, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> EnqueueAsync<T>(this Dispatcher dispatcher, Func<Task<T>> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (dispatcher.CheckAccess())
            {
                try
                {
                    if (function() is Task<T> awaitableResult)
                    {
                        return awaitableResult;
                    }
#if !NET452
                    return Task.FromException<T>(new InvalidOperationException("The Task returned by function cannot be null."));
#else
                    return new Task<T>(() => throw new InvalidOperationException("The Task returned by function cannot be null."));
#endif
                }
                catch (Exception e)
                {
#if !NET452
                    return Task.FromException<T>(e);
#else
                    return new Task<T>(() => throw e);
#endif
                }
            }

            static Task<T> TryEnqueueAsync(Dispatcher dispatcher, Func<Task<T>> function, DispatcherPriority priority)
            {
                var taskCompletionSource = new TaskCompletionSource<T>();

                _ = dispatcher.InvokeAsync(async () =>
                {
                    try
                    {
                        if (function() is Task<T> awaitableResult)
                        {
                            var result = await awaitableResult.ConfigureAwait(false);

                            taskCompletionSource.SetResult(result);
                        }
                        else
                        {
                            taskCompletionSource.SetException(GetEnqueueException("The Task returned by function cannot be null."));
                        }
                    }
                    catch (Exception e)
                    {
                        taskCompletionSource.SetException(e);
                    }
                }, priority);

                return taskCompletionSource.Task;
            }

            return TryEnqueueAsync(dispatcher, function, priority);
        }

        /// <summary>
        /// Creates an <see cref="InvalidOperationException"/> to return when an enqueue operation fails.
        /// </summary>
        /// <param name="message">The message of the exception.</param>
        /// <returns>An <see cref="InvalidOperationException"/> with a specified message.</returns>
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static InvalidOperationException GetEnqueueException(string message)
        {
            return new InvalidOperationException(message);
        }
    }
}