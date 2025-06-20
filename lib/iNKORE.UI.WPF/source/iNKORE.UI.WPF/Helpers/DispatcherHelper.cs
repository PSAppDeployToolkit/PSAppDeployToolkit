using System;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Threading;

namespace iNKORE.UI.WPF.Helpers
{
    /// <summary>
    /// This class provides static methods helper for executing code in UI thread of the main window.
    /// </summary>
    public static class DispatcherHelper
    {
        public static void DoEvents(DispatcherPriority priority = DispatcherPriority.Background)
        {
            DispatcherFrame frame = new DispatcherFrame();
            Dispatcher.CurrentDispatcher.BeginInvoke(
                priority,
                new DispatcherOperationCallback(ExitFrame),
                frame);
            Dispatcher.PushFrame(frame);
        }

        private static object ExitFrame(object f)
        {
            ((DispatcherFrame)f).Continue = false;

            return null;
        }

        public static void RunOnMainThread(Action action)
        {
            _ = ExecuteOnUIThreadAsync(action);
        }

        public static void RunOnUIThread(this DispatcherObject d, Action action)
        {
            _ = d.ExecuteOnUIThreadAsync(action);
        }

        public static void RunOnUIThread(this Dispatcher dispatcher, Action action)
        {
            _ = dispatcher.AwaitableRunAsync(action);
        }

        /// <summary>
        /// Executes the given function on the main view's UI thread.
        /// </summary>
        /// <param name="function">Synchronous function to be executed on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task ExecuteOnUIThreadAsync(Action function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Application.Current?.ExecuteOnUIThreadAsync(function, priority);
        }

