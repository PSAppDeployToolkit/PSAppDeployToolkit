using System;
using System.Threading.Tasks;

namespace PSADT.ProcessManagement
{
    /// <summary>
    /// Represents an abstract base class for managing a standard input, output, or error handle associated with a
    /// process, providing resource management and disposal functionality.
    /// </summary>
    /// <param name="Task">The task associated with the standard I/O handle. This task typically represents asynchronous operations related
    /// to the handle and is disposed when the object is disposed.</param>
    internal abstract record ProcessStream(Task Task) : IDisposable
    {
        /// <summary>
        /// Releases all resources used by the current instance.
        /// </summary>
        /// <remarks>Call this method when you are finished using the object to free unmanaged resources
        /// immediately. After calling this method, the object should not be used.</remarks>
        public void Dispose()
        {
            if (Task.IsCompleted)
            {
                Task.Dispose();
            }
        }
    }
}
