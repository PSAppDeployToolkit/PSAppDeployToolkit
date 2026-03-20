using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.IO.Pipes;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Win32.SafeHandles;
using Windows.Win32.Foundation;
using Windows.Win32.System.Threading;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Provides a container for all data and resources required to launch and manage a new process, including startup
    /// information, handle inheritance, and standard I/O redirection.
    /// </summary>
    /// <remarks>This class encapsulates the configuration and resources needed to create a new process with
    /// specific startup parameters, environment settings, and standard input/output/error redirection. It manages the
    /// lifetime of associated handles and asynchronous I/O tasks, and implements IDisposable to ensure proper cleanup
    /// of both managed and unmanaged resources. Instances of this class are typically used internally to coordinate
    /// process creation and I/O management in scenarios where fine-grained control over process startup and
    /// communication is required.</remarks>
    internal sealed class ProcessLaunchData : IDisposable
    {
        /// <summary>
        /// Initializes a new instance of the ProcessLaunchData class using the specified process launch information.
        /// </summary>
        /// <remarks>This constructor prepares all necessary startup structures and handle inheritance
        /// lists required to launch a new process according to the provided launch information. It configures standard
        /// input, output, and error redirection if requested, and sets process creation flags based on the specified
        /// options. The resulting ProcessLaunchData instance is ready to be used for process creation and
        /// management.</remarks>
        /// <param name="launchInfo">The configuration object containing startup parameters, environment settings, window style, priority class,
        /// standard I/O redirection, and other options for the process to be launched. Cannot be null.</param>
        internal ProcessLaunchData(ProcessLaunchInfo launchInfo)
        {
            // Ensure we dispose of any resources we create if an exception is thrown during initialization.
            try
            {
                // Set up the startup information for the process.
                if (launchInfo.WindowStyle is not null)
                {
                    StartupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                    StartupInfo.wShowWindow = (ushort)launchInfo.WindowStyle.Value;
                }

                // We must create a console window for console apps when the window is shown.
                if (launchInfo.IsCliApplication())
                {
                    if (launchInfo.CreateNoWindow)
                    {
                        // If STARTF_USESHOWWINDOW is set, a console app showing UI elements
                        // won't appear. Because we have CREATE_NO_WINDOW, the console window
                        // (aka. the window we actually want hidden) will be hidden as expected.
                        StartupInfo.dwFlags |= STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES;
                        StartupInfo.dwFlags &= ~STARTUPINFOW_FLAGS.STARTF_USESHOWWINDOW;
                        CreationFlags |= PROCESS_CREATION_FLAGS.CREATE_NO_WINDOW;
                    }
                    else
                    {
                        CreationFlags |= PROCESS_CREATION_FLAGS.CREATE_NEW_CONSOLE;
                    }
                }

                // Set the process priority class if specified.
                if (launchInfo.PriorityClass is not null)
                {
                    CreationFlags |= (PROCESS_CREATION_FLAGS)launchInfo.PriorityClass.Value;
                }

                // Set up required stdio stuff if we're configured to capture these streams.
                List<nint> handlesToInherit = launchInfo.HandlesToInherit.Count > 0 ? [.. launchInfo.HandlesToInherit] : [];
                if ((StartupInfo.dwFlags & STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES) == STARTUPINFOW_FLAGS.STARTF_USESTDHANDLES)
                {
                    StdOutReadStream = new(PipeDirection.In, HandleInheritability.Inheritable);
                    StdErrReadStream = new(PipeDirection.In, HandleInheritability.Inheritable);
                    StdOutReadTask = Task.Run(() => ReadPipe(StdOutReadStream, StdOutData, InterleavedData, launchInfo.StreamEncoding));
                    StdErrReadTask = Task.Run(() => ReadPipe(StdErrReadStream, StdErrData, InterleavedData, launchInfo.StreamEncoding));
                    StartupInfo.hStdOutput = (HANDLE)(StdOutWritePipe = StdOutReadStream.ClientSafePipeHandle).DangerousGetHandle();
                    StartupInfo.hStdError = (HANDLE)(StdErrWritePipe = StdErrReadStream.ClientSafePipeHandle).DangerousGetHandle();
                    handlesToInherit.Add(StartupInfo.hStdOutput);
                    handlesToInherit.Add(StartupInfo.hStdError);
                    if (launchInfo.StandardInput.Count > 0)
                    {
                        StdInWriteStream = new(PipeDirection.Out, HandleInheritability.Inheritable);
                        StdInInputData = [.. launchInfo.StandardInput]; StdIoEncoding = launchInfo.StreamEncoding;
                        StartupInfo.hStdInput = (HANDLE)(StdInReadPipe = StdInWriteStream.ClientSafePipeHandle).DangerousGetHandle();
                        handlesToInherit.Add(StartupInfo.hStdInput);
                    }
                    else
                    {
                        StartupInfo.hStdInput = HANDLE.INVALID_HANDLE_VALUE;
                    }
                }
                else
                {
                    StartupInfo.hStdOutput = HANDLE.INVALID_HANDLE_VALUE;
                    StartupInfo.hStdError = HANDLE.INVALID_HANDLE_VALUE;
                    StartupInfo.hStdInput = HANDLE.INVALID_HANDLE_VALUE;
                }
                HandlesToInherit = new ReadOnlyCollection<nint>(handlesToInherit);
            }
            catch
            {
                Dispose();
                throw;
            }
        }

        /// <summary>
        /// Starts writing standard input data if configured.
        /// </summary>
        /// <remarks>This method is idempotent and intended to be called after the child process has
        /// been resumed.</remarks>
        internal void StartStdInWriteTask()
        {
            if (StdInWriteTaskStarted || StdInWriteStream is null || StdInInputData is null || StdIoEncoding is null)
            {
                return;
            }
            StdInWriteTask = Task.Run(() => WritePipe(StdInWriteStream, StdInInputData, StdIoEncoding));
            StdInWriteTaskStarted = true;
        }

        /// <summary>
        /// Asynchronously waits for the completion of all standard input, output, and error I/O tasks.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token used to cancel waiting for standard I/O completion.</param>
        /// <returns>A task that completes when all standard I/O tasks have finished or waiting is canceled.</returns>
        internal async Task WaitForStdIoTaskCompletionAsync(CancellationToken cancellationToken)
        {
            await Task.WhenAll(StdOutReadTask, StdErrReadTask, StdInWriteTask).WaitAsync(cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Releases the parent-side local copies of inheritable client pipe handles after process creation.
        /// </summary>
        /// <remarks>This method is idempotent and may be called multiple times safely. Releasing these
        /// handles early allows EOF to flow correctly to the parent reader when the child exits.</remarks>
        internal void ReleaseInheritedPipeHandles()
        {
            if (InheritedPipeHandlesReleased)
            {
                return;
            }
            StdOutWritePipe?.Dispose();
            StdErrWritePipe?.Dispose();
            StdInReadPipe?.Dispose();
            StdOutReadStream?.DisposeLocalCopyOfClientHandle();
            StdErrReadStream?.DisposeLocalCopyOfClientHandle();
            StdInWriteStream?.DisposeLocalCopyOfClientHandle();
            InheritedPipeHandlesReleased = true;
        }

        /// <summary>
        /// Reads lines from a pipe stream and records output/interleaved data.
        /// </summary>
        /// <param name="stream">The stream to read from.</param>
        /// <param name="output">The target output collection.</param>
        /// <param name="interleaved">The target interleaved output collection.</param>
        /// <param name="encoding">The stream encoding.</param>
        private static void ReadPipe(AnonymousPipeServerStream stream, List<string> output, ConcurrentQueue<string> interleaved, Encoding encoding)
        {
            using StreamReader reader = new(stream, encoding);
            while (reader.ReadLine()?.TrimEnd() is string line)
            {
                interleaved.Enqueue(line); output.Add(line);
            }
        }

        /// <summary>
        /// Writes input lines to a pipe stream.
        /// </summary>
        /// <param name="stream">The stream to write to.</param>
        /// <param name="input">The lines to write.</param>
        /// <param name="encoding">The stream encoding.</param>
        private static void WritePipe(AnonymousPipeServerStream stream, IReadOnlyList<string> input, Encoding encoding)
        {
            using StreamWriter writer = new(stream, encoding);
            try
            {
                foreach (string line in input)
                {
                    writer.WriteLine(line);
                }
            }
            catch (IOException)
            {
                // The child process didn't read all input before exiting.
                return;
            }
        }

        /// <summary>
        /// Contains the Windows startup information used when creating a new process.
        /// </summary>
        internal readonly STARTUPINFOW StartupInfo = new() { cb = (uint)Marshal.SizeOf<STARTUPINFOW>() };

        /// <summary>
        /// Specifies the flags used to control the behavior of the process creation operation.
        /// </summary>
        internal readonly PROCESS_CREATION_FLAGS CreationFlags = PROCESS_CREATION_FLAGS.CREATE_UNICODE_ENVIRONMENT | PROCESS_CREATION_FLAGS.CREATE_NEW_PROCESS_GROUP | PROCESS_CREATION_FLAGS.CREATE_SUSPENDED;

        /// <summary>
        /// Gets a read-only List containing the lines written to the standard output stream during process execution.
        /// </summary>
        internal IReadOnlyList<string> StdOut => StdOutData;

        /// <summary>
        /// Gets a read-only list containing the lines written to the standard error stream during process execution.
        /// </summary>
        internal IReadOnlyList<string> StdErr => StdErrData;

        /// <summary>
        /// Gets a read-only collection of interleaved standard output and error lines written during process execution.
        /// </summary>
        internal IReadOnlyCollection<string> Interleaved => InterleavedData;

        /// <summary>
        /// Gets the collection of native handles that will be inherited by a child process when it is created.
        /// </summary>
        internal readonly IReadOnlyList<nint> HandlesToInherit;

        /// <summary>
        /// Represents the task responsible for reading from the standard output stream.
        /// </summary>
        private readonly Task StdOutReadTask = Task.CompletedTask;

        /// <summary>
        /// Represents the anonymous pipe server stream used to read standard output from a client process.
        /// </summary>
        private readonly AnonymousPipeServerStream? StdOutReadStream;

        /// <summary>
        /// Represents the write handle for the standard output pipe used in inter-process communication.
        /// </summary>
        private readonly SafePipeHandle? StdOutWritePipe;

        /// <summary>
        /// Represents the task responsible for reading from the standard error stream asynchronously.
        /// </summary>
        private readonly Task StdErrReadTask = Task.CompletedTask;

        /// <summary>
        /// Represents the anonymous pipe server stream used to read standard error output from a client process.
        /// </summary>
        private readonly AnonymousPipeServerStream? StdErrReadStream;

        /// <summary>
        /// Represents the write handle for the standard error stream of a named pipe, if available.
        /// </summary>
        private readonly SafePipeHandle? StdErrWritePipe;

        /// <summary>
        /// Represents the current asynchronous operation for writing to the standard input stream.
        /// </summary>
        private Task StdInWriteTask = Task.CompletedTask;

        /// <summary>
        /// Represents the anonymous pipe server stream used for writing to the standard input of a client process.
        /// </summary>
        private readonly AnonymousPipeServerStream? StdInWriteStream;

        /// <summary>
        /// Represents the read handle for the standard input pipe used in inter-process communication.
        /// </summary>
        private readonly SafePipeHandle? StdInReadPipe;

        /// <summary>
        /// Represents pending standard input lines to write, if any.
        /// </summary>
        private readonly IReadOnlyList<string>? StdInInputData;

        /// <summary>
        /// Represents the encoding used for standard I/O.
        /// </summary>
        private readonly Encoding? StdIoEncoding;

        /// <summary>
        /// Stores the lines of standard output data captured from a process or command execution.
        /// </summary>
        private readonly List<string> StdOutData = [];

        /// <summary>
        /// Stores the collected standard error output data from a process execution.
        /// </summary>
        private readonly List<string> StdErrData = [];

        /// <summary>
        /// Stores the interleaved standard output and error data from a process execution.
        /// </summary>
        private readonly ConcurrentQueue<string> InterleavedData = [];

        /// <summary>
        /// Indicates whether the object has been disposed (0 = not disposed, 1 = disposed).
        /// </summary>
        /// <remarks>This field is used with <see cref="Interlocked.Exchange(ref int, int)"/> to ensure
        /// thread-safe disposal and prevent multiple calls to the dispose logic.</remarks>
        private int Disposed;

        /// <summary>
        /// Indicates whether inheritable parent-side client pipe handles have been released.
        /// </summary>
        private bool InheritedPipeHandlesReleased;

        /// <summary>
        /// Indicates whether standard input writing has been started.
        /// </summary>
        private bool StdInWriteTaskStarted;

        /// <summary>
        /// Releases the unmanaged resources used by the object and optionally releases the managed resources.
        /// </summary>
        /// <remarks>This method is called by both the public Dispose() method and the finalizer. When
        /// disposing is true, this method releases all resources held by managed objects. When disposing is false, only
        /// unmanaged resources are released.</remarks>
        /// <param name="disposing">true to release both managed and unmanaged resources; false to release only unmanaged resources.</param>
        private void Dispose(bool disposing)
        {
            if (Interlocked.Exchange(ref Disposed, 1) != 0 || !disposing)
            {
                return;
            }
            ReleaseInheritedPipeHandles();
            if (StdOutReadTask.IsCompleted)
            {
                StdOutReadTask.Dispose();
            }
            if (StdErrReadTask.IsCompleted)
            {
                StdErrReadTask.Dispose();
            }
            if (StdInWriteTask.IsCompleted)
            {
                StdInWriteTask.Dispose();
            }
            StdOutReadStream?.Dispose();
            StdErrReadStream?.Dispose();
            StdInWriteStream?.Dispose();
        }

        /// <summary>
        /// Releases all resources used by the current instance of the class.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to free unmanaged resources
        /// immediately. After calling Dispose, the object should not be used further. This method suppresses
        /// finalization to optimize garbage collection.</remarks>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