        /// <summary>
        /// Executes the given function on the main view's UI thread and returns its result.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="function">Synchronous function to be executed on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> ExecuteOnUIThreadAsync<T>(Func<T> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Application.Current?.ExecuteOnUIThreadAsync(function, priority);
        }

        /// <summary>
        /// Executes the given <see cref="Task"/>-returning function on the main view's UI thread and returns either that <see cref="Task"/>
        /// or a proxy <see cref="Task"/> that completes when the one produced by the given function completes.
        /// </summary>
        /// <param name="function">Asynchronous function to be executed asynchronously on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        public static Task ExecuteOnUIThreadAsync(Func<Task> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Application.Current?.ExecuteOnUIThreadAsync(function, priority);
        }

        /// <summary>
        /// Executes the given <see cref="Task{TResult}"/>-returning function on the main view's UI thread and returns either that <see cref="Task{TResult}"/>
        /// or a proxy <see cref="Task{TResult}"/> that completes when the one produced by the given function completes.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="function">Asynchronous function to be executed asynchronously on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        public static Task<T> ExecuteOnUIThreadAsync<T>(Func<Task<T>> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            return Application.Current?.ExecuteOnUIThreadAsync(function, priority);
        }

        /// <summary>
        /// Executes the given function on a given view's UI thread.
        /// </summary>
        /// <param name="viewToExecuteOn">View for the <paramref name="function"/> to be executed on.</param>
        /// <param name="function">Synchronous function to be executed on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task ExecuteOnUIThreadAsync(this DispatcherObject viewToExecuteOn, Action function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (viewToExecuteOn is null)
            {
                throw new ArgumentNullException(nameof(viewToExecuteOn));
            }

            return GetDispatcher(viewToExecuteOn).AwaitableRunAsync(function, priority);
        }

        /// <summary>
        /// Executes the given function on a given view's UI thread.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="viewToExecuteOn">View for the <paramref name="function"/> to be executed on.</param>
        /// <param name="function">Synchronous function with return type <typeparamref name="T"/> to be executed on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> ExecuteOnUIThreadAsync<T>(this DispatcherObject viewToExecuteOn, Func<T> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (viewToExecuteOn is null)
            {
                throw new ArgumentNullException(nameof(viewToExecuteOn));
            }

            return GetDispatcher(viewToExecuteOn).AwaitableRunAsync(function, priority);
        }

        public static Dispatcher GetDispatcher(DispatcherObject viewToExecuteOn)
        {
            return viewToExecuteOn?.Dispatcher ?? Dispatcher.CurrentDispatcher;
        }

        /// <summary>
        /// Executes the given function on a given view's UI thread.
        /// </summary>
        /// <param name="viewToExecuteOn">View for the <paramref name="function"/> to be executed on.</param>
        /// <param name="function">Asynchronous function to be executed asynchronously on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task ExecuteOnUIThreadAsync(this DispatcherObject viewToExecuteOn, Func<Task> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (viewToExecuteOn is null)
            {
                throw new ArgumentNullException(nameof(viewToExecuteOn));
            }

            return GetDispatcher(viewToExecuteOn).AwaitableRunAsync(function, priority);
        }

        /// <summary>
        /// Executes the given function on a given view's UI thread.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="viewToExecuteOn">View for the <paramref name="function"/>  to be executed on.</param>
        /// <param name="function">Asynchronous function to be executed asynchronously on UI thread.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> ExecuteOnUIThreadAsync<T>(this DispatcherObject viewToExecuteOn, Func<Task<T>> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (viewToExecuteOn is null)
            {
                throw new ArgumentNullException(nameof(viewToExecuteOn));
            }

            return GetDispatcher(viewToExecuteOn).AwaitableRunAsync(function, priority);
        }

        /// <summary>
        /// Extension method for <see cref="Dispatcher"/>. Offering an actual awaitable <see cref="Task"/> with optional result that will be executed on the given dispatcher.
        /// </summary>
        /// <param name="dispatcher">Dispatcher of a thread to run <paramref name="function"/>.</param>
        /// <param name="function"> Function to be executed on the given dispatcher.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task AwaitableRunAsync(this Dispatcher dispatcher, Action function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            /* Run the function directly when we have thread access.
             * Also reuse Task.CompletedTask in case of success,
             * to skip an unnecessary heap allocation for every invocation. */
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

        /// <summary>
        /// Extension method for <see cref="Dispatcher"/>. Offering an actual awaitable <see cref="Task{T}"/> with optional result that will be executed on the given dispatcher.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="dispatcher">Dispatcher of a thread to run <paramref name="function"/>.</param>
        /// <param name="function"> Function to be executed on the given dispatcher.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> AwaitableRunAsync<T>(this Dispatcher dispatcher, Func<T> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            // Skip the dispatch, if possible
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

        /// <summary>
        /// Extension method for <see cref="Dispatcher"/>. Offering an actual awaitable <see cref="Task"/> with optional result that will be executed on the given dispatcher.
        /// </summary>
        /// <param name="dispatcher">Dispatcher of a thread to run <paramref name="function"/>.</param>
        /// <param name="function">Asynchronous function to be executed on the given dispatcher.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task AwaitableRunAsync(this Dispatcher dispatcher, Func<Task> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            /* If we have thread access, we can retrieve the task directly.
             * We don't use ConfigureAwait(false) in this case, in order
             * to let the caller continue its execution on the same thread
             * after awaiting the task returned by this function. */
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
                        taskCompletionSource.SetException(new InvalidOperationException("The Task returned by function cannot be null."));
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }, priority);

            return taskCompletionSource.Task;
        }

        /// <summary>
        /// Extension method for <see cref="Dispatcher"/>. Offering an actual awaitable <see cref="Task{T}"/> with optional result that will be executed on the given dispatcher.
        /// </summary>
        /// <typeparam name="T">Returned data type of the function.</typeparam>
        /// <param name="dispatcher">Dispatcher of a thread to run <paramref name="function"/>.</param>
        /// <param name="function">Asynchronous function to be executed asynchronously on the given dispatcher.</param>
        /// <param name="priority">Dispatcher execution priority, default is normal.</param>
        /// <returns>An awaitable <see cref="Task{T}"/> for the operation.</returns>
        /// <remarks>If the current thread has UI access, <paramref name="function"/> will be invoked directly.</remarks>
        public static Task<T> AwaitableRunAsync<T>(this Dispatcher dispatcher, Func<Task<T>> function, DispatcherPriority priority = DispatcherPriority.Normal)
        {
            if (function is null)
            {
                throw new ArgumentNullException(nameof(function));
            }

            // Skip the dispatch, if possible
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
                        taskCompletionSource.SetException(new InvalidOperationException("The Task returned by function cannot be null."));
                    }
                }
                catch (Exception e)
                {
                    taskCompletionSource.SetException(e);
                }
            }, priority);

            return taskCompletionSource.Task;
        }
    }
}
