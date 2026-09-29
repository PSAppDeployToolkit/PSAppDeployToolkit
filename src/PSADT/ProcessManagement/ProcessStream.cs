using System;
using System.IO.Pipes;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an abstract base class for managing a standard input, output, or error handle associated with a
    /// process, providing resource management and disposal functionality.
    /// </summary>
    /// <param name="Stream">The anonymous pipe server stream used for reading from or writing to the process. Cannot be null.</param>
    /// <param name="Task">The task to associate with the process stream. Cannot be null.</param>
    internal abstract class ProcessStream(AnonymousPipeServerStream Stream, Task Task) : IDisposable
    {
        /// <summary>
        /// Represents the underlying asynchronous operation associated with this instance.
        /// </summary>
        internal readonly Task Task = Task;

        /// <summary>
        /// Represents the anonymous pipe server stream used for reading from or writing to the process. This stream is used for inter-process communication and is associated with the process's standard input, output, or error streams.
        /// </summary>
        private readonly AnonymousPipeServerStream Stream = Stream;

        /// <summary>
        /// Releases all resources used by the current instance of the <see cref="ProcessStream"/> class.
        /// </summary>
        public void Dispose()
        {
            if (Task.IsCompleted)
            {
                Task.Dispose();
            }
            Stream.Dispose();
        }

        /// <summary>
        /// Releases the local copy of the client handle associated with the anonymous pipe server stream. This is important for proper resource management and to avoid handle leaks when the stream is no longer needed.
        /// </summary>
        internal void DisposeLocalCopyOfClientHandle()
        {
            Stream.DisposeLocalCopyOfClientHandle();
        }
    }
}
