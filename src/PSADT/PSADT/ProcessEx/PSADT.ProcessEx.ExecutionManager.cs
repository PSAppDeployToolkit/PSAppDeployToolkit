using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using PSADT.Logging;

namespace PSADT.ProcessEx
{
    /// <summary>
    /// Helps with management and monitoring of multiple processes across different user sessions.
    /// </summary>
    public class ExecutionManager : IDisposable
    {
        private readonly List<ManagedProcess> _processes = new List<ManagedProcess>();
        private bool _disposed;

        /// <summary>
        /// Gets a read-only list of all managed processes.
        /// </summary>
        public IReadOnlyList<ManagedProcess> Processes => _processes.AsReadOnly();

        /// <summary>
        /// Creates a new ManagedProcess object and adds it to the managed collection.
        /// </summary>
        /// <param name="sessionInfo">The session information for the process.</param>
        /// <returns>A new ManagedProcess object.</returns>
        public ManagedProcess InitializeManagedProcess(SessionDetails sessionInfo)
        {
            var newProcess = new ManagedProcess(sessionInfo);
            _processes.Add(newProcess);
            return newProcess;
        }

        /// <summary>
        /// Waits for processes to exit based on the specified wait options.
        /// </summary>
        /// <param name="waitOptions">Options specifying how to wait for processes.</param>
        /// <param name="timeout">The maximum time to wait.</param>
        /// <param name="cancellationToken">A token to cancel the wait operation.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains information about the exited processes.</returns>
        public async Task<ExecutionResult> WaitForAllProcessExitsAsync(WaitType waitOptions, TimeSpan timeout, CancellationToken cancellationToken)
        {
            try
            {
                var runningProcesses = _processes.FindAll(p => p.Process != null && !p.HasExited);
                if (runningProcesses.Count == 0)
                    return new ExecutionResult();

                UnifiedLogger.Create().Message($"Waiting for [{runningProcesses.Count}] processes to exit.").Severity(LogLevel.Debug);

                var tasks = runningProcesses.Select(p => WaitForExitAsync(p, cancellationToken)).ToArray();
                var timeoutTask = Task.Delay(timeout, cancellationToken);

                Task completedTask;
                if (waitOptions == WaitType.WaitForAny)
                {
                    completedTask = await Task.WhenAny(Task.WhenAny(tasks), timeoutTask);
                }
                else // WaitForAll
                {
                    completedTask = await Task.WhenAny(Task.WhenAll(tasks), timeoutTask);
                }

                var exitedProcesses = runningProcesses
                    .Where(p => p.HasExited)
                    .Select(p => new ExecutionDetails(
                        String.IsNullOrWhiteSpace(p.Process?.ProcessName) ? String.Empty : $"{p.Process!.ProcessName}.exe",
                        p.ProcessId,
                        p.ExitCode ?? -1,
                        p.SessionInfo.SessionId,
                        p.SessionInfo.Username))
                    .ToList();

                UnifiedLogger.Create().Message($"[{exitedProcesses.Count}] processes have exited.").Severity(LogLevel.Debug);

                return new ExecutionResult(completedTask == timeoutTask, exitedProcesses);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"An error occurred while waiting for processes to exit:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Waits asynchronously for a process to exit.
        /// </summary>
        /// <param name="processInfo">The ManagedProcess containing the process to wait for.</param>
        /// <param name="cancellationToken">A token to cancel the wait operation.</param>
        /// <returns>A task that completes when the process exits.</returns>
        private static Task WaitForExitAsync(ManagedProcess processInfo, CancellationToken cancellationToken)
        {
            var process = processInfo?.Process;

            if (process == null || process.HasExited)
            {
                return Task.CompletedTask;
            }

            var tcs = new TaskCompletionSource<bool>();
            process.EnableRaisingEvents = true;
            process.Exited += (sender, args) => tcs.TrySetResult(true);

            if (process.HasExited)
            {
                tcs.TrySetResult(true);
            }

            cancellationToken.Register(() => tcs.TrySetCanceled());

            UnifiedLogger.Create().Message($"Waiting for process id [{process.Id}] to exit.").Severity(LogLevel.Debug);

            return process.HasExited ? Task.CompletedTask : tcs.Task;
        }

        /// <summary>
        /// Stops all redirection monitors and optionally terminates the processes.
        /// </summary>
        /// <param name="terminateProcesses">If true, terminates all running processes.</param>
        public async Task StopAllRedirectionMonitorsAsync(bool terminateProcesses)
        {
            try
            {
                var tasks = _processes.Select(async process =>
                {
                    await Task.Yield(); // This ensures the lambda is truly asynchronous

                    process.StopRedirectionMonitors();

                    if (terminateProcesses && !process.HasExited)
                    {
                        try
                        {
                            process.Process?.Kill();
                            UnifiedLogger.Create().Message($"Terminated process id [{process.ProcessId}").Severity(LogLevel.Information);
                        }
                        catch (Exception ex)
                        {
                            UnifiedLogger.Create().Message($"Failed to terminate process id [{process.ProcessId}").Error(ex);
                        }
                    }
                }).ToList();

                await Task.WhenAll(tasks);
                UnifiedLogger.Create().Message("All redirection monitors stopped.").Severity(LogLevel.Debug);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"An error occurred while stopping redirection monitors:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Waits for all redirection monitors to complete.
        /// </summary>
        /// <returns>A task that completes when all redirection monitors have finished.</returns>
        public async Task WaitForAllRedirectionMonitorsAsync()
        {
            try
            {
                var tasks = _processes
                    .SelectMany(p => new[] { p.StandardOutputRedirectionTask, p.StandardErrorRedirectionTask })
                    .Where(t => t != null)
                    .Select(t => t!)
                    .ToArray();

                UnifiedLogger.Create().Message($"Waiting for [{tasks.Length}] redirection monitors to complete.").Severity(LogLevel.Debug);

                await Task.WhenAll(tasks);
                UnifiedLogger.Create().Message("All redirection monitors completed.").Severity(LogLevel.Debug);
            }
            catch (Exception ex)
            {
                UnifiedLogger.Create().Message($"An error occurred while waiting for redirection monitors to complete:{Environment.NewLine}{ex.Message}").Error(ex);
                throw;
            }
        }

        /// <summary>
        /// Clears all managed processes and disposes of their resources.
        /// </summary>
        public void Clear()
        {
            foreach (var process in _processes)
            {
                process.Dispose();
            }
            _processes.Clear();
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ExecutionManager"/> class.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Disposes of the resources used by the <see cref="ExecutionManager"/> class.
        /// </summary>
        /// <param name="disposing">Indicates whether the method is called from Dispose() or from a finalizer.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed)
            {
                if (disposing)
                {
                    Clear();
                }
                _disposed = true;
            }
        }

        /// <summary>
        /// Destructor for <see cref="ExecutionManager"/>.
        /// </summary>
        ~ExecutionManager()
        {
            Dispose(false);
        }
    }
}
