using System;
using System.Threading;
using System.Threading.Tasks;
using System.Management.Automation;

namespace PSADT.Shared
{
    public static class ExecuteMethod
    {
        /// <summary>
        /// Executes an action asynchronously without impersonation.
        /// </summary>
        /// <typeparam name="T">The type of the value to return from the action. If the action does not return a value, use <see cref="object"/>.</typeparam>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>A task representing the asynchronous operation, with the result of the action returned upon completion.</returns>
        public static async Task<T> RunMethodAsync<T>([PowerShellScriptBlock] Func<Task<T>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "The action cannot be null.");
            }

            // Check if the provided method is a PowerShell ScriptBlock and wrap it accordingly
            if (action.Target is ScriptBlock scriptBlock)
            {
                var scriptBlockWrapper = new ScriptBlockWrapper(action, scriptBlock);
                action = scriptBlockWrapper.GetWrappedAsyncFunc<T>();
            }

            // Execute the provided action and return its result
            return await action().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an action asynchronously without impersonation.
        /// </summary>
        /// <param name="action">The asynchronous action to execute.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RunMethodAsync([PowerShellScriptBlock] Func<Task> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "The action cannot be null.");
            }

            // Check if the provided method is a PowerShell ScriptBlock and wrap it accordingly
            if (action.Target is ScriptBlock scriptBlock)
            {
                var scriptBlockWrapper = new ScriptBlockWrapper(action, scriptBlock);
                action = scriptBlockWrapper.GetWrappedAsyncAction();
            }

            // Execute the provided action
            await action().ConfigureAwait(false);
        }

        /// <summary>
        /// Executes an asynchronous action that returns a result within a new thread configured to use the Multi-Threaded Apartment (MTA) model asynchronously, without impersonation.
        /// </summary>
        /// <typeparam name="T">The type of the result returned by the action.</typeparam>
        /// <param name="action">The asynchronous action to execute, which returns a result of type <typeparamref name="T"/>.</param>
        /// <returns>A task representing the asynchronous operation, with the result of the action of type <typeparamref name="T"/>.</returns>
        public static async Task<T> RunMethodWithMTAAsync<T>([PowerShellScriptBlock] Func<Task<T>> action)
        {
            if (action == null)
            {
                throw new ArgumentNullException(nameof(action), "The action cannot be null.");
            }

            Exception? exception = null;
            var taskCompletionSource = new TaskCompletionSource<T>();

            // Check if the provided method is a PowerShell ScriptBlock and wrap it accordingly
            if (action.Target is ScriptBlock scriptBlock)
            {
                var scriptBlockWrapper = new ScriptBlockWrapper(action, scriptBlock);
                action = scriptBlockWrapper.GetWrappedAsyncFunc<T>();
            }

            var thread = new Thread(() =>
            {
                try
                {
                    // Execute the action synchronously in the MTA thread
                    T result = action().GetAwaiter().GetResult();
                    taskCompletionSource.SetResult(result);
                }
                catch (Exception ex)
                {
                    exception = ex;
                    taskCompletionSource.SetException(ex);
                }
            });

            thread.SetApartmentState(ApartmentState.MTA);
            thread.Start();

            T result = await taskCompletionSource.Task.ConfigureAwait(false);

            if (exception != null)
            {
                throw new InvalidOperationException("Execution in MTA thread failed.", exception);
            }

            return result;
        }


        /// <summary>
        /// Executes an asynchronous action within a new thread configured to use the Multi-Threaded Apartment (MTA) model asynchronously, without returning a result, and without impersonation.
        /// </summary>
        /// <param name="action">The asynchronous action to execute. This action does not return a value.</param>
        /// <returns>A task representing the asynchronous operation.</returns>
        public static async Task RunMethodWithMTAAsync([PowerShellScriptBlock] Func<Task> action)
        {
            // Run the action in MTA thread
            await RunMethodWithMTAAsync(async () =>
            {
                await action().ConfigureAwait(false);
                return Task.CompletedTask;
            }).ConfigureAwait(false);
        }

    }
}
